// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Effect/Noise_UV_Mask_BuiltIn"
{
	Properties
	{
		_ColorIntensity("ColorIntensity", Float) = 1
		_Color("Color", Color) = (1,1,1,1)
		[Enum(Add,1,AlphaBlend,10)]_Dst("BlendMode", Int) = 1
		[KeywordEnum(UV,UClamp,VClamp,UVClamp)] _Clamp("Clamp", Float) = 0
		[Enum(OFF,0,ON,2)]_CullMode1("CullMode", Int) = 0
		_MainTex("MainTex", 2D) = "white" {}
		_NoiseIntensity("NoiseIntensity", Float) = 0.2
		_NoiseTex("NoiseTex", 2D) = "white" {}
		[Header(XY_MainTex_ZW_Noise)]_UVSpeed("UV Speed", Vector) = (0,0,0,0)
		_MaskIntensity("MaskIntensity", Float) = 1
		_Mask("Mask", 2D) = "white" {}
		_Mask_U_Speed("Mask_U_Speed", Float) = 0
		_Mask_V_Speed("Mask_V_Speed", Float) = 0
		[Toggle][Header(MainCustomData)]_Custom1xy("Custom1xy", Float) = 0
		[Toggle][Header(MaskCustomData)]_Custom1zw("Custom1zw", Float) = 0

	}
	
	SubShader
	{
		
		
		Tags { "RenderType"="Opaque" }
	LOD 100

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend Off
		AlphaToMask Off
		Cull Back
		ColorMask RGBA
		ZWrite On
		ZTest LEqual
		Offset 0 , 0
		
		
		
		Pass
		{
			Name "Unlit"

			CGPROGRAM

			

			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			//only defining to not throw compilation error over Unity 5.5
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			#include "UnityShaderVariables.cginc"
			#define ASE_NEEDS_FRAG_COLOR
			#pragma shader_feature_local _CLAMP_UV _CLAMP_UCLAMP _CLAMP_VCLAMP _CLAMP_UVCLAMP


			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 worldPos : TEXCOORD0;
				#endif
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform int _CullMode1;
			uniform int _Dst;
			uniform sampler2D _MainTex;
			uniform float4 _UVSpeed;
			uniform float4 _MainTex_ST;
			uniform float _Custom1xy;
			uniform sampler2D _NoiseTex;
			uniform float4 _NoiseTex_ST;
			uniform float _NoiseIntensity;
			uniform float4 _Color;
			uniform float _ColorIntensity;
			uniform sampler2D _Mask;
			uniform float _Mask_U_Speed;
			uniform float _Mask_V_Speed;
			uniform float4 _Mask_ST;
			uniform float _Custom1zw;
			uniform float _MaskIntensity;

			
			v2f vert ( appdata v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.ase_texcoord1.xy = v.ase_texcoord.xy;
				o.ase_texcoord2 = v.ase_texcoord1;
				o.ase_color = v.color;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord1.zw = 0;
				float3 vertexValue = float3(0, 0, 0);
				#if ASE_ABSOLUTE_VERTEX_POS
				vertexValue = v.vertex.xyz;
				#endif
				vertexValue = vertexValue;
				#if ASE_ABSOLUTE_VERTEX_POS
				v.vertex.xyz = vertexValue;
				#else
				v.vertex.xyz += vertexValue;
				#endif
				o.vertex = UnityObjectToClipPos(v.vertex);

				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				#endif
				return o;
			}
			
			fixed4 frag (v2f i ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				fixed4 finalColor;
				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 WorldPosition = i.worldPos;
				#endif
				float2 appendResult35 = (float2(_UVSpeed.x , _UVSpeed.y));
				float2 uv_MainTex = i.ase_texcoord1.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float2 panner56 = ( 1.0 * _Time.y * appendResult35 + uv_MainTex);
				float4 texCoord71 = i.ase_texcoord2;
				texCoord71.xy = i.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult72 = (float2(texCoord71.x , texCoord71.y));
				float2 Custom1xy78 = appendResult72;
				float2 lerpResult74 = lerp( panner56 , ( Custom1xy78 + uv_MainTex ) , _Custom1xy);
				float2 temp_output_9_0_g1 = lerpResult74;
				float2 break12_g1 = temp_output_9_0_g1;
				float2 appendResult13_g1 = (float2(saturate( break12_g1.x ) , break12_g1.y));
				float2 break3_g1 = temp_output_9_0_g1;
				float2 appendResult1_g1 = (float2(break3_g1.x , saturate( break3_g1.y )));
				#if defined(_CLAMP_UV)
				float2 staticSwitch105 = temp_output_9_0_g1;
				#elif defined(_CLAMP_UCLAMP)
				float2 staticSwitch105 = appendResult13_g1;
				#elif defined(_CLAMP_VCLAMP)
				float2 staticSwitch105 = appendResult1_g1;
				#elif defined(_CLAMP_UVCLAMP)
				float2 staticSwitch105 = saturate( temp_output_9_0_g1 );
				#else
				float2 staticSwitch105 = temp_output_9_0_g1;
				#endif
				float2 appendResult26 = (float2(_UVSpeed.z , _UVSpeed.w));
				float2 uv_NoiseTex = i.ase_texcoord1.xy * _NoiseTex_ST.xy + _NoiseTex_ST.zw;
				float2 panner55 = ( 1.0 * _Time.y * appendResult26 + uv_NoiseTex);
				float4 tex2DNode1 = tex2D( _MainTex, ( staticSwitch105 + ( tex2D( _NoiseTex, panner55 ).r * _NoiseIntensity ) ) );
				float2 appendResult49 = (float2(_Mask_U_Speed , _Mask_V_Speed));
				float2 uv_Mask = i.ase_texcoord1.xy * _Mask_ST.xy + _Mask_ST.zw;
				float2 panner54 = ( 1.0 * _Time.y * appendResult49 + uv_Mask);
				float2 appendResult77 = (float2(texCoord71.z , texCoord71.w));
				float2 Custom1zw79 = appendResult77;
				float2 lerpResult83 = lerp( panner54 , ( Custom1zw79 + uv_Mask ) , _Custom1zw);
				float4 appendResult18 = (float4(( tex2DNode1 * _Color * _ColorIntensity * i.ase_color ).rgb , ( tex2DNode1.a * i.ase_color.a * _Color.a * ( tex2D( _Mask, lerpResult83 ).r * _MaskIntensity ) )));
				
				
				finalColor = appendResult18;
				return finalColor;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	Fallback Off
}
/*ASEBEGIN
Version=19105
Node;AmplifyShaderEditor.TextureCoordinatesNode;71;-3183.829,-542.395;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;43;-2367.597,-56.33525;Inherit;False;Property;_UVSpeed;UV Speed;8;0;Create;True;1;Header(XY_Mask1_ZW_Mask2);0;0;False;1;Header(XY_MainTex_ZW_Noise);False;0,0,0,0;0.3,0,0.5,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;72;-2886.015,-546.8203;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;77;-2884.265,-377.44;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;47;-1718.041,933.8539;Inherit;False;Property;_Mask_V_Speed;Mask_V_Speed;12;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;48;-1720.259,855.4382;Inherit;False;Property;_Mask_U_Speed;Mask_U_Speed;11;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;79;-2675.509,-409.9337;Inherit;False;Custom1zw;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;78;-2680.509,-546.9336;Inherit;False;Custom1xy;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;26;-1941.226,249.1595;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;49;-1478.588,859.8036;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;55;-1720.798,225.6326;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;82;-1522.726,638.3794;Inherit;False;79;Custom1zw;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;50;-1557.229,716.2391;Inherit;False;0;52;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;84;-1109.517,968.1319;Inherit;False;Property;_Custom1zw;Custom1zw;14;1;[Toggle];Create;True;0;0;0;False;1;Header(MaskCustomData);False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;54;-1123.783,838.8009;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;42;-1404.996,442.0645;Inherit;False;Property;_NoiseIntensity;NoiseIntensity;6;0;Create;True;0;0;0;False;0;False;0.2;0.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;81;-1142.857,696.2156;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;41;-1149.496,272.3648;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;83;-835.1689,734.1446;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;90;-516.7114,907.3423;Inherit;False;Property;_MaskIntensity;MaskIntensity;9;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;52;-628.1162,702.4846;Inherit;True;Property;_Mask;Mask;10;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;40;-858.5203,-40.21967;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;14;-460.3334,177.1782;Inherit;False;Property;_Color;Color;1;0;Create;True;0;0;0;False;0;False;1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;19;-409.39,453.6371;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;88;-278.7112,764.3421;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;18;161.8211,174.6267;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;13;-55.85604,-24.05638;Inherit;False;4;4;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;3;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;20;-51.74371,300.1828;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;15;-525.7546,362.0974;Inherit;False;Property;_ColorIntensity;ColorIntensity;0;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode;45;402.7402,353.1696;Inherit;False;Property;_CullMode1;CullMode;4;1;[Enum];Create;False;0;2;OFF;0;ON;2;0;True;0;False;0;0;False;0;1;INT;0
Node;AmplifyShaderEditor.SamplerNode;1;-546.0198,-76.67545;Inherit;True;Property;_MainTex;MainTex;5;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;31;-1479.358,197.3745;Inherit;True;Property;_NoiseTex;NoiseTex;7;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;44;-2024.9,104.3937;Inherit;False;0;31;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;35;-2104.247,-99.55281;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;80;-2050.403,-407.1599;Inherit;False;78;Custom1xy;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;38;-2098.53,-273.9992;Inherit;False;0;1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;73;-1647.4,-297.6088;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;75;-1685.672,8.459036;Inherit;False;Property;_Custom1xy;Custom1xy;13;1;[Toggle];Create;True;0;0;0;False;1;Header(MainCustomData);False;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;56;-1708.468,-119.937;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;74;-1478.08,-85.6814;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;104;-1291.967,-57.08595;Inherit;False;Group_UVClamp;-1;;1;555097ac66bdbc646ac6bd2f86213952;0;1;9;FLOAT2;0,0;False;4;FLOAT2;23;FLOAT2;22;FLOAT2;15;FLOAT2;21
Node;AmplifyShaderEditor.StaticSwitch;105;-1096.471,-64.27087;Inherit;False;Property;_Clamp;Clamp;3;0;Create;True;0;0;0;False;0;False;0;0;0;True;;KeywordEnum;4;UV;UClamp;VClamp;UVClamp;Create;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.IntNode;103;405.131,433.9577;Inherit;False;Property;_Dst;BlendMode;2;1;[Enum];Create;False;0;2;Add;1;AlphaBlend;10;0;True;0;False;1;1;False;0;1;INT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;106;401.0272,176.4635;Float;False;True;-1;2;ASEMaterialInspector;100;5;Effect/Noise_UV_Mask_BuiltIn;0770190933193b94aaa3065e307002fa;True;Unlit;0;0;Unlit;2;False;True;0;1;False;;0;False;;0;1;False;;0;False;;True;0;False;;0;False;;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;1;RenderType=Opaque=RenderType;True;2;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;0;;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;0;1;True;False;;False;0
WireConnection;72;0;71;1
WireConnection;72;1;71;2
WireConnection;77;0;71;3
WireConnection;77;1;71;4
WireConnection;79;0;77;0
WireConnection;78;0;72;0
WireConnection;26;0;43;3
WireConnection;26;1;43;4
WireConnection;49;0;48;0
WireConnection;49;1;47;0
WireConnection;55;0;44;0
WireConnection;55;2;26;0
WireConnection;54;0;50;0
WireConnection;54;2;49;0
WireConnection;81;0;82;0
WireConnection;81;1;50;0
WireConnection;41;0;31;1
WireConnection;41;1;42;0
WireConnection;83;0;54;0
WireConnection;83;1;81;0
WireConnection;83;2;84;0
WireConnection;52;1;83;0
WireConnection;40;0;105;0
WireConnection;40;1;41;0
WireConnection;88;0;52;1
WireConnection;88;1;90;0
WireConnection;18;0;13;0
WireConnection;18;3;20;0
WireConnection;13;0;1;0
WireConnection;13;1;14;0
WireConnection;13;2;15;0
WireConnection;13;3;19;0
WireConnection;20;0;1;4
WireConnection;20;1;19;4
WireConnection;20;2;14;4
WireConnection;20;3;88;0
WireConnection;1;1;40;0
WireConnection;31;1;55;0
WireConnection;35;0;43;1
WireConnection;35;1;43;2
WireConnection;73;0;80;0
WireConnection;73;1;38;0
WireConnection;56;0;38;0
WireConnection;56;2;35;0
WireConnection;74;0;56;0
WireConnection;74;1;73;0
WireConnection;74;2;75;0
WireConnection;104;9;74;0
WireConnection;105;1;104;23
WireConnection;105;0;104;22
WireConnection;105;2;104;15
WireConnection;105;3;104;21
WireConnection;106;0;18;0
ASEEND*/
//CHKSM=5271F3DF84744D055A773DE84942364282E6CC08