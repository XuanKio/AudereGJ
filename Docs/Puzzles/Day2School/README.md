# Day 2 — single cooperative supplies puzzle

**Design Intent requested by Xuân — 2026-09-20.** Current production scene 60 contains one cooperative board, then continues directly to `D2_SCHOOL_WRONG_SUPPLIES`.

## Current layout

- 5×3 bounds, 9 walkable cells, 2 shared red tiles, 5 cards. Scene objects own the layout; no runtime generation.
- Audere: start `(1,0)`, goal `(2,2)`. Bianca: start `(0,1)`, goal `(4,1)`. Red cells: `(1,1)`, `(2,1)`. Coordinates are zero-based, bottom row `y=0`.
- Hand order: LCorner4, LCorner3, Line2, LCorner3, Line2. The winning route is not the left-to-right hand order.
- Bianca opens by holding the first red tile. Audere crosses and holds the second. Bianca temporarily stands on Audere's destination; Audere finishes while Bianca remains available for the last path.
- Opening dialogue names the first holder. The encouragement after move 2 confirms Audere is holding and Bianca can cross. Same-cell drops select unfinished Audere; after she arrives, only Bianca moves.

## Intended solution

1. Bianca: `(0,1)→(1,1)` — Line2, hold first red.
2. Audere: `(1,0)→(1,1)→(2,1)` — LCorner3, hold second red.
3. Bianca: `(1,1)→(2,1)→(2,2)` — LCorner3, step onto Audere's goal without arriving herself.
4. Audere: `(2,1)→(2,2)` — Line2, arrive and fade.
5. Bianca: `(2,2)→(3,2)→(4,2)→(4,1)` — LCorner4, arrive.

The design solver finds 2 solutions and 1 plausible first-move trap: Bianca can leave the first red too early and strand Audere. All cards must be consumed; Backspace/fall reset restores both actors and shared tiles. Camera stays fixed while solving. The shared player, preview, input gate and the Bianca combat event are retained.

Current specs/proofs: `single_coop.json`, `single_coop.scene_export.json`, `single_coop_proof.json`. The proof was rerun against the serialized scene export and confirms two complete solutions. The `coop_01/02/03` files below are historical snapshots, not current production content.

Current Unity verification: actual five preview/drop placements complete both arrivals; fall reset, shared-cell Audere priority, individual arrival separation, cancel/replay and destroyed-UI teardown pass. Focused authoring rerun preserves the wrong-supplies event and retained Transform identities. Comparing the scene before and after migration found no changes in the 162 serialized objects/components under `D2_SCHOOL_WRONG_SUPPLIES`, no references to removed scene objects, and no missing scripts.

## Historical layouts before the single-board revision


**Design Intent requested by Xuân.** Serialized production content in `60_D2_School_Morning`, not a new canon claim.

## Current rules

- The scene owns both actors, every tile, both starts, A/B destinations and camera anchors. One shared hand and existing path runtime serve all three boards. No board/actor is generated during play.
- There is no actor selector, Tab switching, or Retry button. Connect a path endpoint to an unfinished actor's cell to move that actor. The opening move follows the dialogue: Audere on boards 01 and 03, Bianca on board 02. After that, either unfinished actor can move. If both share the starting cell, preview and drop always choose Audere. Finished actors cannot receive paths.
- A shared red tile allows one entry per actor. Its first visitor must remain there for the second visitor to enter. After both have entered and both have exited, it disappears immediately. Leaving before the second arrives strands that route and also hides the now-unusable tile. Cooperative red tiles and the morning's single-actor `OneUseTileBehaviour` disable their renderers entirely, with no faded remnant. Reset restores every authored renderer.
- Both actors may share a cell: Audere shifts left/up, Bianca right/down by small presentation offsets; logical coordinates stay identical. The Player/Bianca prefab root has a `SortingGroup` on layer Player. During COOP the whole upper actor sorts at 5, the lower actor at 6, using grounded foot Y (not the hop arc or sprite pivot). Equal rows keep Bianca in front. Body/shadow orders inside each group stay 5/4. Ending/resetting an attempt restores the authored group order.
- Arrival preserves that separation until the arriving actor has faded out. The finished actor keeps its landing pose and no longer receives standing-position updates; only the unfinished actor can take the next path from their shared cell.
- Only basic tile visuals and red color are used. The only tile labels are A/B destinations. There are no plate numbers, bridge bars, auxiliary icons or Retry buttons.
- Exhausting all pieces before both finish, reaching both goals with spare pieces, or falling resets the whole attempt: both actors, all tiles/colors/scales, shared-red visit/occupancy flags, arrival fades and the complete hand. Backspace also resets an active attempt; there is still no separate Retry button.
- Each actor fades independently at their own goal and is locked there. Both arrivals plus an empty hand complete the puzzle. A neutral fullscreen fade introduces the next board and restores both actors at its authored starts. Both actors remain the same scene instances.
- Camera stays fixed while solving. Each board fits `PuzzleViewportMask`; camera framing changes only during covered inter-board staging.

