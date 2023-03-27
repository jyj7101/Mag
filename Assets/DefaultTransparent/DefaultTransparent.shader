Shader "Transparent/DefaultTransparent"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        
        //! 알파를 제거할 임계점 변수
        _Cutoff ("Cutout", Range(0,1)) = 0.5        
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue" = "AlphaTest" }
       
 
        CGPROGRAM
      
        //! _Cutoff는 Properties인터페이스에 선언한 변수와 동일하게 지어야함
        #pragma surface surf Lambert alphatest:_Cutoff    
 
        sampler2D _MainTex;
 
        struct Input
        {
            float2 uv_MainTex;
        };
 
    
        fixed4 _Color;
 
        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
            
    FallBack "Diffuse"
}

