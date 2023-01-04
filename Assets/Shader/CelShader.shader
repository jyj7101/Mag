Shader "Unlit/CelShader"
{
    Properties
    {
        _Outline_Bold("Outline Bold", Range(0.0, 1.0)) = 0.5
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _StairNum ("Stair Num", float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        cull front
        Pass
        {
            CGPROGRAM
            #pragma vertex _VertexFuc
            #pragma fragment _FragmentFuc
            #include "UnityCG.cginc"

            float _Outline_Bold;

            // struct vertex input
            struct ST_VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            struct ST_VertexOutput {
                float4 vertex : SV_POSITION;
            };

            ST_VertexOutput _VertexFuc(ST_VertexInput stInput) {
                ST_VertexOutput stOutput;
                float3 fNormalized_Normal = normalize(stInput.normal);
                float3 fOutline_Position = stInput.vertex + fNormalized_Normal * (_Outline_Bold * 0.1f);

                stOutput.vertex = UnityObjectToClipPos(fOutline_Position);
                return stOutput;
            }

            float4 _FragmentFuc(ST_VertexOutput i) : SV_Target {
                return 0.0f;
            }
            ENDCG
        }

        cull back


        CGPROGRAM
        fixed4 frag(v2f i) : SV_TARGET{
            // half4 col;
            // half4 MainTex = tex2D(_MainTex, i.TEXCOORD);
            // half3 WorldSpaceLightDir = normalize(_WorldSpaceLightPos0);
            // half ndotl = dot(WorldSpaceLightDir, i.normal);
            // half halfLambert = ndotl * 0.5 + 0.5;
            // half floorToon = floor(halfLambert * _StairNum) * (1/ _StairNum);
            col = MainTex;
            return col;
        }

        ENDCG
    }
}