## Authored layouts

All coordinates below are local zero-based `(x,y)`; `y=0` is the lowest row. The JSON `*.scene_export.json` files were exported from the serialized tiles, references and path assets, then rechecked by `Tools/PuzzleValidation/cooperative_step_tile_solver.py`.

| Board | Size | Audere start → goal | Bianca start → goal | First holder | Shared red cells | Piece multiset | Valid solutions |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 01 | 4×3 | (0,1) → (3,1) | (1,0) → (2,2) | Audere | (1,2) | LCorner3, Line3, LCorner, Line2 | 4 |
| 02 | 5×3 | (1,0) → (2,2) | (0,1) → (4,1) | Bianca | (1,1), (2,1) | Line2 ×2, LCorner3 ×2, LCorner | 2 |
| 03 | 5×3 | (0,1) → (4,1) | (2,0) → (3,2) | Audere | (2,1), (3,1) | Line3, LCorner3 ×3, Line2 | 2 |

The three boards ask for different deductions: bend a path to the first hold; exchange holders across two red tiles; then use Bianca's goal as a temporary step while the actors take different turns to finish. Every valid solution consumes every card and reaches both goals. Cards remain in one intended-route order in the hand. Counts come from the scene export with the authored opening actor and Audere's shared-cell priority enforced.

### Intended routes

1. **01:** A `(0,1)→(0,2)→(1,2)`; B `(1,0)→(1,1)→(1,2)`; A `(1,2)→(2,2)→(3,2)→(3,1)`; B `(1,2)→(2,2)`.
2. **02:** B `(0,1)→(1,1)`; A `(1,0)→(1,1)→(2,1)`; B `(1,1)→(2,1)→(2,2)`; A `(2,1)→(2,2)`; B `(2,2)→(3,2)→(4,2)→(4,1)`.
3. **03:** A `(0,1)→(1,1)→(2,1)`; B `(2,0)→(2,1)→(3,1)`; A `(2,1)→(3,1)→(3,2)`; A `(3,2)→(4,2)→(4,1)`; B `(3,1)→(3,2)`.

Leaving a red tile too early can still strand the partner. Backspace restarts immediately without waiting to run out of cards.

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

### Redesign verification — 2026-09-20

- The Unity MCP scene export exactly matches the three authored layouts and piece sets above: 8/9/8 tiles, 1/2/2 red tiles, 4/5/5 cards, and zero position delta for both actors at both board hand-offs.
- The solver enforces the first actor and Audere's shared-cell priority. It finds 4/2/2 complete routes and at least one plausible first-placement trap on each board.
- The direct Unity scene check passed for camera mask clearance, references, red-tile counts, card counts and dialogue length. A Play check ran all 14 real preview/drop placements, both arrivals on every board, fall reset and replay/cancel assertions. Its final NUnit-only log-scope assertion cannot run outside Test Runner; Unity Console contained only that harness error.
- The latest board screenshots in `Temp/CoopQA/board01.png` through `board03.png` show all five cards within the visible hand on boards 02 and 03.

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

### Arrival separation — 2026-09-20

- Fixed the visible collapse into one sprite during goal arrival: the arrival flag previously removed both standing offsets before the 0.4 s fade completed. Separation now remains until the fade ends, and finished actors retain their landing pose without further standing-position updates.
- Focused Unity Test Runner result: **2/2 passed** (`CooperativeArrivalTests.SecondBoard_GoalArrivalStaysSeparateAndOnlyRemainingActorMoves` and `Day2SchoolMorningTests.CooperativeRules_SharedCellArrivalResetAndCancel`). The first test performs board 02's five real pointer-preview/commit placements, checks separation during visible fade, checks that only the remaining actor moves, and verifies replay restoration.
- No puzzle layout, path cards, goal ownership, actor hierarchy or scale changed. A persistent parent/follow link was not reproduced; the captured failure was the presentation collapsing during arrival.
