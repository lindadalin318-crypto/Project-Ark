Shader "ProjectArk/VFX/TrailMainEffect"
{
    Properties
    {
        [Header(Disturbance Trail)]
        _BaseMap ("Trail Shape", 2D) = "white" {}
        [HDR]_BaseColor ("Base Color", Color) = (2.45, 1.2, 0.28, 1)
        [HDR]_EdgeColor ("Edge Glow", Color) = (3.8, 2.1, 0.7, 1)
        _Brightness ("Brightness", Range(0, 8)) = 1.35
        _Alpha ("Alpha", Range(0, 2)) = 0.9
        _NoiseScale ("Noise Scale", Range(0.1, 12)) = 2.5
        _DistortStrength ("Distort Strength", Range(0, 1)) = 0.18
        _FlowSpeed ("Flow Speed", Range(0, 8)) = 1.8
        _FlickerStrength ("Flicker Strength", Range(0, 1)) = 0.3
        _EdgePower ("Edge Power", Range(0.2, 6)) = 1.65
        _UseLegacySlots ("Use Legacy Slot Pipeline", Float) = 1

        [Header(Legacy Slot Pipeline)]
        _Slot0 ("Trail Core Sprite Sheet (slot0)", 2D) = "white" {}
        _Slot1 ("Trail Second Sprite Sheet (slot1)", 2D) = "white" {}
        _Slot2 ("Trail Edge Glow (slot2)", 2D) = "white" {}
        _Slot3 ("Trail Color LUT (slot3)", 2D) = "white" {}

        _Child0 ("Child0 (slot0 frame ctrl)", Vector) = (1, 1, 1, 0)
        _Child1 ("Child1 (slot1 frame ctrl)", Vector) = (1, 1, 1, 0)
        _Child2 ("Child2 (color blend weights)", Vector) = (0.5, 0.5, 0.5, 0.5)
        _Child3 ("Child3 (brightness boost flag)", Float) = 0
        _Child4 ("Child4 (edge glow color+ellipse)", Vector) = (1, 1, 1, 1)
        _Child5 ("Child5 (edge glow offset+scale)", Vector) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "TrailMainEffect"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_Slot0); SAMPLER(sampler_Slot0);
            TEXTURE2D(_Slot1); SAMPLER(sampler_Slot1);
            TEXTURE2D(_Slot2); SAMPLER(sampler_Slot2);
            TEXTURE2D(_Slot3); SAMPLER(sampler_Slot3);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _EdgeColor;
                float _Brightness;
                float _Alpha;
                float _NoiseScale;
                float _DistortStrength;
                float _FlowSpeed;
                float _FlickerStrength;
                float _EdgePower;
                float _UseLegacySlots;

                float4 _Slot0_ST;
                float4 _Child0;
                float4 _Child1;
                float4 _Child2;
                float _Child3;
                float4 _Child4;
                float4 _Child5;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float2 baseUV      : TEXCOORD1;
                float4 color       : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.baseUV = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.color = IN.color;
                return OUT;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);

                float a = Hash21(cell);
                float b = Hash21(cell + float2(1.0, 0.0));
                float c = Hash21(cell + float2(0.0, 1.0));
                float d = Hash21(cell + float2(1.0, 1.0));

                float2 smoothLocal = local * local * (3.0 - 2.0 * local);
                return lerp(lerp(a, b, smoothLocal.x), lerp(c, d, smoothLocal.x), smoothLocal.y);
            }

            float FBM(float2 p)
            {
                float sum = 0.0;
                float amplitude = 0.55;
                sum += ValueNoise(p) * amplitude;
                p = p * 2.03 + 17.13;
                amplitude *= 0.5;
                sum += ValueNoise(p) * amplitude;
                p = p * 2.01 + 29.71;
                amplitude *= 0.5;
                sum += ValueNoise(p) * amplitude;
                return sum;
            }

            float3 SRGBToLinear(float3 c)
            {
                float3 lo = c * 0.0774;
                float3 hi = pow(abs((c + 0.0550) * 0.9479), 2.4);
                return lerp(lo, hi, step(0.0404, c));
            }

            float3 LinearToSRGB(float3 c)
            {
                float3 lo = c * 12.92;
                float3 hi = pow(abs(c), 0.4167) * 1.0550 - 0.0550;
                return lerp(lo, hi, step(0.0031, c));
            }

            float4 ShadeLegacy(Varyings IN)
            {
                float2 uv = IN.uv;

                float4 s0 = SAMPLE_TEXTURE2D(_Slot0, sampler_Slot0, uv);
                float3 col0 = SRGBToLinear(s0.xyz);

                float4 s1 = SAMPLE_TEXTURE2D(_Slot1, sampler_Slot1, uv);
                float3 col1 = s1.xyz * s1.xyz;

                if (_Child3 > 0.0)
                {
                    col1 = s1.www * col1 * 8.0;
                }

                float3 blended = col1 * _Child2.x + col0 * _Child2.yzw;

                if (_Child5.z > 0.0)
                {
                    float2 glowUV = uv - _Child5.xy;
                    float2 scaledUV = abs(glowUV) * _Child5.zz;
                    float2 ellipseVec = float2(scaledUV.x * _Child4.w, scaledUV.y);
                    float dotVal = dot(ellipseVec, ellipseVec);
                    float radial = max(1.0 - dotVal, 0.0);
                    radial = pow(radial, _Child5.w);
                    float3 glowColor = lerp(float3(1.0, 1.0, 1.0), _Child4.xyz, radial);
                    blended = blended * glowColor;
                }

                float animTime0 = IN.color.w * _Child0.w;
                float frameIdx0 = floor(animTime0);
                float blendT0 = animTime0 - frameIdx0;
                float frameWidth0 = 1.0 / max(_Child0.x, 1.0);

                float2 tiledUV0 = uv * float2(frameWidth0, 1.0);
                float2 ssUV0a = float2(tiledUV0.x + frameIdx0 * frameWidth0, tiledUV0.y);
                float2 ssUV0b = float2(ssUV0a.x + frameWidth0, ssUV0a.y);

                float4 ss0a = SAMPLE_TEXTURE2D_LOD(_Slot2, sampler_Slot2, ssUV0a, 0);
                float4 ss0b = SAMPLE_TEXTURE2D_LOD(_Slot2, sampler_Slot2, ssUV0b, 0);
                float3 ssCol0 = lerp(ss0a.xyz, ss0b.xyz, blendT0);

                float animTime1 = IN.color.z * _Child1.z;
                float frameIdx1 = floor(animTime1);
                float blendT1 = animTime1 - frameIdx1;
                float frameWidth1 = 1.0 / max(_Child1.x, 1.0);

                float2 tiledUV1 = uv * float2(frameWidth1, 1.0);
                float2 ssUV1a = float2(tiledUV1.x + frameIdx1 * frameWidth1, tiledUV1.y);
                float2 ssUV1b = float2(ssUV1a.x + frameWidth1, ssUV1a.y);

                float4 ss1a = SAMPLE_TEXTURE2D_LOD(_Slot3, sampler_Slot3, ssUV1a, 0);
                float4 ss1b = SAMPLE_TEXTURE2D_LOD(_Slot3, sampler_Slot3, ssUV1b, 0);
                float3 ssCol1 = lerp(ss1a.xyz, ss1b.xyz, blendT1);

                float3 ssModulate = ssCol0 * ssCol1;
                float3 finalCol = blended * ssModulate;
                finalCol = LinearToSRGB(finalCol);
                return float4(finalCol, 1.0);
            }

            float4 ShadeDisturbance(Varyings IN)
            {
                float2 baseUV = IN.baseUV;
                float2 rawUV = IN.uv;
                float time = _Time.y;

                float across = rawUV.y * 2.0 - 1.0;
                float centerMask = saturate(1.0 - abs(across));
                float edgeMask = pow(saturate(1.0 - centerMask), max(_EdgePower, 0.001));

                float2 flowA = float2(rawUV.x * 6.0 + time * (_FlowSpeed * 1.2), across * _NoiseScale + time * (_FlowSpeed * 0.45));
                float2 flowB = float2(rawUV.x * 10.5 - time * (_FlowSpeed * 1.8), across * (_NoiseScale * 1.7) - 4.3 - time * 0.35);

                float noiseA = FBM(flowA);
                float noiseB = FBM(flowB);
                float swirl = noiseA * 2.0 - 1.0;
                float ribbon = noiseB * 2.0 - 1.0;

                float2 distortion = float2(
                    swirl * (_DistortStrength * 0.12),
                    ribbon * (_DistortStrength * 0.42) * (0.35 + centerMask * 0.65));

                float4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV + distortion);
                float4 smearSample = SAMPLE_TEXTURE2D(
                    _BaseMap,
                    sampler_BaseMap,
                    baseUV + distortion + float2(-_DistortStrength * 0.08 * (0.45 + noiseA), ribbon * _DistortStrength * 0.18));

                float3 texColor = lerp(baseSample.rgb, smearSample.rgb, 0.35);
                float texAlpha = max(baseSample.a, smearSample.a);

                float flicker = 1.0
                    + (noiseA - 0.5) * _FlickerStrength
                    + sin((rawUV.x - time * (_FlowSpeed * 1.6)) * 22.0) * 0.08;

                float hotCore = pow(centerMask, 0.55) * (0.65 + noiseB * 0.35);
                float3 coreColor = texColor * _BaseColor.rgb * hotCore;
                float3 edgeColor = _EdgeColor.rgb * edgeMask * (0.4 + noiseA * 0.6);

                float3 finalColor = (coreColor + edgeColor) * _Brightness * flicker;
                finalColor *= IN.color.rgb;

                float alpha = texAlpha * _Alpha * IN.color.a;
                alpha *= saturate(0.2 + centerMask * 0.85 + edgeMask * 0.3);

                return float4(finalColor, alpha);
            }

            float4 frag(Varyings IN) : SV_Target
            {
                if (_UseLegacySlots > 0.5)
                {
                    return ShadeLegacy(IN);
                }

                return ShadeDisturbance(IN);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Particles/Unlit"
}
