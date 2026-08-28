# Day 2 — School morning and classroom supplies

> Scene: `Assets/_Audere/Scenes/60_D2_School_Morning.unity`.
> Narrative status: implemented **Design Intent** requested by Xuân; this document does not promote the beat or placeholder artwork to cross-scene canon.

## Entry and encounter

`50_D2_Home_Morning / D2_TO_BUS_STOP` loads this scene after its opaque closing fade.

`D2_SCHOOL_BIANCA_MORNING` starts under cover, with Audere alone on an authored tile. `PuzzleViewportMask` is enabled from arrival and remains enabled through the initial Bianca dialogue, departure and classroom. After three seconds Bianca appears to the left, facing the same direction. Audere startles vertically, nudges toward her, and immediately withdraws. Timor notices. Bianca turns and approaches through authored reveal/hop/hide steps.

After answering Bianca's greeting, Audere turns right and leaves in the opposite direction. She does not cross Bianca. A scene-authored `PositionConstraint` begins following Audere horizontally, preserving the opening framing without copying hop height. Bianca fades in a separate presentation branch. The two last Audere–Timor exchanges run alongside tile traversal. After the final landing, follow releases and an authored `MoveActorStep` pans the camera for `0.8 s` to `Camera_ClassroomPose`, then holds `0.35 s` before the fade. Audere remains grounded on the last tile, now on the left of the frame.

## Classroom continuation

`D2_CLASSROOM_SUPPLIES` is the morning event's direct auto-next reference.

- Neutral fade out `0.32 s`, fade in `0.45 s`. Audere, her floor tile and the camera keep exactly the final hallway positions across the cut. Teacher/classroom presentation is staged under cover; the direct-entry normalization steps target the same ending pose rather than the opening camera pose.
- Teacher is on the right. Audere is on the left, facing her. Bianca's reveal tile is one tile behind Audere, with its full bounds inside the left mask edge.
- Teacher asks for paper and tape from the art storeroom. Bianca appears and volunteers, then faces toward the exit.
- Teacher asks for a partner. Timor suggests waiting for someone else. Audere hesitates, then the existing Scene 40 choice UI offers three ways to volunteer.
- Bianca turns back toward Audere after the selected spoken line. All branches join the same relieved reply.
- Timor remains silent for `3 s`. Audere keeps the decision small: `Tớ chỉ đi lấy đồ thôi.`

### Three convergent spoken answers

1. `…Em đi cùng Bianca ạ.`
2. `Em cầm giúp một ít ạ.`
3. `Để em đi cùng bạn ạ.`

The player chooses Audere's phrasing. All three accept the same task; no refusal, hidden route, or larger relationship consequence is implied.

## Cooperative return with supplies

The classroom now auto-continues into `D2_SCHOOL_COOP_01 → 02 → 03`, then `D2_SCHOOL_WRONG_SUPPLIES`. A neutral fade establishes that the supplies have been collected and are being carried back. Both actors share the path hand, but a path connects directly to whoever should move; there is no actor selection UI and no Retry button. Same-cell drops choose one unfinished actor randomly, once per drop.

Only ordinary tile visuals, shared red coloring, and A/B goal labels remain. Red tiles require the first carrier to hold while the other crosses, and disappear completely once both have entered and exited. A holder leaving too early also removes the stranded tile, since neither actor can enter it again. Falling or using the last path without completing the pair resets every tile and both carriers. Full layouts, solver proofs and exact two-actor Goal→Start continuity are in `Docs/Puzzles/Day2School/README.md`.

The current easier revision has **one shared red tile per board**, with four cards retained. Backspace can restart an active attempt. Entry uses a `0.30 s` fade out and `0.40 s` reveal; the only top objective is `Giúp Audere và Bianca lấy đồ về lớp`. The separate Supplies Return Board is hidden during COOP, then enabled by its original later StoryStep. No Bianca combat content is rebuilt by this revision.

Each puzzle interleaves a short practical encouragement after the second completed path. The arriving actor fades at their own goal; the other continues alone until both are finished. A covered fade restores both on the next board. The camera never follows either carrier while solving, and all three boards fit inside the puzzle mask. Both bodies separate slightly and use distinct draw order if they share a logical cell.

## Wrong supplies and Bianca combat opening

