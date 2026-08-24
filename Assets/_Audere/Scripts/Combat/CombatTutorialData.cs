using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Tutorial Data", fileName = "CombatTutorial_New")]
    public sealed class CombatTutorialData : ScriptableObject
    {
        [SerializeField] private string tutorialId;
        [SerializeField] private CombatEnemyDefinition enemyDefinition;
        [SerializeField, Min(1f)] private float playerTime = 120f;
        [SerializeField] private CombatSymbol[] openingDice;
        [SerializeField] private CombatDialogueCue[] cues;

        public string TutorialId => tutorialId;
        public CombatEnemyDefinition EnemyDefinition => enemyDefinition;
        public float PlayerTime => playerTime;
        public IReadOnlyList<CombatSymbol> OpeningDice => openingDice;
        public IReadOnlyList<CombatDialogueCue> Cues => cues;

        public bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(tutorialId))
            {
                error = $"Combat tutorial '{name}' requires a stable Tutorial ID.";
                return false;
            }
            if (enemyDefinition == null)
            {
                error = $"Combat tutorial '{tutorialId}' requires a tutorial Enemy Definition.";
                return false;
            }
            if (!enemyDefinition.Validate(out string enemyError))
            {
                error = $"Combat tutorial '{tutorialId}' has an invalid enemy: {enemyError}";
                return false;
            }
            if (enemyDefinition.PhaseCount != 1)
            {
                error = $"Combat tutorial '{tutorialId}' must use exactly one isolated tutorial phase.";
                return false;
            }
            if (playerTime <= 0f)
            {
                error = $"Combat tutorial '{tutorialId}' requires Player Time greater than zero.";
                return false;
            }
            if (openingDice == null || openingDice.Length == 0)
            {
                error = $"Combat tutorial '{tutorialId}' requires authored opening dice.";
                return false;
            }

            var openingSymbols = new HashSet<CombatSymbol>();
            for (int i = 0; i < openingDice.Length; i++)
            {
                if (!openingSymbols.Add(openingDice[i]))
                {
                    error = $"Combat tutorial '{tutorialId}' opening dice contains duplicate symbol '{openingDice[i]}'.";
                    return false;
                }
            }
            if (openingSymbols.Count != 3 ||
                !openingSymbols.Contains(CombatSymbol.Attack) ||
                !openingSymbols.Contains(CombatSymbol.Shield) ||
                !openingSymbols.Contains(CombatSymbol.Heal))
            {
                error = $"Combat tutorial '{tutorialId}' opening batch must contain Attack, Shield and Heal exactly once.";
                return false;
            }

            if (cues == null || cues.Length == 0)
            {
                error = $"Combat tutorial '{tutorialId}' requires tutorial cues.";
                return false;
            }

            bool hasOverview = false;
            bool hasCompletion = false;
            var cueIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < cues.Length; i++)
            {
                CombatDialogueCue cue = cues[i];
                if (cue == null)
                {
                    error = $"Combat tutorial '{tutorialId}' has a null cue at index {i}.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(cue.CueId) || !cueIds.Add(cue.CueId))
                {
                    error = $"Combat tutorial '{tutorialId}' has an empty or duplicate Cue ID at index {i}.";
                    return false;
                }
                if (!cue.IsTutorial || !cue.HasContent)
                {
                    error = $"Combat tutorial '{tutorialId}' cue '{cue.CueId}' must be marked tutorial and contain presentation data.";
                    return false;
                }
                if (cue.Sequence != null)
                {
                    for (int dialogueIndex = 0; dialogueIndex < cue.Sequence.Count; dialogueIndex++)
                    {
                        if (cue.Sequence[dialogueIndex] == null)
                        {
                            error = $"Combat tutorial '{tutorialId}' cue '{cue.CueId}' has a null DialogueData reference.";
                            return false;
                        }
                    }
                }
                hasOverview |= cue.Trigger == CombatDialogueCueTrigger.DiceBatchReady &&
                    cue.TutorialFocus == CombatTutorialFocus.DiceAll;
                hasCompletion |= cue.Trigger == CombatDialogueCueTrigger.AllDiceTypesCaught;
            }

            if (!hasOverview || !hasCompletion)
            {
                error = $"Combat tutorial '{tutorialId}' requires a DiceAll opening overview and an AllDiceTypesCaught completion cue.";
                return false;
            }

            error = null;
            return true;
        }

        private void OnValidate()
        {
            if (!Validate(out string error))
                Debug.LogError($"[CombatTutorialData] {error}", this);
        }
    }
}
