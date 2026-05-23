// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Effect/SoftDissolve_Additive_BuiltIn"
{
	Properties
	{
		_ColorIntensity("ColorIntensity", Float) = 1
		_Color("Color", Color) = (1,1,1,1)
		[Enum(Add,1,AlphaBlend,10)]_Dst("BlendMode", Int) = 1
		[Enum(OFF,0,ON,2)]_CullMode("CullMode", Int) = 0
		_MainTex("MainTex", 2D) = "white" {}
		_DissolveTex("DissolveTex", 2D) = "white" {}
		_DissolveProgress("DissolveProgress", Range( 0 , 1)) = 0
		_Hardness("Hardness", Range( 0.5 , 1)) = 0.5
		[Header(DissolveTex_Speed_Cus)][Toggle]_Custom1xy("Custom1xy", Float) = 0

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
			#define ASE_NEEDS_FRAG_COLOR


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
				float4 ase_color : COLOR;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform int _Dst;
			uniform int _CullMode;
			uniform float4 _Color;
			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform float _ColorIntensity;
			uniform float _Hardness;
			uniform sampler2D _DissolveTex;
			uniform float4 _DissolveTex_ST;
			uniform float _Custom1xy;
			uniform float _DissolveProgress;

			
			v2f vert ( appdata v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.ase_texcoord1.xy = v.ase_texcoord.xy;
				o.ase_color = v.color;
				o.ase_texcoord2 = v.ase_texcoord1;
				
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
				float2 uv_MainTex = i.ase_texcoord1.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float4 tex2DNode25 = tex2D( _MainTex, uv_MainTex );
				float2 uv_DissolveTex = i.ase_texcoord1.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
				float4 texCoord38 = i.ase_texcoord2;
				texCoord38.xy = i.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 appendResult33 = (float2(texCoord38.x , texCoord38.y));
				float2 lerpResult36 = lerp( uv_DissolveTex , ( uv_DissolveTex + appendResult33 ) , _Custom1xy);
				float smoothstepResult16 = smoothstep( ( 1.0 - _Hardness ) , _Hardness , saturate( ( ( tex2D( _DissolveTex, lerpResult36 ).r + 1.0 ) - ( ( texCoord38.z + _DissolveProgress ) * 2.0 ) ) ));
				float4 appendResult18 = (float4(( _Color * tex2DNode25 * _ColorIntensity * i.ase_color ).rgb , ( tex2DNode25.a * smoothstepResult16 * i.ase_color.a )));
				
				
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
Node;AmplifyShaderEditor.TextureCoordinatesNode;38;-3208.696,290.058;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;33;-2952.034,282.0499;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;39;-3024.094,-6.342408;Inherit;False;0;6;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;34;-2728.873,119.0691;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;37;-2757.822,248.164;Inherit;False;Property;_Custom1xy;Custom1xy;8;2;[Header];[Toggle];Create;True;1;DissolveTex_Speed_Cus;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;36;-2535.069,92.7513;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-2562.819,441.8317;Inherit;False;Property;_DissolveProgress;DissolveProgress;6;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;7;-2205.355,274.1062;Inherit;False;Constant;_Float3;Float 3;11;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;6;-2359.947,73.32875;Inherit;True;Property;_DissolveTex;DissolveTex;5;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;8;-2206.904,482.2644;Inherit;False;Constant;_Float7;Float 7;11;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;35;-2193.722,364.0088;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;-1997.948,363.039;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;10;-1989.062,102.4284;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;13;-1810.083,104.2964;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;12;-1968.306,488.2484;Inherit;False;Property;_Hardness;Hardness;7;0;Create;True;0;0;0;False;0;False;0.5;0.5;0.5;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;22;-2671.98,-562.4258;Inherit;False;0;25;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;14;-1632.061,177.0225;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;15;-1618.958,105.5298;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;16;-1429.92,107.1864;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;28;-2040.685,-221.0385;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;23;-1939.021,-508.1467;Inherit;False;Property;_ColorIntensity;ColorIntensity;0;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;24;-2327.364,-807.3026;Inherit;False;Property;_Color;Color;1;0;Create;True;0;0;0;False;0;False;1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;26;-1666.474,-605.6389;Inherit;True;4;4;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;3;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;27;-1250.651,-161.4914;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;25;-2374.104,-586.8987;Inherit;True;Property;_MainTex;MainTex;4;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.IntNode;51;-820.2695,-299.5249;Inherit;False;Property;_Dst;BlendMode;2;1;[Enum];Create;False;0;2;Add;1;AlphaBlend;10;0;True;0;False;1;1;False;0;1;INT;0
Node;AmplifyShaderEditor.IntNode;2;-824.5245,-386.6332;Inherit;False;Property;_CullMode;CullMode;3;1;[Enum];Create;True;0;2;OFF;0;ON;2;0;True;0;False;0;0;False;0;1;INT;0
Node;AmplifyShaderEditor.DynamicAppendNode;18;-1080.237,-605.3187;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;52;-879.8757,-604.2565;Float;False;True;-1;2;ASEMaterialInspector;100;5;Effect/SoftDissolve_Additive_BuiltIn;0770190933193b94aaa3065e307002fa;True;Unlit;0;0;Unlit;2;False;True;0;1;False;;0;False;;0;1;False;;0;False;;True;0;False;;0;False;;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;1;RenderType=Opaque=RenderType;True;2;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;0;;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;0;1;True;False;;False;0
WireConnection;33;0;38;1
WireConnection;33;1;38;2
WireConnection;34;0;39;0
WireConnection;34;1;33;0
WireConnection;36;0;39;0
WireConnection;36;1;34;0
WireConnection;36;2;37;0
WireConnection;6;1;36;0
WireConnection;35;0;38;3
WireConnection;35;1;5;0
WireConnection;11;0;35;0
WireConnection;11;1;8;0
WireConnection;10;0;6;1
WireConnection;10;1;7;0
WireConnection;13;0;10;0
WireConnection;13;1;11;0
WireConnection;14;0;12;0
WireConnection;15;0;13;0
WireConnection;16;0;15;0
WireConnection;16;1;14;0
WireConnection;16;2;12;0
WireConnection;26;0;24;0
WireConnection;26;1;25;0
WireConnection;26;2;23;0
WireConnection;26;3;28;0
WireConnection;27;0;25;4
WireConnection;27;1;16;0
WireConnection;27;2;28;4
WireConnection;25;1;22;0
WireConnection;18;0;26;0
WireConnection;18;3;27;0
WireConnection;52;0;18;0
ASEEND*/
//CHKSM=21A7528C809720A3405119174576B29133C96071