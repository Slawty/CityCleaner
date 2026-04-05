Shader "Custom/URP_ObjectDirt"
{
    Properties
    {
        _BaseMap ("Clean Texture", 2D) = "white" {}
        _DirtMap ("Dirty Texture", 2D) = "white" {}
        _DirtMask ("Dirt Mask", 2D) = "white" {}

        _Smoothness ("Smoothness", Range(0,1)) = 0.2
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
            "UniversalMaterialType"="Lit"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            sampler2D _BaseMap;
            sampler2D _DirtMap;
            sampler2D _DirtMask;

            float _Smoothness;
            float _Metallic;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float2 uv1 : TEXCOORD3;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;

                VertexPositionInputs posInputs =
                    GetVertexPositionInputs(v.positionOS.xyz);

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(v.normalOS);

                o.positionCS = posInputs.positionCS;
                o.positionWS = posInputs.positionWS;
                o.normalWS = normalInputs.normalWS;

                o.uv = v.uv;
                o.uv1 = v.uv1;

                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                // Dirt mask from UV1
                float dirt =
                    tex2D(_DirtMask, i.uv1).r;

                // Base textures from UV0
                float3 clean =
                    tex2D(_BaseMap, i.uv).rgb;

                float3 dirty =
                    tex2D(_DirtMap, i.uv1).rgb;

                float3 albedo =
                    lerp(clean, dirty, dirt);

                float3 normalWS =
                    normalize(i.normalWS);

                // Main light
                Light mainLight =
                    GetMainLight();

                float NdotL =
                    saturate(dot(normalWS,
                                 mainLight.direction));

                float3 color =
                    albedo *
                    mainLight.color *
                    NdotL *
                    mainLight.shadowAttenuation;

                // Ambient
                float3 ambient =
                    SampleSH(normalWS) *
                    albedo;

                return float4(color + ambient, 1);
            }

            ENDHLSL
        }
    }
}
