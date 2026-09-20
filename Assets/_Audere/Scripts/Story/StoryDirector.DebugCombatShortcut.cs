using Audere.Core;
using Audere.Story.Steps;
using Audere.World;
using UnityEngine;

namespace Audere.Story
{
    public sealed partial class StoryDirector
    {
        private int debugCombatKeyPressCount;
        private float debugCombatLastPressTime;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
                RegisterDebugCombatPress(Time.unscaledTime);
        }

        internal bool RegisterDebugCombatPress(float now)
        {
            if (now - debugCombatLastPressTime > 1.5f)
                debugCombatKeyPressCount = 0;
            debugCombatLastPressTime = now;
            if (++debugCombatKeyPressCount < 5)
                return false;
            debugCombatKeyPressCount = 0;
            return DebugJumpToCombat();
        }

        public bool DebugJumpToCombat()
        {
            if (!isActiveAndEnabled || (SceneFlow.Instance != null && SceneFlow.Instance.IsBusy))
                return false;

            // Discovery is bounded to the director's authored registry, never a scene-wide search.
            foreach (StoryEvent entry in registeredEvents)
                foreach (CombatStep combat in entry.GetComponentsInChildren<CombatStep>(true))
                    if (combat.IsRunning || (combat.CombatController != null && combat.CombatController.IsPlaying))
                        return false;

            int start = Mathf.Max(0, registeredEvents.IndexOf(currentEvent));
            for (int offset = 0; offset < registeredEvents.Count; offset++)
            {
                StoryEvent entry = registeredEvents[(start + offset) % registeredEvents.Count];
                if (!entry.isActiveAndEnabled) continue;
                foreach (Transform child in entry.transform)
                {
                    CombatStep combat = child.GetComponent<CombatStep>();
                    if (combat == null || !combat.isActiveAndEnabled || combat.CombatController == null ||
                        combat.CombatEncounterData == null || combat.CombatController.BoardView == null)
                        continue;
                    if (entry == currentEvent && entry.CurrentStep != null &&
                        child.GetSiblingIndex() < entry.CurrentStep.transform.GetSiblingIndex())
                        continue;

                    WorldModeController world = FindCombatWorld(entry);
                    if (world == null || !world.isActiveAndEnabled) continue;
                    CancelCurrentEvent();
                    // An interrupted authored cover must not obscure the destination or block its input.
                    foreach (CanvasFadeStep fade in storyEventsRoot.GetComponentsInChildren<CanvasFadeStep>(true))
                    {
                        CanvasGroup cover = fade.CanvasGroup;
                        if (cover == null) continue;
                        cover.alpha = 0f;
                        cover.blocksRaycasts = false;
                        cover.interactable = false;
                    }
                    world.DebugShowCombatImmediate();
                    return StartEvent(entry, null, combat);
                }
            }
            return false;
        }

        private static WorldModeController FindCombatWorld(StoryEvent entry)
        {
            foreach (FullscreenWorldModeTransitionStep step in entry.GetComponentsInChildren<FullscreenWorldModeTransitionStep>(true))
                if (step.TargetMode == WorldGameplayMode.Combat && step.WorldModeController != null)
                    return step.WorldModeController;
            foreach (WorldModeStep step in entry.GetComponentsInChildren<WorldModeStep>(true))
                if (step.WorldModeController != null)
                    return step.WorldModeController;
            return null;
        }
    }
}
