using System.Collections;
using Audere.Story.Presentation;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class DreamFallStep : StoryStep
    {
        [SerializeField] private Transform actor;
        [SerializeField] private Transform fallTarget;
        [SerializeField] private DreamTileFragments fragments;
        [SerializeField] private DreamAtmosphereView atmosphere;
        [SerializeField] private StoryEvent owner;
        [SerializeField] private FallingWindView wind;
        [SerializeField, Min(.05f)] private float breakLead = .07f;
        [SerializeField, Min(.1f)] private float duration = 2.6f;
        [SerializeField, Range(0f, 120f)] private float backwardLean = 78f;
        [SerializeField, Min(0f)] private float floatHeight = .025f;
        [SerializeField, Min(.5f)] private float floatPeriod = 2.8f;
        [SerializeField, Range(0f, 5f)] private float floatTilt = 2.5f;
        private Vector3 fallPosition;
        private Quaternion fallRotation;
        private float floatAge;
        public bool IsFloating { get; private set; }
        private Quaternion uprightRotation;
        private bool poseCaptured;
        public float Progress { get; private set; }
        public DreamTileFragments Fragments => fragments;

        private void OnEnable() { if (owner != null) owner.Ended += OnEventEnded; }
        private void OnDisable()
        {
            Cancel();
            if (owner != null) owner.Ended -= OnEventEnded;
            RestorePose();
        }
        private void OnEventEnded(StoryEventResult result)
        {
            if (result != StoryEventResult.Completed) RestorePose();
            else StopFloating();
        }
        private void RestorePose()
        {
            StopFloating();
            if (poseCaptured && actor != null) actor.rotation = uprightRotation;
            poseCaptured = false;
        }

        private void StopFloating()
        {
            if (IsFloating && actor != null) actor.SetPositionAndRotation(fallPosition, fallRotation);
            IsFloating = false;
            floatAge = 0f;
        }

        private void LateUpdate()
        {
            if (!IsFloating) return;
            if (owner == null || !owner.IsPlaying || actor == null) { StopFloating(); return; }
            floatAge += Time.unscaledDeltaTime;
            float strength = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Progress - .6f) / .4f));
            float phase = floatAge * Mathf.PI * 2f / Mathf.Max(.5f, floatPeriod);
            actor.SetPositionAndRotation(fallPosition + Vector3.up * (Mathf.Sin(phase) * floatHeight * strength),
                fallRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(phase * .8f) * floatTilt * strength));
        }

        protected override IEnumerator Execute()
        {
            if (actor == null || fallTarget == null || atmosphere == null || fragments == null || !fragments.Break())
            { FailStep(); yield break; }
            StopFloating();
            Progress = 0f;
            uprightRotation = actor.rotation;
            poseCaptured = true;
            if (wind == null || !wind.Begin()) { fragments.Restore(); FailStep(); yield break; }
            // Give the break one readable instant as the foot is almost at the tile center.
            float elapsed = 0f;
            while (elapsed < breakLead) { elapsed += Time.unscaledDeltaTime; yield return null; }
            Vector3 start = actor.position;
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed = Mathf.Min(duration, elapsed + Time.unscaledDeltaTime);
                Progress = elapsed / duration;
                float drop = Progress * Progress;
                fallPosition = new Vector3(Mathf.Lerp(start.x, fallTarget.position.x, Progress),
                    Mathf.Lerp(start.y, fallTarget.position.y, drop), start.z);
                fallRotation = uprightRotation * Quaternion.Euler(0f, 0f,
                    backwardLean * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Progress / .58f)));
                actor.SetPositionAndRotation(fallPosition, fallRotation);
                if (Progress > .6f) IsFloating = true;
                // Follow only the descent: the small airborne float must remain visible relative to the camera.
                atmosphere.SetFall(fallPosition.y - start.y, Mathf.SmoothStep(0f, 1f, Progress * 2f));
                atmosphere.SetChaos(Mathf.Clamp01(Progress * 2.5f));
                yield return null;
            }
        }

        protected override void OnCancelled()
        {
            // The fall pose stays through dialogue; only cancellation/replay restores standing.
            fragments?.Restore();
            atmosphere?.StopAndRestore();
            wind?.Stop();
            RestorePose();
        }
    }
}
