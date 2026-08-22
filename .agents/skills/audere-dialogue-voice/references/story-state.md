# Story State and Canon Ledger

## Source priority

When sources disagree, use this order for identifying the current implemented state, while still recording the conflict:

1. Current `DialogueData` referenced by the real `D1_HOME_MORNING` StoryEvent.
2. Current serialized values in `Assets/_Audere/Scenes/20_Game.unity`.
3. Current runtime/data scripts that define character ids and content ownership.
4. Project documentation under `Docs/`.
5. Test/sample assets, filenames, imported asset names, and comments.

Designer statements can define `Design Intent` but do not retroactively make an unimplemented event `Established Canon`.

## Implemented story sequence

### D1_HOME_MORNING — opening

Source: `Dialogue_D1_HOME_MORNING.asset`, referenced by `10_MorningDialogue`.

- **Established Canon:** Audere wakes late. Timor wakes her, discourages another five minutes of sleep, tells her not to think through everything at once, and directs her to wash up first. Timor goes with her.
- **Audere trust in Timor:** behaviorally sufficient to accept his guidance; exact emotional depth is `Strongly Implied`, not stated.
- **Audere self-directed choice:** low or untested in this exchange; she reacts rather than planning.
- **Audere resistance:** limited to mild sleepy protest.
- **Timor protectiveness:** active, practical, and close.
- **Timor control tendency:** subtle; he chooses sequence and next action, but it remains plausibly helpful.
- **Relationship:** familiar, cooperative, asymmetrical in who provides direction.

### Washroom StepTile tutorial

Source: `D1_HOME_MORNING/30_WashroomStepTileTutorial` and `PZ_D1_WASHROOM` in `20_Game`.

- **Established Canon/game state:** the player learns to select, place, and rotate path pieces; failure can reset with gentle retry text.
- Most serialized instructions are technical UI, not confirmed spoken Timor dialogue.
- The sequence currently labels the destination inconsistently: opening dialogue says `rửa mặt`, while the following DialogueData and toothbrush item imply `đánh răng`. Resolution is `Unresolved`.

### After bathroom task

Source: `Dialogue_D1_AFTER_BRUSHING.asset`, referenced by `50_AfterBrushingDialogue`.

- **Established Canon:** Audere mentions mint waking her up. Timor notices she looks less sleepy. Audere minimizes this to “a little.” Timor introduces breakfast as the next task and says Audere's father prepared bread.
- **Audere trust/self-direction:** still accepts Timor's sequence; asks a practical location question.
- **Audere resistance:** none beyond minimizing how awake she feels.
- **Timor protectiveness/control:** caring observation plus immediate selection of the next task.
- **Relationship:** stable trusted guidance; no rupture.

### Breakfast StepTile puzzle

Source: `PZ_D1_BREAKFAST`, `UseAllPiecesTutorialGuide`, and `70_PlayBreakfastPuzzle`.

- **Established Canon/game state:** the objective is bread and the puzzle requires using all available pieces.
- **Current Timor feedback:** if the player reaches the goal while skipping a piece, he rejects the shortcut, says nothing should be omitted, and asks Audere to try again; retry language remains reassuring.
- Whether every tutorial message is diegetic spoken dialogue or HUD attribution is `Unresolved`.

## Later story

- **Design Intent:** Timor's protection gradually becomes deciding for Audere and preventing her choices.
- **Design Intent:** Audere's agency develops slowly through small decisions and resistance.
- Specific school events, additional characters, combat meaning, branch order, endings, and exact turning points are not documented in the current project and remain `Unresolved` for this bible.

## Conflict and ambiguity ledger

### Audere / Nilah / Nhật Linh

- Current dialogue enum, catalog, portraits, and story assets use `Audere`.
- `Dialogue_Sample.asset` addresses “Nhật Linh” while its speaker slot is assigned to Audere.
- Audio ids and older docs use `Nilah` for player step/hurt sounds.
- **Status:** likely legacy naming, but no project source explicitly declares the rename. Use `Audere` in current story content; do not treat the other names as aliases in-universe without confirmation.

### Timor presentation

- `Docs/00_ProjectOverview.md` infers Timor is a cat from audio/assets.
- The current character catalog uses `Timor_Human_ver.png` as Timor's portrait.
- **Status:** could indicate outdated docs, multiple forms, or non-literal representation. Do not choose one interpretation in dialogue without confirmation.

### Bathroom action

- Opening line instructs Audere to wash her face.
- The next asset is named `AFTER_BRUSHING`, mentions mint toothpaste, and scene art includes a toothbrush item.
- **Status:** unresolved continuity mismatch; confirm whether the goal is washing face, brushing teeth, or a combined bathroom routine when the distinction affects a scene.

### Documentation freshness

- `Docs/05_DialogueSystem.md` says Timor has no portrait, while the current catalog assigns one.
- `Docs/00_ProjectOverview.md` predates the Story System and says concrete design is still inferred.
- **Status:** architecture docs are useful context but not authoritative character canon when contradicted by current assets.

## Missing data to collect later

- Character ages, histories, family context beyond Audere's father preparing bread, and in-world nature of Timor.
- Audere's specific fears and sources of social difficulty.
- Timor's private fear, goal, origin, limits, and awareness of his controlling behavior.
- Voice under anger, grief, comfort, social pressure, and combat.
- Exact later-story milestones and when each relationship progression band begins.
- Canon status and voice of future characters.

