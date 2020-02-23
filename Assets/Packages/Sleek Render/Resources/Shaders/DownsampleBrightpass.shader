Shader "Sleek Render/Post Process/Downsample Brightpass"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_LuminanceConst("Luminance", Vector) = (1.0, 1.0, 1.0, 1.0)
		_TexelSize("_TexelSize", Vector) = (1.0, 1.0, 1.0, 1.0)
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct appdata
			{
				half4 vertex : POSITION;
				half4 uv : TEXCOORD0;
			};

			struct v2f
			{
				half4 vertex : SV_POSITION;
				half2 uv_0 : TEXCOORD0;
				half2 uv_1 : TEXCOORD1;
				half2 uv_2 : TEXCOORD2;
				half2 uv_3 : TEXCOORD3;
				half2 uv_4 : TEXCOORD4;
			};

			sampler2D_half _MainTex;
			float4 _TexelSize;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = v.vertex;
				half2 halfTexelSize = _TexelSize.xy * 0.5h;

				o.uv_0 = v.uv;
				o.uv_1 = v.uv - halfTexelSize;
				o.uv_2 = v.uv + halfTexelSize * half2(1.0h, -1.0h);
				o.uv_3 = v.uv + halfTexelSize * half2(-1.0h, 1.0h);
				o.uv_4 = v.uv + halfTexelSize;

				if (_ProjectionParams.x < 0)
				{
					o.uv_0.y = 1-o.uv_0.y;
					o.uv_1.y = 1-o.uv_1.y;
					o.uv_2.y = 1-o.uv_2.y;
					o.uv_3.y = 1-o.uv_3.y;
					o.uv_4.y = 1-o.uv_4.y;
				}

				return o;
			}
			
			half4 _LuminanceConst;

			half4 frag (v2f i) : SV_Target
			{
				half4 col_0 = tex2D(_MainTex, i.uv_0);
				half luma_0 = saturate(dot(half4(col_0.rgb, 1.0h), _LuminanceConst));

				half3 sum = col_0.rgb * 4.0h;
				half luma = luma_0 * 4.0h;

				half4 col_1 = tex2D(_MainTex, i.uv_1);
				half luma_1 = saturate(dot(half4(col_1.rgb, 1.0h), _LuminanceConst));

				sum += col_1.rgb;
				luma += luma_1;

				half4 col_2 = tex2D(_MainTex, i.uv_2);
				half luma_2 = saturate(dot(half4(col_2.rgb, 1.0h), _LuminanceConst));

				sum += col_2.rgb;
				luma += luma_2;

				half4 col_3 = tex2D(_MainTex, i.uv_3);
				half luma_3 = saturate(dot(half4(col_3.rgb, 1.0h), _LuminanceConst));

				sum += col_3.rgb;
				luma += luma_3;

				half4 col_4 = tex2D(_MainTex, i.uv_4);
				half luma_4 = saturate(dot(half4(col_4.rgb, 1.0h), _LuminanceConst));

				sum += col_4.rgb;
				luma += luma_4;

				sum = sum * 0.125h;

				half4 col = half4(sum, luma * 0.125h);

				return col;
			}
			ENDCG
		}
	}
}
