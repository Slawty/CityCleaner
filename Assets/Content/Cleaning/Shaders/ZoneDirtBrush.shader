Shader "Hidden/ZoneDirtBrush"
{
    Properties
    {
        _MainTex ("Zone Dirt", 2D) = "white" {}
        _BrushTex ("Brush", 2D) = "white" {}
        _BrushWorldPos ("Brush World Pos", Vector) = (0,0,0,0)
        _ZoneMinXZ ("Zone Min XZ", Vector) = (0,0,0,0)
        _ZoneMaxXZ ("Zone Max XZ", Vector) = (1,1,0,0)
        _ZoneMinXY ("Zone Min XY", Vector) = (0,0,0,0)
        _ZoneMaxXY ("Zone Max XY", Vector) = (1,1,0,0)
        _ZoneMinYZ ("Zone Min YZ", Vector) = (0,0,0,0)
        _ZoneMaxYZ ("Zone Max YZ", Vector) = (1,1,0,0)
        _ProjectionMode ("Projection Mode", Float) = 0
        _BrushSize ("Brush Size", Float) = 0.5
        _Strength ("Strength", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _BrushTex;
            float3 _BrushWorldPos;
            float2 _ZoneMinXZ;
            float2 _ZoneMaxXZ;
            float2 _ZoneMinXY;
            float2 _ZoneMaxXY;
            float2 _ZoneMinYZ;
            float2 _ZoneMaxYZ;
            float _ProjectionMode;
            float _BrushSize;
            float _Strength;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float current = tex2D(_MainTex, input.uv).r;

                float2 zoneMin = _ZoneMinXZ;
                float2 zoneMax = _ZoneMaxXZ;
                float2 brushPos = _BrushWorldPos.xz;

                if (_ProjectionMode > 0.5 && _ProjectionMode < 1.5)
                {
                    zoneMin = _ZoneMinXY;
                    zoneMax = _ZoneMaxXY;
                    brushPos = _BrushWorldPos.xy;
                }
                else if (_ProjectionMode >= 1.5)
                {
                    zoneMin = _ZoneMinYZ;
                    zoneMax = _ZoneMaxYZ;
                    brushPos = _BrushWorldPos.yz;
                }

                float2 worldProjected = lerp(zoneMin, zoneMax, input.uv);
                float distanceToBrush = distance(worldProjected, brushPos);
                float falloff = saturate(1.0 - distanceToBrush / max(_BrushSize, 0.0001));

                float2 brushUv = float2(falloff, 0.5);
                float brushSample = tex2D(_BrushTex, brushUv).r;

                float cleanAmount = brushSample * falloff * _Strength;
                float next = saturate(current - cleanAmount);

                return float4(next, next, next, 1);
            }
            ENDHLSL
        }
    }
}
