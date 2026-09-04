Shader "Hidden/Audere/DreamFractureTransition"
{
 Properties {
  [HideInInspector] _EffectTime ("Time", Float) = 0
  [HideInInspector] _AspectRatio ("Aspect", Float) = 1.77778
  [HideInInspector] _Shake ("Shake", Float) = 0
  [HideInInspector] _Cover ("Cover", Float) = 0
 }
 SubShader {
  Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
  ZWrite Off ZTest Always Cull Off
  Pass {
   Name "DreamFracture"
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
   float _EffectTime, _AspectRatio, _Shake, _Cover;
   half4 Frag(Varyings input) : SV_Target {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 shake=float2(sin(_EffectTime*43)+.45*sin(_EffectTime*71),cos(_EffectTime*53))*_Shake;
    half3 color=SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,sampler_LinearClamp,saturate(input.texcoord+shake),_BlitMipLevel).rgb;
    return half4(lerp(color,half3(.017,.015,.027),saturate(_Cover)),1);
   }
   ENDHLSL
  }
 }
 Fallback Off
}
