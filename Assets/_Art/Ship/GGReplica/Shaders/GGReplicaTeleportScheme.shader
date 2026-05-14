Shader "ProjectArk/GGReplica/TeleportScheme"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _SDFTex ("SDF Texture", 2D) = "gray" {}
        _NoiseTex ("Noise Texture", 2D) = "gray" {}
        _Intensity ("Intensity", Float) = 1
        _State ("State", Float) = 0
        _ScanScale ("Scan Scale", Float) = 8
        _GlitchStrength ("Glitch Strength", Float) = 0.3
        _ScrollSpeed ("Scroll Speed", Float) = 0.002
        _SchemeAlpha ("Scheme Alpha", Float) = 0.45
        _Pulse ("Pulse", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
            float _Intensity;
            float _State;
            float _ScanScale;
            float _GlitchStrength;
            float _ScrollSpeed;
            float _SchemeAlpha;
            float _Pulse;

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
                float scan = frac((i.uv.y + _Time.y * _ScrollSpeed) * max(_ScanScale, 0.001));
                float scanLine = smoothstep(0.0, 0.12, scan) * (1.0 - smoothstep(0.12, 0.24, scan));
                float pulse = _Pulse * (0.5 + 0.5 * sin(_Time.y * 16.0));
                float glitch = scanLine * (_GlitchStrength + pulse * 0.25);
                fixed3 baseColor = fixed3(0.0, 0.0, 0.0);
                fixed3 schemeColor = fixed3(0.545098, 0.090196, 1.0) * glitch;
                fixed3 color = (baseColor + schemeColor) * _Intensity;
                return fixed4(color, tex.a * saturate(_SchemeAlpha + pulse * 0.2));
            }
            ENDCG
        }
    }
}
