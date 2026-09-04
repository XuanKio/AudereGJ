Shader "Audere/UI/DriftingSpriteField"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mouth texture", 2D) = "white" {}
        _Color ("Tint", Color) = (.42,.32,.48,1)
        _UVRect ("Sprite UV rectangle", Vector) = (0,0,1,1)
        _Opacity ("Opacity", Range(0,1)) = .55
        _MotionTime ("Owned time", Float) = 0
        _Aspect ("Aspect", Float) = 1.7778
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
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
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float4 local:TEXCOORD1; };
            sampler2D _MainTex;
            float4 _Color, _UVRect, _ClipRect;
            float _MotionTime, _Aspect, _Opacity;
            v2f vert(appdata v) { v2f o; o.local=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color*_Color; o.uv=v.uv; return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float layer(float2 p, float density, float seed, float speed)
            {
                float time=_MotionTime*speed;
                p=p*float2(_Aspect,1)*density+float2(time*.08,-time*.13)+seed;
                float2 cell=floor(p), q=frac(p)-.5;
                float h=hash(cell+seed);
                q-=float2(sin(time*.7+h*20),cos(time*.55+h*15))*.09;
                float angle=(h-.5)*.7+sin(time*.42+h*19)*.13;
                q=mul(float2x2(cos(angle),-sin(angle),sin(angle),cos(angle)),q);
                q*=float2(1.55+sin(time*.8+h*12)*.3,2.4+cos(time*.65+h*15)*.55);
                q.x+=sin(q.y*7+time*1.3+h*20)*.075;
                q.y+=sin(q.x*8-time*.8+h*12)*.06;
                float2 uv=q+.5;
                float inside=step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1);
                fixed4 sample=tex2D(_MainTex,_UVRect.xy+saturate(uv)*_UVRect.zw);
                float edge=smoothstep(0,.065,uv.x)*smoothstep(0,.065,1-uv.x)*smoothstep(0,.07,uv.y)*smoothstep(0,.07,1-uv.y);
                return sample.a*max(sample.r,max(sample.g,sample.b))*inside*edge*(.45+h*.55);
            }
            fixed4 frag(v2f i):SV_Target
            {
                float distant=layer(i.uv,5.2,13.1,.45)*.48;
                float near=layer(i.uv,3.3,47.7,1);
                // Dim the play area center while retaining mouths across the full background.
                float quietCenter=lerp(.38,1,smoothstep(.12,.52,length((i.uv-.5)*float2(_Aspect,.9))));
                fixed4 c=i.color;
                c.a*=saturate(distant+near)*_Opacity*quietCenter;
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
