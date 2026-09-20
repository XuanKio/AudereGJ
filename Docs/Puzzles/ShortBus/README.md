# Compact bus approaches

Design Intent requested by Xuân, 2026-09-20. Scene 20 and scene 50 retain their existing bus-stop items and their arrangement. Each approach now fits a 4×3 footprint with nine walkable cells and three required cards. The scene owns the board; the shared player, preview and input flow are reused.

## Day 1 — choose the correct length

Start `(0,0)`, goal `(3,2)`. Hand: Line3, Line2, LCorner3.

```text
. # # G
. # . #
S # # #
```

One solution: Line3 `(0,0)→(1,0)→(2,0)`; LCorner3 `(2,0)→(3,0)→(3,1)`; Line2 `(3,1)→(3,2)`.

Solver rechecked against `d1.scene_export.json`: two solutions, one plausible first-move trap. Starting with the short straight consumes the length needed later. Neither solution follows the displayed hand order.

## Day 2 — save the straight for the finish

Start `(0,0)`, goal `(3,2)`, single-use red `(1,0)`. Hand: Line3, LCorner4, LCorner3.

```text
. # . G
. # # #
S R # #
```

Solution: LCorner3 `(0,0)→(1,0)→(1,1)`; LCorner4 `(1,1)→(2,1)→(3,1)→(3,0)`; Line3 `(3,0)→(3,1)→(3,2)`.

Solver rechecked against `d2.scene_export.json`: one solution, two first-move traps. The red cell prevents returning to rescue a poor opening; the final straight retraces an ordinary cell safely.

## Preservation and reproduction

The fixed stop is the layout anchor. Earlier breakfast/washroom roots align backwards so each previous Goal equals the next PlayerStart. The bus goal, its child items, rotation, scale and world position remain unchanged. Day 1 grid math absorbs its existing fractional offset instead of shifting the station art.

All coordinates above are zero-based local board coordinates (`y=0` bottom row). Reproduce the design proofs with:

```powershell
python .agents/skills/audere-puzzle-map-generator/scripts/step_tile_solver.py --input docs/Puzzles/ShortBus/d1.scene_export.json
python .agents/skills/audere-puzzle-map-generator/scripts/step_tile_solver.py --input docs/Puzzles/ShortBus/d2.scene_export.json
```

Focused editor authoring: `Audere/Story/Shorten Bus Puzzles And Merge Cooperative Puzzle`. The bus Play tests exercise the production prepare/reveal event, actual preview/drop route, completion and replay/reset. Dialogue is fast-forwarded through its normal completion callback during these checks; reading pace is not measured.

## Verification — 2026-09-20

Both bus Play checks passed: three real preview/drop placements, empty hand at completion, fixed station/goal poses, zero input claims after cancel, and replay restoring three cards and the unused red tile. The preceding Goal and bus PlayerStart match within 0.0001 world units on both days. The existing goal Visual Root still bobs on arrival; that animation is excluded from the stationary-art assertion.

Across the focused runs, all 13 relevant checks passed (school cooperative completion/arrival/reset/teardown, home scene contracts, bus completion/replay). Final Console readback contained zero errors. Saved scenes 20/50/60 have zero missing scripts and no references to removed scene objects. Visual checks used the current 16:9 Game View; other aspect ratios and first-player solve times were not measured.

Results are retained locally in `Temp/ShortPuzzleQA/initial-results.xml`, `completion-results.xml` and `bus-final-results.xml`; later passing results supersede the test-harness failures in earlier runs. Screenshots: `Temp/ShortPuzzleQA/day1.png`, `day2.png`, and `Temp/CoopQA/board01.png`.
