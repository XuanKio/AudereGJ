using System;
using System.Collections.Generic;
using Audere.Dialogue;
using UnityEngine;

namespace Audere.Combat
{
    public enum CombatPhasePolicy
    {
        PerPhaseHealth = 0,
        SharedHealthThresholds = 1,
        TimedSequence = 2,
    }

    public enum CombatMoveSelectionPolicy
    {
        OrderedLoop = 0,
        WeightedRandom = 1,
    }

    public enum CombatDialogueCueTrigger
    {
        PhaseEnter = 0,
        PhaseExit = 1,
        HealthAtOrBelow = 2,
        ElapsedActiveTime = 3,
        DiceBatchReady = 4,
        DiceCaught = 5,
        DiceRerolled = 6,
        CursorEnteredStunZone = 7,
        PlayerHit = 8,
        AllDiceTypesCaught = 9,
    }

    public enum CombatTutorialFocus
    {
        None = 0,
        Time = 1,
        StunZone = 2,
        Dice = 3,
        DiceAll = 4,
    }

    [Serializable]
    public sealed class CombatDialogueCue
    {
        [SerializeField] private string cueId;
        [SerializeField] private string oneShotKey;
        [SerializeField] private CombatDialogueCueTrigger trigger;
        [SerializeField, Min(0f)] private float triggerValue;
        [SerializeField] private bool filterBySymbol;
        [SerializeField] private CombatSymbol symbol;
        [SerializeField] private DialogueData[] sequence;
        [SerializeField, TextArea(2, 4)] private string instruction;
        [SerializeField, Min(0f)] private float instructionDuration;
        [SerializeField] private CombatTutorialFocus tutorialFocus;
        [SerializeField] private CombatSymbol showcasedSymbol;
        [SerializeField] private bool isTutorial;

        public string CueId => cueId;
        public string OneShotKey => string.IsNullOrWhiteSpace(oneShotKey) ? cueId : oneShotKey;
        public CombatDialogueCueTrigger Trigger => trigger;
        public float TriggerValue => triggerValue;
        public bool FilterBySymbol => filterBySymbol;
        public CombatSymbol Symbol => symbol;
        public IReadOnlyList<DialogueData> Sequence => sequence;
        public bool HasDialogue => sequence != null && sequence.Length > 0;
        public string Instruction => instruction;
        public float InstructionDuration => instructionDuration;
        public CombatTutorialFocus TutorialFocus => tutorialFocus;
        public CombatSymbol ShowcasedSymbol => showcasedSymbol;
        public bool IsTutorial => isTutorial;
        public bool HasInstruction => !string.IsNullOrWhiteSpace(instruction);
        public bool HasContent => HasDialogue || HasInstruction;
        public bool PausesCombatForPresentation => HasInstruction && tutorialFocus != CombatTutorialFocus.None;

        public bool MatchesSymbol(CombatSymbol value)
        {
            return !filterBySymbol || symbol == value;
        }
    }

    [Serializable]
    public sealed class CombatPhaseDefinition
    {
        [SerializeField] private string phaseId;
        [SerializeField, Min(1)] private int maxHealth = 1;
        [SerializeField, Min(0)] private int sharedExitThreshold;
        [SerializeField, Min(.01f)] private float duration = 1f;
        [SerializeField] private CombatMoveSet moveSet;
        [SerializeField] private CombatDialogueCue[] dialogueCues;

        public string PhaseId => phaseId;
        public int MaxHealth => maxHealth;
        public int SharedExitThreshold => sharedExitThreshold;
        public float Duration => duration;
        public CombatMoveSet MoveSet => moveSet;
        public IReadOnlyList<CombatDialogueCue> DialogueCues => dialogueCues;
    }

}