After the third puzzle a neutral fade stages Audere and Bianca facing one another on two separate authored tiles. Audere notices the wrong class label, startles vertically with a grounded shadow, and stops. Bianca only identifies the other box; she does not scold or diagnose Audere. Timor interprets the small mistake as having inconvenienced her, recalls his advice to remain in class, and asks what Bianca might now think. Bianca's only interruption is `Audere?`.

`220_EnterBiancaPressure` directly references the same shared `WorldTransition_DreamyDisorientation` profile used in Scene 40. The scene directly owns the Bianca enemy prefab under `Combat Root/CombatBoard/Enemy/Enemy Mount`; no runtime replacement actor is spawned. Story/puzzle visuals hide when Combat takes ownership.

**Design Intent / placeholder:** `CombatEncounter_D2_BIANCA_SUPPLIES_PLACEHOLDER` is a separate Bianca encounter, using the existing generic sample moveset (one 12-HP phase, 45 seconds, normal defeat/Retry). No hostile Bianca dialogue, forced defeat, new bespoke attack design, or narrative meaning of victory has been established. Victory returns to the two-tile story staging without inventing a subsequent conversation. Boss-specific tuning and the story after combat remain unresolved.

## Authoring contract

- Menu: `Audere/Story/Author Day 2 School Morning`.
- Actors, floor tiles, camera constraint, inactive staging anchors, choice UI and branch events are serialized in the scene. No production actor/tile/path generation is added.
- Main/classroom events are direct children of `STORY`; each executable direct child has one `StoryStep`.
- Actor body/shadow sorting remains `Player 5/4`. Motion preserves shadow projection, scale and color. Bianca's fade is an explicit separate presentation action.
- Existing dialogue wording and portrait overrides are preserved when the setup tool reruns.
- The scene-local choice view uses its direct gate during standalone play, or the retained `GameplayUIRoot` gate when the duplicate local UI root has been discarded during a scene load.
- Replaying the morning hides classroom presentation, disables camera follow and restores the hallway under cover.

## Verification — 2026-08-28

- Five focused checks passed across the final test runs: scene references/order, idempotent authoring and home link, three convergent choices and mask clearance, retained input gate cancel/replay, and parallel branch join/cancel/replay.
- Production playback ran from the school arrival through the classroom, then replayed the classroom for the other two answers. All three ended with the same grounded actor positions, hidden choice UI, no active input claims, no playing dialogue and time scale restored to 1.
- The motion sampler checked 308 active motion frames with no shadow projection/scale/color or underfoot-color failures. Camera sampling confirmed horizontal follow with zero sampled X offset/Y drift, in the original departure setup; the continuity revision below supersedes the covered camera reset.
- The three silence beats measured 3.00 seconds each. Fully typed long lines fit their bubbles; choice layout was visually checked at 1920×1080.
- Normal playback produced no Console error/warning entries. Play was stopped; the saved scene was clean afterward.

### Continuity and opening-mask revision — 2026-08-28

- `PuzzleViewportMask` is active in the saved scene and explicitly enabled by morning normalization, including replay after class.
- Three relevant EditMode tests passed: production references/order, authoring idempotence, and convergent choices with mask clearance measured at the classroom camera pose.
- Production playback sampled the final hold and classroom fade: actor world displacement, camera displacement and actor screen displacement were all zero across 161 samples. The last hallway floor tile and classroom floor tile share their exact pose.
- Screenshots at 1920×1080 confirm the initial Bianca greeting is masked and her rear classroom tile remains fully visible. The camera reframes before the fade; no actor/tile/anchor runtime generation or new runtime script was added.
- The revised full playback completed all three answers with zero failures: 341 sampled motion frames, 1,632 follow samples with zero X/Y error, and 16,881 samples with the mask enabled. Choice/input/dialogue cleanup completed after every branch.
- A separate camera cancellation check stopped the visible reframe at 0.30 s, verified no subsequent movement, replayed it to its target, and replayed morning normalization to restore the opening camera/Audere poses with the mask enabled. No failures remained.


### Cooperative puzzle and red-tile verification — 2026-08-28

The two-red layout counts below are historical; the later one-red revision and its verification are documented in `Docs/Puzzles/Day2School/README.md`.

