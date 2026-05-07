// Boundary tint for cleaning zones: soft blue wall that fades out toward the top.
// Vertical fade uses UV.y (0 = bottom / stronger, 1 = top / fades away).
// Pattern uses world-space triplanar mapping so repeat density stays consistent when you scale the mesh.
Shader "Cleaning/Barrier"
{
    Properties
    {
        _Color ("Tint", Color) = (0.55, 0.78, 1.0, 0.38)
        _FadePower ("Top Fade Sharpness", Range(0.15, 4)) = 1.15
        [ToggleUI] _InvertVerticalUV ("Invert UV Vertical", Float) = 0

        [Header(Pattern)]
        [NoScaleOffset] _PatternMap ("Pattern", 2D) = "white" {}
        _PatternScale ("Pattern Density (tiles per world unit)", Float) = 2
        _PatternStrength ("Pattern Strength", Range(0, 1)) = 0.45
        _TriplanarSharpness ("Projection Sharpness", Range(1, 8)) = 4
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
            Name "BarrierTransparent"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_PatternMap);
            SAMPLER(sampler_PatternMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half fade : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _FadePower;
                half _InvertVerticalUV;
                float _PatternScale;
                half _PatternStrength;
                half _TriplanarSharpness;
            CBUFFER_END

            half4 SamplePatternTriplanar(float3 worldPos, half3 worldNormal)
            {
                float3 p = worldPos * _PatternScale;
                half3 w = pow(abs(worldNormal), _TriplanarSharpness);
                w /= max(w.x + w.y + w.z, 1e-4h);

                half4 sx = SAMPLE_TEXTURE2D(_PatternMap, sampler_PatternMap, p.zy);
                half4 sy = SAMPLE_TEXTURE2D(_PatternMap, sampler_PatternMap, p.xz);
                half4 sz = SAMPLE_TEXTURE2D(_PatternMap, sampler_PatternMap, p.xy);
                return sx * w.x + sy * w.y + sz * w.z;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                half v = input.uv.y;
                if (_InvertVerticalUV > 0.5h)
                    v = 1.0h - v;

                // Full visibility at bottom (v=0), fade toward top (v=1).
                output.fade = saturate(pow(1.0h - v, _FadePower));

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 n = normalize(input.normalWS);
                half4 pattern = SamplePatternTriplanar(input.positionWS, n);

                half4 c = _Color;
                c.rgb *= lerp(half3(1, 1, 1), pattern.rgb, _PatternStrength);
                c.a *= input.fade;
                return c;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
