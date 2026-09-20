using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Retry Presentation", fileName = "CombatRetryPresentation")]
    public sealed class CombatRetryPresentationProfile : ScriptableObject
    {
        [SerializeField, TextArea] private string message =
            "Audere, cùng thử lại nào.\nMình tin cậu sẽ làm được mà.";
        [SerializeField, Min(0f)] private float crackDelay = .45f;
        [SerializeField, Min(.01f)] private float splitDuration = .65f;
        [SerializeField, Min(0f)] private float blackoutDelayAfterCrack = .3f;
        [SerializeField, Min(.01f)] private float blackoutDuration = 1.15f;
        [SerializeField, Min(0f)] private float messageDelay = .25f;
        [SerializeField, Min(1f)] private float charactersPerSecond = 32f;
        [SerializeField, Min(0f)] private float halfSeparation = 24f;
        [SerializeField, Min(0f)] private float halfDrop = 240f;
        [SerializeField, Range(0f, 45f)] private float halfRotation = 16f;

        public string Message => message;
        public float CrackDelay => Mathf.Max(0f, crackDelay);
        public float SplitDuration => Mathf.Max(.01f, splitDuration);
        public float BlackoutStart => CrackDelay + Mathf.Max(0f, blackoutDelayAfterCrack);
        public float BlackoutDuration => Mathf.Max(.01f, blackoutDuration);
        public float HeartFadeStart => BlackoutStart;
        public float HeartHiddenTime => BlackoutStart + BlackoutDuration;
        public float MessageStart => Mathf.Max(CrackDelay + SplitDuration,
            BlackoutStart + BlackoutDuration) + Mathf.Max(0f, messageDelay);
        public float CharactersPerSecond => Mathf.Max(1f, charactersPerSecond);
        public float HalfSeparation => halfSeparation;
        public float HalfDrop => halfDrop;
        public float HalfRotation => halfRotation;
    }
}
