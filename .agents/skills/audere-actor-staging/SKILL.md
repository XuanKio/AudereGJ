---
name: audere-actor-staging
description: Author, revise, or review Audere character staging and motion in Unity, including grounded shadows, movement anchors, hops, startle reactions, facing, timing, cancellation, and scene-first StoryStep setup. Use whenever an actor is moved, posed, revealed, hidden, or given an in-scene reaction; do not use for combat-enemy mechanics or dialogue wording alone.
---

# Audere Actor Staging

Stage actors as readable story performances while keeping their world position, body motion, and ground shadow logically separate. Work scene-first with direct references and the existing focused StorySteps.

## Required reading by task

- Always read [actor-contract.md](references/actor-contract.md) before changing an actor prefab, reference, pivot, scale, renderer, or shadow.
- Read [motion-language.md](references/motion-language.md) before authoring or tuning a movement, hop, startle, recoil, hesitation, landing, or facing change.
- Read [story-authoring.md](references/story-authoring.md) when placing actor actions inside a production `StoryEvent` or an editor setup tool.
- Read [verification.md](references/verification.md) before declaring actor work complete.

## Core invariants

- `Actor` represents the character's authored world pose. `Target Transform` represents the destination or ground pose; do not hardcode destination vectors in a StoryStep.
- A hop has two simultaneous paths: the actor follows the visible arc, while the grounded shadow follows only the ground projection. An in-place reaction must leave the shadow at the same world pose.
- Assign `CharacterMotionStep.Grounded Shadow` directly in production scenes. Runtime name fallback exists only for legacy compatibility.
- Every story/puzzle actor body renderer uses Sorting Layer `Player`, Order in Layer `5`;
  its grounded shadow renderer uses the same Sorting Layer `Player`, Order in Layer `4`.
- The shadow must not inherit actor hop height, landing squash, horizontal flip, or cancellation residue. Keep its authored world rotation, scale, color, alpha, and material unchanged during actor staging.
- Actor staging must not recolor, tint, fade, highlight, or otherwise restyle the tile beneath an actor. The shadow and floor tile remain visually basic and authored; communicate reactions through body position, facing, arc, and timing.
- If Xuân explicitly requests a separate shadow or environment visual effect, author it as its own presentation action with clear ownership. Never hide that effect inside an actor movement or reaction step.
- Use `MoveActorStep` for plain blocking or hidden normalization. Use `CharacterMotionStep` for a visible hop or reaction. Puzzle traversal remains owned by `GridPlayer`.
- A startle is primarily vertical and local. Do not express surprise by sliding the actor sideways unless the script explicitly calls for a physical step back.
- Cancellation must remove temporary lift and squash, stop at the current grounded projection, and never teleport back unless the requested action explicitly defines rollback.
- Use unscaled time for story staging that must continue while dialogue or world presentation has paused gameplay time.
- Do not put actor motion inside `DialogueController`. Split DialogueSteps around visible actions so hierarchy order remains the readable performance timeline.

## Working procedure

1. Inspect the actor hierarchy, renderer orientation, grounded shadow, current world scale, target anchor, adjacent StorySteps, and whether another system owns the actor.
2. State the action's dramatic verb: approach, hesitate, startle, recoil, settle, leave, or similar. Select motion mode and timing from `motion-language.md` based on that verb.
3. Bind direct scene references. Confirm the target is an authored staging anchor rather than a visible tile or temporary runtime object unless that ownership is intentional.
4. Author one readable action per StoryStep. Add small waits only where a held pose or audience comprehension beat needs them.
5. Verify the actor and shadow throughout the motion, then test completion, cancellation, replay, inactive/active normalization, and scene save state using `verification.md`.

## Coordination boundaries

- For production StoryEvent structure, also follow `../audere-story-scene-builder/SKILL.md`.
- For dialogue or character-interaction wording, also follow `../audere-dialogue-voice/SKILL.md`; this skill governs performance, not voice.
- For fullscreen/world-mode transitions, also follow `../audere-world-transition-authoring/SKILL.md`.
- Do not infer a character emotion, relationship change, or canon fact merely from a placeholder animation. Preserve the project's canon classification rules.
