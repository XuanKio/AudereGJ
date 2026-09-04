Shader "Audere/UI/ScreenShatter"
{
    Properties { [PerRendererData] _MainTex ("Frozen frame", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex : POSITION; float4 color : COLOR; float4 uv : TEXCOORD0; };
            struct Varyings { float4 vertex : SV_POSITION; float4 color : COLOR; float3 uv : TEXCOORD0; };
            sampler2D _MainTex;
            Varyings Vert(Input input)
            {
                Varyings output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv.xyz;
                output.color = input.color;
                return output;
            }
            fixed4 Frag(Varyings input) : SV_Target
            {
                fixed4 face = tex2D(_MainTex, input.uv.xy);
                // ScreenCapture stores display-encoded pixels in a non-sRGB texture.
                // Decode before the linear project's final display conversion.
                #ifndef UNITY_COLORSPACE_GAMMA
                face.rgb = GammaToLinearSpace(face.rgb);
                #endif
                return lerp(face, fixed4(1,1,1,1), step(.5, input.uv.z)) * input.color;
            }
            ENDCG
        }
    }
}
