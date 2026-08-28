# Day 2 — cooperative supplies puzzles

**Design Intent requested by Xuân.** Serialized production content in `60_D2_School_Morning`, not a new canon claim.

## Current rules

- The scene owns both actors, every tile, both starts, A/B destinations and camera anchors. One shared hand and existing path runtime serve all three boards. No board/actor is generated during play.
- There is no actor selector, Tab switching, or Retry button. Connect a path endpoint to an unfinished actor's cell to move that actor. If both actors share the starting cell, the drop rolls once between them; preview does not consume randomness. Finished actors cannot receive paths.
- A shared red tile allows one entry per actor. Its first visitor must remain there for the second visitor to enter. After both have entered and both have exited, it disappears immediately. Leaving before the second arrives strands that route and also hides the now-unusable tile. Both cooperative red tiles and the morning's single-actor `OneUseTileBehaviour` disable their renderers entirely, with no faded remnant. Reset restores every authored renderer.
- Both actors may share a cell: Audere shifts left/up, Bianca right/down by small presentation offsets; logical coordinates stay identical. The Player/Bianca prefab root has a `SortingGroup` on layer Player. During COOP the whole upper actor sorts at 5, the lower actor at 6, using grounded foot Y (not the hop arc or sprite pivot). Equal rows keep Bianca in front. Body/shadow orders inside each group stay 5/4. Ending/resetting an attempt restores the authored group order.
- Only basic tile visuals and red color are used. The only tile labels are A/B destinations. There are no plate numbers, bridge bars, auxiliary icons or Retry buttons.
- Exhausting all pieces before both finish, reaching both goals with spare pieces, or falling resets the whole attempt: both actors, all tiles/colors/scales, shared-red visit/occupancy flags, arrival fades and the complete hand. Backspace also resets an active attempt; there is still no separate Retry button.
- Each actor fades independently at their own goal and is locked there. Both arrivals plus an empty hand complete the puzzle. A neutral fullscreen fade introduces the next board and restores both actors at its authored starts. Both actors remain the same scene instances.
- Camera stays fixed while solving. Each board fits `PuzzleViewportMask`; camera framing changes only during covered inter-board staging.

## Authored layouts

All coordinates below are local zero-based `(x,y)`; `y=0` is the lowest row. The JSON `*.scene_export.json` files were exported from the serialized tiles, references and path assets, then rechecked by `Tools/PuzzleValidation/cooperative_step_tile_solver.py`.

| Board | Size | Audere start → goal | Bianca start → goal | Shared red cells | Piece multiset | Solutions |
| --- | --- | --- | --- | --- | --- | --- |
| 01 | 4×3 | (0,1) → (3,1) | (1,0) → (2,2) | (1,1) | Line2 ×2, LCorner3, Line3 | 8 |
| 02 | 5×3 | (1,0) → (2,2) | (0,1) → (4,1) | (1,1) | Line2 ×2, LCorner3, Line4 | 8 |
| 03 | 5×3 | (0,1) → (4,1) | (2,0) → (3,2) | (2,1) | Line3 ×2, LCorner3, Line2 | 6 |

The simplified boards require only one shared-red hold each. Every solution uses all four cards and finishes both actors; no automatic victory or bypass is used. Cards remain in intended-route order in the hand. Counts deduplicate interchangeable cards, not actor order or distinct paths. The second former red tile is ordinary grass, allowing more flexibility after the initial crossing.

### Intended routes

1. **01:** A `(0,1)→(1,1)`; B `(1,0)→(1,1)→(2,1)`; A `(1,1)→(2,1)→(3,1)`; B `(2,1)→(2,2)`.
2. **02:** B `(0,1)→(1,1)`; A `(1,0)→(1,1)→(2,1)`; B `(1,1)→(2,1)→(3,1)→(4,1)`; A `(2,1)→(2,2)`.
3. **03:** A `(0,1)→(1,1)→(2,1)`; B `(2,0)→(2,1)→(3,1)`; A `(2,1)→(3,1)→(4,1)`; B `(3,1)→(3,2)`.

