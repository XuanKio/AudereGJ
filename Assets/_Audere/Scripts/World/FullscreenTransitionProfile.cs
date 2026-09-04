using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.World
{
    [Serializable]
    public sealed class FullscreenTransitionFloatTrack
    {
        [SerializeField] private string shaderProperty;
        [SerializeField] private AnimationCurve values = AnimationCurve.Linear(0f, 0f, 1f, 0f);

        [NonSerialized] private int propertyId;
        [NonSerialized] private bool propertyIdCached;

        public string ShaderProperty => shaderProperty;
        public AnimationCurve Values => values;

        public void Apply(Material material, float time)
        {
            if (material == null || string.IsNullOrWhiteSpace(shaderProperty) || values == null)
                return;

            if (!propertyIdCached)
            {
                propertyId = Shader.PropertyToID(shaderProperty);
                propertyIdCached = true;
            }

            material.SetFloat(propertyId, values.Evaluate(time));
        }
    }

    [Serializable]
    public sealed class FullscreenShatterSettings
    {
        [SerializeField] private bool enabled;
        [SerializeField] private bool revealTargetBehindShards;
        [SerializeField] private Material shardMaterial;
        [SerializeField] private float captureTime = .8f;
        [SerializeField] private float breakTime = 2.2f;
        [SerializeField] private float clearTime = 4.25f;
        [SerializeField] private AnimationCurve crack = AnimationCurve.Linear(.9f, 0f, 2.2f, 1f);
        [SerializeField] private Vector2 impact = new Vector2(.54f, .53f);
        [SerializeField, Range(8, 24)] private int spokeCount = 14;
        [SerializeField] private int seed = 8027;
        [SerializeField, Min(.1f)] private float impulse = 1f;
        [SerializeField, Min(.1f)] private float gravity = 2.8f;
        [SerializeField, Min(0f)] private float spin = 1f;
        [SerializeField, Range(.001f, .025f)] private float thickness = .007f;
        [SerializeField] private Color frontTint = new Color(.55f, .63f, .8f, 1f);
        [SerializeField] private Color backTint = new Color(.018f, .025f, .045f, 1f);
        [SerializeField] private Color edgeTint = new Color(.12f, .17f, .22f, 1f);
        [SerializeField] private Color crackTint = new Color(.38f, .48f, .58f, 1f);

        public bool Enabled => enabled;
        public bool RevealTargetBehindShards => revealTargetBehindShards;
        public Material ShardMaterial => shardMaterial;
        public float CaptureTime => captureTime;
        public float BreakTime => breakTime;
        public float ClearTime => clearTime;
        public AnimationCurve Crack => crack;
        public Vector2 Impact => impact;
        public int SpokeCount => Mathf.Clamp(spokeCount, 8, 24);
        public int Seed => seed;
        public float Impulse => impulse;
        public float Gravity => gravity;
        public float Spin => spin;
        public float Thickness => thickness;
        public Color FrontTint => frontTint;
        public Color BackTint => backTint;
        public Color EdgeTint => edgeTint;
        public Color CrackTint => crackTint;

        public bool Validate(float swapTime, float duration)
        {
            return !enabled || (shardMaterial != null && crack != null && captureTime >= 0f &&
                captureTime < breakTime && breakTime < clearTime && clearTime <= duration &&
                (revealTargetBehindShards ? swapTime >= breakTime && swapTime <= clearTime : clearTime <= swapTime) &&
                impulse > 0f && gravity > 0f && spin >= 0f && thickness > 0f &&
                impact.x >= 0f && impact.x <= 1f && impact.y >= 0f && impact.y <= 1f);
        }
    }
    [CreateAssetMenu(
        fileName = "WorldTransition_New",
        menuName = "Audere/World/Fullscreen Transition Profile")]
    public sealed class FullscreenTransitionProfile : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string profileId = "fullscreen-transition";
        [SerializeField] private string displayName = "Fullscreen Transition";

        [Header("Shared Presentation")]
        [SerializeField] private Material material;
        [SerializeField, Min(.01f)] private float duration = 1f;
        [SerializeField, Min(0f)] private float modeSwapTime = .8f;
        [SerializeField] private bool usesFocusRenderer;
        [SerializeField] private string effectTimeProperty = "_EffectTime";
        [SerializeField] private FullscreenTransitionFloatTrack[] floatTracks =
            Array.Empty<FullscreenTransitionFloatTrack>();

        [Header("Optional Frozen Screen Shatter")]
        [SerializeField] private FullscreenShatterSettings screenShatter = new FullscreenShatterSettings();

        public FullscreenShatterSettings ScreenShatter => screenShatter;
        public string ProfileId => profileId;
        public string DisplayName => displayName;
        public Material Material => material;
        public float Duration => Mathf.Max(.01f, duration);
        public float ModeSwapTime => Mathf.Clamp(modeSwapTime, 0f, Duration);
        public bool UsesFocusRenderer => usesFocusRenderer;
        public IReadOnlyList<FullscreenTransitionFloatTrack> FloatTracks => floatTracks;

        public bool Validate(out string error)
        {
            if (material == null)
            {
                error = $"Transition profile '{name}' has no material.";
                return false;
            }

            if (duration <= 0f)
            {
                error = $"Transition profile '{name}' needs a positive duration.";
                return false;
            }

            if (modeSwapTime < 0f || modeSwapTime > duration)
            {
                error = $"Transition profile '{name}' has a mode swap outside its duration.";
                return false;
            }

            if (screenShatter != null && !screenShatter.Validate(modeSwapTime, duration))
            {
                error = $"Transition profile '{name}' needs valid capture/break/clear timing and a swap concealed by the frozen pane or cover.";
                return false;
            }
            if (screenShatter != null && screenShatter.Enabled && !material.HasProperty("_Cover"))
            {
                error = $"Transition profile '{name}' needs a _Cover property behind screen shards.";
                return false;
            }

            error = null;
            return true;
        }

        public void Apply(Material runtimeMaterial, float time)
        {
            if (runtimeMaterial == null)
                return;

            if (!string.IsNullOrWhiteSpace(effectTimeProperty))
                runtimeMaterial.SetFloat(Shader.PropertyToID(effectTimeProperty), time);

            if (floatTracks == null)
                return;

            foreach (FullscreenTransitionFloatTrack track in floatTracks)
                track?.Apply(runtimeMaterial, time);
        }

        private void OnValidate()
        {
            duration = Mathf.Max(.01f, duration);
            modeSwapTime = Mathf.Clamp(modeSwapTime, 0f, duration);
        }
    }
}
