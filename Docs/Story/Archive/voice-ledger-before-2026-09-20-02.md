# Voice ledger cũ — phần 2

**Historical / superseded:** bản trước đợt audit 2026-09-20, KHÔNG phải canon hiện tại.
Nguồn gốc: `.agents/skills/audere-dialogue-voice/references/story-state.md`.
Các nhãn và thông số bên dưới giữ nguyên để truy vết; có nội dung đã lỗi thời.
Tra [Story hiện tại](../README.md) trước khi viết hoặc sửa game.
Đoạn nguồn dòng 151–300; giữ thứ tự nối phần khi tra cứu.

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
  the left slot and Timor in the right slot; it auto-advances without claiming Dialogue input while
  the shared controller pauses combat-local simulation for the visible sequence.
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
