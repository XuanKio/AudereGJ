using System.Collections;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class SetActorFacingStep : StoryStep
    {
        [SerializeField] private SpriteRenderer actorRenderer;
        [SerializeField] private bool faceRight = true;
        [SerializeField] private bool sourceSpriteFacesLeft = true;

        public SpriteRenderer ActorRenderer => actorRenderer;
        public bool FaceRight => faceRight;

        protected override IEnumerator Execute()
        {
            if (actorRenderer == null)
            {
                Debug.LogError("[SetActorFacingStep] Actor Renderer reference is required.", this);
                FailStep();
                yield break;
            }

            actorRenderer.flipX = sourceSpriteFacesLeft ? faceRight : !faceRight;
            CompleteStep();
        }
    }
}
