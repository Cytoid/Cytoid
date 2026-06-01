Shader "Cytoid/Storyboard/FallbackBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Direction ("Direction", Vector) = (1, 0, 0, 0)
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
            float2 _Direction;

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
                float2 offset = _Direction * _MainTex_TexelSize.xy;
                fixed4 c = tex2D(_MainTex, i.uv) * 0.4;
                c += tex2D(_MainTex, i.uv + offset) * 0.15;
                c += tex2D(_MainTex, i.uv - offset) * 0.15;
                c += tex2D(_MainTex, i.uv + offset * 2.0) * 0.15;
                c += tex2D(_MainTex, i.uv - offset * 2.0) * 0.15;
                return c;
            }
            ENDCG
        }
    }
}
