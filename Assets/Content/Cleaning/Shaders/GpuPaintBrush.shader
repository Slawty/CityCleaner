Shader "Hidden/GPUPaintBrush"
{
    Properties
    {
        _MainTex ("Mask", 2D) = "white" {}
        _BrushTex ("Brush", 2D) = "white" {}
        _BrushWorldPos ("BrushPos", Vector) = (0,0,0,0)
        _BrushWorldNormal ("BrushNormal", Vector) = (0,1,0,0)
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
            float3 _BrushWorldNormal;
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

            float2 BrushWorldUv(float3 worldPos)
            {
                float3 normal = normalize(_BrushWorldNormal);
                float3 upReference = abs(normal.y) < 0.999 ? float3(0, 1, 0) : float3(1, 0, 0);
                float3 tangent = normalize(cross(upReference, normal));
                float3 bitangent = cross(normal, tangent);
                float3 delta = worldPos - _BrushWorldPos;
                float brushDiameter = max(_BrushSize * 2.0, 0.0001);
                return float2(dot(delta, tangent), dot(delta, bitangent)) / brushDiameter + 0.5;
            }

            float SampleBrush2D(float2 brushUv)
            {
                if (brushUv.x < 0.0 || brushUv.x > 1.0 || brushUv.y < 0.0 || brushUv.y > 1.0)
                    return 0.0;

                return tex2D(_BrushTex, brushUv).r;
            }

            v2f vert(appdata v)
            {
                v2f o;

                float2 uv = v.uv1;
                uv.y = 1.0 - uv.y;

                o.pos = float4(uv * 2 - 1, 0, 1);
                o.uv = v.uv1;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float current = tex2D(_MainTex, i.uv).r;

                float dist = distance(i.worldPos, _BrushWorldPos);
                float sphereFalloff = saturate(1.0 - dist / _BrushSize);
                float splat = SampleBrush2D(BrushWorldUv(i.worldPos));
                float clean = splat * sphereFalloff * _Strength;
                float result = current * (1 - clean);

                return float4(result, result, result, 1);
            }

            ENDHLSL
        }
    }
}