Wrong early departures still strand the partner at the single red crossing. Backspace provides a way to restart immediately without waiting to run out of cards. This is an easier puzzle, not a promise that every arbitrary move wins.

### Both-actor continuity

The authoring tool rejects a nonzero previous-Goal/next-Start delta for either actor. With the current SCHOOL transform:

| Cut | Audere Goal = next Start | Bianca Goal = next Start | Position delta |
| --- | --- | --- | --- |
| 01 → 02 | (0.125, −0.125, 0) | (−0.125, 0.125, 0) | 0 for both |
| 02 → 03 | (0.375, 0.375, 0) | (0.875, 0.125, 0) | 0 for both |

Each next start tile is enabled before the remaining board reveals. Individual actor fades and covered restoration are intentional per Xuân's request.

## Reproduce validation

```powershell
python Tools/PuzzleValidation/cooperative_step_tile_solver.py Docs/Puzzles/Day2School/coop_01.scene_export.json
python Tools/PuzzleValidation/cooperative_step_tile_solver.py Docs/Puzzles/Day2School/coop_02.scene_export.json
python Tools/PuzzleValidation/cooperative_step_tile_solver.py Docs/Puzzles/Day2School/coop_03.scene_export.json
```

The extension reuses the project's original StepTile placement/rotation solver and also checks each actor's projected path with the original solver. `coop_0*_proof.json` contains exact routes and traps. Runtime verification is recorded in the school story workflow document.

## Entry and cancellation polish — 2026-08-28

- Fade out `0.30 s`, reveal `0.40 s`; the top line reads only `Giúp Audere và Bianca lấy đồ về lớp`. It is a non-blocking objective, not a dialogue bubble or a music-duck cover.
- Hide the separate Supplies Return Board during COOP so its floor art cannot be mistaken for puzzle cells. Its later StoryStep still enables it normally.
- The project had `TimeManager.m_TimeScale = 0`, which stalled scaled-time traversal. The saved default is now `1`; dialogue still owns and restores its temporary pause.
- Cancellation normalizes only a live scene with complete Board/Player/Hand/Placement references. Scene teardown releases the session without reconstructing destroyed UI/actors. Normal Play still reports invalid authoring rather than silently ignoring it.
- Focused authoring menu: `Audere/Story/Polish Existing Cooperative Puzzles Only`. It changes existing COOP content without rebuilding SCHOOL or the Bianca combat event.

### Verification of the simpler revision

- Unity compiled successfully. Final EditMode run: **11/11 passed**, covering `CooperativePuzzleCompletionTests`, `Day2HomeMorningTests` and `PathPiecePresentationTests`. The cooperative UnityTests enter Play Mode and exercise production StoryEvents.
- All 12 placements used the actual pointer-preview and commit path, consumed four cards per board, and completed both arrivals. Dialogue was accelerated through its normal completion cleanup for this test, not evaluated for reading pace.
- Fall reset, interrupted traversal cancellation, replay and destroyed-UI-before-Story-disable + scene unload passed. No unexpected runtime logs were reported. Unity Test Runner emitted its normal results-save/post-build-cleanup messages.
- Focused migration rerun preserved all scene Transform identities and the serialized wrong-supplies event/children. Both Goal→Start deltas remain zero. Final scene validation: 0 missing scripts, 0 broken prefabs.
- Screenshots `Temp/CoopQA/board01.png` through `board03.png` show the single-line objective and no extra return-floor tiles at 1920×1080. 4:3/ultrawide were not visually replayed in this pass.

### Grounded depth revision — 2026-08-28

- All three real preview/drop routes were replayed successfully after adding prefab SortingGroups. Tests also reverse the actors' upper/lower positions and raise a visible hop while retaining its grounded position; depth remains correct and internal body order stays 5.
- Shared-cell separation/reset/cancel passed separately. Existing focused-authoring preservation and destroyed-UI teardown checks passed. No puzzle layout, path cards, red-tile rules or goal anchors changed in this revision.
- `Temp/CoopQA/upper-behind-lower.png` reproduces the reported third-board overlap: Bianca on the lower tile now renders over Audere's feet on the upper tile. Screenshot checked at 1920×1080. Scene60 remains saved/clean with startup enabled.
