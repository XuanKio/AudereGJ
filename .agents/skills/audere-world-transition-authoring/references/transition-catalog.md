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

## Dream Fracture — Scene80

- **Design Intent requested by Xuân:** an ordinary tile conversation fractures into the existing dream. This is not a claim about Bianca's real thoughts.
- Profile `Assets/_Audere/Data/Transitions/WorldTransition_DreamFracture.asset`, material `Assets/_Audere/Materials/PostProcess/FullscreenDreamFracture.mat`, shader `Assets/_Audere/Shaders/FullscreenDreamFracture.shader`.
- Duration **6.95 s**, scenery swap **4.8 s** beneath the frozen pane; no focus renderer required.
- Shake → freeze the source frame at **0.8 s** → cracks crawl from the top/left/right edges inward in pulses, then branch back out toward the corners → actual polygon pieces burst/rotate/fall from **4.8 s**, immediately revealing the walking puzzle behind them → pieces clear by **6.85 s**. No black interlude.
- Reference direction: [Code Monkey, How to BREAK your Screen!](https://unitycodemonkey.com/video.php?v=RP1-PZD4Ab4). Independently implemented screenshot geometry, not a fixed-region UV distortion. `ScreenShatterGraphic` draws clipped radial shards with thickness, perspective and gravity; shared profile owns all settings. Snapshot/overlay are runtime-only and do not modify actor sorting.
- Consumer: `80_D2_Dream/D2_DREAM_ONLY_ME/019_TheClassroomFractures`, `FullscreenPresentationStep`. Authored enable/disable references swap under the intact screenshot; `revealTargetBehindShards` exposes the dream through the separating pieces. Cool charcoal/violet faces, darker backs and restrained rim highlights remain profile settings. No world-mode change or music duck/cue is added.
- Use for explicit glass/fracture perception changes, not ordinary travel or mild fatigue. Keep Dreamy Disorientation and Fatigue Sway unchanged.
- Cancellation restores source root states, disables the shared feature, and destroys the snapshot/overlay; later puzzle input is owned by the following PuzzleStep.

## Adding a catalog entry

Record the profile asset, material, shader, duration, swap time, focus requirement, visual
order, intended use, exclusions and every accepted scene consumer. Prefer one shared profile
for several scenes that should change together; create a variant only when those scenes need
independent future tuning.

## Day4 Crowd classroom

- `140_D4_Classroom/D4_CLASSROOM_CROWD/070_TheRoomBecomesPressure`: shared Dreamy Disorientation, Story→Combat, focus Audere lying renderer. Profile unchanged.
- `090_TheNoiseFallsAway` neutral0.9s cover → `100_BackInTheClassroom` Story → Bianca placed under cover → reveal0.9s.
- `200_EveningCover` neutral1.15s → `150_D4_Home_Evening` covered arrival/reveal1.2s. Evening opening continues with Timor Shadow below. See Docs17.

## Timor Shadow Encroachment — Scene150

- **Design Intent:** Timor's fear of becoming unnecessary fills Audere's quiet room; source Story → target Combat. Do not use for neutral travel or mild fatigue.
- Profile `Assets/_Audere/Data/Transitions/WorldTransition_TimorShadow.asset`; material `Assets/_Audere/Materials/PostProcess/FullscreenTimorShadow.mat`; shader `Assets/_Audere/Shaders/FullscreenTimorShadow.shader`.
- Consumer: `150_D4_Home_Evening/D4_EVENING_TIMOR_RETURNS/150_HisShadowFillsTheRoom`, direct `FullscreenWorldModeTransitionStep`. No focus renderer required; aspect-correct silhouette grows from the right using existing `Enemyy/timor.png` alpha, with slight shape drift and dark indigo details.
- Duration5.4s; opaque cover held4.0–4.4s; mode swap4.2s; shadow/cover clear5.4s. Only the following CombatStep owns combat input. Shared transition controller retains existing music duck/reveal behavior; no new sound or low-pass logic.
- Cancellation before/after swap restores Story and PuzzleViewportMask, disables feature and destroys material. Scene40 and other profiles unchanged. Final encounter outcome remains Unresolved; current re-entry preserves Scene40 rules until Xuân chooses.
- Verified production reveal, opaque swap, clean Timor target and both cancellation sides; `Temp/Day4Timor/tests_4_pass.xml` and GameView screenshots. See Docs17 for narrative/authoring limits.
