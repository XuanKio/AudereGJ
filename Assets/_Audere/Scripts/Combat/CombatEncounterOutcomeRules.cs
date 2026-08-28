using System;
using UnityEngine;

namespace Audere.Combat
{
    [Flags]
    public enum CombatAllowedOutcome
    {
        None = 0,
        Victory = 1 << 0,
        Defeat = 1 << 1,
        Special = 1 << 2,
        All = Victory | Defeat | Special,
    }

    public enum CombatPlayerDefeatGate
    {
        Always = 0,
        CurrentPhaseAndRequiredCues = 1,
    }

    [Serializable]
    public sealed class CombatEncounterOutcomeRules
    {
        [SerializeField] private CombatAllowedOutcome allowedOutcomes = CombatAllowedOutcome.All;
        [SerializeField] private CombatPlayerDefeatGate playerDefeatGate;
        [SerializeField] private bool showRetryOnDefeat = true;

        public CombatAllowedOutcome AllowedOutcomes => allowedOutcomes;
        public CombatPlayerDefeatGate PlayerDefeatGate => playerDefeatGate;
        public bool ShowRetryOnDefeat => showRetryOnDefeat;

        public bool Allows(CombatResult result)
        {
            CombatAllowedOutcome flag = result switch
            {
                CombatResult.Victory => CombatAllowedOutcome.Victory,
                CombatResult.Defeat => CombatAllowedOutcome.Defeat,
                CombatResult.Special => CombatAllowedOutcome.Special,
                _ => CombatAllowedOutcome.None,
            };
            return flag == CombatAllowedOutcome.None || (allowedOutcomes & flag) != 0;
        }
    }
}
