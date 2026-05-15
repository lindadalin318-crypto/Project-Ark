Shader "ProjectArk/GGReplica/DodgeParticles"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 0.78035855, 0, 1)
        _TintColor ("Tint Color", Color) = (1, 1, 1, 1)
        _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _InvFade ("Soft Particles Factor", Float) = 3
        _SrcBlend ("Source Blend", Float) = 1
        _DstBlend ("Destination Blend", Float) = 0
        _ZWrite ("Z Write", Float) = 1
        _ShellIntensity ("Shell Intensity", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite [_ZWrite]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TintColor;
            fixed4 _EmissionColor;
            float _InvFade;
            float _ShellIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv) * i.color;
                float radial = distance(i.uv, float2(0.5, 0.5)) * 2.0;
                float shell = smoothstep(0.15, 1.0, radial) * (1.0 - smoothstep(0.92, 1.0, radial));
                fixed4 color = _Color * _TintColor;
                color.rgb += _EmissionColor.rgb;
                color.rgb *= 1.0 + shell * _InvFade * 0.35 * _ShellIntensity;
                color.a *= tex.a * saturate(shell + 0.2);
                return color;
            }
            ENDCG
        }
    }
}
