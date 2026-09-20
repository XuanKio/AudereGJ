using System.Collections;
using Audere.Dialogue;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class GameplayUiVisibilityStep : StoryStep
    {
        [SerializeField] private bool visible;
        private StoryEvent ownerEvent;
        private GameplayUIRoot hiddenUi;

        protected override IEnumerator Execute()
        {
            GameplayUIRoot ui = GameplayUIRoot.Instance;
            if (ui == null)
            {
                Debug.LogError("[GameplayUiVisibilityStep] GameplayUIRoot is required.", this);
                FailStep();
                yield break;
            }

            ui.SetGameplayCanvasVisible(visible);
            if (!visible)
            {
                hiddenUi = ui;
                ownerEvent = GetComponentInParent<StoryEvent>();
                if (ownerEvent != null)
                    ownerEvent.Ended += HandleEventEnded;
            }
            CompleteStep();
        }

        private void HandleEventEnded(StoryEventResult result)
        {
            if (result != StoryEventResult.Completed && hiddenUi != null)
                hiddenUi.SetGameplayCanvasVisible(true);
            Detach();
        }

        private void OnDestroy() => Detach();

        private void Detach()
        {
            if (ownerEvent != null)
                ownerEvent.Ended -= HandleEventEnded;
            ownerEvent = null;
            hiddenUi = null;
        }
    }
}
