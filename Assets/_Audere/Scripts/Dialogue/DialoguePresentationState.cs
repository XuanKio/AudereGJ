using UnityEngine;

namespace Audere.Dialogue
{
    /// <summary>Per-speaker, per-playback presentation; authored assets stay immutable.</summary>
    public sealed class DialoguePresentationState
    {
        public DialogueCharacterCatalog.Entry Character { get; private set; }
        public Sprite PortraitOverride { get; private set; }
        public Sprite Portrait => PortraitOverride != null ? PortraitOverride : Character.Portrait;

        public DialoguePresentationState(DialogueCharacterCatalog.Entry character, Sprite portraitOverride = null)
        {
            Character = character;
            PortraitOverride = portraitOverride;
        }

        public bool TryApply(DialogueData.Line line, DialogueCharacterCatalog catalog)
        {
            if (line.CharacterOverride != DialogueCharacterId.None && line.CharacterOverride != Character.Character)
            {
                if (catalog == null || !catalog.TryGet(line.CharacterOverride, out var next))
                    return false;
                Character = next;
                PortraitOverride = null;
            }

            if (line.PortraitOverride != null)
                PortraitOverride = line.PortraitOverride;
            return true;
        }
    }
}
