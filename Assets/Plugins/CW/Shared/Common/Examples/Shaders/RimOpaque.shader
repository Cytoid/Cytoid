Shader "CW/RimOpaque"
{
	Properties
	{
		_MainTex("Main Tex", 2D) = "white" {}
		_Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_Color1("Color 1", Color) = (1.0, 0.5, 0.5, 1.0)
		_Color2("Color 2", Color) = (0.5, 0.5, 1.0, 1.0)
		_Rim("Rim", Float) = 1.0
		_Shift("Shift", Float) = 1.0
	}

	SubShader
	{
		Cull Off

		Tags
		{
			"Queue" = "Geometry"
			"DisableBatching" = "True"
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4    _MainTex_ST;

			float4 _Color;
			float4 _Color1;
			float4 _Color2;
			float  _Rim;
			float  _Shift;

			struct a2v
			{
				float4 vertex    : POSITION;
				float4 normal    : NORMAL;
				float4 color     : COLOR;
				float2 texcoord0 : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex    : SV_POSITION;
				float2 uv        : TEXCOORD0;
				float3 normal    : TEXCOORD1;
				float3 direction : TEXCOORD2;
				float4 screenPos : TEXCOORD3;
				float4 color     : COLOR;
			};

			struct f2g
			{
				float4 color : SV_TARGET;
			};

			float Dither(float4 ScreenPosition)
			{
				float2 uv = ScreenPosition.xy * _ScreenParams.xy;
				float DITHER_THRESHOLDS[16] =
				{
					1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
					13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
					4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
					16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
				};
				uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
				return DITHER_THRESHOLDS[index];
			}

			void Vert(a2v i, out v2f o)
			{
				o.vertex    = UnityObjectToClipPos(i.vertex);
				o.uv        = TRANSFORM_TEX(i.texcoord0, _MainTex);
				o.normal    = mul((float3x3)unity_ObjectToWorld, i.normal);
				o.direction = _WorldSpaceCameraPos - mul(unity_ObjectToWorld, i.vertex).xyz;
				o.screenPos = ComputeScreenPos(o.vertex);
				o.color     = i.color * _Color;
			}

			void Frag(v2f i, out f2g o)
			{
				float ang = abs(dot(normalize(i.direction), normalize(i.normal)));
				float rim = _Shift - pow(saturate(1.0f - ang), _Rim);

				o.color = tex2D(_MainTex, i.uv) * i.color * lerp(_Color1, _Color2, rim);

				clip(o.color.a - Dither(i.screenPos / i.screenPos.w));
			}
			ENDCG
		} // Pass
	} // SubShader
} // Shader