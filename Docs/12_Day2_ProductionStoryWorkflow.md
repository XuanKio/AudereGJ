# Day 2 — Home Morning production workflow

> **Implementation scene:** `Assets/_Audere/Scenes/50_D2_Home_Morning.unity`  
> **Source layout:** copied from `20_D1_Home_Morning`; the Day 2 scene owns its
> scene-authored boards and does not rewrite Day 1 prefabs at runtime.

## Scene naming

New timeline/location scenes use:

```text
NN_D{day}_{Location}_{TimeOfDay}
```

Current examples:

- `20_D1_Home_Morning`
- `50_D2_Home_Morning`

`30_Classroom` and `40_Evening` keep their current names until a separate controlled
migration is requested; both contain substantial uncommitted production work.

## Story flow

```text
D2_HOME_MORNING
├── reset/prepare the reused home presentation
├── Audere and Timor acknowledge the fear left by the previous night
├── reveal the washroom board
├── explain the red OneUse tile while it is visible
├── brushing puzzle
├── breakfast puzzle
├── re-check the bag
├── re-check the locked door
└── auto-next → D2_TO_BUS_STOP

D2_TO_BUS_STOP
├── reveal the bus-stop board
├── Audere thinks about Bianca; Timor redirects to physical safety
├── bus-stop puzzle
├── bus-arrival audio
├── Audere mentions the school event; Timor selects where she should stand
├── keep the completed Goal, bus stop and board visible through the final dialogue
├── neutral fade only after the final line and hold
└── load 60_D2_School_Morning
```

After the fully opaque neutral fade, `120_LoadDay2School` loads the build-listed
`60_D2_School_Morning` scene. Its arrival/encounter and classroom supplies continuation are
recorded in `Docs/13_Day2_School_StoryWorkflow.md`. The Day 1 classroom is not reloaded.

## Dialogue calibration

- **Established implementation state:** Audere is always the left DialogueUI slot; Timor
  is always the right slot.
- **Design Intent:** Timor knows Audere is slightly afraid of him and says he does not want
  to frighten her. His voice remains quiet and caring.
- **Design Intent:** control is communicated through accumulation: eat before leaving,
  inspect the bag again, test the door handle again, keep distance from a passer-by, and
  stand only where Timor calls safe.
- **Design Intent:** when Audere mentions Bianca, the message, or the year-end event, Timor
  acknowledges the words but immediately replaces the subject with an immediate safety task.
- The content does not establish that Timor's care is fake or that Audere is ready for open
  confrontation.

## OneUse tile contract

`OneUse.prefab` is a shared red StepTile with `OneUseTileBehaviour`.

```text
first entry → allowed and consumed
leave tile  → all tile renderers turn off immediately (no dim remnant)
later entry → treated as empty space; Audere falls
reset/replay → authored red presentation and traversal state restored
```

`GridPlayer`, preview validation, and the board query the generic
`IBoardTileTraversalRule`; they do not switch on Day 2, puzzle ID, or tile ID.

The first board reveals the red tile before `25_OneUseTileTutorial` explains:

```text
Timor: “Ô đỏ chỉ đứng được một lần.”
Timor: “Rời khỏi nó rồi sẽ không quay lại được.”
Audere: “…Tớ hiểu.”
```

## Authored puzzle layouts

### Chained traversal contract

Every puzzle keeps Audere's journey spatially continuous:

```text
Puzzle A Goal world position == Puzzle B PlayerStart world position
```

The authored Scene pose already satisfies this equality. At runtime the source Goal remains
as the temporary ground anchor through dialogue; the next board aligns to that captured Goal
before reveal, then the superseded board hides. Never teleport the shared Player to an unrelated
start point and never disable/re-enable the Player between chained puzzles.

### Goal presentation and retry

- Washroom Goal uses `AssetGame/Item/banchai.aseprite` with the same floating pose as Day 1.
- Breakfast Goal uses `AssetGame/Item/banhmi.aseprite` with the same floating pose as Day 1.
- Bus Stop Goal restores the two authored `busstop.aseprite` scenery layers from Scene 20.
- Completing the Bus Stop puzzle does not collapse its board. The Goal and both scenery
  layers remain visible while the bus-arrival and safety dialogue plays, and disappear only
  underneath the final opaque scene fade.
- Falling, exhausting the authored pieces, replaying, or manually restarting begins a fresh attempt.
  Every OneUse tile restores its original red visual and accepts one new entry.

Coordinates below are zero-based Unity grid coordinates. `R` is OneUse.

### Washroom tutorial

```text
S R # G
```

- Cells: `(0,0)` through `(3,0)`.
- Pieces: Line2 ×3; require all.
- Solver: 6 solutions only because the three identical authored cards are counted as
  distinct slots; geometrically there is one route.

### Breakfast

```text
# . # # .
# # R R G
# # . R .
S . # # #
```

- Start `(0,0)`, Goal `(4,2)`.
- Pieces: LCorner4 → Line2 → LCorner3 → Line3.
- Solver: 1 solution; three of four valid first placements are traps.
- Along the winning route the legal-choice counts are `4 → 8 → 4 → 2`, so every
  placement still asks the player to preserve the correct remaining piece and direction.
- Intended route: LCorner4 up/up/right → Line2 down → LCorner3 up/right → Line3
  right through the two red cells to Goal.

### Bus stop

```text
# . . . . . G
# . . . # R #
# . . . R . .
S R # # # . .
```

- Start `(0,0)`, Goal `(6,3)`.
- Pieces: Line4 → LCorner → Line3 → Line2.
- Solver: 1 solution, 5 trap first moves.
- The vertical branch and short early pieces are valid-looking starts but cannot finish
  after consuming the red route.

Reproducible solver specs live under
`.agents/skills/audere-puzzle-map-generator/examples/`.

## Authoring and replay

- Run `Audere/Story/Author Day 2 Home Morning` to refresh assets and direct references.
- The tool copies the source only when Scene 50 does not exist; reruns preserve the Day 2
  scene and update its owned boards idempotently.
- All three boards share the location's one Player, PuzzleRuntime, preview, placement
  controller, hand UI, and placed-path root.
- Body/shadow sorting remains `Player 5/4`.
- Puzzle failure resets the current attempt; OneUse state cannot leak to the next attempt.

## Unresolved

- Day 2 school destination and scene number.
- Consequence-specific memory of the three Day 1 reply choices.
- Final difficulty/balance after player testing.
- Whether the red tile receives final art, sound, or an in-world narrative name.
