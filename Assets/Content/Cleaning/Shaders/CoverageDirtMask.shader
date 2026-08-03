Shader "Hidden/CoverageDirtMask"
{
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 dirtUv : TEXCOORD3;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;

                float2 uv = v.dirtUv;
                uv.y = 1.0 - uv.y;

                o.pos = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return float4(1.0, 0.0, 0.0, 1.0);
            }

            ENDHLSL
        }
    }
}
