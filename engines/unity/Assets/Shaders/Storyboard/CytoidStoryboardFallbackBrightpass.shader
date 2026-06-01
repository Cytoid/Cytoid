Shader "Cytoid/Storyboard/FallbackBrightpass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Threshold ("Threshold", Float) = 0.7
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Threshold;

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

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float luma = dot(c.rgb, float3(0.299, 0.587, 0.114));
                float bright = max(0, luma - _Threshold) / max(1e-4, 1.0 - _Threshold);
                return fixed4(c.rgb * bright, bright);
            }
            ENDCG
        }
    }
}
