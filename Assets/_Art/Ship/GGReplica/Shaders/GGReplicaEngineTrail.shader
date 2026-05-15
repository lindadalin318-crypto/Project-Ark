Shader "ProjectArk/GGReplica/EngineTrail"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _BottomColor ("Bottom Color", Color) = (1, 0, 0.914567, 1)
        _TopColor ("Top Color", Color) = (0.0990566, 0.8460265, 1, 1)
        _GhostColor ("Ghost Color", Color) = (0.09211465, 0.8490566, 0.6468398, 0.3882353)
        _MixEffect ("Mix Effect", Float) = 1
        _NoiseScale ("Noise Scale", Float) = 1.31
        _Spread ("Spread", Float) = 2
        _Power ("Power", Float) = 7
        _WobbleSpeed ("Wobble Speed", Float) = 0.4
        _Speed1 ("Speed 1", Vector) = (-2, 0, 1, 1)
        _Speed2 ("Speed 2", Vector) = (-1, 0, 1, 1)
        _Speed3 ("Speed 3", Vector) = (-1.61, 0, 1, 0.51)
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
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _BottomColor;
            fixed4 _TopColor;
            fixed4 _GhostColor;
            float _MixEffect;
            float _NoiseScale;
            float _Spread;
            float _Power;
            float _WobbleSpeed;
            float4 _Speed1;
            float4 _Speed2;
            float4 _Speed3;

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
                float t = saturate(i.uv.y);
                float n1 = sin((i.uv.x * _NoiseScale + _Time.y * _WobbleSpeed) * 6.28318 + _Speed1.x) * 0.5 + 0.5;
                float n2 = sin((i.uv.x * _Spread + _Time.y * _WobbleSpeed * _Speed2.z) * 6.28318 + _Speed2.x) * 0.5 + 0.5;
                float n3 = sin((i.uv.x * (_NoiseScale + _Spread) + _Time.y * _WobbleSpeed * _Speed3.w) * 6.28318 + _Speed3.x) * 0.5 + 0.5;
                float noise = saturate(n1 * n2 * n3 * _MixEffect);
                fixed4 color = lerp(_BottomColor, _TopColor, t);
                color.rgb = lerp(color.rgb, _GhostColor.rgb, saturate(noise * _GhostColor.a));
                color.rgb *= max(1.0, _Power * 0.35);
                color.a = tex.a * i.color.a * saturate(0.25 + noise);
                return color;
            }
            ENDCG
        }
    }
}