- Final focused suite: 12/12 School + Day2Home tests passed after the shared Editor compile/batch conflict was resolved. Tests cover direct references, choice cancellation, fully hidden red tiles, restoration of authored renderer state, held/shared red cells, individual arrival fade/lock, falling and out-of-pieces auto-reset.
- Serialized board solver reports 2, 1 and 1 solutions. Each consumes all four cards and requires both actors to cross both red tiles.
- Boards are 4×3, 5×3 and 5×3. Conservative mask measurement includes full hop height, sprite stretch, landing width and shared-cell offsets: minimum clearance 0.0597 world units (about 25.8 pixels at 1080p with orthographic size 1.25). Visual QA targets 16:9; other aspect ratios have not been visually checked in this pass.
- School WorldModeController disables child-fade fallback. Runtime inspection confirms it no longer auto-binds Combat's Stun Zone CanvasGroup; authored Story fade steps remain the sole neutral-cover owners.
- Before the latest immediate-hide revision, full classroom → three boards → wrong-supplies dialogue → Bianca combat playback passed. Generic placeholder combat defeat/Retry, cancel cleanup and victory callbacks also passed; the separate Bianca combat authoring task owns subsequent boss changes.
- After the immediate-hide revision, actual preview/drop playback completed all 12 placements across all three boards. Both actors faded at their own goals, every hand was empty, spent red renderers were disabled with alpha zero, and cameras stayed fixed. Final Play Console had 0 errors; scene60 was left stopped, saved, playOnStart=true, with 0 missing scripts/prefabs. The reported cannot-win case was not reproduced on these routes; its exact board/state still needs clarification.

### Bianca supplies combat and return — 2026-08-28

**Design Intent:** the boss speaks Audere's feared interpretation, then the real Bianca answers a small direct question. This does not establish that Bianca secretly judges Audere.

- `D2_SCHOOL_WRONG_SUPPLIES` now binds the dedicated 10HP/90TIME encounter, Wrong Box at 6HP, returning bullets at 2HP, and the supplied creepy portrait/aseprite art. Mechanics are described in Docs/06_CombatGameplay.md.
- Under the post-victory cover, the existing return anchors put Audere at x=-0.25 and Bianca at x=0.25 on their respective adjacent tiles. Their authored facing steps point toward one another. No new board/actor generation is used at runtime.
- Four scene-first dialogue assets cover Bianca checking on Audere, Timor discouraging the question, Audere asking, and “...Cậu cũng đâu biết.” The user's meaning is preserved; speech beats are split to at most 42 characters and use existing portraits. The sequence ends in a 0.9-second neutral fade.
- The dedicated author preserves puzzle/BGM wiring and `allowChildFadeFallback=false`. A Play-discovered inactive `CombatBoard` override was corrected; Combat Root continues to control visibility.

**Verification:** instrumented Play used actual cursor catch/reroll/choice handlers with the natural RNG and timer, plus accelerated Story dialogue. The successful attempt performed 27 catches, 6 rerolls, Wrong Box success → failure → success, all five phases, and returning waves 1/2/3. Retry restored 10HP/90TIME; enemy fade was sampled; the full StoryEvent reached Completed. Cleanup readback: no active bullets, no playing combat/dialogue/Retry, TIME scale 1, Battle Box width restored to 1, final cover alpha 1. Separate portrait playback observed both creepy/normal sprites, an active glitch, and a settled transform. Screenshots/logs are in `Temp/BiancaQA`.

Visual QA was at 1920×1080 (16:9). Full manual balancing and 4:3/ultrawide visual playthroughs were not performed.

Final EditMode regression: 73/73 passed (CombatEnemyRuntimeTests, EveningNightPressureTests, MusicPresentationTests); scene validation found 0 missing scripts and 0 broken prefabs.

The new mechanic-hint rectangle also passed camera viewport containment checks at 16:9, 4:3 and 21:9; this is a geometry check, not an additional visual playthrough.

### School closure → home → Dream — 2026-08-28

The previous ending fade now follows two additional conversations: preparations are done today; tomorrow Bianca and Audere will decorate the board and prepare with the class. Bianca turns right, `School_Bell` plays, the existing 0.9-second fade covers departure, and `360_GoHomeAfterBell` loads `70_D2_Home_Night`. Earlier combat/puzzle/post-combat steps remain unchanged. The continuation and its separate 5/5 focused QA are documented in [Day2 Night/Dream workflow](14_Day2_NightDream_StoryWorkflow.md); this does not constitute a fresh full combat regression.
