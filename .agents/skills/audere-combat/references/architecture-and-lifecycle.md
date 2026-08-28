# Architecture and lifecycle

## Ownership

```text
CombatEncounterData
        ↓
CombatEnemyDefinition
        ↓
CombatEnemyRuntime (one Play/Retry attempt)
        ↓
CombatEnemyActor (scene-authored production instance)
        ↓
CombatMoveDefinition → ICombatMoveExecution
```

| Owner | Owns | Must not own |
| --- | --- | --- |
| `CombatEncounterData` | encounter TIME, dice batch pacing, Heart/bullet tuning, optional tutorial, outcome/defeat presentation, enemy definition | shared Attack/Shield/Heal constants; boss-specific branches |
| `CombatController` | session lifecycle, input claim, TIME-as-health, dice batch runtime, cue orchestration, Victory/Defeat/Cancel | enemy-specific patterns, actor animation, phase-specific projectile code |
| `CombatEnemyRuntime` | phase index/version, HP/timer policy, move selector/execution, cue one-shots, mechanic lifecycle | scene search; mutable state in ScriptableObjects |
| `CombatBoardView` | scene presentation, Heart/cursor bounds, dice/bullet/laser pools, Stun Zone views, player constraint, scene-authored actor binding | boss identity or narrative meaning |
| `CombatStep` | maps combat result to StoryStep Complete/Fail/Retry | internal enemy or Retry presentation logic |

`CombatDiceConstants` remains the only shared source for Attack, Shield, and Heal values.

## Phase policies

- `PerPhaseHealth`: reset authored HP on phase enter; overkill does not cross a phase; last phase at zero can Victory.
- `SharedHealthThresholds`: one shared HP pool; thresholds strictly descend; one hit crosses at most one authored threshold and cannot skip phase-break work.
- `TimedSequence`: phase duration advances only during combat-active time; damage does not drive progression; health UI may be hidden.
- `CapturedDiceBatchSequence`: shared HP can show damage feedback, but authored captured batches drive phase progression; reroll does not resolve a die.

Use `CombatEncounterOutcomeRules` and phase/cue gates for scripted outcomes. Never implement forced defeat, disabled Victory, or Retry behavior with an enemy-ID check.

## Runtime states and versioning

The enemy runtime has one active phase and one active move. Async work must validate session version; phase-owned work must validate both session and phase version. A retry creates clean state: phase, HP, selector index/RNG, cues, actor modules, bullets, lasers, dice, pause ownership, constraints, and callbacks.

## Atomic phase break

Preserve this ordering or an equivalent safe transaction:

1. Mark transition and stop accepting new damage/input for the old phase.
2. Stop queued dice spawning and invalidate/return old dice.
3. Cancel the active move and delayed emissions.
4. Return old-phase bullets/lasers and release player/board/Stun Zone state.
5. Pause encounter-local TIME.
6. Run actor/mechanic phase-exit hooks.
7. Resolve required phase-break cue/dialogue.
8. Enter the next phase; reset phase HP/timer/selector/cues and increment phase version.
9. Run phase-enter hooks, resume TIME/input, then start move and dice batch.

The final phase enters Victory cleanup instead of indexing a missing next phase.

## Pause contracts

- Prefer combat-local pause. Tutorial instruction and modal combat dialogue stop TIME, move, bullets, dice, and Heart simulation without global `Time.timeScale`.
- Mid-phase `CallerOwnedPause` preserves Heart, projectile, active dice, and move cadence; resume must not restart or duplicate the move.
- `AutoCombatDialogue` and background text do not claim input or pause combat.
- Priority: Victory/Defeat > phase break > mid-phase cue > normal continuation.

## Cleanup

Every move execution and mechanic must have idempotent cancellation. Phase break clears phase-owned hazards. Victory, Defeat, Retry, Cancel, board disable/destroy, and scene unload clear the whole session. A stale callback from an old version must be harmless.
