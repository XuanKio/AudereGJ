# Story State and Canon Ledger

## Source priority

When sources disagree, use this order for identifying the current implemented state, while still recording the conflict:

1. Current `DialogueData` referenced by real production StoryEvents.
2. Current serialized values in `Assets/_Audere/Scenes/20_Game.unity` and `30_Classroom.unity`.
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

### D1_TO_BUS_STOP — arrival and safety anchor

Source: `D1_TO_BUS_STOP` in `20_Game`, `Dialogue_D1_BUS_STOP_ARRIVAL.asset`, and
`Dialogue_D1_BUS_STOP_SAFE.asset`.

- **Established Canon:** Audere reaches the bus stop in time and remains on the Goal while
  the path disappears and the bus approaches.
- **Established Canon:** Timor frames the success as doing one thing at a time. Audere
  thanks him; Timor answers `Tớ ở đây mà.`
- **Strongly Implied:** this reassurance feels safe and dependable to Audere and deepens the
  practical reliance established during the morning routine.
- **Design Intent:** the line becomes emotionally costly later because Timor's constant
  presence will make refusing his protection harder. The later payoff is not yet implemented.
- **Design Intent:** a subtle shoulder-relax animation may communicate relief later, but it
  must not move the shared gameplay Player away from the Goal.

### D1_CLASSROOM_ANNOUNCEMENT

Source: production event `D1_CLASSROOM_ANNOUNCEMENT` in `30_Classroom` and its referenced
`Dialogue_D1_CLASSROOM_*` / `Dialogue_D1_TEACHER_*` assets.

- **Established Canon:** Audere settles into her usual classroom seat. The teacher announces
  an end-of-year class party with decoration, food, and games, and invites each student to
  help with one small part.
- **Established Canon:** the teacher frames participation as a calm, shared invitation: each
  student may choose one manageable task, without urgency or pressure to do a lot.
- **Design Intent:** the teacher's baseline presence is healing, gentle, cheerful, and mature;
  this should be expressed through patience and reduced pressure rather than therapeutic
  exposition.
- **Established Canon:** Audere quietly notices the decoration task and admits she may like
  it a little.
- **Established Canon:** Timor tells her it does not need to involve them, says she has
  already done enough that morning, and suggests sitting still. Audere yields and returns to
  her seat.
- **Relationship state:** the scene is the first implemented edge from `Trusted guidance`
  into `Protective pre-emption`: Timor still sounds caring, but closes an option before
  Audere can try it.
- **Unresolved:** Teacher portrait and personal name, final classroom art, named classmates, and whether Timor
  is externally perceptible. Placeholder actors do not settle these questions.

### D1_CLASSROOM_RECESS_BIANCA

Source: production event `D1_CLASSROOM_RECESS_BIANCA` in `30_Classroom` and its referenced
`Dialogue_D1_CLASSROOM_BIANCA_*` / `Dialogue_D1_CLASSROOM_TIMOR_INTERVENES` assets.

- **Established Canon:** after a light fade into recess, Bianca approaches Audere from the
  right while the board tiles appear ahead of her and fade behind her.
- **Established Canon:** Bianca calls Audere, moves a little closer when she receives no
  response, and accidentally startles her. Audere hops once in surprise and turns toward Bianca.
- **Established Canon:** Bianca apologizes, explains that she is helping with decoration,
  invites Audere to help with the board, and explicitly says it is fine if it is inconvenient.
- **Established Canon:** Audere remains silent. Timor tells her not to answer yet, redirects
  her attention to him, and says he will help.
- **Relationship state:** `Protective pre-emption` is reinforced. Bianca gives Audere room
  to choose; Timor occupies that pause before Audere can answer while still sounding helpful.
- **Unresolved:** Bianca's portrait/final art and wider character history. The current prefab
  is a presentation placeholder and does not establish appearance.
- **Established implementation state:** after Timor's last line, the event fades into a
  reusable combat prototype and returns to the same Story presentation after either Victory
  or Defeat. This establishes the technical hand-off only.
- **Unresolved:** the enemy, combat's in-world/psychological meaning, canonical outcome,
  post-combat dialogue, and whether the prototype rules survive into the accepted story.
  `PROTOTYPE`, placeholder art, and temporary result mapping are not narrative evidence.

## Later story

- **Design Intent:** Timor's protection gradually becomes deciding for Audere and preventing her choices.
- **Design Intent:** Audere's agency develops slowly through small decisions and resistance.
- Combat meaning, branch order, endings, and exact later turning points remain `Unresolved`
  unless separately implemented and recorded.

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

- Architecture docs are implementation context, while this ledger and current production
  assets remain authoritative for character canon when a conflict appears.

## Missing data to collect later

- Character ages, histories, family context beyond Audere's father preparing bread, and in-world nature of Timor.
- Audere's specific fears and sources of social difficulty.
- Timor's private fear, goal, origin, limits, and awareness of his controlling behavior.
- Voice under anger, grief, comfort, social pressure, and combat.
- Exact later-story milestones and when each relationship progression band begins.
- Canon status and voice of future characters.
