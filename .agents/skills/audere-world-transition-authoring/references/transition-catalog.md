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
| `40_Evening` | `D1_HOME_NIGHT_MESSAGE/90_EnterNightPressure` | Story → Combat | Dreamy Disorientation | Audere `SpriteRenderer` | Implemented presentation for the Timor night-pressure Design Intent; combat ontology remains `Unresolved`. |

| `60_D2_School_Morning` | `D2_SCHOOL_WRONG_SUPPLIES/220_EnterBiancaPressure` | Story → Combat | Dreamy Disorientation | Audere `SpriteRenderer` | Design Intent: wrong supplies trigger Timor pressure; Bianca encounter moves/outcome remain placeholder. |

`30_Classroom/220_ReturnToStory` continues to use Neutral fade. Scene `20_D1_Home_Morning` does not
consume the fullscreen profile and must remain unaffected while the feature is idle.
`40_Evening/110_ReturnToEvening` also uses Neutral fade after the required Defeat; no reverse
fullscreen profile or scene-local transition timeline is authored.

### Day 2 night / Dream consumers

These are neutral **scene-load covers**, using the existing CanvasFadeStep/SceneLoadStep contract rather than a new fullscreen profile or duplicated shader timeline.

| Source step | Target | Cover / destination reveal | Status |
| --- | --- | --- | --- |
| `60_D2_School_Morning/D2_SCHOOL_WRONG_SUPPLIES/350_FadeOutAfterAnswer` | `70_D2_Home_Night` | 0.9 s / 0.65 s | School bell before departure; Design Intent. |
| `70_D2_Home_Night/D2_HOME_NIGHT_DOUBT/140_FadeToSleep` | `80_D2_Dream` | 1.35 s / 0.9 s | Sleep → dream; Design Intent. |
| `80_D2_Dream/D2_DREAM_ONLY_ME/320_FadeOutOfDream` | `90_D2_Home_Awakening` | 0.85 s / 0.35 s | Wake startled; next authored beat ends Day2 and loads Day3. |

Destination covers start active at alpha 1; shared BGM fade hooks remain in use. Dream RGB tile fringes/text distortion are environmental presentation, not modifications to Dreamy Disorientation. See `Docs/14_Day2_NightDream_StoryWorkflow.md` for QA and authoring ownership.

## Day 3 consumers and presentation-only profile

- `90_D2_Home_Awakening/040_FadeOutDayTwo` → black end-Day2 title → `100_D3_Home_Morning`: neutral scene cover.
- `100_D3_Home_Morning/060_FadeToSchool` → `110_D3_School_Board`: neutral0.9s/reveal0.6s, shared SchoolBell before fade.
- `110_D3_School_Board/170_TheRoomDriftsWhileBiancaCalls/WorldSway`: **Fatigue Sway**, `Assets/_Audere/Data/Transitions/WorldTransition_FatigueSway.asset`, ID `fatigue-sway`. Same Dreamy material/shader, independent shared profile, duration4.4s; tiny tilt/wave/zoom/drift/smear, no black veil. Focus Audere; visual-only `FullscreenPresentationStep`/`PlayPresentation`, **no mode swap or music ownership**. Parallel PlayAuto calls, no Dialogue input claim. Do not use this as a concealed mode swap.
- `110_D3_School_Board/180_FadeToTheTeacher` → `120_D3_School_Teacher`: neutral0.7s/reveal0.6s.
- `120_D3_School_Teacher/080_EnterTeacherPressure`: shared **Dreamy Disorientation**, Story→Combat, focus Audere. Return uses neutral cover/WorldModeStep/reveal. Teacher encounter/interpretation is Design Intent, not proof of a hostile teacher.

See `Docs/15_Day3_BoardTeacher_StoryWorkflow.md` for bindings and QA. Keep profile curves in the asset, not in scene/runtime code. Presentation cancellation must restore material, disable feature and resolve callback once.

## Adding a catalog entry

Record the profile asset, material, shader, duration, swap time, focus requirement, visual
order, intended use, exclusions and every accepted scene consumer. Prefer one shared profile
for several scenes that should change together; create a variant only when those scenes need
independent future tuning.
