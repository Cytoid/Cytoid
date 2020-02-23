
Shader "DragonBones/BlendModes/Grab"
{
	Properties
	{
		[PerRendererData] 
		_MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
	}

	SubShader
	{
		Tags
		{ 
			"Queue" = "Transparent" 
			"IgnoreProjector" = "True" 
			"RenderType" = "Transparent" 
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Fog { Mode Off }
		Blend SrcAlpha OneMinusSrcAlpha
		
		GrabPass { }
		
		Pass
		{
			CGPROGRAM
			
			#include "UnityCG.cginc"

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			
			sampler2D _MainTex;
			sampler2D _GrabTexture;
			fixed4 _Color;
			
			struct input
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct output
			{
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				half2 texcoord : TEXCOORD0;
				float4 screenPos : TEXCOORD1;
			};
			
            output vert(input vi)
			{
                output vo;

                vo.vertex = UnityObjectToClipPos(vi.vertex);
                vo.screenPos = vo.vertex;
                vo.texcoord = vi.texcoord;
                vo.color = vi.color * _Color;
							
				return vo;
			}
			
			fixed4 frag(output vo) : SV_Target
			{
				// compute the texture coordinates
				float2 screenPos = vo.screenPos.xy / vo.screenPos.w; // screenpos ranges from -1 to 1
				screenPos.x = (screenPos.x + 1.0) * 0.5; // I need 0 to 1
				screenPos.y = (screenPos.y + 1.0) * 0.5; // I need 0 to 1

				//解决平台差异 D3D原点在顶部，openGL在底部
				#if UNITY_UV_STARTS_AT_TOP
				screenPos.y = 1.0 - screenPos.y;
				#endif
				               
				fixed4 color = tex2D(_MainTex, vo.texcoord) * vo.color;
                //抓取的当前屏幕颜色
				fixed4 grabColor = tex2D(_GrabTexture, screenPos); 
				
                //Add Mode TODO others blendMode
                fixed4 result = grabColor + color;
                result.a = color.a;
                return result;
			}
			
			ENDCG
		}
	}
	
	Fallback "Sprites/Default"
}
