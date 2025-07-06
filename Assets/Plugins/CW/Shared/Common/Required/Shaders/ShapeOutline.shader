Shader "Hidden/CW/ShapeOutline"
{
	Properties
	{
	}
	SubShader
	{
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex   Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv     : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv     : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _CW_ShapeTex;
			float4    _CW_ShapeChannel;
			float4    _CW_ShapeCoords;
			float4    _CW_ShapeColor;

			void Vert (in appdata i, out v2f o)
			{
				o.vertex = UnityObjectToClipPos(i.vertex);
				o.uv     = i.uv;
			}

			fixed4 Frag (v2f i) : SV_Target
			{
				float u     = lerp(_CW_ShapeCoords.x, _CW_ShapeCoords.z, i.uv.x);
				float v     = lerp(_CW_ShapeCoords.y, _CW_ShapeCoords.w, i.uv.y);
				float shape = dot(tex2D(_CW_ShapeTex, float2(u, v)), _CW_ShapeChannel);
				float side  = shape + abs(ddx(shape)) + abs(ddy(shape));

				if (shape >= 0.5f || side < 0.5f)
				{
					discard;
				}

				return _CW_ShapeColor;
			}
			ENDCG
		}
	}
}
