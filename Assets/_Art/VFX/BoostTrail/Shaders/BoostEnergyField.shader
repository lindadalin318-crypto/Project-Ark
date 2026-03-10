Shader "ProjectArk/VFX/BoostEnergyField"
{
    Properties
    {
        // Boost intensity: 0 = hidden, 1 = fully visible
        _BoostIntensity ("Boost Intensity", Range(0, 1)) = 0
        
        // Optional LUT texture (5.1MB from GG, fallback to procedural if not assigned)
        _LUTTex ("LUT Texture (Optional)", 2D) = "white" {}
        _UseLUT ("Use LUT Texture", Range(0, 1)) = 0
        
        // 4x4 matrix transform parameters (from GG SPIR-V)
        _MatrixScale ("Matrix Scale", Float) = 1.0
        _MatrixRotation ("Matrix Rotation", Float) = 0.0
        
        // 4-layer gradient noise parameters
        _NoiseScale0 ("Noise Scale 0", Float) = 2.0
        _NoiseScale1 ("Noise Scale 1", Float) = 4.0
        _NoiseScale2 ("Noise Scale 2", Float) = 8.0
        _NoiseScale3 ("Noise Scale 3", Float) = 16.0
        
        // Noise animation speeds per layer
        _NoiseSpeed0 ("Noise Speed 0", Float) = 0.1
        _NoiseSpeed1 ("Noise Speed 1", Float) = 0.15
        _NoiseSpeed2 ("Noise Speed 2", Float) = 0.2
        _NoiseSpeed3 ("Noise Speed 3", Float) = 0.25
        
        // Step threshold for binary alpha (matches GG Step(0.01))
        _StepThreshold ("Step Threshold", Range(0, 1)) = 0.01
        
        // Color tint
        _Color ("Color Tint", Color) = (0.5, 0.8, 1.0, 1.0)
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
            Name "BoostEnergyField"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 worldPos     : TEXCOORD1;  // World position for world-space noise
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float _BoostIntensity;
                float _UseLUT;
                float _MatrixScale;
                float _MatrixRotation;
                float _NoiseScale0;
                float _NoiseScale1;
                float _NoiseScale2;
                float _NoiseScale3;
                float _NoiseSpeed0;
                float _NoiseSpeed1;
                float _NoiseSpeed2;
                float _NoiseSpeed3;
                float _StepThreshold;
                float4 _Color;
                float4 _LUTTex_ST;
            CBUFFER_END

            TEXTURE2D(_LUTTex); SAMPLER(sampler_LUTTex);

            // Gradient noise (same implementation as Layer 2, matches GG SPIR-V 289.0 constant)
            float2 mod289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float3 mod289_3(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float3 permute(float3 x) { return mod289_3(((x * 34.0) + 1.0) * x); }

            float gradientNoise(float2 uv, float scale)
            {
                uv *= scale;
                float2 i = floor(uv);
                float2 f = frac(uv);
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

            // 4x4 matrix transform for world-space coordinates (matches GG Transform node)
            float2 transformWorldPos(float3 worldPos, float scale, float rotation)
            {
                float cosR = cos(rotation);
                float sinR = sin(rotation);
                float2x2 rotMat = float2x2(cosR, -sinR, sinR, cosR);
                float2 pos = mul(rotMat, worldPos.xy) * scale;
                return pos;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                // World position for world-space noise (matches GG World Position node)
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Early exit if boost is not active
                if (_BoostIntensity <= 0.001)
                    return half4(0, 0, 0, 0);

                float time = _Time.y;
                
                // Transform world position (matches GG 4x4 matrix transform)
                float2 transformedPos = transformWorldPos(IN.worldPos, _MatrixScale, _MatrixRotation + time * 0.05);

                // 4-layer gradient noise in world space (matches GG Gradient Noise × 4)
                float n0 = gradientNoise(transformedPos + float2(time * _NoiseSpeed0, 0), _NoiseScale0);
                float n1 = gradientNoise(transformedPos + float2(0, time * _NoiseSpeed1), _NoiseScale1);
                float n2 = gradientNoise(transformedPos + float2(time * _NoiseSpeed2, time * _NoiseSpeed2), _NoiseScale2);
                float n3 = gradientNoise(transformedPos - float2(time * _NoiseSpeed3, time * _NoiseSpeed3 * 0.7), _NoiseScale3);

                // Combine 4 noise layers
                float combinedNoise = (n0 * 0.5 + n1 * 0.25 + n2 * 0.15 + n3 * 0.1);

                float alpha;
                half3 color;

                if (_UseLUT > 0.5)
                {
                    // LUT lookup (matches GG Sample Texture 2D(LUT))
                    float2 lutUV = float2(combinedNoise, 0.5);
                    half4 lutSample = SAMPLE_TEXTURE2D(_LUTTex, sampler_LUTTex, lutUV);
                    // Step(0.01) binary alpha (matches GG Step(0.01) node)
                    alpha = step(_StepThreshold, lutSample.r);
                    color = lutSample.rgb * _Color.rgb;
                }
                else
                {
                    // Fallback: procedural gradient noise without LUT
                    alpha = step(_StepThreshold, combinedNoise);
                    color = _Color.rgb * (0.5 + combinedNoise * 0.5);
                }

                // Multiply by BoostIntensity for fade in/out
                alpha *= _BoostIntensity;
                
                // HDR color boost
                color *= (1.0 + _BoostIntensity * 1.5);

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
