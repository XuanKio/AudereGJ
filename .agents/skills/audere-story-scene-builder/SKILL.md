---
name: audere-story-scene-builder
description: Build or revise Audere narrative flows as scene-first StoryEvent hierarchies, including staging, dialogue-action interleaving, puzzle hand-offs, and scene transitions. Use for production story scene authoring; do not use for isolated gameplay-system refactors.
---

# Audere Story Scene Builder

Build the story as visible Unity hierarchy and direct scene references. Preserve the project's small `StoryDirector -> StoryEvent -> direct-child StoryStep` runner instead of introducing a graph or data-driven framework.

## Before authoring

1. Inspect the current scene, adjacent production `StoryEvent` objects, referenced `DialogueData`, and any gameplay controller the beat touches.
2. Classify new narrative claims as `Established Canon`, `Strongly Implied`, `Design Intent`, or `Unresolved`. A requested beat remains `Design Intent` until implemented and accepted in the production scene.
3. For any dialogue, character interaction, bark, or narrative sequence, read and follow `../audere-dialogue-voice/SKILL.md` and its required references before editing.
4. State the beat's primary story job in one sentence and identify the observable state change that ends it.
5. Read [scene-authoring-checklist.md](references/scene-authoring-checklist.md) when creating a scene, chaining multiple systems, or transitioning between scenes.

## Authoring rules

- Sibling order is execution order. Each active direct child of a `StoryEvent` must hold exactly one `StoryStep`.
- Use direct serialized references for scene objects and controllers. Use IDs only where a cross-scene boundary prevents a direct reference.
- Reuse existing focused steps first: `DialogueStep`, `WaitStep`, `SetActiveStep`, `MoveActorStep`, `WorldModeStep`, `PuzzleStep`, `CombatStep`, and board transition steps.
- Add one small step only when the beat has a missing reusable action. Do not enlarge the core runner for a scene-specific convenience.
- Split `DialogueData` at any point where movement, a pause, audio, tile reveal, or another action must occur between lines. Do not hide staging inside the dialogue controller.
- Keep scene-authored puzzle layout as source of truth. Story may prepare, reveal, play, collapse, or hide the referenced puzzle; it must not regenerate the board.
- Keep one shared puzzle runtime, player, preview, and placed-path root per location. Production puzzle level objects contain content/configuration, not duplicate runtime systems.
- Use unscaled timing for story presentation that must continue while dialogue has paused gameplay time.
- Use the gameplay controller lifecycle to own input. Changing world presentation alone must not grant puzzle or combat input.
- Treat production scenes as production: remove stale `TEST_*` roots after their behavior is covered elsewhere, but preserve unrelated user work.

## Scene transitions

- Do not serialize a direct `StoryEvent` reference across scenes.
- End the source beat completely, hide gameplay UI, fade to an opaque overlay, then request the next scene through the project's scene-flow service.
- Begin the destination scene under an opaque overlay, normalize its authored starting state, and fade in before its first visible beat.
- Make the first destination event safe for direct scene testing when possible; clearly log a missing persistent service instead of failing silently.

## Narrative staging

- Prefer visible scene anchors and direct transforms over hardcoded vectors.
- Stage one readable action per step: reveal, move, react, wait, hide, then continue.
- Reuse `BoardTileTransitionStep` for scene-authored tile reveal/hide when its presentation matches; do not invent a second tween system.
- Placeholder art must be labeled visibly in hierarchy and documentation. Do not turn a placeholder's appearance into canon.
- Audio cues belong in their own step so dialogue timing and scene timing remain inspectable.

## Completion checks

- Verify direct-child order, active state, references, and unique `EventId` values.
- Verify cancellation cannot leave input claims, callbacks, dialogue, puzzle UI, or deferred scene transitions alive.
- Compile and read Console after every script batch.
- Run the production event from its preceding beat when possible, then test the destination scene directly.
- Confirm the scene is saved, present in Build Settings if loaded by name, and documented only after the implemented flow works.
