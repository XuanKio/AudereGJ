using System.Collections;
using Audere.Core;
using Audere.Dialogue;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class SceneLoadStep : StoryStep
    {
        [Header("Scene Transition")]
        [SerializeField] private string sceneName = GameScenes.Classroom;
        [SerializeField] private bool hidePuzzleUiBeforeLoad = true;

        protected override IEnumerator Execute()
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[SceneLoadStep] Scene Name is required.", this);
                FailStep();
                yield break;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"[SceneLoadStep] Scene '{sceneName}' is not available in Build Settings.",
                    this);
                FailStep();
                yield break;
            }

            SceneFlow sceneFlow = SceneFlow.Instance;
            if (sceneFlow == null)
            {
                Debug.LogError(
                    "[SceneLoadStep] SceneFlow is unavailable. Start the game through 00_Bootstrap.",
                    this);
                FailStep();
                yield break;
            }

            if (sceneFlow.IsBusy)
            {
                Debug.LogWarning(
                    $"[SceneLoadStep] SceneFlow is already loading; '{sceneName}' was not requested.",
                    this);
                FailStep();
                yield break;
            }

            if (hidePuzzleUiBeforeLoad)
            {
                GameplayUIRoot uiRoot = GameplayUIRoot.Instance;
                if (uiRoot != null && uiRoot.PuzzleUi != null)
                    uiRoot.PuzzleUi.gameObject.SetActive(false);
            }

            sceneFlow.Load(sceneName);
            CompleteStep();
        }
    }
}
