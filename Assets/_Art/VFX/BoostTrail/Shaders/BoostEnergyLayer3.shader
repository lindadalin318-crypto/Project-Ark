Shader "ProjectArk/VFX/BoostEnergyLayer3"
{
    Properties
    {
        // Boost intensity: 0 = hidden, 1 = fully visible
        _BoostIntensity ("Boost Intensity", Range(0, 1)) = 0
        
        // Two textures for dual-noise blend (from GG Layer 3 SPIR-V)
        _Tex0 ("Texture 0", 2D) = "white" {}
        _Tex1 ("Texture 1", 2D) = "white" {}
        
        // Step threshold for binary alpha (matches GG Step(0.01))
        _StepThreshold ("Step Threshold", Range(0, 1)) = 0.01
        
        // UV scale multiplier (matches GG UV × 2 - 0.5)
        _UVScale ("UV Scale", Float) = 2.0
        _UVOffset ("UV Offset", Float) = -0.5
        
        // Animation speed
        _AnimSpeed ("Animation Speed", Float) = 0.3
        
        // Sprite support
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
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
            Name "BoostEnergyLayer3"
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
                float4 color        : COLOR;  // Vertex color: w component drives blend
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
                float _StepThreshold;
                float _UVScale;
                float _UVOffset;
                float _AnimSpeed;
                float4 _Tex0_ST;
                float4 _Tex1_ST;
            CBUFFER_END

            TEXTURE2D(_Tex0); SAMPLER(sampler_Tex0);
            TEXTURE2D(_Tex1); SAMPLER(sampler_Tex1);

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

                // Vertex color w component drives the blend (matches GG Vertex Color(w) → Lerp)
                float vertexW = IN.color.w;

                // UV Scale: UV × 2 - 0.5 (matches GG UV Scale node)
                float2 scaledUV = uv * _UVScale + _UVOffset;
                
                // Animate UV
                float2 animUV0 = scaledUV + float2(time * _AnimSpeed, time * _AnimSpeed * 0.7);
                float2 animUV1 = scaledUV + float2(-time * _AnimSpeed * 0.5, time * _AnimSpeed * 0.3);

                // Sample 2 textures
                float2 uv0 = TRANSFORM_TEX(animUV0, _Tex0);
                float2 uv1 = TRANSFORM_TEX(animUV1, _Tex1);

                half4 t0 = SAMPLE_TEXTURE2D(_Tex0, sampler_Tex0, uv0);
                half4 t1 = SAMPLE_TEXTURE2D(_Tex1, sampler_Tex1, uv1);

                // Lerp driven by vertex color w component (matches GG Vertex Color(w) → Lerp)
                half4 blended = lerp(t0, t1, vertexW);

                // Step(0.01) for binary alpha (matches GG Step(0.01) node)
                float alpha = step(_StepThreshold, blended.r);
                
                // Multiply by BoostIntensity for fade in/out
                alpha *= _BoostIntensity;

                // HDR color output
                half3 color = blended.rgb * (1.0 + _BoostIntensity * 1.5);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
