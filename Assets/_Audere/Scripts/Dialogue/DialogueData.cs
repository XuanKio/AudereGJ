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

            public DialogueSpeakerSide Speaker => speaker;
            public string Text => text;
        }

        [SerializeField] private string dialogueId = "dialogue-new";
        [SerializeField] private DialogueCharacterId leftCharacter = DialogueCharacterId.Audere;
        [SerializeField] private DialogueCharacterId rightCharacter = DialogueCharacterId.Timor;
        [SerializeField] private List<Line> lines = new List<Line>();

        public string DialogueId => string.IsNullOrWhiteSpace(dialogueId) ? name : dialogueId;
        public DialogueCharacterId LeftCharacter => leftCharacter;
        public DialogueCharacterId RightCharacter => rightCharacter;
        public IReadOnlyList<Line> Lines => lines;
        public bool HasLines => lines != null && lines.Count > 0;
    }
}
