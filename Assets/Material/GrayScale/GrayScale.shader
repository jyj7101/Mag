Shader "Custom/GrayScale"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}

        _CircleRadius("CircleRadius", Range(0, 1)) = 0

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

            float _CircleRadius; 

            
            fixed4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                float gray = dot(col.rgb, float3(0.2989, 0.587, 0.114));
                _CircleRadius *= _ScreenParams.x;
                _CenterX *= _ScreenParams.x;
                _CenterY *= _ScreenParams.y;

                float uvX = i.uv.x * _ScreenParams.x;
                float uvY = i.uv.y * _ScreenParams.y;
                float2 myUv = float2(uvX, uvY);

                float dist = distance(myUv, float2(_CenterX, _CenterY));
                //float len = length(dist);

                if(dist < _CircleRadius)
                    col.rgb = gray;

                return col;
            }
            
            ENDCG
        }
    }
}