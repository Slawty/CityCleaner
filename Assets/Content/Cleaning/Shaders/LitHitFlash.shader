// Hit flash uses shader time after you set _FlashStartTime once per hit.
// The GPU cannot detect hits by itself; script sets _FlashStartTime when damaged.
// Leave _FlashFlickerHz at 0 for a smooth fade to white.
Shader "Cleaning/Lit Hit Flash"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0, 2)) = 1

        _Smoothness ("Smoothness", Range(0, 1)) = 0.2
        _Metallic ("Metallic", Range(0, 1)) = 0

        [Header(HitFlash)]
        _FlashStartTime ("Flash Start Time", Float) = -999999
        _FlashDuration ("Flash Duration", Range(0.01, 1)) = 0.08
        _FlashStrength ("Flash Strength", Range(0, 1)) = 1
        _FlashFlickerHz ("Flash Flicker Hz", Float) = 0
        _FlashFlickerBlend ("Flash Flicker Blend", Range(0, 1)) = 0

        [Header(Heat)]
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 0)
        _EmissionStrength ("Emission Strength", Range(0, 1)) = 0
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

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BumpMap_ST;
                half _BumpScale;
                half _Smoothness;
                half _Metallic;
                float _FlashStartTime;
                half _FlashDuration;
                half _FlashStrength;
                half _FlashFlickerHz;
                half _FlashFlickerBlend;
                half3 _EmissionColor;
                half _EmissionStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
            };

            half3 SampleNormalTS(float2 uv)
            {
                half4 p = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv);
                half3 n;
                n.xy = (p.xy * 2 - 1) * _BumpScale;
                n.z = sqrt(1.0h - saturate(dot(n.xy, n.xy)));
                return normalize(n);
            }

            half HitFlashFactor()
            {
                half age = (half)_Time.y - (half)_FlashStartTime;
                half dur = max((half)_FlashDuration, (half)1e-4);
                half pulse = saturate(1.0h - age / dur);
                half flicker = 1.0h;
                if (_FlashFlickerHz > (half)1e-3)
                {
                    flicker = lerp(1.0h,
                        0.5h + 0.5h * sin((half)_Time.y * _FlashFlickerHz * 6.28318548h),
                        _FlashFlickerBlend);
                }
                return pulse * flicker * _FlashStrength;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;

                VertexPositionInputs posInputs =
                    GetVertexPositionInputs(v.positionOS.xyz);

                VertexNormalInputs normInputs =
                    GetVertexNormalInputs(v.normalOS, v.tangentOS);

                o.positionCS = posInputs.positionCS;
                o.positionWS = posInputs.positionWS;
                o.normalWS = normInputs.normalWS;
                o.tangentWS = half4(normInputs.tangentWS, v.tangentOS.w);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.shadowCoord = TransformWorldToShadowCoord(posInputs.positionWS);

                return o;
            }

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
                half rough = saturate(1.0h - _Smoothness);
                half specPow = lerp(8.0h, 256.0h, _Smoothness);
                half3 specColor = lerp((half3)0.04h, albedo, _Metallic);
                half spec =
                    pow(nh, specPow) * rough * ndotl *
                    mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                lit += specColor * mainLight.color * spec;

                lit += SampleSH(normalWS) * albedo;
                return lit;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 t = normalize(i.tangentWS.xyz);
                half3 n = normalize(i.normalWS);
                half3 b = normalize(cross(n, t) * i.tangentWS.w);

                half2 bumpUv = TRANSFORM_TEX(i.uv, _BumpMap);
                half3 normalTS = SampleNormalTS(bumpUv);
                half3 normalWS = normalize(t * normalTS.x + b * normalTS.y + n * normalTS.z);

                half3 albedo =
                    SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb;

                half flash = HitFlashFactor();
                albedo = lerp(albedo, half3(1, 1, 1), flash);

                half3 rgb =
                    SimpleLighting(normalWS, albedo, i.positionWS, i.shadowCoord);
                rgb += _EmissionColor * _EmissionStrength;
                return half4(rgb, 1);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
