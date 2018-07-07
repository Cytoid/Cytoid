Shader "Cytus/SpriteDefault"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		_LightColor ("LightColor", Float) = 0
		_Start ("Head Ratio", Range(0, 1)) = 0
		_End ("Tail Ratio", Range(0, 1)) = 0
	}
	SubShader
	{
		Tags { "IGNOREPROJECTOR" = "true" "QUEUE" = "Transparent" "RenderType" = "Transparent" }
		// No culling or depth
		Cull Off ZWrite Off ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
		Pass
		{
			Tags { "IGNOREPROJECTOR" = "true" "QUEUE" = "Transparent" "RenderType" = "Transparent" }
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 v : POSITION;
				float2 t : TEXCOORD0;
				float4 c: COLOR0;
			};

			struct v2f
			{
				float4 v : POSITION0;
				float2 t0 : TEXCOORD0;
				float2 t1 : TEXCOORD1;
				float4 c : COLOR0;
			};


            float4 _MainTex_ST;
			float4 _Color;

			v2f vert (appdata v)
			{
				v2f o;
				o.v = UnityObjectToClipPos(v.v);
				o.t0 = TRANSFORM_TEX(v.t, _MainTex);
				o.t1 = v.t;
				o.c = v.c * _Color;
				return o;
			}
			
			float _LightColor;
			float _Start;
			float _End;
			sampler2D _MainTex;
			float u_xlat0;
			float u_xlat1;
			float4 u_xlat16_0;
			float4 u_xlat10_0;
			float u_xlatb0;
			float u_xlatb1;
			float u_xlat16_1;
			fixed4 result;
			fixed4 frag (v2f i) : SV_Target
			{
				u_xlat10_0 = tex2D(_MainTex, i.t0.xy);
				u_xlat16_0 = u_xlat10_0 * i.c;
				u_xlat16_1 = dot(u_xlat16_0.xyz, float3(0.21, 0.72, 0.070));
				u_xlat16_1 = log2(u_xlat16_1);
			    u_xlat16_1 = u_xlat16_1 * 1.5;
				u_xlat16_1 = exp2(u_xlat16_1);
				u_xlat16_1 = u_xlat16_0.w * u_xlat16_1 + (-u_xlat16_0.w);
				result.w = _LightColor * u_xlat16_1 + u_xlat16_0.w;
				result.xyz = u_xlat16_0.xyz;
				return result;
			}
			ENDCG
		}
	}
}
