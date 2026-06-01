Shader "Cytoid/Storyboard/Fallback"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ GRAYSCALE_ON SEPIA_ON COLOR_FILTER_ON COLOR_ADJUST_ON NOISE_ON VIGNETTE_HINT_ON

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _GrayFade;
            float _SepiaFade;
            float4 _ColorRgb;
            float _Brightness;
            float _Saturation;
            float _Contrast;
            float _NoiseAmount;
            float _TimeX;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 ApplyContrastSaturationBrightness(float3 c)
            {
                c = (c - 0.5) * _Contrast + 0.5;
                float luma = dot(c, float3(0.299, 0.587, 0.114));
                c = lerp(luma.xxx, c, _Saturation);
                return c * _Brightness;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                #if COLOR_ADJUST_ON
                col.rgb = ApplyContrastSaturationBrightness(col.rgb);
                #endif

                #if COLOR_FILTER_ON
                col.rgb *= _ColorRgb.rgb;
                #endif

                #if GRAYSCALE_ON
                float luma = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(col.rgb, luma.xxx, _GrayFade);
                #endif

                #if SEPIA_ON
                float3 sepia = float3(
                    dot(col.rgb, float3(0.393, 0.769, 0.189)),
                    dot(col.rgb, float3(0.349, 0.686, 0.168)),
                    dot(col.rgb, float3(0.272, 0.534, 0.131)));
                col.rgb = lerp(col.rgb, sepia, _SepiaFade);
                #endif

                #if NOISE_ON
                float n = frac(sin(dot(i.uv * 1200.0 + _TimeX, float2(12.9898, 78.233))) * 43758.5453);
                col.rgb += (n - 0.5) * _NoiseAmount;
                #endif

                #if VIGNETTE_HINT_ON
                float2 d = i.uv - 0.5;
                float vig = saturate(1.0 - dot(d, d) * 2.5);
                col.rgb *= lerp(0.85, 1.0, vig);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
