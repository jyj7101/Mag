Shader "Unlit/Heart"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Height ("Height", float) = 0
        _Thickness("Thickness", float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

            Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2g
            {
                float4 positionCS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct g2f
            {
                float4 positionCS : POSITION;
                float2 uv : TEXCOORD0;
                float3 distance : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Height;
            float _Thickness; 

            float3 VertexDistance(float4 vertex0, float4 vertex1, float4 vertex2){
                float distance0 = length(vertex0);
                float distance1 = length(vertex1);
                float distance2 = length(vertex2);
                return float3(distance0, distance1, distance2);
            }

            float WireFrame(float3 dist, float thickness){
                float wireframe = min(dist.x, min(dist.y, dist.z));
                wireframe = 1 - smoothstep(0, thickness, wireframe);
                return wireframe;
            }

            v2g vert(appdata v)
            {
                v2g output;
                output.positionCS = UnityObjectToClipPos(v.positionOS);
                output.uv = TRANSFORM_TEX (v.uv, _MainTex);

                return output;
            }

            [maxvertexcount (3)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                g2f output;

                output.positionCS = input[0].positionCS;
                output.uv = input[0].uv;
                output.distance = float3(1, 0, 0);
                triStream.Append(output);

                output.positionCS =  input[1].positionCS;
                output.uv = input[1].uv;
                output.distance = float3(0, 1, 0);
                triStream.Append(output);
                
                output.positionCS = input[2].positionCS;
                output.uv = input[2].uv;
                output.distance = float3(0, 0, 1);
                triStream.Append(output);
            }

            fixed4 frag (g2f i) : SV_Target
            {
                float4 finalColor = 1;
                //finalColor.rgb = i.distance.xyz;
                finalColor.rgb = WireFrame(i.distance.xyz, _Thickness);

                return finalColor;
            }
            ENDCG
        }
    }
}