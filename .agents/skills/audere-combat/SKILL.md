---
name: audere-combat
description: Author, revise, or review Audere combat code and content in Unity, including encounters, enemy definitions and prefabs, phases, moves, dice, bullets, lasers, Stun Zones, Battle Box behavior, tutorial, Retry UI, and scene bindings. Use for any combat-system or combat-authoring change; do not use for dialogue wording alone or non-combat actor staging.
---

# Audere Combat

Use this skill to keep combat data-driven, scene-editable, cancellable, and reusable across enemies.

## Read before acting

- Read [references/architecture-and-lifecycle.md](references/architecture-and-lifecycle.md) for runtime, ownership, phase policy, pause, result, Retry, or cancellation work.
- Read [references/authoring-contract.md](references/authoring-contract.md) for encounter, enemy, prefab, phase, moveset, scene, or authoring-tool work.
- Read [references/moves-and-mechanics.md](references/moves-and-mechanics.md) for bullets, lasers, Battle Box transforms, Stun Zones, player constraints, or new attack patterns.
- Read [references/tutorial-dialogue-and-ui.md](references/tutorial-dialogue-and-ui.md) for tutorial, combat cue, DialogueUI, background text, or Retry UI work.
- Read [references/verification.md](references/verification.md) before reporting any combat task complete.

Read only the references relevant to the current request, except verification, which is always required before handoff.

## Required workflow

1. Inspect `git status` and the relevant diff. Preserve existing Shield, dice, board sizing, scene, prefab, and balance edits; never reset or regenerate broad assets to simplify the task.
2. Read the current implementation and authored assets before designing a change. Treat documentation as a contract to verify against code, not a substitute for inspection.
3. Use the Unity MCP operator workflow for Unity authoring: check editor state/custom tools, stop Play Mode before asset changes, prefer idempotent authoring tools and direct serialized references, compile, inspect Console, and run tests.
4. Keep authored data immutable. Runtime state belongs to the session execution, enemy runtime, board view, or controller owner.
5. Do not branch on enemy ID for mechanics, patterns, outcome, Retry, dialogue, or presentation. Add reusable data/policy/move/module contracts instead.
6. Preserve `CombatController.Play(...)` compatibility unless Xuân explicitly requests an API migration.
7. Make every move and mechanic stop deterministically on phase break, dialogue ownership change, Victory, Defeat, Retry, cancel, disable, destroy, and scene unload.
8. Update docs only after implementation and validation reflect the actual production flow.

## Cross-skill routing

- If combat text, barks, tutorial wording, DialogueData, portrait behavior, or character interaction changes, also use `audere-dialogue-voice` before editing content.
- If Story/Puzzle/Combat presentation changes through a fullscreen or world-mode transition, also use `audere-world-transition-authoring`.
- If a story actor moves, turns, hops, reacts, or changes grounded shadow outside combat-enemy mechanics, also use `audere-actor-staging`.
- If production StoryEvent hierarchy or story hand-off changes, also use `audere-story-scene-builder`.

Do not promote placeholder mechanics, art, names, balance, interpretation, or sample content to canon. Preserve `Established Canon`, `Strongly Implied`, `Design Intent`, and `Unresolved` labels.
