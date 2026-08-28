using System.Collections;
using Audere.Story.Presentation;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class ChalkDrawingStep : StoryStep
    {
        [SerializeField] private ChalkDrawingView view;
        public ChalkDrawingView View => view;
        protected override IEnumerator Execute()
        {
            bool ended = false;
            if (view == null || !view.Show(this, () => ended = true)) { FailStep(); yield break; }
            while (!ended)
            {
                if (view == null || !view.IsShowing) { Cancel(); yield break; }
                yield return null;
            }
            CompleteStep();
        }
        protected override void OnCancelled() => view?.ForceHide(this);
    }
}
