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
  standard auto-advancing DialogueUI while combat-local TIME, dice, bullets and input continue.
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
  final dialogue, portrait/art and final moveset/balance. The implemented post-combat victory beat
  is current `Design Intent`; broader combat outcome/branching and whether the prototype rules
  survive into accepted story remain unresolved. `PLACEHOLDER` art is not narrative evidence.

### D1_CLASSROOM_POST_COMBAT — accepted invitation

Source: production steps after `210_PlayKhoangLangPrototype` in `30_Classroom` and the
`Dialogue_D1_CLASSROOM_POST_COMBAT_*` assets.

- **Established implementation state:** only Victory advances. Defeat opens Retry and cannot enter
  the post-combat story by accident.
- **Design Intent:** Audere returns to Bianca with the same physical trembling but gives the small,
  concrete answer `Tớ muốn thử.` Bianca treats it as an ordinary agreement, confirms the board task,
  and accepts Audere writing her own name without celebration or rescue framing.
- **Design Intent:** a reusable registration overlay dims the classroom and shows a white
  `RegistrationSheet_PLACEHOLDER`; click dismisses it. The final art remains `Unresolved`.
- **Design Intent:** Bianca settles a short distance to the right onto her tile center, turns away,
  then leaves through three authored hop anchors while the tiles reveal ahead and fade behind.
- **Design Intent:** after Bianca leaves, Audere thanks Timor. Her line `Tay tớ vẫn run` keeps the
  success small; `Nhưng tớ đã nói được` establishes action despite the remaining fear, not a cure.
- **Design Intent:** Timor answers warmly and remains the trusted presence Audere relies on. This
  keeps the relationship inside `Protective pre-emption`; it does not erase the agency cost already
  established earlier in the recess beat.
- **Established implementation state:** `School_Bell` begins, a neutral black fade covers the
  classroom, and `SceneFlow` loads the build-listed `40_Evening` scene. Official room art remains
  `Unresolved`.

### D1_HOME_NIGHT_MESSAGE — Bianca message and Timor pressure

Source: production event `D1_HOME_NIGHT_MESSAGE` in `40_Evening`, its Day1/Evening
`DialogueData`, and `CombatEncounter_D1_TIMOR_NIGHT_PRESSURE`.

- **Established implementation state:** Audere stands alone on a centered Night Tile placeholder.
  Her grounded shadow, not the center of her body sprite, is aligned to the tile center.
- **Established implementation state:** Audere says she spoke to too many people that day. The
  `Message_Arrive` sound is followed by the authored red `dauchamthan` alert above Audere; she
  startles vertically in place, says `Bianca nhắn cho tớ này.`, then the DialogueUI reveals
  Bianca's message. This ordering makes the remote text-message context explicit before Bianca's
  invitation appears. Audere remains in the left slot; Timor/Bianca use the right slot.
- **Established implementation state:** Timor first asks whether Audere is afraid Bianca is only
  approaching her to ask for more work. He admits `Nhưng tớ sợ lắm`, invokes Audere losing her
  mother after she trusted someone, and treats that past loss as evidence for the current danger.
  Audere says the situations are different, offers to ask Bianca directly, and tentatively says
  she can refuse if the request becomes too much.
- **Established implementation state:** when Audere says she still wants to answer, Timor moves
  from concern into anger: `Đừng bảo tớ đừng lo!`, `Cậu phải nghe tớ lần này`, and `Giữ khoảng
  cách với cô ấy`. Audere resists with `Lần này, để tớ tự trả lời`; Timor answers `Tớ không thể để
  cậu làm vậy`. This final attempt to remove her choice is the immediate cause of the combat handoff.
- **Established implementation state:** Story enters Combat through the shared `Dreamy
  Disorientation` profile focused on Audere. Combat bark uses standard DialogueUI with Audere in
  the left slot and Timor in the right slot; it auto-advances without claiming Dialogue input or
  pausing combat.
- **Established implementation state:** the eleven combat barks continue the conflict instead of
  introducing a new threat. Protection becomes instruction to stand still; Audere's claim that she
  can choose is reframed as being pulled in; disagreement becomes not trusting or abandoning Timor;
  the finale explicitly says `Tớ sẽ không để cậu trả lời`.
