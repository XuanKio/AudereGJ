---
name: audere-world-transition-authoring
description: Select, author, revise, and propagate shared fullscreen world-mode transitions in Audere Unity scenes. Use when a Story beat changes Puzzle, Combat, Story, or scene presentation through a transition; do not use for ordinary actor motion or dialogue-only timing.
---

# Audere World Transition Authoring

Author transitions through shared profile assets so scenes select a visual language without
copying its timeline. A later profile edit must propagate to every scene that references it.

## Before changing a transition

1. Read [transition-catalog.md](references/transition-catalog.md) completely.
2. Read `Docs/11_FullscreenWorldTransitions.md` for the runtime and cancellation contract.
3. For a production Story hierarchy, also use `../audere-story-scene-builder/SKILL.md` and
   its scene-authoring checklist.
4. State the transition's story job, source presentation, target presentation, and observable
   end state. Keep narrative meaning labeled as `Established Canon`, `Strongly Implied`,
   `Design Intent`, or `Unresolved`.

## Selection rules

- Reuse an existing catalog entry when its emotional and spatial job matches the beat.
- Use neutral fade for a clean location/time/mode hand-off that should not imply subjective
  distress.
- Use a subjective fullscreen profile only when the player should feel Audere's perception
  changing; do not infer that meaning from a gameplay mode alone.
- Create a new profile only when the beat needs a materially different visual grammar. A
  strength or timing adjustment belongs in the existing shared profile when every current
  consumer should receive it.
- Audio ducking, low-pass and cues are separate contracts. Do not add them merely because a
  visual profile suggests them.

## Shared-profile contract

- Scene steps reference one `FullscreenTransitionProfile` asset directly.
- Duration, mode-swap time, material and shader float curves live in the profile, never in a
  scene component or scene-specific coroutine.
- `FullscreenTransitionController` owns the single inactive URP fullscreen renderer feature,
  clones the selected material at runtime and destroys the clone on complete/cancel.
- A profile that sets `UsesFocusRenderer` requires the scene step to reference the visible
  actor renderer directly. Profiles without that requirement use viewport center.
- `FullscreenWorldModeTransitionStep` changes presentation only. The following gameplay step
  remains the sole owner of Puzzle/Combat input.
- Cancel before or after the swap restores `sourceMode`, disables the renderer feature and
  leaves no runtime material state.

## Propagating changes

- To retune an existing transition everywhere, edit its profile asset or its shared material/
  shader. Do not visit and rewrite each scene.
- When shader property names change, update every profile using that material and the editor
  setup that creates those tracks in the same change.
- Search all `.unity`, prefab and profile assets for the profile GUID before removing or
  replacing it. Update the usage registry in the catalog after scene authoring is accepted.
- Scene setup tools may bind a shared profile but must not reconstruct a private copy for each
  scene.

## QA

- Compile C# and shader with zero errors; validate the scene for missing scripts/references.
- Capture readable early, peak, swap and clean-target frames at the target aspect ratio.
- Verify the focus stays on the authored renderer where required.
- At mode swap, the source/target cut must be hidden by the profile's intended cover state.
- Verify the next gameplay step has not claimed input before the fullscreen step completes.
- Cancel once before and once after mode swap; replay from a clean profile state.
- Confirm the renderer feature is inactive while idle and in unrelated scenes.
