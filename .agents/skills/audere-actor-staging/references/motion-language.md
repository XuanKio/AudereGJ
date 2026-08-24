# Motion language

Choose motion from the dramatic verb, not from a generic desire to make the scene less static. Values below are production starting ranges in current world units; tune against the active camera and actor scale.

## Motion types

| Intent | Component/mode | Typical duration | Typical arc | Facing | Notes |
|---|---|---:|---:|---|---|
| Hidden normalization or instant placement | `MoveActorStep`, duration `0` | `0` | none | separate | Perform under opaque fade or before reveal. |
| Quiet nudge/lean in blocking | `MoveActorStep` | `0.12–0.25s` | none | Preserve/explicit | Use a short distance; this is not a hop. |
| Friendly approach across tiles | `CharacterMotionStep.TravelToTarget` | `0.28–0.38s` per beat | `0.05–0.08` | Follow travel | One readable hop per staging anchor. |
| Small positive acknowledgement | `VerticalInPlace` | `0.16–0.22s` | `0.03–0.05` | Preserve | Light body lift; avoid comic squash. |
| Startle/surprise | `VerticalInPlace` | `0.16–0.22s` | `0.06–0.10` | Explicit after/with beat | Sharp takeoff, local reaction, fixed shadow. |
| Heavy recoil or physical step back | Separate authored target plus travel | `0.20–0.35s` | context dependent | Away from source | Use only when the script calls for actual displacement. |
| Settle after tension | Wait or very small in-place response | `0.20–0.45s` | `0–0.025` | Preserve | Often a held pause reads better than extra motion. |

Current scene benchmarks:

- Bianca travel hops: `0.32s`, arc `0.075`.
- Audere classroom startle: `0.19s`, arc `0.09`, `VerticalInPlace`, turns toward Bianca.
- Audere small interest/attention hop: `0.18s`, arc `0.045`.
- Shared landing response: around `0.10s`, squash `0.105`, widen `0.075`.

Treat these as scale references, not universal constants.

## Startle recipe

A readable Audere startle is restrained and anxious rather than slapstick:

1. Hold the preceding silence or interruption long enough for the player to register the cause.
2. Use `VerticalInPlace`; do not introduce horizontal displacement.
3. Use a short duration and comparatively clear lift. The upward motion should dominate over squash.
4. Keep `Grounded Shadow` fixed in world position, scale, and rotation.
5. Turn toward the speaker only if the beat calls for recognition. Avoid a flip-flip sequence.
6. Land cleanly, then leave a short comprehension beat before the next dialogue if needed.

Keep the shadow and the tile under the actor at their authored colors throughout the reaction. Use lift, timing, facing, and the actor's body pose—not tint or tile highlighting—to make the startle readable.

Avoid these failure modes:

- sliding right/left as the only sign of surprise;
- moving the shadow upward with the body;
- shaking for so long that the actor appears electrocuted;
- a huge bounce that makes anxiety comedic;
- combining hop, turn, tile reveal, and dialogue advancement in one opaque action;
- tinting or fading the shadow or the tile beneath the actor as part of the reaction;
- ending cancellation while the actor is still above its ground projection.

## Hesitation and anxiety

Audere's anxiety should usually read through timing and incomplete commitment:

- a small forward nudge followed by a hold;
- a delayed facing change;
- a modest in-place lift or body compression;
- beginning movement only after another character reassures or directs her;
- returning to the same ground anchor when the script explicitly describes withdrawal.

Do not encode a diagnosis through constant trembling. Repeated shake effects become visual noise and flatten emotional specificity.

## Approach and social distance

- Use intermediate anchors when a full displacement would cross several readable story spaces.
- Reveal the next floor/tile cue before movement when the environment is part of the thought process.
- Let the approaching character stop outside Audere's personal space unless the story explicitly calls for a touch or intrusion.
- A final small `MoveActorStep` nudge may communicate leaning closer without another full hop.
- After reaching a target, do not immediately stack another motion unless the performance is intentionally urgent.

## Curves and body feel

`CharacterMotionStep` currently uses smoother-step ground travel, a sine vertical arc, light travel stretch, and a short landing squash. Preserve this shared language unless a different physical intent justifies a new focused mode.

- Increase duration before increasing arc for a gentler movement.
- Increase arc before squash for a sharper startle.
- Keep landing response shorter than the main motion.
- Large stretch plus large squash reads rubbery; do not max both.
- Test at actual game resolution. Small world-unit changes can be visually large after camera scaling.
