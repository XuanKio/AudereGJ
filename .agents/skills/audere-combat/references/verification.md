# Verification

## Preflight

- Check `git status` and relevant diff; identify overlapping user edits.
- Read Unity editor state/custom tools and relevant scene/prefab resources.
- Stop Play Mode before authoring assets, prefabs, or scenes.
- Inspect current code/data and direct references; do not infer production state from docs alone.

## Compile and tests

- Refresh/compile after script edits, wait for Editor ready, then read Console errors.
- Run existing EditMode tests and focused tests for changed logic.
- Add deterministic coverage for policy, selector, move, mechanic, versioned callback, cleanup, or authoring validation.
- Test cancellation before, during, and after the mechanic's dangerous state.

## Lifecycle assertions

- Play/Retry starts clean phase, HP/TIME, selector, cue, actor, dice, projectile, Stun Zone, board layout, and callback state.
- Phase break cancels old move and clears old hazards/dice before next phase.
- Mid-phase pause resumes without restart/duplication.
- Victory/Defeat fires once; stale callbacks cannot affect the new session.
- Cancel/disable/destroy/unload releases constraints, board resize, Stun Zone, Retry, dialogue, pools, and input.

## Scene/prefab QA

- Validate Scene 20 debug behavior and the touched production scene.
- Confirm direct controller/board/actor/encounter/Retry/anchor references.
- Check missing scripts, broken prefabs, missing references, and unexpected broad YAML changes.
- Capture telegraph, active, and cleanup states for visual mechanics when supported.
- Check 16:9, 4:3, and ultrawide for screen UI, Retry, bounds, camera, or layout changes.

## Stun Zone acceptance

- Hidden outside tutorial/phase ownership.
- Telegraph visible without blocking catch.
- Blocking starts after telegraph; fade restores catch immediately.
- Dice keep moving and right-click reroll remains available.
- Geometry stays inside current Dice Field, including shifted/narrow fields.
- Completion, phase break, cancel, defeat, retry, and disable hide zone and clear cursor stun.

## Handoff evidence

Report exact tests, Console errors, inspected scenes/prefabs/assets, and unrun manual QA. Separate pre-existing warnings. Do not claim pass without evidence or commit unless Xuân asks.
