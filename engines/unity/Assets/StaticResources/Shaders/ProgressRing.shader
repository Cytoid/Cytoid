Shader "Game/ProgressRing"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		_MaxCutoff ("MaxCutoff", Float) = 0
		_FillColor ("FillColor", Color) = (1,1,1,1)
		_FillCutoff ("FillCutOff", Float) = 0
	}
	SubShader
	{
		Tags { "CanUseSpriteAtlas" = "true" "IGNOREPROJECTOR" = "true" "PreviewType" = "Plane" "QUEUE" = "Transparent" "RenderType" = "Transparent" }		
		Pass
		{
			Tags { "CanUseSpriteAtlas" = "true" "IGNOREPROJECTOR" = "true" "PreviewType" = "Plane" "QUEUE" = "Transparent" "RenderType" = "Transparent" }
			Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha
			Cull Off ZWrite Off
			Fog {
				Mode Off
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile DUMMY PIXELSNAP_ON
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION0;
				float4 color : COLOR0;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : POSITION0;
				float2 uv : TEXCOORD0;
				float4 uv2 : TEXCOORD1;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.uv2 = v.color;
				#ifdef PIXELSNAP_ON
				o.vertex = UnityPixelSnap(o.vertex);
				#endif
				return o;
			}
			
			sampler2D _MainTex;
			float _MaxCutoff;
			fixed4 _FillColor;
			float _FillCutoff;

			fixed4 frag (v2f i) : SV_Target
			{
				float2 u_xlat0;
				fixed4 u_xlat16_0;
				bool2 u_xlatb0;
				float4 u_xlat1;
				float4 u_xlat10_1;
				float4 u_xlat16_2;
				float u_xlat3;
				float u_xlat6;
				bool u_xlatb6;
				float u_xlat9;
			
				u_xlat0.xy = i.uv.xy - 0.5;
				u_xlat0.x = dot(u_xlat0.xy, u_xlat0.xy);
				u_xlat0.x = sqrt(u_xlat0.x);
				u_xlat0.x = u_xlat0.y / u_xlat0.x;

				u_xlat3 = abs(u_xlat0.x) * -0.0187292993 + 0.0742610022;
				u_xlat3 = u_xlat3 * abs(u_xlat0.x) + -0.212114394;
				u_xlat3 = u_xlat3 * abs(u_xlat0.x) + 1.57072878;
				u_xlat6 = -abs(u_xlat0.x) + 1.0;

				u_xlatb0.x = u_xlat0.x<(-u_xlat0.x);
				u_xlat6 = sqrt(u_xlat6);
				u_xlat9 = u_xlat6 * u_xlat3;
				u_xlat9 = u_xlat9 * -2.0 + 3.14159274;
				u_xlat0.x = u_xlatb0.x ? u_xlat9 : float(0.0);
				u_xlat0.x = u_xlat3 * u_xlat6 + u_xlat0.x;
				u_xlat3 = (-u_xlat0.x) * 2.0 + 6.28318501;

				u_xlatb6 = i.uv.x>=0.5;

				u_xlat6 = (u_xlatb6) ? 0.0 : 1.0;
				u_xlat0.x = u_xlat3 * u_xlat6 + u_xlat0.x;
				u_xlat0.x = u_xlat0.x * 0.159154952;
				u_xlatb0.xy = (float4(_MaxCutoff, _FillCutoff, _MaxCutoff, _MaxCutoff)<u_xlat0.xxxx).xy;
				u_xlat10_1 = tex2D(_MainTex, i.uv.xy);
				u_xlat1 = u_xlat10_1 * i.uv2;
				u_xlat16_2 = (u_xlatb0.y) ? u_xlat1 : _FillColor;
				u_xlat16_0 = (u_xlatb0.x) ? float4(0.0, 0.0, 0.0, 0.0) : u_xlat16_2;
				u_xlat16_0 = u_xlat16_0 * u_xlat1;
				
				fixed4 col;
				col.xyz = u_xlat16_0.www * u_xlat16_0.xyz;
				col.w = u_xlat16_0.w;

				return col;
			}
			ENDCG
		}
	}
}
