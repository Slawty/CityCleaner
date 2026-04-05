Shader "Hidden/BrushStrength"
{
    Properties
    {
        _MainTex ("Brush", 2D) = "white" {}
        _Strength ("Strength", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            Blend DstColor Zero

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Strength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;

                o.pos = UnityObjectToClipPos(v.vertex);

                o.uv = v.uv;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float brush =
                    tex2D(_MainTex, i.uv).r;

                float factor =
                    1.0 - brush * _Strength;

                return float4(
                    factor,
                    factor,
                    factor,
                    1);
            }

            ENDHLSL
        }
    }
}
