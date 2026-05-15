Shader "ProjectArk/GGReplica/FakeFluxy"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0, 0, 0, 0)
        [HDR] _GlowColor ("Glow Color", Color) = (1.160784, 0, 2.996078, 0)
        _DistortionOffset ("Distortion Offset", Float) = -1
        _DepthOffset ("Depth Offset", Float) = -0.3
        _NoiseScale ("Noise Scale", Float) = 6
        _RimWidth ("Rim Width", Float) = 0.07
        _FlowPower ("Flow Power", Float) = 3.77
        _Alpha ("Runtime Alpha", Range(0, 1)) = 0.62
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
            fixed4 _BaseColor;
            fixed4 _GlowColor;
            float _DistortionOffset;
            float _DepthOffset;
            float _NoiseScale;
            float _RimWidth;
            float _FlowPower;
            float _Alpha;

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
                float flow = sin((i.uv.x * _NoiseScale) + (_Time.y * _FlowPower) + _DistortionOffset) * 0.5 + 0.5;
                float edge = abs(i.uv.y - 0.5) * 2.0;
                float rim = smoothstep(1.0 - _RimWidth, 1.0, edge);
                fixed4 color = lerp(_BaseColor, _GlowColor, saturate(flow * 0.65 + rim));
                color.rgb *= 1.0 + saturate(_DepthOffset + 1.0) * 0.35;
                color.a = tex.a * _Alpha * saturate(0.35 + flow + rim);
                return color;
            }
            ENDCG
        }
    }
}
