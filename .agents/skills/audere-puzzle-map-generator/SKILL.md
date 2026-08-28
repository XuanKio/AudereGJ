---
name: audere-puzzle-map-generator
description: Design, regenerate, and verify Audere StepTile puzzle boards and path-piece sets. Use for scene-first puzzle layout work where every supplied piece must be consumed before the player reaches the goal.
---

# Audere Puzzle Map Generator

Design a readable StepTile puzzle, prove it is solvable under the project's actual placement rules, then materialize the result as scene or prefab GameObjects.

## Invariants

- Keep the level prefab or scene as the source of truth for BoardTiles, PlayerStart, Goal, and presentation.
- Use `PuzzleData` only for reusable configuration and the available PathPiece list. Do not regenerate or overwrite a scene-authored board at runtime.
- Represent an inaccessible cell by omitting its BoardTile. Do not invent a blocked tile type or add a visual placeholder unless the user explicitly requests one.
- Reuse the shared `Puzzle Runtime`, `Path Preview`, `PathPlacementController`, Player, HUD, and `Placed Path Root`. A level prefab must not duplicate them.
- Keep exactly one shared Player per location. A level prefab owns only its `PlayerStart`; never add or hide a level-specific Player during a puzzle chain.
- Prefer existing `PathPieceData` assets. Create a new piece only when no existing shape can satisfy the requested design.
- Treat user-facing row and column numbers as one-based when stated that way: Unity coordinate is `(column - 1, row - 1)`. Preserve an explicitly supplied Unity coordinate such as Player `(0,0)`.
- Sequential puzzles always form one continuous route: the previous Goal world position and the next PlayerStart world position must match exactly. This is mandatory across StoryEvents and location beats, not an optional presentation flourish. Keep the current Goal tile as the visible hand-off anchor during intervening dialogue, then hide the superseded level when the next board begins revealing.
- Do not put the shared Player in a `SetActiveStep` disable list between puzzle events. Reposition the existing Player at the next `PlayerStart` without an inactive frame.
- After moving a level root to an anchor, refresh board registration before sorting reveal order or starting gameplay. The first revealed tile must overlap the next `PlayerStart`.

## Design workflow

1. Read the current board prefab, `PuzzleData`, available `PathPieceData`, `PathPlacementValidator`, and completion/reset rules before editing.
2. Normalize coordinates and check parity: the parity of the total movement count across all required pieces must match the parity of the start-to-goal displacement.
3. Draft the walkable-cell set and piece multiset. Keep the intended route visually legible, while including at least one valid-looking first placement that cannot finish when the requested difficulty calls for thought.
4. Validate the draft with `scripts/step_tile_solver.py`. The spec can be piped on stdin or passed with `--input`.
5. Require at least one solution that uses every piece and reaches Goal only after the last piece. Prefer a small solution count for authored puzzles; do not claim uniqueness unless the solver reports it.
6. Materialize only the validated walkable cells in the level prefab, place PlayerStart and Goal directly, update the level's `PuzzleData` piece list, and retain shared-runtime references.
7. Re-run the solver against the final authored coordinates, compile Unity, check Console, and Play-test completion plus reset/replay.
8. For chained puzzles, Play-test the actual Goal-to-PlayerStart hand-off: one visible anchor tile, zero position delta, no duplicate old Goal, no duplicate preview, and no Player blink.

## Solver spec

```json
{
  "width": 7,
  "height": 4,
  "start": [0, 0],
  "goal": [6, 0],
  "cells": [[0, 0], [1, 0], [2, 0]],
  "one_use_cells": [[1, 0]],
  "pieces": [
    {"id": "line-3", "path": [[0, 0], [1, 0], [2, 0]]}
  ],
  "require_all": true,
  "max_solutions": 2000
}
```

Run:

```powershell
$spec | python .agents/skills/audere-puzzle-map-generator/scripts/step_tile_solver.py
```

The report includes solution count, valid first moves, first moves that belong to a solution, dead-end states, premature Goal hits, an ASCII board, and sample solutions. A nonzero exit code means the spec is malformed or unsolved.

`one_use_cells` is optional. A listed cell may be entered once per attempt; after the
player leaves, later moves cannot enter it again. The solver renders these cells as `R`
and carries their consumed state across every supplied path piece.

## Handoff

Report the zero-based Unity coordinates, the equivalent one-based row/column meaning when useful, the selected piece assets, intended solution, number of solver solutions, trap rationale, prefab/data files changed, and Unity runtime verification. Clearly separate validated facts from intended difficulty.

For a chained level, also report the source Goal, target PlayerStart, measured world-position delta, first revealed tile, and whether the shared Player stayed active throughout prepare/reveal.
