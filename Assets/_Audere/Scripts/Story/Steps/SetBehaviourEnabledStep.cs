using System.Collections;
using UnityEngine;

namespace Audere.Story.Steps
{
    // Toggles a component already authored in the scene; never creates a runtime object.
    public sealed class SetBehaviourEnabledStep : StoryStep
    {
        [SerializeField] private Behaviour target;
        [SerializeField] private bool enable = true;

        public Behaviour Target => target;
        public bool Enable => enable;

        protected override IEnumerator Execute()
        {
            if (target == null || target == this)
            {
                Debug.LogError("[SetBehaviourEnabledStep] Assign another scene component as Target.", this);
                FailStep();
                yield break;
            }
            target.enabled = enable;
            CompleteStep();
            yield break;
        }
    }
}
