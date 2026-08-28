using System.Collections;
using Audere.Story.Presentation;
using Audere.Dialogue;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class DreamAtmosphereStep : StoryStep
    {
        public enum Action { Begin, Collapse, Stop }
        [SerializeField] private DreamAtmosphereView atmosphere;
        [SerializeField] private Action action;
        [SerializeField, Min(0f)] private float duration = 1.2f;

        protected override IEnumerator Execute()
        {
            if (atmosphere == null) { FailStep(); yield break; }
            if (action == Action.Begin)
            {
                atmosphere.Begin();
                if (GameplayUIRoot.Instance != null) GameplayUIRoot.Instance.PuzzleUi.gameObject.SetActive(true);
            }
            else if (action == Action.Stop) atmosphere.StopAndRestore();
            else
            {
                if (GameplayUIRoot.Instance != null) GameplayUIRoot.Instance.PuzzleUi.gameObject.SetActive(false);
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    atmosphere.SetChaos(Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Max(.01f, duration)));
                    yield return null;
                }
                atmosphere.SetChaos(1f);
            }
        }

        protected override void OnCancelled() => atmosphere?.StopAndRestore();
    }
}
