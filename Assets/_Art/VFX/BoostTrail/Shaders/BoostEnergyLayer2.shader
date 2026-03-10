Shader "ProjectArk/VFX/BoostEnergyLayer2"
{
    Properties
    {
        // Boost intensity: 0 = hidden, 1 = fully visible
        _BoostIntensity ("Boost Intensity", Range(0, 1)) = 0
        
        // Four layered noise textures (from GG reverse engineering)
        _Tex0 ("Texture 0", 2D) = "white" {}
        _Tex1 ("Texture 1", 2D) = "white" {}
        _Tex2 ("Texture 2", 2D) = "white" {}
        _Tex3 ("Texture 3", 2D) = "white" {}
        
        // Noise scale and animation speed
        _NoiseScale ("Noise Scale", Float) = 3.0
        _NoiseSpeed ("Noise Speed", Float) = 0.5
        
        // Smoothstep edge parameters
        _SmoothEdge0 ("Smooth Edge 0", Range(0, 1)) = 0.2
        _SmoothEdge1 ("Smooth Edge 1", Range(0, 1)) = 0.8
        
        // Sprite support
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "BoostEnergyLayer2"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float _BoostIntensity;
                float _NoiseScale;
                float _NoiseSpeed;
                float _SmoothEdge0;
                float _SmoothEdge1;
                float4 _Tex0_ST;
                float4 _Tex1_ST;
                float4 _Tex2_ST;
                float4 _Tex3_ST;
            CBUFFER_END

            TEXTURE2D(_Tex0); SAMPLER(sampler_Tex0);
            TEXTURE2D(_Tex1); SAMPLER(sampler_Tex1);
            TEXTURE2D(_Tex2); SAMPLER(sampler_Tex2);
            TEXTURE2D(_Tex3); SAMPLER(sampler_Tex3);

            // Gradient noise implementation (matches GG SPIR-V constant 289.0)
            float2 mod289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float3 mod289_3(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float3 permute(float3 x) { return mod289_3(((x * 34.0) + 1.0) * x); }

            float gradientNoise(float2 uv, float scale)
            {
                uv *= scale;
                float2 i = floor(uv);
                float2 f = frac(uv);
                
                // Cubic Hermite interpolation
                float2 u = f * f * (3.0 - 2.0 * f);
                
                float3 p = permute(permute(float3(i.x, i.x + 1.0, i.x)) + float3(i.y, i.y, i.y + 1.0));
                float3 q = permute(p + float3(0.0, 1.0, 0.0));
                
                float3 r = frac(p * (1.0 / 41.0)) * 2.0 - 1.0;
                float3 s = frac(q * (1.0 / 41.0)) * 2.0 - 1.0;
                
                float3 ox = floor(r + 0.5);
                float3 oy = floor(s + 0.5);
                float3 ax = r - ox;
                float3 ay = s - oy;
                
                float2 g00 = float2(ox.x, oy.x);
                float2 g10 = float2(ox.y, oy.y);
                float2 g01 = float2(ox.z, oy.z);
                float2 g11 = float2(ax.x, ay.x);
                
                float n00 = dot(g00, f);
                float n10 = dot(g10, f - float2(1.0, 0.0));
                float n01 = dot(g01, f - float2(0.0, 1.0));
                float n11 = dot(g11, f - float2(1.0, 1.0));
                
                float2 n_x = lerp(float2(n00, n01), float2(n10, n11), u.x);
                float n_xy = lerp(n_x.x, n_x.y, u.y);
                return n_xy * 0.5 + 0.5;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Early exit if boost is not active
                if (_BoostIntensity <= 0.001)
                    return half4(0, 0, 0, 0);

                float2 uv = IN.uv;
                float time = _Time.y;

                // Generate gradient noise for UV distortion
                float noise = gradientNoise(uv, _NoiseScale);
                
                // UV distortion: offset UV by noise * time animation
                float2 distortedUV = uv + float2(noise, noise) * 0.1 + float2(time * _NoiseSpeed * 0.03, time * _NoiseSpeed * 0.02);

                // Sample 4 textures with slightly different UV offsets (4-layer blend from GG)
                float2 uv0 = TRANSFORM_TEX(distortedUV, _Tex0);
                float2 uv1 = TRANSFORM_TEX(distortedUV + float2(0.1, 0.0), _Tex1);
                float2 uv2 = TRANSFORM_TEX(distortedUV + float2(0.0, 0.1), _Tex2);
                float2 uv3 = TRANSFORM_TEX(distortedUV + float2(0.1, 0.1), _Tex3);

                half4 t0 = SAMPLE_TEXTURE2D(_Tex0, sampler_Tex0, uv0);
                half4 t1 = SAMPLE_TEXTURE2D(_Tex1, sampler_Tex1, uv1);
                half4 t2 = SAMPLE_TEXTURE2D(_Tex2, sampler_Tex2, uv2);
                half4 t3 = SAMPLE_TEXTURE2D(_Tex3, sampler_Tex3, uv3);

                // 3-stage Lerp blend (matches GG Lerp × 3 node chain)
                half4 blend01 = lerp(t0, t1, noise);
                half4 blend012 = lerp(blend01, t2, noise * 0.7);
                half4 blendFinal = lerp(blend012, t3, noise * 0.5);

                // Smoothstep alpha (matches GG Smoothstep node)
                float alpha = smoothstep(_SmoothEdge0, _SmoothEdge1, blendFinal.r);
                
                // Multiply by BoostIntensity for fade in/out
                alpha *= _BoostIntensity;

                // Output: use noise-driven color with smoothstep alpha
                half3 color = blendFinal.rgb * (1.0 + _BoostIntensity * 2.0); // HDR boost
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
