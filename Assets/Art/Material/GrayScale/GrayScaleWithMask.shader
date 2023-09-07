Shader "Unlit/GrayScaleWithMask"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _Mask("mask", 2D) = "white" {}
    }
    SubShader
    {
        Cull off ZWrite off ZTest off

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

            sampler2D _MainTex;
            sampler2D _Mask;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                fixed4 mask = tex2D(_Mask, i.uv);
                fixed4 gray = dot(col.rgb, float3(0.2989, 0.587, 0.114));
                col = (gray) * mask + col * (1-mask);
                
                return col;
            }
            ENDCG
        }
    }
}
