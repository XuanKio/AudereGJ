using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.Dialogue
{
    [CreateAssetMenu(
        fileName = "DialogueCharacterCatalog",
        menuName = "Audere/Dialogue/Character Catalog")]
    public sealed class DialogueCharacterCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [SerializeField] private DialogueCharacterId character;
            [SerializeField] private string displayName;
            [SerializeField] private Sprite portrait;

            public DialogueCharacterId Character => character;
            public string DisplayName => displayName;
            public Sprite Portrait => portrait;
        }

        [SerializeField] private List<Entry> characters = new List<Entry>();

        public bool TryGet(DialogueCharacterId character, out Entry entry)
        {
            foreach (Entry candidate in characters)
            {
                if (candidate.Character != character)
                    continue;

                entry = candidate;
                return true;
            }

            entry = default;
            return false;
        }

        private void OnValidate()
        {
            HashSet<DialogueCharacterId> foundCharacters = new HashSet<DialogueCharacterId>();
            foreach (Entry entry in characters)
            {
                if (!foundCharacters.Add(entry.Character))
                    Debug.LogError($"[DialogueCharacterCatalog] Duplicate character: {entry.Character}.", this);
            }
        }
    }
}
