Shader "Lean/Common/Alpha"
{
	Properties
	{
		_MainTex("Main Tex", 2D) = "white" {}
		_Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"PreviewType" = "Sphere"
			"DisableBatching" = "True"
		}

		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4    _MainTex_ST;
			float4    _Color;

			struct a2v
			{
				float4 vertex    : POSITION;
				float2 texcoord0 : TEXCOORD0;
				float4 color     : COLOR;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv     : TEXCOORD0;
				float4 color  : COLOR;
			};

			void Vert(a2v i, out v2f o)
			{
				o.vertex = UnityObjectToClipPos(i.vertex);
				o.uv     = TRANSFORM_TEX(i.texcoord0, _MainTex);
				o.color  = i.color * _Color;
			}

			void Frag(v2f i, out float4 o:COLOR0)
			{
				o = tex2D(_MainTex, i.uv) * i.color;
			}
			ENDCG
		} // Pass
	} // SubShader
} // Shader