- **Established implementation state:** `D1_TIMOR_NIGHT_PRESSURE` has `66 TIME`, `36 shared HP`,
  ten authored batches of exactly Attack/Shield/Heal and an eleventh no-dice finale. Catching all
  three dice advances one phase only after the phase bark resolves. Audere cannot Defeat before
  phase 11; Timor cannot be defeated. Phases 2, 8 and 10 use telegraphed vertical/sweeping/pendulum
  laser hazards, and the final volley mixes bullets with vertical laser pressure. Timor's DialogueUI
  portrait—not the enemy sprite—follows Worried → WorriedUneasy → Angry → Sad from the authored
  art folder. The final two
  lines resolve before the center lock and lethal volley may reduce TIME to zero. Retry is disabled,
  Defeat is the only allowed Story result. The return to the room is deferred until the authored
  defeat presentation below resolves.
- **Established implementation state:** TIME reaching zero now begins a defeat presentation before
  Story resumes. Every bullet and laser freezes, loses collision, and fades while the Timor actor
  remains. With Timor's Sad portrait, Audere answers only `…` while Timor says `Thấy chưa`,
  `Cậu mệt rồi`, and that she has tried enough. His line is staged as a tired conclusion rather
  than triumph; Audere yields with `…Ừ` before the neutral fade returns to the room.
- **Established implementation state:** back in the room, Timor says `Không cần ép mình` and asks
  Audere to choose the easiest sentence. The player chooses a direct refusal, a delay, or silence.
  Each option runs a scene-authored nested StoryEvent: refusal/delay show `Đã gửi`, while silence
  holds longer and leaves Audere wondering what Bianca will think. All branches fade the room out
  and end on `Ngày 1 - Kết thúc`.
- **Design Intent:** the choice is intentionally constrained to avoidance strategies. It gives the
  player authorship over how Audere withdraws, not a hidden healthy answer or a victory over Timor.
  Timor sounds caring again because he believes he is reducing pain; the cost is that his framing
  has removed Audere's earlier wish to answer.
- **Design Intent:** this is the first implemented beat where Timor's fear of losing Audere makes
  him visibly lose composure and require obedience. His anger grows out of sincere fear and a need
  to keep her, not enjoyment of hurting her. This is a specific relationship beat, not a universal
  clinical model of anxiety.
- **Design Intent:** the assertion that Audere's mother trusted someone and Audere then lost her is
  authored for this scene at Xuân's direction. It is not yet promoted to cross-scene Established
  Canon; the exact event, causal truth, and whether Timor's account is reliable remain `Unresolved`.
- **Unresolved:** combat ontology, final psychological meaning, consequences of each reply,
  final moveset/balance and whether every authored bark remains canon.

### D2_HOME_MORNING — care becomes step-by-step control

Source: production events `D2_HOME_MORNING` and `D2_TO_BUS_STOP` in
`50_D2_Home_Morning`, plus the Day2/Home `DialogueData` assets.

- **Established implementation state:** the scene reuses the Day 1 home and bus-stop
  presentation, shared Player and puzzle flow. Audere remains the left DialogueUI slot;
  Timor remains the right slot.
- **Design Intent:** Audere opens with more distance than Day 1. Timor asks whether she is
  still afraid of him; she admits `...Một chút`. He says he knows and does not want to
  frighten her, then immediately resumes choosing the next task.
- **Design Intent:** Timor's help stays concrete and plausibly caring, but becomes more
  granular: finish breakfast before leaving, inspect the bag again, test the locked door
  again, keep distance from another person, and stand in the place he calls safer.
- **Design Intent:** Audere thinks about Bianca's message and the school event without
  assuming which reply branch was chosen. Timor acknowledges hearing her, then redirects
  attention to the road, the approaching bus, an open bag, or another immediate safety cue.
- **Established implementation state:** a shared red OneUse StepTile is introduced after
  its board becomes visible. Timor explains that it accepts one entry and cannot be used
  again after Audere leaves. This is gameplay clarity, while the more controlling wording
  of the surrounding routine carries the relationship subtext.
- **Unresolved:** the next Day 2 scene, consequence-specific memory of the three night
  replies, and when Audere first openly refuses one of Timor's safety instructions.

## Later story

- **Design Intent:** Timor's protection gradually becomes deciding for Audere and preventing her choices.
- **Design Intent:** Audere's agency develops slowly through small decisions and resistance.
- Combat meaning, cross-scene consequences of the three replies, and exact later turning points
  remain `Unresolved` unless separately implemented and recorded.

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
