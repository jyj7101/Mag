Shader "Custom/GrayScale"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _MaskingTex("MaskingTex", 2D) = "white" {}

        _GrayScale("Grayscale", Range(0.0, 1)) = 0.0
        _CircleSize("CircleSize", Range(0.0, 100)) = 0
    }
        SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _MaskingTex;
            float _GrayScale;
            float _CircleSize;

                //float aspectratio = _ScreenParams.x * (_ScreenParams.w - 1);
                //float dist = saturate(distance(i.uv, float2(0.5, 0.5)) / _CircleSize);
                
                //float circle = saturate(1.0 - dist) * _CircleSize;
                //float circle = round(1.0 - dist);
                
                //fixed4 col = tex2D(_MainTex, i.uv);
                //fixed4 col = tex2D(_MainTex, i.uv) * circle;
                //float aa = circle * gray;
                //col = aa;

            fixed4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                
                float gray = (col.r * 0.2989) + (col.g * 0.587) + (col.b * 0.114);
                float dist = distance(i.uv, float2(0.5, 0.5));
                float roundDist = round(dist);


                col = tex2D(_MainTex, i.uv) + roundDist;
                
                return col;
            }
            
            ENDCG
        }
    }
}