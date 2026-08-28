using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.Dialogue
{
    [CreateAssetMenu(fileName = "Dialogue_", menuName = "Audere/Dialogue/Dialogue Data")]
    public sealed class DialogueData : ScriptableObject
    {
        [Serializable]
        public struct Line
        {
            [SerializeField] private DialogueSpeakerSide speaker;
            [SerializeField, TextArea(2, 6)] private string text;
            [Tooltip("Optional portrait used from this line onward for its speaker.")]
            [SerializeField] private Sprite portraitOverride;
            [Tooltip("Briefly flicker from the previous portrait into this line's portrait, then settle.")]
            [SerializeField] private bool glitchPortraitTransition;

            public DialogueSpeakerSide Speaker => speaker;
            public string Text => text;
            public Sprite PortraitOverride => portraitOverride;
            public bool GlitchPortraitTransition => glitchPortraitTransition;
        }

        [SerializeField] private string dialogueId = "dialogue-new";
        [SerializeField] private DialogueCharacterId leftCharacter = DialogueCharacterId.Audere;
        [SerializeField] private DialogueCharacterId rightCharacter = DialogueCharacterId.Timor;
        [SerializeField] private Sprite leftPortraitOverride;
        [SerializeField] private Sprite rightPortraitOverride;
        [SerializeField] private List<Line> lines = new List<Line>();

        public string DialogueId => string.IsNullOrWhiteSpace(dialogueId) ? name : dialogueId;
        public DialogueCharacterId LeftCharacter => leftCharacter;
        public DialogueCharacterId RightCharacter => rightCharacter;
        public Sprite LeftPortraitOverride => leftPortraitOverride;
        public Sprite RightPortraitOverride => rightPortraitOverride;
        public IReadOnlyList<Line> Lines => lines;
        public bool HasLines => lines != null && lines.Count > 0;
    }
}
