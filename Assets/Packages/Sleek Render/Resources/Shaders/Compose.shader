Shader "Sleek Render/Post Process/Compose"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_PreComposeTex("PreCompose Texture", 2D) = "black" {}
		_Colorize("Colorize", color) = (1.0, 1.0, 1.0, 0.0)
		_ContrastBrightness("Contrast And Brightness", vector) = (1.0, 0.5, 0, 0)
		_LuminanceConst("Luminance Const", vector) = (0.2126, 0.7152, 0.0722, 0.0)
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ COLORIZE_ON
			#pragma multi_compile _ BRIGHTNESS_CONTRAST_ON
			
			struct appdata
			{
				half4 vertex : POSITION;
				half2 uv : TEXCOORD0;
			};

			struct v2f
			{
				half4 vertex : SV_POSITION;
				half2 uv : TEXCOORD0;
			};

			half4 _Colorize, _LuminanceConst, _MainTex_TexelSize;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = v.vertex;
				o.uv = v.uv;

				if (_ProjectionParams.x < 0)
				{
					o.uv.y = 1 - v.uv.y;
				}

				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
				{
					o.uv.y = 1 - v.uv.y;
				}
				#endif

				return o;
			}
			
			sampler2D_half _MainTex, _PreComposeTex;
			half3 _BrightnessContrast;

			half4 frag (v2f i) : SV_Target
			{
				half4 precompose = tex2D(_PreComposeTex, i.uv);
				half4 col = tex2D(_MainTex, i.uv);
				half3 mainColor = col.rgb * precompose.a + precompose.rgb;

				#ifdef COLORIZE_ON
				half3 result = mainColor * _Colorize.a + _Colorize.rgb * dot(_LuminanceConst, mainColor);
				#else 
				half3 result = mainColor;
				#endif

				#ifdef BRIGHTNESS_CONTRAST_ON
				result = saturate((result * _BrightnessContrast.x) + _BrightnessContrast.z);
				#endif

				return half4(result, 1.0h);
			}
			ENDCG
		}
	}
}
