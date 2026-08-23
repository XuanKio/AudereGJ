using System.Collections;
using Audere.Audio;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class PlayAudioStep : StoryStep
    {
        [Header("Audio Cue")]
        [SerializeField] private AudioId audioId;
        [SerializeField] private bool failIfAudioServiceMissing;

        protected override IEnumerator Execute()
        {
            if (audioId == AudioId.None)
            {
                Debug.LogWarning("[PlayAudioStep] Audio Id is None; skipping this cue.", this);
                CompleteStep();
                yield break;
            }

            AudioService service = AudioService.Instance;
            if (service == null)
            {
                string message =
                    $"[PlayAudioStep] AudioService is unavailable; cannot play '{audioId}'.";
                if (failIfAudioServiceMissing)
                {
                    Debug.LogError(message, this);
                    FailStep();
                }
                else
                {
                    Debug.LogWarning(message, this);
                    CompleteStep();
                }

                yield break;
            }

            service.Play(audioId);
            CompleteStep();
        }
    }
}
