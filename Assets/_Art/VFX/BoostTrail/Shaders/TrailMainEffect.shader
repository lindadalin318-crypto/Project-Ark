Shader "ProjectArk/VFX/TrailMainEffect"
{
    // Precise recreation of GG uniforms141 (SPIR-V bound=710).
    // Binding(1)=slot0 (Trail core sprite sheet)
    // Binding(2)=slot1 (Trail second sprite sheet)
    // Binding(3)=slot2 (Trail edge glow / second sample of slot0)
    // Binding(4)=slot3 (Trail color LUT / second sample of slot1)
    Properties
    {
        _Slot0 ("Trail Core Sprite Sheet (slot0)", 2D) = "white" {}
        _Slot1 ("Trail Second Sprite Sheet (slot1)", 2D) = "white" {}
        _Slot2 ("Trail Edge Glow (slot2)", 2D) = "white" {}
        _Slot3 ("Trail Color LUT (slot3)", 2D) = "white" {}

        // _child0: xy = frame count for slot0, z = blend weight, w = frame index channel
        _Child0 ("Child0 (slot0 frame ctrl)", Vector) = (1, 1, 1, 0)
        // _child1: xy = frame count for slot1, z = blend weight, w = brightness flag (>0 = ×8)
        _Child1 ("Child1 (slot1 frame ctrl)", Vector) = (1, 1, 1, 0)
        // _child2: x = slot1 weight, yzw = slot0 weight
        _Child2 ("Child2 (color blend weights)", Vector) = (0.5, 0.5, 0.5, 0.5)
        // _child3: >0 enables ×8 brightness boost
        _Child3 ("Child3 (brightness boost flag)", Float) = 0
        // _child4: xyz = edge glow base color, w = ellipse parameter
        _Child4 ("Child4 (edge glow color+ellipse)", Vector) = (1, 1, 1, 1)
        // _child5: xy = glow offset, z = glow scale enable (>0), w = glow falloff exponent
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

        Blend SrcAlpha OneMinusSrcAlpha
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

            // ── Textures ──────────────────────────────────────────────────────
            TEXTURE2D(_Slot0); SAMPLER(sampler_Slot0);
            TEXTURE2D(_Slot1); SAMPLER(sampler_Slot1);
            TEXTURE2D(_Slot2); SAMPLER(sampler_Slot2);
            TEXTURE2D(_Slot3); SAMPLER(sampler_Slot3);

            // ── Uniforms (uniforms141) ─────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float4 _Slot0_ST;
                float4 _Child0;   // xy=frameCount0, z=blendWeight0, w=frameIndexChannel
                float4 _Child1;   // xy=frameCount1, z=blendWeight1, w=brightnessFlag
                float4 _Child2;   // x=slot1Weight, yzw=slot0Weight
                float  _Child3;   // >0 → ×8 brightness boost
                float4 _Child4;   // xyz=glowBaseColor, w=ellipseParam
                float4 _Child5;   // xy=glowOffset, z=glowScaleEnable, w=glowFalloff
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
                float4 color       : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                OUT.color       = IN.color;
                return OUT;
            }

            // ── sRGB → Linear (matches GG gamma 2.4 curve) ───────────────────
            float3 SRGBToLinear(float3 c)
            {
                // GG uses: if(c <= 0.0404) linear = c * 0.0774 ... else pow curve
                // Exact constants from SPIR-V: 0.0774, 0.0550, 0.9479, 2.4
                // HLSL step(edge, x) = x >= edge ? 1 : 0
                // step(0.0404, c) = 1 when c >= 0.0404 → use hi curve
                float3 lo = c * 0.0774;
                float3 hi = pow(abs((c + 0.0550) * 0.9479), 2.4);
                return lerp(lo, hi, step(0.0404, c));
            }

            // ── Linear → sRGB (matches GG output curve) ──────────────────────
            float3 LinearToSRGB(float3 c)
            {
                // GG uses: if(c <= 0.0031) srgb = c * 12.92 ... else pow curve
                // Exact constants from SPIR-V: 12.92, 0.4167, 1.0550, -0.0550
                // HLSL step(edge, x) = x >= edge ? 1 : 0
                // step(0.0031, c) = 1 when c >= 0.0031 → use hi curve
                float3 lo = c * 12.92;
                float3 hi = pow(abs(c), 0.4167) * 1.0550 - 0.0550;
                return lerp(lo, hi, step(0.0031, c));
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // ── Step 1: Sample slot0, convert sRGB→Linear ─────────────────
                float4 s0 = SAMPLE_TEXTURE2D(_Slot0, sampler_Slot0, uv);
                float3 col0 = SRGBToLinear(s0.xyz);

                // ── Step 2: Sample slot1, square it (slot1 * slot1) ───────────
                float4 s1 = SAMPLE_TEXTURE2D(_Slot1, sampler_Slot1, uv);
                float3 col1 = s1.xyz * s1.xyz;

                // ── Step 3: Brightness boost (×8) if _child3 > 0 ─────────────
                // GG: col1 = col1.www * col1.xyz * 8.0
                if (_Child3 > 0.0)
                {
                    col1 = s1.www * col1 * 8.0;
                }

                // ── Step 4: Color blend: result = col1 * _child2.x + col0 * _child2.yzw
                float3 blended = col1 * _Child2.x + col0 * _Child2.yzw;

                // ── Step 5: Edge glow (if _child5.z > 0) ─────────────────────
                if (_Child5.z > 0.0)
                {
                    // Offset UV by _child5.xy
                    float2 glowUV = uv - _Child5.xy;
                    // Scale by _child5.zz (abs)
                    float2 scaledUV = abs(glowUV) * _Child5.zz;
                    // Ellipse: float2(scaledUV.x * _child4.w, scaledUV.y), then dot
                    // GG SPIR-V: both X and Y components participate in ellipse
                    float2 ellipseVec = float2(scaledUV.x * _Child4.w, scaledUV.y);
                    float dotVal = dot(ellipseVec, ellipseVec);
                    // Radial gradient: 1 - dot, clamped, then pow(_child5.w)
                    float radial = max(1.0 - dotVal, 0.0);
                    radial = pow(radial, _Child5.w);
                    // Glow color: center = _child4.xyz (tinted), edge = 1.0 (white/neutral)
                    // GG SPIR-V: lerp(1.0, _child4.xyz, radial)
                    // radial=1 at center → glowColor = _Child4.xyz (full tint)
                    // radial=0 at edge  → glowColor = 1.0 (no tint, neutral)
                    float3 glowColor = lerp(float3(1.0, 1.0, 1.0), _Child4.xyz, radial);
                    blended = blended * glowColor;
                }

                // ── Step 6: Sprite Sheet animation for slot0 (_child0) ────────
                // GG: _child0.w = total frame count, IN.color.w = normalized animation time
                // frameIdx0 = floor(animTime * frameCount), blendT0 = frac(animTime * frameCount)
                // Frame width = 1.0 / _Child0.x  (reciprocal of column count)
                float animTime0  = IN.color.w * _Child0.w;
                float frameIdx0  = floor(animTime0);
                float blendT0    = animTime0 - frameIdx0; // frac part for inter-frame blend
                float frameWidth0 = 1.0 / max(_Child0.x, 1.0); // avoid div-by-zero

                float2 tiledUV0 = uv * float2(frameWidth0, 1.0);
                float2 ssUV0_a = float2(tiledUV0.x + frameIdx0 * frameWidth0, tiledUV0.y);
                float2 ssUV0_b = float2(ssUV0_a.x + frameWidth0, ssUV0_a.y);

                float4 ss0a = SAMPLE_TEXTURE2D_LOD(_Slot2, sampler_Slot2, ssUV0_a, 0);
                float4 ss0b = SAMPLE_TEXTURE2D_LOD(_Slot2, sampler_Slot2, ssUV0_b, 0);
                float3 ssCol0 = lerp(ss0a.xyz, ss0b.xyz, blendT0);

                // ── Step 7: Sprite Sheet animation for slot1 (_child1) ────────
                // GG: _child1.z = total frame count, IN.color.z = normalized animation time
                // Frame width = 1.0 / _Child1.x  (reciprocal of column count)
                float animTime1  = IN.color.z * _Child1.z;
                float frameIdx1  = floor(animTime1);
                float blendT1    = animTime1 - frameIdx1; // frac part for inter-frame blend
                float frameWidth1 = 1.0 / max(_Child1.x, 1.0); // avoid div-by-zero

                float2 tiledUV1 = uv * float2(frameWidth1, 1.0);
                float2 ssUV1_a = float2(tiledUV1.x + frameIdx1 * frameWidth1, tiledUV1.y);
                float2 ssUV1_b = float2(ssUV1_a.x + frameWidth1, ssUV1_a.y);

                float4 ss1a = SAMPLE_TEXTURE2D_LOD(_Slot3, sampler_Slot3, ssUV1_a, 0);
                float4 ss1b = SAMPLE_TEXTURE2D_LOD(_Slot3, sampler_Slot3, ssUV1_b, 0);
                float3 ssCol1 = lerp(ss1a.xyz, ss1b.xyz, blendT1);

                // ── Step 8: Final blend of sprite sheet results ───────────────
                // GG SPIR-V: ssModulate = ssCol0 * ssCol1  (multiplicative modulation)
                // Both sprite sheet layers multiply together, then modulate the main color.
                // blendT1 is the intra-frame blend weight for slot1, NOT a cross-slot lerp.
                float3 ssModulate = ssCol0 * ssCol1;
                float3 finalCol = blended * ssModulate;
                finalCol = LinearToSRGB(finalCol);

                // ── Step 9: Output w = 1.0 (fully opaque, matches GG) ─────────
                return float4(finalCol, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Particles/Unlit"
}
