# Audere World Transition Catalog

This catalog is the maintained source for choosing a transition and locating every accepted
production consumer. Update it whenever a profile or scene assignment changes.

## Selection table

| Transition | Use when | Avoid when | Implementation |
| --- | --- | --- | --- |
| Neutral fade | Clean return, ordinary mode hand-off, time/location change without subjective emphasis. | The beat must communicate Audere losing spatial stability. | Existing `WorldModeStep` fade; no fullscreen profile. |
| Dreamy Disorientation | Audere briefly loses balance or spatial certainty: gentle tilt, small zoom, broad water-like warp, drift, radial bend and smear around her. | Neutral travel, celebratory cuts, digital/system failure, or a scene that should read as VHS/static. | `WorldTransition_DreamyDisorientation.asset`; focus renderer required. |

The former glitch/static direction is not an active catalog entry. Do not reintroduce static
noise or VHS tearing into `Dreamy Disorientation`; create a separately justified profile if a
future beat genuinely needs digital signal failure.

## Active profile: Dreamy Disorientation

- Asset: `Assets/_Audere/Data/Transitions/WorldTransition_DreamyDisorientation.asset`
- Material: `Assets/_Audere/Materials/PostProcess/FullscreenDreamyDisorientation.mat`
- Shader: `Assets/_Audere/Shaders/FullscreenDreamyDisorientation.shader`
- Duration: `1.50 s`
- Mode swap: `1.10 s`
- Focus: renderer bounds center, aspect-correct.
- Visual order: subtle tilt/zoom → wide wave and UV drift → radial bend/smear → dark dreamy
  veil at swap → target presentation appears through decreasing distortion → clean target.
- Audio: `Unresolved`; this profile currently owns no low-pass or cue.

## Production usage registry

| Scene | Story step | Source → target | Transition | Focus | Narrative status |
| --- | --- | --- | --- | --- | --- |
| `30_Classroom` | `D1_CLASSROOM_RECESS_BIANCA/200_ClassroomIsConsumed` | Story → Combat | Dreamy Disorientation | Audere `SpriteRenderer` | Technical presentation is implemented; enemy, combat meaning and outcome remain `Unresolved`. |

`30_Classroom/220_ReturnToStory` continues to use Neutral fade. Scene `20_Game` does not
consume the fullscreen profile and must remain unaffected while the feature is idle.

## Adding a catalog entry

Record the profile asset, material, shader, duration, swap time, focus requirement, visual
order, intended use, exclusions and every accepted scene consumer. Prefer one shared profile
for several scenes that should change together; create a variant only when those scenes need
independent future tuning.
