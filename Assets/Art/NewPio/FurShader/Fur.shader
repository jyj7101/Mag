Shader "Unlit/Fur"
{
    Properties
    {
        [Header(Albedo)]
        _Color("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture(RGB)", 2D) = "white" {}
        
        [Header(Normal)]
        [Toggle] _NormalMap("Enable Normal Map", float) = 0.0
        _BumpMap("Normal Texture", 2D) = "bump" {}
        _BumpScale("Normal Intensity", Range(0.01, 5)) = 1
        
        [Header(Highlight)]
        _SpecularColor("SpecularColor", Color) = (1, 1, 1, 1)
        _Shininess ("Shininess", Range(0.01, 256.0)) = 8.0
        _RimColor ("Rim Color", Color) = (0, 0, 0, 1)
        _RimPower ("Rim Power", Range(0.01, 8.0)) = 6.0
        
        [Header(Fur)]
        _FurTex ("Fur Pattern(R)", 2D) = "white" { }
        _FurThinness ("Fur Pattern Tiling", Range(0.01, 10)) = 1
        _FurMaskTex ("Fur Alpha Mask(R)", 2D) = "white" { }
         _FurAlphaThreshold("Fur Alpha", Range(0, 1)) = 0.5
         _FurLength("Fur Length", Range(0.01, 2)) = 0.4

         [IntRange] _vertPassCount("Add VertPass Count", Range(1, 8)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue"="AlphaTest" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "FurLit"
            Tags {"LightMode" = "UniversalForward"}
            Cull off
            ZWrite On
            
            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #define _FOG 1
            #define _HALFLAMBERT 1

            #pragma target 4.5
            #pragma prefer_hlslcc gles  
            #pragma exclude_renderers d3d11_9x 
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #pragma shader_feature_local _NORMALMAP_ON

            #pragma vertex vert
            #pragma require geometry    // geometry 쉐이더를 사용한다고 명시 (Metal API는 geometry쉐이더를 지원 안함)
            #pragma geometry geom       // geom함수로 geometry쉐이더 선언
            #pragma fragment frag


            struct appdata
            {
                float4 positionOS : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;

                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;

                float3 positionWS       : TEXCOORD1;
                float3 normalWS         : TEXCOORD2;
                float4 tangentWS        : TEXCOORD3;
                float4 shadowCoord      : TEXCOORD4;

                float4 fogCoordAndVertexLight : TEXCOORD5;
                float layerIndex : TEXCOORD6;
            }; 
            

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            TEXTURE2D(_FurTex);
            SAMPLER(sampler_FurTex);

            TEXTURE2D(_FurMaskTex);
            SAMPLER(sampler_FurMaskTex);

           int _vertPassCount;
            
            // SRP Batcher
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;

                float4 _BumpMap_ST;
                float _BumpScale;

                float4 _FurTex_ST;
                float4 _FurMaskTex_ST;

                float4 _SpecularColor;
                float _Shininess;

                float4 _RimColor;
                float _RimPower;

                float _FurLength;
                float _FurThinness;
                float _FurAlphaThreshold;
            CBUFFER_END
             
            // 버텍스 쉐이더에서 아무것도 하지 않고 그대로 버텍스 데이터를 지오메트리 쉐이더로 return.
            appdata vert(appdata v)
            {
                return v;
            }

            // 지오메트리 쉐이더에서 버텍스 쉐이더 연산 진행.
            void AppendVertexPass(inout TriangleStream<v2f> stream, appdata v, int index)
            {
                v2f o = (v2f)0;

                v.positionOS.xyz += v.normalOS.xyz * _FurLength * 0.01f * index;    // Normal 방향으로 메쉬 크기 확장
                
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 positionVS = TransformWorldToView(positionWS);
                float4 positionCS = TransformWorldToHClip(positionWS);

                o.normalWS = normalize(TransformObjectToWorldNormal(v.normalOS));
                #ifdef _NORMALMAP_ON
                    o.tangentWS.xyz = normalize(TransformObjectToWorldDir(v.tangentOS.xyz));
                    o.tangentWS.w = v.tangentOS.w * GetOddNegativeScale();
                #endif

                o.positionWS = positionWS;
                o.positionCS = positionCS;
                o.uv.xy = v.uv.xy; 
                o.layerIndex = index;   // 반복호출 카운트 저장
               
                o.shadowCoord = TransformWorldToShadowCoord(positionWS);
                #ifdef _ADDITIONAL_LIGHTS_VERTEX 
                    o.fogCoordAndVertexLight.xyz = VertexLighting(positionWS, o.normalWS);
                #endif
                #if _FOG
                    o.fogCoordAndVertexLight.w = ComputeFogFactor(o.positionCS.z);
                #endif

                stream.Append(o);
            }

            [maxvertexcount(21)]    // 최대 사용할 버텍스 갯수 (_vertPassCount최대값 * triangle버텍스 수)
            void geom(triangle appdata input[3], inout TriangleStream<v2f> stream)
            {
                for (int i = 0; i < _vertPassCount + 1; i++)
                {
                    for (float j = 0; j < 3; j++)   // 3 = 1개의 triangle의 버텍스 수
                    {
                        AppendVertexPass(stream, input[j], i);  // 버텍스 쉐이더 기능을 반복 호출
                    }
                    stream.RestartStrip();  // Triangle Weld 끊기
                }
            }

            float4 frag(v2f i) : SV_Target
            {
                Light mainlight = GetMainLight(i.shadowCoord);
                float3 lightDirWS = mainlight.direction;
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float3 halfDirWS = normalize(viewDirWS + lightDirWS);
                
                float3 normalWS = i.normalWS;
                #ifdef _NORMALMAP_ON
                    float2 bumpTexUV = i.uv.xy * _BumpMap_ST.xy + _BumpMap_ST.zw;
                    float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, bumpTexUV), _BumpScale);
                    float sign = i.tangentWS.w; 
                    float3 bitangent = sign * cross(i.normalWS.xyz, i.tangentWS.xyz);
                    half3x3 tangentToWorld = half3x3(i.tangentWS.xyz, bitangent.xyz, i.normalWS.xyz);
                    normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
                    normalWS = normalize(normalWS);
                #endif
                
                float NDotV = saturate(dot(normalWS, viewDirWS));
                float NDotL = dot(normalWS, lightDirWS);
                #if _HALFLAMBERT
                     float Lambert = NDotL * 0.5f + 0.5f;
                #else
                     float Lambert = saturate(NDotL); 
                #endif
                float NDotH = saturate(dot(normalWS, halfDirWS));
                float3 reflectVector = reflect(-viewDirWS, normalWS);
               
                float3 lightColor = mainlight.color;
                float shadowAtten = mainlight.shadowAttenuation * mainlight.distanceAttenuation;
                float3 ambientColor = SampleSH(normalWS);

                float smoothness = 0;
                half mip = PerceptualRoughnessToMipmapLevel(smoothness);
                half4 encodedIrradiance = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip);
                half3 irradiance = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
               
                float2 mainTexUV = i.uv.xy * _MainTex_ST.xy + _MainTex_ST.zw;
                float4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mainTexUV) * _Color;
                float3 ambient = ambientColor * albedo.rgb;
                float3 diffuse = (lightColor.rgb * albedo.rgb) * Lambert * shadowAtten;
                float3 specular = lightColor.rgb * _SpecularColor.rgb * pow(NDotH, _Shininess);
                float3 rim = (_RimColor.rgb * pow((1.0f - NDotV), _RimPower)) * irradiance;

                #ifdef _ADDITIONAL_LIGHTS
                    uint pixelLightCount = GetAdditionalLightsCount();
                    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                    { 
                        Light addLight = GetAdditionalLight(lightIndex, i.positionWS);
                        float3 attenLightColor = addLight.color.rgb * (addLight.distanceAttenuation * addLight.shadowAttenuation);
                        float NDotL_Add = saturate(dot(normalWS, addLight.direction));
                        diffuse.rgb += attenLightColor.rgb * albedo.rgb * NDotL_Add;
                    }
                #endif 
                #ifdef _ADDITIONAL_LIGHTS_VERTEX 
                    diffuse.rgb += i.fogCoordAndVertexLight.xyz * albedo.rgb;
                #endif

                float3 finalColor = diffuse + ambient + ((specular + rim));
                #if _FOG
                    finalColor.rgb = MixFog(finalColor.rgb, i.fogCoordAndVertexLight.w);
                #endif

               
                // 0번 레이어를 제외하고 AlphaClip 연산
                if (i.layerIndex > 0.0f) 
                {
                    float2 furrMaskTexUV = i.uv.xy * _FurMaskTex_ST.xy + _FurMaskTex_ST.zw;
                    float4 furrMask = SAMPLE_TEXTURE2D(_FurMaskTex, sampler_FurMaskTex, furrMaskTexUV);

                    float2 furrTexUV = i.uv.xy * _FurTex_ST.xy + _FurTex_ST.zw;
                    float4 furrPattern = SAMPLE_TEXTURE2D(_FurTex, sampler_FurTex, furrTexUV * _FurThinness);
                
                    clip((furrMask.r * furrPattern.r) - _FurAlphaThreshold);
                }
               
                return float4(finalColor, 1);
            }
            
            ENDHLSL
        }
    }
}