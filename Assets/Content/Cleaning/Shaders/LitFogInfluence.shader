Shader "Cleaning/LitFogInfluence"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _FogInfluence ("Fog Influence", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;
                half _FogInfluence;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
            };

            half3 SimpleLighting(half3 normalWS, half3 albedo, float3 positionWS, half4 shadowCoord)
            {
                Light mainLight = GetMainLight(shadowCoord);
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half3 lit =
                    albedo * mainLight.color *
                    (ndotl * mainLight.distanceAttenuation * mainLight.shadowAttenuation);

                half3 viewDir = GetWorldSpaceNormalizeViewDir(positionWS);
                half3 halfDir = normalize(mainLight.direction + viewDir);
                half nh = saturate(dot(normalWS, halfDir));
                half specPow = lerp(8.0h, 256.0h, _Smoothness);
                half3 specColor = lerp((half3)0.04h, albedo, _Metallic);
                half spec =
                    pow(nh, specPow) * (1.0h - _Smoothness) * ndotl *
                    mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                lit += specColor * mainLight.color * spec;

                lit += SampleSH(normalWS) * albedo;
                return lit;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.shadowCoord = TransformWorldToShadowCoord(posInputs.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                half3 color = SimpleLighting(normalWS, albedo, input.positionWS, input.shadowCoord);

                half fogFactor = ComputeFogFactor(input.positionCS.z);
                half3 fogged = MixFog(color, fogFactor);
                color = lerp(color, fogged, _FogInfluence);

                return half4(color, 1);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
