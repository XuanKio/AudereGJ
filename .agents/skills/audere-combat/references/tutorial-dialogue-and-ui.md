# Tutorial, dialogue, and UI

Use `audere-dialogue-voice` as well whenever wording, DialogueData, voice, portrait, or character interaction changes.

## Tutorial isolation

Tutorial and real combat are separate sessions. Tutorial data owns a one-phase high-HP enemy, generous TIME, deterministic opening dice, and one-shot cues. Completing tutorial cancels/clears that session, increments session version, restores full real-combat TIME, then starts production enemy phase one.

While an instruction/spotlight is visible, combat-local TIME, dice, bullets, Heart feedback, moves, and spawn cadence pause. Instruction stays until the next left/right interaction; that interaction is consumed. Tutorial UI remains active and hides through CanvasGroup to avoid inactive-object coroutine errors.

Teach all three dice together first, then spotlight Stun Zone and individual dice as the player interacts. Tutorial safety must prevent accidental defeat or boss completion.

## Combat cues

`CombatDialogueCue` supports phase enter/exit, HP, active time, dice ready/caught/rerolled, Stun Zone entry, player hit, all dice types, move start, and cue completion.

- `ModalDialogue`: caller-owned combat pause; preserves mid-phase state.
- `AutoCombatDialogue`: timed DialogueUI lines with no click or input claim; the controller pauses
  combat-local TIME, moves, bullets, dice, and Heart simulation until the sequence completes.
- `BackgroundTextField`: non-interactive pressure text behind gameplay.

Required gates resolve once per attempt and ignore stale callbacks. Skip/close/cancel resolves the owner callback at most once. Dialogue portraits and combat enemy visuals are separate systems.

## TIME, defeat, and Retry

TIME is player health. Reusable outcome rules and phase/cue gates authorize scripted defeat; never force it by enemy ID.

Retry UI belongs to `GameplayUIRoot.CombatRetry` on a high-order screen-space overlay. Its blocker covers the viewport above Dialogue/Puzzle/InputGate. `ForceHide()` clears owner/callback without invoking it, and double-click cannot start duplicate sessions. Load, cancel, destroy, result, and session-version changes clear stale Retry presentation.

An authored `DefeatPresentation` stops dice/input/move, freezes collision, fades hazards, and may play caller-owned dialogue while the actor remains. Only afterward does controller clear the session and report Defeat.
