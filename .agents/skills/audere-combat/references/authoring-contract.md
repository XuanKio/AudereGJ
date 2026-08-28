# Authoring contract

## Data assets

`CombatEnemyDefinition` requires a stable enemy ID, display name, actor prefab, phase policy, and ordered phase list. It stores no runtime state.

Each `CombatPhaseDefinition` authors only the fields used by its policy: HP, shared threshold, duration, captured-batch data, moveset, cues, player-defeat gate, or minimum TIME floor. Validation reports invalid data; runtime must not silently repair it.

`CombatMoveSet` supports:

- `OrderedLoop`: authored order, wraps, resets on phase enter/retry.
- `WeightedRandom`: ignores non-positive weights, supports deterministic fixed-seed tests, and errors when no valid entry exists.

`CombatMoveDefinition` and subclasses are immutable data. Execution state—elapsed time, shot index, active projectile, callbacks, session/phase references—belongs to a fresh `ICombatMoveExecution`.

## Enemy prefab and production scene

Each enemy owns a separate `CombatEnemyActor` prefab with direct references to visual root, projectile/VFX anchors, animator/renderers, and mechanic modules. Placeholder art and hierarchy names must contain `PLACEHOLDER`.

Production scenes place the encounter enemy prefab instance directly under:

```text
CombatBoard/Enemy/Enemy Mount
```

Bind that exact instance to `CombatBoardView.authoredEnemyActor`. Runtime uses its scene-authored position, scale, rotation, sprite overrides, and size; it does not normalize the actor or clone over local edits. `CombatEnemyDefinition.ActorPrefab` remains the authoring source and fallback for unmigrated debug scenes.

Do not use `FindFirstObjectByType`, `FindObjectOfType`, or scene-wide search to make authoring convenient. Use direct serialized references or runtime context passed by the owner. Local discovery inside the enemy prefab or a directly referenced board subtree is acceptable when bounded and validated.

## Board and UI setup

The shared board lives at `Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab`.

- `Frame` is the fixed visible border.
- `Dice Field` is the gameplay rectangle and movement bound.
- `Projectile Mask` is inset inside the field so bullets/lasers never draw over the border.
- `Airborne Dice Overlay` follows horizontal field changes but remains outside field clipping while dice bounce.
- Enemy name/image, enemy actor scale/offset, and scene-specific enemy appearance remain editable in the scene.
- Retry is a screen-space overlay owned by `GameplayUIRoot`, not a child of the world-space board.

Use idempotent editor tools for repeatable asset migration. Add a focused authoring command when only one subsystem should change; do not rerun a broad scene builder merely to update one move asset.

## Validation

Reject or clearly report missing/duplicate IDs, null prefab/phase/move/projectile/cue, invalid policy values, invalid weights or normalized positions, composite self/null children, missing direct scene references, broken prefabs, and missing scripts. Keep asset GUIDs and unrelated serialized overrides intact; do not regenerate `.meta` files without a migration reason.
