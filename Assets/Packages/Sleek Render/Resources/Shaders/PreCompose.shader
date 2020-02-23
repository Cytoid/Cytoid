Shader "Sleek Render/Post Process/PreCompose"
{
	Properties
	{
		_MainTex("Main Texture", 2D) = "black" {}
		_BloomTex("Bloom", 2D) = "black" {}
		_BloomIntencity("Bloom Intensity", float) = 0.672
		_VignetteShape("Vignette Form", vector) = (1.0, 1.0, 1.0, 1.0)
		_VignetteColor("Vignette Color", color) = (0.0, 0.0, 0.0, 1.0)
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ BLOOM_ON
			#pragma multi_compile _ VIGNETTE_ON

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

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = v.vertex;
				o.uv = v.uv;

				if (_ProjectionParams.x < 0)
				{
					o.uv.y = 1 - o.uv.y;
				}

				return o;
			}
			
			sampler2D_half _BloomTex, _MainTex;
			half4 _VignetteShape, _VignetteColor, _BloomTint;
			half _BloomIntencity;

			half4 frag (v2f i) : SV_Target
			{
				half3 mainColor = tex2D(_MainTex, i.uv).rgb;

				#ifdef VIGNETTE_ON
				half2 vignetteCenter = i.uv - half2(0.5h, 0.5h);
				half vignetteShape = saturate(dot(vignetteCenter, vignetteCenter) * _VignetteShape.x + _VignetteShape.y);
				half4 vignette = half4(_VignetteColor.rgb * vignetteShape, 1.0h - _VignetteColor.a * vignetteShape);

				half vignetteAlpha = vignette.a;
				half3 vignetteRGB = vignette.rgb;
				half3 alphaMultiplier = half3(vignette.a, vignette.a, vignette.a);
				#else
				half vignetteAlpha = 1.0h;
				half3 vignetteRGB = half3(0.0h, 0.0h, 0.0h);
				half3 alphaMultiplier = half3(1.0h, 1.0h, 1.0h);
				#endif

				#ifdef BLOOM_ON
				half4 rawBloom = tex2D(_BloomTex, i.uv);
				half rawBloomIntencity = dot(rawBloom.rgb, half3(0.2126h, 0.7152h, 0.0722h));
				half3 bloom = rawBloom * _BloomIntencity * _BloomTint;
				mainColor = mainColor + bloom;
				alphaMultiplier *= bloom;
				vignetteAlpha *= 1.0h - rawBloomIntencity;
				#else
				alphaMultiplier *= 0.0h;
				#endif

				half4 result = half4(alphaMultiplier + vignetteRGB, vignetteAlpha);
				return result;
			}
			ENDCG
		}
	}
}
