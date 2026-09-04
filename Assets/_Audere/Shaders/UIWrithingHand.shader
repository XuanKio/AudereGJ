Shader "Audere/UI/WrithingHand"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _UVRect ("Sprite UV rectangle", Vector) = (0,0,1,1)
        _Bend ("Shaft bend", Range(0,.12)) = .075
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float4 local:TEXCOORD1; };
            sampler2D _MainTex;
            fixed4 _Color;
            float4 _UVRect;
            float _Bend;
            float4 _ClipRect;
            v2f vert(appdata v) { v2f o; o.local=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color*_Color; o.uv=v.uv; return o; }
            float grain(float2 p) { return frac(sin(dot(floor(p),float2(127.1,311.7)))*43758.5453); }
            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=(i.uv-_UVRect.xy)/_UVRect.zw;
                // Palm/fingers stay aligned with the hitbox; only the translucent shaft writhes.
                float mask=sin(saturate(uv.y/.8)*3.14159265)*(1-smoothstep(.68,.82,uv.y));
                uv.x+=sin(uv.y*10-_Time.y*5)*_Bend*mask;
                fixed4 c=tex2D(_MainTex,_UVRect.xy+uv*_UVRect.zw)*i.color;
                // Dissolve the cut base into the field instead of exposing a rectangular wrist edge.
                float baseFade=smoothstep(.18,.42,uv.y);
                float sideFade=smoothstep(0,.035,uv.x)*smoothstep(0,.035,1-uv.x);
                c.a*=baseFade*sideFade*step(uv.y,1)*lerp(.5,1,smoothstep(.5,.83,uv.y));
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a-.001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
