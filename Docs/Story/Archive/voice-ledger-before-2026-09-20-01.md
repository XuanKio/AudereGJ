# Voice ledger cũ — phần 1

**Historical / superseded:** bản trước đợt audit 2026-09-20, KHÔNG phải canon hiện tại.
Nguồn gốc: `.agents/skills/audere-dialogue-voice/references/story-state.md`.
Các nhãn và thông số bên dưới giữ nguyên để truy vết; có nội dung đã lỗi thời.
Tra [Story hiện tại](../README.md) trước khi viết hoặc sửa game.
Đoạn nguồn dòng 1–150; giữ thứ tự nối phần khi tra cứu.

# Story State and Canon Ledger

## Source priority

When sources disagree, use this order for identifying the current implemented state, while still recording the conflict:

1. Current `DialogueData` referenced by real production StoryEvents.
2. Current serialized values in `Assets/_Audere/Scenes/20_D1_Home_Morning.unity` and `30_Classroom.unity`.
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

Source: `D1_HOME_MORNING/30_WashroomStepTileTutorial` and `PZ_D1_WASHROOM` in `20_D1_Home_Morning`.

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

Source: `D1_TO_BUS_STOP` in `20_D1_Home_Morning`, `Dialogue_D1_BUS_STOP_ARRIVAL.asset`, and
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
- **Established Canon:** Audere quietly notices the decoration task. She first says she does
  not know whether she likes it, then admits that she probably does.
- **Established Canon:** Timor says liking it is fine, but they do not need to sign up yet.
  He points out that Audere has not sat still all morning, asks her to rest, and postpones the
  decision until later. Audere yields and returns to her seat.
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
- **Established Canon:** after Bianca leaves room for an answer, Audere notices that her hands
  keep trembling. She wants to answer, but the immediate thought in her head is `trốn đi`.
- **Established Canon:** Timor tells her not to answer yet, redirects her attention to him, and
  says anxiety is answering in her place. Audere says she no longer wants it to choose for her
  and decides that she must face it herself. Timor answers that he will stay with her.
- **Relationship state:** `Protective pre-emption` is reinforced. Bianca gives Audere room
  to choose; Timor occupies that pause before Audere can answer while still sounding helpful.
- **Unresolved:** Bianca's portrait/final art and wider character history. The current prefab
  is a presentation placeholder and does not establish appearance.
- **Established implementation state:** after Timor's last line, a shared dreamy distortion
  profile tilts, drifts, bends, and smears the classroom around Audere before revealing the
  reusable combat runtime. The current encounter instantiates a one-phase, `6 HP` actor prototype
  named `Khoảng Lặng` and returns to the same Story presentation after either Victory or Defeat.
  This establishes the technical hand-off only.
- **Design Intent:** the display name `Khoảng Lặng` and its D1 Classroom placement identify the
  prototype Xuân currently wants to develop. The pre-combat framing presents the encounter as
  Audere facing the anxiety that is trying to answer for her; it does not settle whether the boss
  is literal, symbolic, or another kind of presentation device.
- **Established implementation state:** the prototype now starts with an isolated one-phase tutorial
  runtime (`99 HP`, `120 TIME`) instead of attaching tutorial cues to the production phase.
  Its opening batch contains Attack, Shield, and Heal exactly once; the first HUD beat previews all
  three, then Timor narrows attention to Stun Zone and each dice rule in turn. Exact controls remain
  a separate one-line HUD. Dialogue and instruction both pause combat-local TIME, dice, projectile,
  and enemy move simulation until a left/right click closes the card; that click is consumed. Between
  cards TIME drains at `0.25x`, with a `1 s` tutorial safety floor. The closing dialogue now anchors
  Audere on the concrete sentence `Tớ muốn thử`. After Timor tells her not to lose that sentence,
  the tutorial session is destroyed and a fresh one-phase `Khoảng Lặng` session starts at `6 HP`
  and `45 s`, with Aimed Fan, a converging Side Sweep, and Rain looping in authored order.
  The shared enemy runtime still supports multiple phases, but this encounter has no phase marker.
- **Established implementation state:** Khoảng Lặng's supplied opening and Side Sweep lines use the
  standard auto-advancing DialogueUI without claiming Dialogue input; combat-local TIME, dice,
  bullets, Heart simulation, and enemy moves pause until each sequence completes.
  Audere occupies the left slot and Khoảng Lặng the right slot, following the project-wide Audere
  presentation contract. Legacy assets authored with Audere on the right are mirrored at runtime.
  After the Side Sweep dialogue, Audere's Heart briefly loses rhythm; the Audere–Timor anchor dialogue
  then pauses combat-local simulation and resumes the same move state. At `2 HP`, supplied worry
  text densely fills the background with low-opacity smear/wobble until cleanup. A required dialogue cue may hold only an
  early lethal hit at `1 HP`; it does not create another phase.
- **Design Intent:** all Khoảng Lặng dialogue and the reused Audere portrait are temporary
  presentation approved for this prototype, not settled voice or portrait canon.
- **Design Intent:** the current Timor/Audere tutorial wording is approved for this production
  prototype to make the first combat readable and winnable, but remains distinct from settled
  character canon. Timor remains genuinely useful by narrowing attention to one
  action at a time; the beat does not advance the relationship beyond `Protective pre-emption`.
- **Unresolved:** the boss's exact in-world ontology and final psychological meaning, final voice,
