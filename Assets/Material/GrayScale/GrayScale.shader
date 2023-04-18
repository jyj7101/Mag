Shader "Custom/GrayScale"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _MaskingTex("MaskingTex", 2D) = "white" {}

        _CircleSizeX("CircleSize X", float) = 1
        _CircleSizeY("CircleSize Y", float) = 1

        _CenterX("Circle center x", float) = 0.5
        _CenterY("Circle center y", float) = 0.5

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
        
            float _CenterX;
            float _CenterY;

            float _CircleSizeX;
            float _CircleSizeY;
            
            fixed4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                float gray = (col.r * 0.2989) + (col.g * 0.587) + (col.b * 0.114);

                float dist = length(float2(i.uv.x - _CenterX, i.uv.y - _CenterY) * float2(_CircleSizeX * (_ScreenParams.w - 1), _CircleSizeY *(_ScreenParams.z - 1)));
                //float dist = saturate(distance(i.uv, float2(_CircleCenter.x, _CircleCenter.y)) * _CircleSize);
                float circle = saturate(dist);
                //float circle = round(dist);

                float grayCircle = circle + gray;

                float fCircle = grayCircle;
                return col + circle;
            }
            

            ENDCG
        }
    }
}