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
        }
        private void RestorePose()
        {
            if (poseCaptured && actor != null) actor.rotation = uprightRotation;
            poseCaptured = false;
        }

        protected override IEnumerator Execute()
        {
            if (actor == null || fallTarget == null || atmosphere == null || fragments == null || !fragments.Break())
            { FailStep(); yield break; }
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
                actor.position = new Vector3(Mathf.Lerp(start.x, fallTarget.position.x, Progress),
                    Mathf.Lerp(start.y, fallTarget.position.y, drop), start.z);
                actor.rotation = uprightRotation * Quaternion.Euler(0f, 0f,
                    backwardLean * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Progress / .58f)));
                atmosphere.SetFall(actor.position.y - start.y, Mathf.SmoothStep(0f, 1f, Progress * 2f));
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
