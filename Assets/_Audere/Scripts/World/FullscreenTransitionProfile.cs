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
