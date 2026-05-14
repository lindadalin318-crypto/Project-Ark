Shader "ProjectArk/GGReplica/PlayerShipHighlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector] _SDFLast ("SDF Last", 2D) = "gray" {}
        [HideInInspector] _SDFNew ("SDF New", 2D) = "gray" {}
        [HideInInspector] _SDFLastMask ("SDF Last Mask", 2D) = "gray" {}
        [HideInInspector] _SDFNewMask ("SDF New Mask", 2D) = "gray" {}
        _Smooth ("Smoothness", Float) = 0.01
        _Intensity ("Intensity", Float) = 8
        _Tint ("Tint", Color) = (0.545098, 0.090196, 1, 1)
        _BoostAmount ("Boost Amount", Float) = 0
        _HealAmount ("Heal Amount", Float) = 0
        _Pulse ("Pulse", Float) = 0
        _GrabEmphasis ("Grab Emphasis", Float) = 0
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
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Smooth;
            float _Intensity;
            fixed4 _Tint;
            float _BoostAmount;
            float _HealAmount;
            float _Pulse;
            float _GrabEmphasis;

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
                float alpha = smoothstep(0.001, max(_Smooth, 0.001), tex.a);
                float pulse = 0.5 + 0.5 * sin(_Time.y * 18.0);
                float stateBoost = _BoostAmount * 2.0 + _HealAmount * 2.5 + _Pulse * pulse * 3.0 + _GrabEmphasis * 1.5;
                fixed3 healColor = fixed3(0.35, 1.0, 0.85);
                fixed3 tint = lerp(_Tint.rgb, healColor, saturate(_HealAmount));
                fixed3 color = tex.rgb * tint * (_Intensity + stateBoost);
                return fixed4(color, alpha * _Tint.a * tex.a);
            }
            ENDCG
        }
    }
}
