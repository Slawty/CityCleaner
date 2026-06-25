Shader "Hidden/CoverageDirtMask"
{
    Properties
    {
        _ZoneDirtTexXZ ("Zone Dirt XZ", 2D) = "white" {}
        _ZoneDirtTexXY ("Zone Dirt XY", 2D) = "white" {}
        _ZoneDirtTexYZ ("Zone Dirt YZ", 2D) = "white" {}
        _ZoneMinXZ ("Zone Min XZ", Vector) = (0,0,0,0)
        _ZoneMaxXZ ("Zone Max XZ", Vector) = (1,1,0,0)
        _ZoneMinXY ("Zone Min XY", Vector) = (0,0,0,0)
        _ZoneMaxXY ("Zone Max XY", Vector) = (1,1,0,0)
        _ZoneMinYZ ("Zone Min YZ", Vector) = (0,0,0,0)
        _ZoneMaxYZ ("Zone Max YZ", Vector) = (1,1,0,0)
        _UseZoneDirt ("Use Zone Dirt", Float) = 1
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

            sampler2D _ZoneDirtTexXZ;
            sampler2D _ZoneDirtTexXY;
            sampler2D _ZoneDirtTexYZ;
            float2 _ZoneMinXZ;
            float2 _ZoneMaxXZ;
            float2 _ZoneMinXY;
            float2 _ZoneMaxXY;
            float2 _ZoneMinYZ;
            float2 _ZoneMaxYZ;
            float _UseZoneDirt;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv1 : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            float2 RemapZoneUv(float2 worldAxis, float2 zoneMin, float2 zoneMax)
            {
                float2 range = max(zoneMax - zoneMin, float2(0.0001, 0.0001));
                return saturate((worldAxis - zoneMin) / range);
            }

            float SampleZoneDirtTriplanar(float3 worldPos, float3 worldNormal)
            {
                float2 uvXZ = RemapZoneUv(worldPos.xz, _ZoneMinXZ, _ZoneMaxXZ);
                float2 uvXY = RemapZoneUv(worldPos.xy, _ZoneMinXY, _ZoneMaxXY);
                float2 uvYZ = RemapZoneUv(worldPos.yz, _ZoneMinYZ, _ZoneMaxYZ);

                float dirtXZ = tex2D(_ZoneDirtTexXZ, uvXZ).r;
                float dirtXY = tex2D(_ZoneDirtTexXY, uvXY).r;
                float dirtYZ = tex2D(_ZoneDirtTexYZ, uvYZ).r;

                float3 weights = abs(worldNormal);
                float weightSum = max(dot(weights, float3(1.0, 1.0, 1.0)), 0.0001);
                weights /= weightSum;

                return dirtXZ * weights.y + dirtXY * weights.z + dirtYZ * weights.x;
            }

            v2f vert(appdata v)
            {
                v2f o;

                float2 uv = v.uv1;
                uv.y = 1.0 - uv.y;

                o.pos = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float zoneDirt = _UseZoneDirt > 0.5
                    ? SampleZoneDirtTriplanar(i.worldPos, normalize(i.worldNormal))
                    : 1.0;

                return float4(1.0, zoneDirt, 0.0, 1.0);
            }

            ENDHLSL
        }
    }
}
