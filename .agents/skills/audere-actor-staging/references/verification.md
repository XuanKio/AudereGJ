# Actor staging verification

Run the checks relevant to the changed actor action. Do not rely on the final frame alone.

## References and hierarchy

- Actor, Target Transform, Actor Renderer, and Grounded Shadow are direct serialized references.
- Actor renderer is on Sorting Layer `Player`, Order `5`; grounded shadow is on `Player`, Order `4`.
- The target is an authored scene anchor with the expected baseline and Z plane.
- No duplicate motion owner is active on the actor.
- The StoryStep is a direct child in the intended sibling order and is active when it should run.

## Motion frames

Observe at least takeoff, apex, landing, and settled pose:

- Actor follows the intended path and never teleports at start.
- In-place startle has no unexplained horizontal displacement.
- Travel direction and `flipX` are correct.
- Shadow follows only the ground projection.
- Shadow world Y does not rise with the hop.
- Shadow does not stretch or squash with the actor.
- Shadow color, alpha, and material remain exactly as authored.
- The tile beneath the actor keeps its authored color, alpha, and material throughout the action.
- Landing restores the actor's exact starting scale.
- Target movement during the step behaves as intended for current StoryStep semantics.

## Lifecycle

- Completion fires once and the following StoryStep starts only after landing settles.
- Cancel at or near apex removes temporary lift and leaves the actor grounded at its current progress.
- Disable during motion does not leave actor or shadow in a temporary pose.
- Replay does not reuse stale coroutine state.
- Cancel, disable, and replay do not leave any shadow or underfoot-tile visual override behind.
- Hidden normalization and fade ordering do not expose teleport frames.

## Context and presentation

- Check at the real game resolution and with dialogue UI visible where applicable.
- Ensure tile reveals, dialogue, and actor motion do not compete for attention in the same instant unless intentionally urgent.
- Startle intensity matches the cause and character; Audere should read as startled/anxious, not comedic.
- Placeholder art or animation is not documented as canon.

## Technical verification

- Validate edited scripts and wait for Unity compilation/domain reload.
- Console has no new errors from normal playback.
- Save the production scene/prefab and confirm serialized shadow references remain after reload.
- If a setup tool was changed, rerun it on the intended scene and verify it does not duplicate or erase unrelated content.
