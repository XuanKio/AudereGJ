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
        CapturedDiceBatchSequence = 3,
        SharedHealthPlayerTime = 4,
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
        MoveStarted = 10,
        CueCompleted = 11,
    }

    public enum CombatDialoguePresentation
    {
        ModalDialogue = 0,
        AutoCombatDialogue = 1,
        BackgroundTextField = 2,
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
        [SerializeField] private CombatMoveDefinition triggerMove;
        [SerializeField] private string triggerCueId;
        [SerializeField] private bool filterBySymbol;
        [SerializeField] private CombatSymbol symbol;
        [SerializeField] private DialogueData[] sequence;
        [SerializeField, TextArea(2, 4)] private string instruction;
        [SerializeField, Min(0f)] private float instructionDuration;
        [SerializeField] private CombatTutorialFocus tutorialFocus;
        [SerializeField] private CombatSymbol showcasedSymbol;
        [SerializeField] private bool isTutorial;
        [SerializeField] private CombatDialoguePresentation presentation;
        [SerializeField, Min(.1f)] private float minimumLineDuration = 1.4f;
        [SerializeField, Min(1f)] private float charactersPerSecond = 20f;
        [SerializeField, Min(0f)] private float interLineGap = .18f;
        [SerializeField] private bool repeatOnTrigger;
        [SerializeField] private bool interruptsAutoDialogue;
        public bool InterruptsAutoDialogue => interruptsAutoDialogue;
        [SerializeField] private bool playLoseRhythmOnComplete;
        [SerializeField] private bool requiredBeforeVictory;
        [SerializeField] private bool requiredBeforePhaseAdvance;
        [SerializeField] private bool requiredBeforePlayerDefeat;

        public string CueId => cueId;
        public string OneShotKey => string.IsNullOrWhiteSpace(oneShotKey) ? cueId : oneShotKey;
        public CombatDialogueCueTrigger Trigger => trigger;
        public float TriggerValue => triggerValue;
        public CombatMoveDefinition TriggerMove => triggerMove;
        public string TriggerCueId => triggerCueId;
        public bool FilterBySymbol => filterBySymbol;
        public CombatSymbol Symbol => symbol;
        public IReadOnlyList<DialogueData> Sequence => sequence;
        public bool HasDialogue => sequence != null && sequence.Length > 0;
        public string Instruction => instruction;
        public float InstructionDuration => instructionDuration;
        public CombatTutorialFocus TutorialFocus => tutorialFocus;
        public CombatSymbol ShowcasedSymbol => showcasedSymbol;
        public bool IsTutorial => isTutorial;
        public CombatDialoguePresentation Presentation => presentation;
        public float MinimumLineDuration => minimumLineDuration;
        public float CharactersPerSecond => charactersPerSecond;
        public float InterLineGap => interLineGap;
        public bool RepeatOnTrigger => repeatOnTrigger;
        public bool PlayLoseRhythmOnComplete => playLoseRhythmOnComplete;
        public bool RequiredBeforeVictory => requiredBeforeVictory;
        public bool RequiredBeforePhaseAdvance => requiredBeforePhaseAdvance;
        public bool RequiredBeforePlayerDefeat => requiredBeforePlayerDefeat;
        public bool HasInstruction => !string.IsNullOrWhiteSpace(instruction);
        public bool HasContent => HasDialogue || HasInstruction;
        public bool PausesCombatForPresentation =>
            (HasDialogue && presentation != CombatDialoguePresentation.BackgroundTextField) ||
            (HasInstruction && tutorialFocus != CombatTutorialFocus.None);

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
        [Header("Captured Dice Batch Sequence")]
        [SerializeField] private CombatDiceBatchDefinition diceBatch;
        [SerializeField, Min(1)] private int requiredCapturedBatches = 1;
        [SerializeField] private bool spawnDice = true;
        [Tooltip("Hold HP and advance after the phase's first move completes (a self-contained special).")]
        [SerializeField] private bool advanceOnMoveComplete;
        [SerializeField] private bool allowsPlayerDefeat = true;
        [Tooltip("Optional encounter-TIME floor applied when this phase begins. Zero keeps the current TIME.")]
        [SerializeField, Min(0f)] private float minimumPlayerTimeOnEnter;
        [Tooltip("SharedHealthPlayerTime only: advance once remaining TIME / maximum reaches this fraction. Final phase ends at HP zero.")]
        [SerializeField, Range(0f, 1f)] private float playerTimeExitFraction;
        public float PlayerTimeExitFraction => playerTimeExitFraction;

        public string PhaseId => phaseId;
        public int MaxHealth => maxHealth;
        public int SharedExitThreshold => sharedExitThreshold;
        public float Duration => duration;
        public CombatMoveSet MoveSet => moveSet;
        public IReadOnlyList<CombatDialogueCue> DialogueCues => dialogueCues;
        public CombatDiceBatchDefinition DiceBatch => diceBatch;
        public int RequiredCapturedBatches => Mathf.Max(1, requiredCapturedBatches);
        public bool SpawnDice => spawnDice;
        public bool AdvanceOnMoveComplete => advanceOnMoveComplete;
        public bool AllowsPlayerDefeat => allowsPlayerDefeat;
        public float MinimumPlayerTimeOnEnter => Mathf.Max(0f, minimumPlayerTimeOnEnter);
    }

}
