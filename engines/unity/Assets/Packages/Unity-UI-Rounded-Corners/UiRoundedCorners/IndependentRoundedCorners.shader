Shader "UI/RoundedCorners/IndependentRoundedCorners" {
    
    Properties {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        
        // --- Mask support ---
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        // Definition in Properties section is required to Mask works properly
        _r ("r", Vector) = (0,0,0,0)
        _halfSize ("halfSize", Vector) = (0,0,0,0)
        _rect2props ("rect2props", Vector) = (0,0,0,0)
        _BorderWidth ("BorderWidth", Float) = 0
        // ---
    }
    
    SubShader {
        Tags { 
            "RenderType"="Transparent"
            "Queue"="Transparent" 
        }
        
        // --- Mask support ---
        Stencil {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }    
        Cull Off
        Lighting Off
        ZTest [unity_GUIZTestMode]
        ColorMask [_ColorMask]
        // ---
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass {
            CGPROGRAM
            
            #include "UnityCG.cginc"
            #include "SDFUtils.cginc"
            #include "ShaderSetup.cginc"
            
            #pragma vertex vert
            #pragma fragment frag
            
            float4 _r;
            float4 _halfSize;
            float4 _rect2props;
            float _BorderWidth;
            sampler2D _MainTex;
            
            fixed4 frag (v2f i) : SV_Target {
                float2 sp = (i.uv - .5) * _halfSize.xy * 2;
                float r1 = rectangle(sp, _halfSize.xy);
                float2 r2p = rotate(translate(sp, _rect2props.xy), .125);
                float r2 = rectangle(r2p, _rect2props.zw);
                float2 c0p = translate(sp, float2(-_halfSize.x + _r.x, _halfSize.y - _r.x));
                float c0 = circle(c0p, _r.x);
                float2 c1p = translate(sp, float2(_halfSize.x - _r.y, _halfSize.y - _r.y));
                float c1 = circle(c1p, _r.y);
                float2 c2p = translate(sp, float2(_halfSize.x - _r.z, -_halfSize.y + _r.z));
                float c2 = circle(c2p, _r.z);
                float2 c3p = translate(sp, -_halfSize.xy + _r.w);
                float c3 = circle(c3p, _r.w);
                float dist = max(r1, min(min(min(min(r2, c0), c1), c2), c3));
                float outerAlpha = AntialiasedCutoff(dist);
                float innerAlpha = (_BorderWidth > 0) ? AntialiasedCutoff(dist + _BorderWidth) : 0;
                float alpha = outerAlpha - innerAlpha;
                return mixAlpha(tex2D(_MainTex, i.uv), i.color, alpha);
            }
            
            ENDCG
        }
    }
}
