Shader "Custom/GrayScale"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        //_MaskTex("Mask texture", 2D) = "white" {}
        _GrayScale("Grayscale", Range(0.0, 0.75)) = 0.0
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

            float _GrayScale;
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
            sampler2D _MaskTex;

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float gray = (col.r + col.g + col.b) / 3;
                
                // fixed4 mask = tex2D(_MaskTex, i.uv);
                // col = (1 - col) * mask + col * (1 - mask); reverse
                
                col = lerp(col, gray, _GrayScale);
                return col;
            }
            ENDCG
        }
        
        
    }
}