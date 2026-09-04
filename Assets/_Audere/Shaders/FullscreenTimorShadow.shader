Shader "Hidden/Audere/TimorShadowTransition"
{
 Properties
 {
  _ShadowTex("Timor silhouette",2D)="white"{}
  _ShadowUVRect("Sprite rect",Vector)=(0,0,1,1)
  _ShadowExtent("Extent",Float)=0.15
  _ShadowOpacity("Opacity",Float)=0
  _Cover("Opaque handoff",Float)=0
  _EffectTime("Time",Float)=0
  _AspectRatio("Aspect",Float)=1.77778
 }
 SubShader
 {
  Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
  ZWrite Off ZTest Always Cull Off
  Pass
  {
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
   TEXTURE2D(_ShadowTex); SAMPLER(sampler_ShadowTex);
   float4 _ShadowUVRect;
   float _ShadowExtent,_ShadowOpacity,_Cover,_EffectTime,_AspectRatio;
   half4 Frag(Varyings input):SV_Target
   {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv=input.texcoord.xy;
    half3 scene=SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,sampler_LinearClamp,uv,_BlitMipLevel).rgb;
    // A recognizable silhouette grows from the right corner, then floods the room.
    float2 p=uv-float2(.78,.48);
    p.x*=max(.1,_AspectRatio);
    p/=max(.03,_ShadowExtent);
    p.x+=sin(p.y*9+_EffectTime*1.5)*.018;
    float2 maskUV=p+float2(.5,.42);
    float inside=step(0,maskUV.x)*step(maskUV.x,1)*step(0,maskUV.y)*step(maskUV.y,1);
    half4 silhouette=SAMPLE_TEXTURE2D(_ShadowTex,sampler_ShadowTex,_ShadowUVRect.xy+saturate(maskUV)*_ShadowUVRect.zw);
    float ink=silhouette.a*inside*_ShadowOpacity;
    half3 shadow=half3(.0008,.0005,.002)+silhouette.rgb*.055;
    half3 result=lerp(scene,shadow,ink);
    // Cover is held opaque on both sides of the mode swap; no naked scene cut.
    result=lerp(result,half3(.0004,.0003,.0012),saturate(_Cover));
    return half4(result,1);
   }
   ENDHLSL
  }
 }
}

