Shader "Custom/Fur"
{
Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {} //메인 텍스쳐
		_MaskTex ("MaskTexture",2D) = "balck"{} //마스크 텍스쳐		
		_Length("Length",Range(0,0.1)) = 0.05 //털의 길이
		_Thin ("Thin",Range(0,1)) = 1 //털의 두께 (0에 가까울수록 두꺼워집니다. = 덩어리가 됩니다.)
		_Cutoff("Cutoff",Range(0,1)) = 0.5 //알파 컷아웃을 위한 구문
	}
	SubShader {
		Tags{ "RenderType" = "AlphatestCutout" "Queue" = "Alphatest" }
		LOD 200

		CGPROGRAM
		#pragma surface surf HalfLambert fullforwardshadows 

		sampler2D _MainTex;
		
		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) ;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}

		float4 LightingHalfLambert(SurfaceOutput s, float3 lightDir, float atten)
		{
			float ndotl = dot(s.Normal, lightDir);
			float halfLambert = ndotl * 0.5 + 0.5;
			
			float4 final;
			final.rgb = halfLambert * s.Albedo ;
			final.a = s.Alpha;
			return final;
		}
		ENDCG
		
		//2pass
		CGPROGRAM
		#pragma surface surf HalfLambert alphatest:_Cutoff vertex:vert noshadow noambient nolightmap 

		sampler2D _MainTex, _MaskTex;
		float _Length , _Thin;

		struct Input {
			float2 uv_MainTex, uv_MaskTex;
		}; 

		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) ;
			float m = pow(tex2D(_MaskTex, IN.uv_MaskTex).r, 3 * _Thin );
			o.Albedo = c.rgb;
			o.Alpha = m.r;
		}

		void vert(inout appdata_full v)
		{
			v.vertex.xyz += v.normal * _Length *0.1 ;
		}

		float4 LightingHalfLambert(SurfaceOutput s, float3 lightDir, float atten)
		{
			float halfLambert = dot(s.Normal, lightDir) *0.5 + 0.5;
			float4 final;
			final.rgb = s.Albedo * halfLambert;
			final.a = s.Alpha;
			return final;
		}
		
		ENDCG
		
		//3pass
		CGPROGRAM
		#pragma surface surf HalfLambert alphatest:_Cutoff vertex:vert noshadow noambient nolightmap 

		sampler2D _MainTex, _MaskTex;
		float _Length , _Thin;

		struct Input {
			float2 uv_MainTex, uv_MaskTex;
		}; 

		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) ;
			float m = pow(tex2D(_MaskTex, IN.uv_MaskTex).r, 5 * _Thin);
			o.Albedo = c.rgb;
			o.Alpha = m.r;
		}

		void vert(inout appdata_full v)
		{
			v.vertex.xyz += v.normal * _Length *0.2 ;
		}

		float4 LightingHalfLambert(SurfaceOutput s, float3 lightDir, float atten)
		{
			float halfLambert = dot(s.Normal, lightDir) *0.5 + 0.5;
			float4 final;
			final.rgb = s.Albedo * halfLambert;
			final.a = s.Alpha;
			return final;
		}

		ENDCG
		
		//4pass
		CGPROGRAM
		#pragma surface surf HalfLambert alphatest:_Cutoff vertex:vert noshadow noambient nolightmap 

		sampler2D _MainTex, _MaskTex;
		float _Length , _Thin;

		struct Input {
			float2 uv_MainTex, uv_MaskTex;
		}; 

		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) ;
			float m = pow(tex2D(_MaskTex, IN.uv_MaskTex).r, 7 * _Thin);
			o.Albedo = c.rgb;
			o.Alpha = m.r;
		}

		void vert(inout appdata_full v)
		{
			v.vertex.xyz += v.normal * _Length *0.3 ;
		}

		float4 LightingHalfLambert(SurfaceOutput s, float3 lightDir, float atten)
		{
			float halfLambert = dot(s.Normal, lightDir) *0.5 + 0.5;
			float4 final;
			final.rgb = s.Albedo * halfLambert;
			final.a = s.Alpha;
			return final;
		}

		ENDCG
		
		//5pass
		CGPROGRAM
		#pragma surface surf HalfLambert alphatest:_Cutoff vertex:vert noshadow noambient nolightmap 

		sampler2D _MainTex, _MaskTex;
		float _Length , _Thin;

		struct Input {
			float2 uv_MainTex, uv_MaskTex;
		}; 

		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) ;
			float m = pow(tex2D(_MaskTex, IN.uv_MaskTex).r, 9 * _Thin);
			o.Albedo = c.rgb;
			o.Alpha = m.r;
		}

		void vert(inout appdata_full v)
		{
			v.vertex.xyz += v.normal * _Length *0.4 ;
		}

		float4 LightingHalfLambert(SurfaceOutput s, float3 lightDir, float atten)
		{
			float halfLambert = dot(s.Normal, lightDir) *0.5 + 0.5;
			float4 final;
			final.rgb = s.Albedo * halfLambert;
			final.a = s.Alpha;
			return final;
		}

		ENDCG
		
				
	}
	FallBack "Diffuse"
}