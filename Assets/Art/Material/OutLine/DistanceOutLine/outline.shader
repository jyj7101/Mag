// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'
Shader "Unlit/outline"
{
   Properties 
	{
		_Width ("Width", Float ) = 1
		_Intensity ("Intensity", float ) = 1
		_AlphaStrength("Alpha Strength", Range(0, 1)) = 0
		_Color ("Color", Color) = (1,1,1,1)
	}
	SubShader 
	{
		Tags 
		{
			"IgnoreProjector"="True"
			"RenderType" = "Transparent"
			"Queue"="Transparent"
		}
		AlphaToMask On
		Cull Front
		Pass 
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#pragma target 3.0

			struct VertexInput 
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct VertexOutput 
			{
				float4 pos : SV_POSITION;
				float dist : TEXCOORD0;
			};

			uniform float _Width;
			uniform float4 _Color;
			uniform float _Intensity;
			uniform float _AlphaStrength;

			VertexOutput vert (VertexInput v) 
			{
				VertexOutput o;
				float4 objPos = mul (unity_ObjectToWorld, float4(0,0,0,1));
				o.dist = distance(_WorldSpaceCameraPos, objPos.xyz) / _ScreenParams.g;
				float expand = o.dist * _Width;
				float4 pos = float4(v.vertex.xyz + v.normal * expand, 1);
				
				o.pos = UnityObjectToClipPos(pos);
				return o;
			}

			float4 frag(VertexOutput i) : COLOR 
			{
				float alpha = _Color.a * _AlphaStrength;
				return float4(_Color.rgb * _Intensity, alpha);
			}
			ENDCG
		}
	}
}
