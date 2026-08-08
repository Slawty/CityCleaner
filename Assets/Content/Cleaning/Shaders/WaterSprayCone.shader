Shader "Cleaning/WaterSprayCone"
{
    Properties
    {
        [Header(Color)]
        _WaterColor ("Water Color", Color) = (0.02, 0.43, 0.56, 1)
        _Brightness ("Brightness", Range(0.5, 2)) = 1
        _Opacity ("Opacity", Range(0, 1)) = 0.55

        [Header(Noise)]
        [NoScaleOffset] _NoiseTex ("Noise", 2D) = "gray" {}
        _FlowSpeed ("Flow Speed", Float) = 4
        _NoiseScale ("Noise Scale", Vector) = (2, 6, 0, 0)
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.35

        [Header(Shape)]
        _EdgeSoftness ("Edge Softness", Range(0.5, 8)) = 2.5
        _EdgeWidth ("Edge Fade Width", Range(0.05, 0.5)) = 0.2
        _TipFadeAmount ("Far End Fade", Range(0, 0.5)) = 0.15
        [ToggleUI] _InvertLengthUV ("Invert Length UV", Float) = 1
        [ToggleUI] _InvertFlowV ("Invert Flow Direction", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "WaterSprayCone"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _WaterColor;
                half _Brightness;
                half _Opacity;
                half _FlowSpeed;
                half _NoiseStrength;
                half _EdgeSoftness;
                half _EdgeWidth;
                half _TipFadeAmount;
                half _InvertLengthUV;
                half _InvertFlowV;
                float4 _NoiseScale;
            CBUFFER_END

            half LengthCoord(half v)
            {
                return _InvertLengthUV > 0.5h ? 1.0h - v : v;
            }

            half SideFade(half widthCoord, half softness, half edgeWidth)
            {
                half edgeDistance = min(widthCoord, 1.0h - widthCoord);
                half normalized = saturate(edgeDistance / max(edgeWidth, 0.001h));
                return pow(normalized, softness);
            }

            half FarEndFade(half lengthCoord, half amount)
            {
                if (amount <= 0.0001h)
                    return 1.0h;

                return 1.0h - smoothstep(1.0h - amount, 1.0h, lengthCoord);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half2 uv = input.uv;
                half lengthCoord = LengthCoord(uv.y);
                half sideFade = SideFade(uv.x, _EdgeSoftness, _EdgeWidth);
                half farEndFade = FarEndFade(lengthCoord, _TipFadeAmount);

                half flowLength = _InvertFlowV > 0.5h ? -lengthCoord : lengthCoord;
                half2 noiseUV = half2(uv.x * _NoiseScale.x, flowLength * _NoiseScale.y + _Time.y * _FlowSpeed);
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

                half alpha = sideFade * farEndFade * _Opacity;
                alpha *= lerp(1.0h, noise, _NoiseStrength);

                half3 color = _WaterColor.rgb * _Brightness;
                return half4(color, alpha);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
