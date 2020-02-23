// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/ThreeChannelUnlit"
{
    Properties
    {
        _MainTex ("Island Texture", 2D) = "white" {}
        _FlickerNoise ("Flicker Noise Texture", 2D) = "white" {}
        _BonfireColor("Bonfire Light Color", Color) = (1, 1, 1, 1)
        _TentColor ("Tent Color", Color) = (1, 1, 1, 1)
        _FillLightColor ("Fill Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"

            struct appdata
            {
                half4 vertex : POSITION;
                half2 uv : TEXCOORD0;
            };

            struct v2f
            {
                half2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                half2 noise_uv : TEXCOORD2;
                half4 vertex : SV_POSITION;
            };

            sampler2D _MainTex, _FlickerNoise;
            half4 _MainTex_ST, _FlickerNoise_ST, _TentColor, _BonfireColor, _FillLightColor;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                half sinTime = sin(_Time) * 0.5h + 0.5h;
                o.noise_uv = half2(sinTime, sinTime);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            
            half4 frag (v2f i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.uv);
                half4 noise = tex2D(_FlickerNoise, i.noise_uv);

                half4 bonfireColor = col.r * _BonfireColor * _BonfireColor.a * saturate(noise.g);
                half4 tentColor = col.g * _TentColor * _TentColor.a * saturate(noise.x);
                half4 fillColor = col.b * _FillLightColor * _FillLightColor.a;

                return tentColor + bonfireColor + fillColor;
            }
            ENDCG
        }
    }
}
