Shader "Hidden/GPUPaintBrush"
{
    Properties
    {
        _MainTex ("Mask", 2D) = "white" {}
        _BrushTex ("Brush", 2D) = "white" {}
        _BrushWorldPos ("BrushPos", Vector) = (0,0,0,0)
        _BrushSize ("Size", Float) = 0.5
        _Strength ("Strength", Float) = 1
    }

    SubShader
    {
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _BrushTex;

            float3 _BrushWorldPos;
            float _BrushSize;
            float _Strength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv1 : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;

                // Project mesh into UV1 space
                float2 uv = v.uv1;
                uv.y = 1.0 - uv.y; // match Unity RenderTexture orientation

                o.pos = float4(uv * 2 - 1, 0, 1);

                o.uv = v.uv1;

                o.worldPos =
                    mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float current =
                    tex2D(_MainTex, i.uv).r;

                float dist =
                    distance(i.worldPos, _BrushWorldPos);

                float falloff =
                    saturate(1 - dist / _BrushSize);

                float brush =
                    tex2D(_BrushTex, float2(falloff, 0.5)).r;

                float clean =
                    brush * falloff * _Strength;

                float result =
                    current * (1 - clean);

                return float4(result, result, result, 1);
            }

            ENDHLSL
        }
    }
}
