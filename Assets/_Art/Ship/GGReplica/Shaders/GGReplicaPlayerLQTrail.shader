Shader "ProjectArk/GGReplica/PlayerLQTrail"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _MainColor ("Main Color", Color) = (0.120545, 0, 0.188679, 1)
        [HDR] _EdgeColor ("Edge Color", Color) = (0.613284, 0, 0.807843, 0)
        _NoiseParams ("Noise Params", Vector) = (1, 1, 0.2, 0.7)
        _ScrollSpeed ("Scroll Speed", Float) = 0.2
        _TrailIntensity ("Trail Intensity", Float) = 0
        _EdgeBoost ("Edge Boost", Float) = 0
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
            fixed4 _MainColor;
            fixed4 _EdgeColor;
            float4 _NoiseParams;
            float _ScrollSpeed;
            float _TrailIntensity;
            float _EdgeBoost;

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
                float edge = abs(i.uv.y - 0.5) * 2.0;
                float wave = sin((i.uv.x * _NoiseParams.x + _Time.y * _ScrollSpeed) * 6.28318) * 0.5 + 0.5;
                float edgeMask = smoothstep(1.0 - _NoiseParams.z, 1.0, edge);
                fixed4 color = lerp(_MainColor, _EdgeColor, saturate(edgeMask + wave * _NoiseParams.w * (0.25 + _EdgeBoost * 0.5)));
                color.rgb *= 1.0 + _TrailIntensity * 1.5;
                color.a *= tex.a * saturate(0.25 + _TrailIntensity);
                return color;
            }
            ENDCG
        }
    }
}
