# Bianca supplies encounter và bằng chứng QA

[Mục lục nguồn](../06_CombatGameplay.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
## Bianca supplies encounter — 2026-09-20

**Design Intent (Xuân):** combat expresses Audere's feared interpretation of Bianca. Reused projection dialogue does not establish hostility from the real Bianca.

- Scene 60 keeps its existing encounter/enemy GUIDs. Base balance is **30 HP / 150 TIME**, three HP bands ending at **18 / 9 / 0** (60% / 30% / defeat). Difficulty scaling still applies. Batches contain three dice with at most two Attack reservations; shared dice effects are unchanged.
- **Phase 1:** existing `Move_Bianca_0..3` combinations, including ordinary opening bullets. The opening dialogue runs before active combat; later combination cues are one-shot and wait for recovery. DiceCaught barks no longer interrupt every successful catch. Catching a batch does not restart the attack.
- **Phase 2:** the phase-1 exit dialogue finishes first. Over 1.15 seconds, the player frame/field/Heart/TIME move left and the field width becomes 72%; Bianca, HP, VFX and damage-number anchors move right. The enemy name moves above Bianca to fit 4:3. This phase has no dialogue cues. Its opening `RibbonFanSweepMove` extends two nine-bow rows from the body center. Each bow then follows a fixed-X vertical chord: the rows cross and exchange sides, turn 180 degrees at their tips, and retrace the stroke. A 0.1-second delay per segment carries the crossing outwards; each stroke lasts 1.05 seconds and its turn 0.45 seconds. The row extends over 0.6 seconds after a 0.8-second telegraph; the move lasts 12.8 seconds. The two traveling arc variations follow this signature move. Bow art comes from `dan_bianca.aseprite`; the dedicated ribbon prefab is 56×56 (including its collision rectangle), enlarged from 38×38 for readability.
- **Phase 3:** the full layout returns to center over 1.1 seconds before the existing box dialogue begins. `openingMove=WrongBoxChoiceMove` holds regular dice and boss damage until two successful choices, preserving successes across failures. It then resumes the same HP phase with `RibbonWeaveMove` and `CornerBloomMove`; there is no extra HP phase.
- Ribbon echoes are harmless pooled Images (cap 192, lifetime 0.28 active seconds), using the existing rainbow echo material. They do not create Stun Zones or block catch. Motion and echoes pause with combat; Cancel/defeat/restart/disable clear them.

Natural move handoff and HP phase breaks retire old collision immediately and fade presentation snapshots for 0.4 seconds. Battle Box recovery preserves the Heart position before cancellation. TIME and input pause during a phase transition; normal move breathing gaps still consume active time. Direct runtime phase-completion callers settle recovery before entering the next phase, so existing special/constraint phases retain their timing. Dice stagger waits also survive a dialogue pause beginning on the last delay frame.

Scoped authoring: **Audere → Combat → Apply Bianca Three Phases Only** (`BiancaThreePhaseAuthoring.Apply`). It reuses existing dialogue assets and leaves story hierarchy/post-combat wording alone. The full Bianca author invokes this focused update last. The old returning-orbit assets remain available but are not used by this encounter's three-phase movesets.

Victory still fades the enemy for 0.9 seconds before the existing Story handoff. The board stays locally active beneath the parent Combat Root.

### Crossing ribbon correction — 2026-09-20

The supplied `Recording 2026-09-20 091857.mp4` was resampled every 0.1 seconds. The earlier fixed-radius fan never exchanged sides; it also used the higher projectile muzzle. The revised signature uses the visual body's center, straight opposing chords with delayed crossings, and endpoint rotation instead of constant spinning. Only this move's asset was migrated through MCP; the 30 HP / 150 TIME balance, other moves and scene layout were preserved. Cancellation returns only this execution's recorded projectile leases.

**7/7 BiancaThreePhaseTests passed** at 03:50:10 UTC, including chord/return/turn geometry, production body-center emission, pause, cancellation during telegraph/outward/return stages, stale pool-lease safety and the full scene60 three-phase/restart/cancel flow. Captured 36 production frames and inspected the contact sheet for extension, crossing, turn and return. Evidence: `Temp/BiancaChordQA/tests.xml`, `motion-00.png` through `motion-35.png`, and `contact.jpg`. This proves the revised geometry and flow; final visual likeness remains subject to Xuân's review.

Readability follow-up requested by Xuân: enlarged the shared ribbon prefab to 56×56 and slowed the signature's stroke from 0.64 to 1.05 seconds, turn from 0.28 to 0.45 seconds, and segment delay from 0.065 to 0.1 seconds. The signature lasts 12.8 seconds. The 7-test focused suite was rerun successfully, including production Play, with zero Console errors. Evidence: `Temp/BiancaChordQA/readability-tests.xml`; fresh production captures extend through `motion-47.png`. Other attack timings and encounter HP/TIME remain unchanged.

### Three-phase QA — 2026-09-20

91 relevant tests passed across the regression run and final isolated Play run: 61 runtime, 17 mount-dive, 9 projectile-polish, 3 three-phase, and 1 captured-batch regression. The combined run's production Play case was interrupted by concurrent Editor work; it was rerun with exclusive Editor access and passed at 03:31:04 UTC. Evidence: `Temp/BiancaThreePhaseQA/combined-regression.xml` and `final-play.xml`.

The production Play check covers HP transitions, frozen TIME during transitions, enemy/HP movement, silent phase 2, 18 radial bows with active harmless echoes, centered phase-3 dialogue, box-choice completion into regular attacks, restart and cancellation cleanup. Frame, TIME and enemy-label bounds pass at 4:3, 16:9 and 21:9. Fresh screenshots are in the same QA folder. Final scene60 validation: 30 HP, 150 TIME, thresholds 18/9/0, all new presentation references assigned, StoryDirector auto-start restored, no missing scripts, clean saved scene, Play stopped, and zero Console errors. This verifies flow and presentation; perceived difficulty and fight length still need player feedback.

An unrelated broader-suite failure remains in `EveningNightPressureTests.EveningScene_HasCenteredStagingAndDirectStoryFlow`: its expected 25 Story steps differs from scene40's existing 26, including `200_BeginDayTwo` already present in HEAD. Scene40 was not changed for this work.

The following August QA records the earlier projectile revision, not the current three-phase design.

### Projectile polish QA — 2026-08-28

- `CombatEnemyRuntime` now consumes only the elapsed portion of a move lead-in and ticks the move with the rest of that frame. This prevents a slow startup frame from stretching the 0.01-second opening wait. Paused combat still consumes neither lead-in nor move time.
- Final combined run: **63/63 passed** (`BiancaProjectilePolishTests`, `CombatEnemyRuntimeTests`, `EveningNightPressureTests`). The Scene60 UnityTest enters Play, calls the existing `Play(...)`, observes three ordinary projectiles during the first 0.078 seconds of combat-active time, and verifies single cancellation/input cleanup.
- The same live board/pool then runs the authored returning execution with a fixed seed: waves 1/2/3, 69×69 size, stationary telegraph, constant lane Y, full-width outbound/retrace and idempotent cleanup passed. This is targeted execution QA, not a new full boss victory/Retry playthrough.
- Horizontal trajectory geometry passed 16:9, 4:3 and 21:9 tests in both directions. Screenshots in `Temp/BiancaPolishQA` were visually checked at 1920×1080 only; other aspect ratios and full manual balance were not replayed.
- Unity compiled successfully; Scene60 validation found 0 missing scripts/broken prefabs. No unexpected Play logs. The Editor suite emits existing synthetic Test Board actor-fallback warnings and the Test Runner results-save message; these are not production runtime errors.
- Read-only Scene20/30 inspection also found 0 missing scripts/broken prefabs. Scene20's existing inactive debug CombatController has no board reference (`playOnStart=false`); it was not changed here. A full Scene20/30 gameplay replay was not part of this polish pass. Scene60 was left stopped, saved/clean, with StoryDirector startup enabled.

<!-- END PRESERVED SOURCE -->
