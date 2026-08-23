Shader "Hidden/Audere/DreamyDisorientationTransition"
{
    Properties
    {
        [HideInInspector] _EffectTime ("Effect Time", Float) = 0
        [HideInInspector] _FocusCenter ("Focus Center", Vector) = (0.5, 0.5, 0, 0)
        [HideInInspector] _AspectRatio ("Aspect Ratio", Float) = 1.7777778
        [HideInInspector] _RotationDegrees ("Rotation Degrees", Float) = 0
        [HideInInspector] _Zoom ("Zoom", Float) = 1
        [HideInInspector] _WaveStrength ("Wave Strength", Float) = 0
        [HideInInspector] _DriftX ("Drift X", Float) = 0
        [HideInInspector] _DriftY ("Drift Y", Float) = 0
        [HideInInspector] _RadialStrength ("Radial Strength", Float) = 0
        [HideInInspector] _SmearStrength ("Smear Strength", Float) = 0
        [HideInInspector] _VeilStrength ("Veil Strength", Float) = 0
        [HideInInspector] _ChromaticOffset ("Chromatic Offset", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "DreamyDisorientationTransition"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _EffectTime;
            float2 _FocusCenter;
            float _AspectRatio;
            float _RotationDegrees;
            float _Zoom;
            float _WaveStrength;
            float _DriftX;
            float _DriftY;
            float _RadialStrength;
            float _SmearStrength;
            float _VeilStrength;
            float _ChromaticOffset;

            float InBounds(float2 uv)
            {
                float2 lower = step(0.0, uv);
                float2 upper = step(uv, 1.0);
                return lower.x * lower.y * upper.x * upper.y;
            }

            half4 SampleTransition(float2 uv)
            {
                half4 sample = SAMPLE_TEXTURE2D_X_LOD(
                    _BlitTexture,
                    sampler_LinearClamp,
                    saturate(uv),
                    _BlitMipLevel);
                return sample * InBounds(uv);
            }

            float2 Rotate(float2 value, float radians)
            {
                float sine;
                float cosine;
                sincos(radians, sine, cosine);
                return float2(
                    value.x * cosine - value.y * sine,
                    value.x * sine + value.y * cosine);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 outputUv = input.texcoord.xy;
                float aspect = max(_AspectRatio, 0.0001);
                float2 centered = outputUv - _FocusCenter;
                centered.x *= aspect;

                // The whole frame gently loses balance around the actor rather than
                // collapsing into a vortex.
                centered = Rotate(centered, -_RotationDegrees * 0.0174532925);
                centered /= max(_Zoom, 0.001);

                float radiusSquared = dot(centered, centered);
                centered *= 1.0 + _RadialStrength * radiusSquared;
                centered.x /= aspect;

                float2 sourceUv = _FocusCenter + centered;
                float slowTime = _EffectTime * 1.7;
                float horizontalWave = sin(sourceUv.y * 8.0 + slowTime) * _WaveStrength;
                float verticalWave = cos(sourceUv.x * 6.0 - slowTime * 0.73) * _WaveStrength * 0.55;
                sourceUv += float2(horizontalWave + _DriftX, verticalWave + _DriftY);

                // Directional multi-sampling creates a soft drifting smear, not static noise.
                float2 smearDirection = normalize(float2(1.0, 0.38));
                float2 smear = smearDirection * _SmearStrength;
                half3 color = SampleTransition(sourceUv).rgb * 0.42;
                color += SampleTransition(sourceUv - smear).rgb * 0.18;
                color += SampleTransition(sourceUv - smear * 0.55).rgb * 0.22;
                color += SampleTransition(sourceUv + smear * 0.35).rgb * 0.18;

                // A very small chromatic drift supports disorientation without reading as VHS.
                half red = SampleTransition(sourceUv + float2(_ChromaticOffset, 0.0)).r;
                half blue = SampleTransition(sourceUv - float2(_ChromaticOffset, 0.0)).b;
                color.r = lerp(color.r, red, saturate(_ChromaticOffset * 180.0));
                color.b = lerp(color.b, blue, saturate(_ChromaticOffset * 180.0));

                float edge = smoothstep(0.78, 0.24, length((outputUv - 0.5) * float2(aspect, 1.0)));
                half3 veilColor = half3(0.035, 0.024, 0.055);
                float veil = saturate(_VeilStrength * lerp(1.0, 0.72, edge));
                color = lerp(color, veilColor, veil);

                return half4(saturate(color), 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
