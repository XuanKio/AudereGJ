# Teacher, phấn, Victory và fading pressure

[Mục lục nguồn](../06_CombatGameplay.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
### Day3 teacher pressure — 2026-08-28

Scene120 uses a **Design Intent** encounter: 15 shared HP, thresholds7/4/0,90TIME,3dice/batch,max2Attack. Separate editable `Enemy_Teacher_PLACEHOLDER` actor; no boss-specific code in CombatController or changes to shared dice constants. Composable moves: ChalkFence, ChalkSweep, RadialInwardTrail, SineProjectileStream; combine with existing laser/field-shift. Teacher no longer references VerticalPlayerImpulse; Dice Field width/X shifting remains enabled. Combat projections use the teacher's right-side DialogueUI portrait `Co_giao_Creepy_0`, introduced by Timor's “Chắc cô đang nghĩ…”. Occasional caring calls glitch back to `Co_giao_0`, like Bianca in Scene60. These are distorted perceptions, not canon hostility from the real teacher; enemy art and pre-combat dialogue remain unchanged.

Teacher dialogue data lives in `Data/Dialogue/Day3/TeacherCombat`. Current bindings use one non-repeating, non-interrupting `PhaseEnter` sequence per phase, with required phase-advance/victory gates described above. The older repeatable move/catch cue layout is no longer active. `AutoCombatDialogue` remains non-click and does not claim Dialogue input, while combat-local TIME/projectiles/Heart/moves pause for the visible sequence. This radial-trail update does not alter those cue or dialogue assets; current bindings and historical QA are in the Day3 workflow.

Teacher ordinary bullets use the shared `EnemyBullet.prefab` / `dan.aseprite`, including the sine stream and the laser move's fallback projectile reference. Only the special top/bottom chalk fences, rotating sweeps and inward radial ring use `Bullet_ChalkRod` / `phan.aseprite`; laser presentation stays shared and separate. Do not replace all of this enemy's bullets with chalk. The legacy `Move_ChalkSineStream` filename/GUID is retained; `Bullet_ChalkGrain` is no longer referenced by Teacher moves. Scoped binding, pooling and visual QA are recorded in the Day3 workflow.

`CombatBulletView` optionally owns a per-spawn `ICombatProjectileMotion`; Setup/Return/Fade cancel it, active combat delta alone advances it. `CombatRectCollision` uses oriented-rectangle SAT to avoid false hits in rotating chalk's empty bounding-box corners. `CombatBoardView` exposes owner-scoped vertical control (Y impulse while X stays steerable), released on move/session cleanup. Full authoring, timing and 90-test regression evidence: [Day3 workflow](../15_Day3_BoardTeacher_StoryWorkflow.md).

### Radial chalk and catch-blocking trails — current contract

- `RadialInwardTrailMove` is reusable authored data, not an enemy-ID branch. Teacher starts it at7HP (the first whole HP below half of15), after the existing required phase cue resolves. OrderedLoop then continues sweep/fence. Twelve equally spaced rods point toward field center, fade in from alpha0 for1.1s, then travel together through center over2.6s. Total move8.2s. No forced player Y movement.
- Two direct prefab bindings on `CombatBoardView`: `Exterior Projectile Root` is outside Dice Field's mask, opt-in only for attacks explicitly authored outside the field; `Stun Trail Root` is masked inside Dice Field. Ordinary bullets and lasers keep the existing inset Projectile Mask. Enemy presentation is not repositioned/rescaled.
- `CombatProjectileTrailSettings` decorates a fresh projectile motion. All three Teacher chalk specials enable it; ordinary `dan` bullets do not. Each emitted segment blocks left-click catch for **3.6 combat-active seconds**, then fades for0.3s without blocking. Right-click reroll, dice motion and player steering are unchanged. A trail does not itself deal damage.
- Segments are clipped to current field bounds with inset14 plus half trail width; rotated-strip collision reuses the cursor/rectangle overlap. Material is `UI_StunTrailDots`, alpha0.75; pool cap384. It allocates only when a segment slot is first needed. Field width/X shifting remains supported; the trail root follows/clips with Dice Field.
- Combat-local pause freezes projectile telegraph/flight and trail age. Shield return cancels projectile motion, preventing new trail emission; existing trails expire normally. Move/phase/session cancel, defeat, victory, retry and board disable clear owned trails immediately, even if3.6s has not elapsed.
- Projectile pooling remains keyed by source prefab. Exterior reuse is reparented back to normal Bullet Root on ordinary spawn. Setup resets presentation/rotation/motion/session/phase; a monotonic pool-lease version prevents a delayed radial cleanup from returning a bullet already reused by another execution.
- `CombatBoardView.ProjectileTrails.cs` is a partial of the same board owner, not a second controller/singleton. `CombatController.Play(...)`, shared Shield/dice constants, dialogue text/cue assets and scene-authored enemy art are unchanged. The reusable vertical-control API remains for other content, but Teacher's production movesets no longer reference it. Teacher still references ShiftingBattleBox through `Move_TeacherShiftAndSweep`.
- Author via `Audere/Combat/Author Teacher Radial Trails` for targeted prefab/data migration; do not rebuild Scene120. New scenes use updated Day3 defaults. Timing/balance and final art remain Design Intent/Unresolved as in the Day3 workflow.

### Optional Victory dialogue presentation

`CombatEncounterData.VictoryPresentation` is opt-in and unconfigured for existing encounters. Like Defeat presentation, it stops dice and move execution, disables hazard collision/fades hazards, and retains the authored enemy actor while a caller-owned DialogueUI sequence finishes. Victory keeps the actor paused; Defeat keeps its existing Cancel path. The shared result routine validates request version and terminal state before cleanup/callback; cancellation clears dialogue, actor, input and coroutine ownership. An optional existing `VictoryFadeDuration` runs after the dialogue. No enemy-ID branch is used.

Scene120 uses this for the real Teacher reassurance before returning to Story, with normal Teacher portrait and tired Audere. Its subsequent three-way reply and consent/staging belong to scene-first StorySteps, not the combat controller. Day3 + Evening regression20/20 passed at2026-08-28 14:27:17Z; see Docs15 for evidence and limits.

### Optional fading-pressure encounter (Day3 Bianca reprise)

- `CombatEnemyDefinition.PassiveHealthDecayInterval` defaults to zero. Opt-in per-phase-health encounters lose one HP per active-time interval through the normal damage/gate path; final required victory dialogue holds HP at one. Pause does not spend the interval, blocked hits do not accumulate, and phase/restart resets elapsed time. Controller refreshes HP presentation after passive changes.
- `FadingPressureMove` uses ordinary bullet and returning-orbit prefabs with per-lease `ConfigureHarmlessAvoidance`. It reads the live Dice Catch center/radius in play-area space, adds the projectile footprint, and reflects the whole visible projectile at an outer band (36 units for fan, 44 for orbit). Avoidance never fades alpha. A chasing/teleporting catch pushes the projectile out; only overlap within the 3-unit rim skin with no in-board escape may recycle it. Swept segment reflection also handles long frames and overrides an approaching authored orbit. Ordinary off-board/move-end cleanup still applies. Setup/pool return clears both geometry callbacks; cancellation checks lease/session/phase. Victory fade stays frozen and separate from avoidance. Attack activation remains separate from damage collision so grouped volley SFX still plays.
- Optional `CombatStep.EnemyActorOverride` selects a direct scene-authored actor on the shared board after resetting the previous session. Board rejects rebinding during an active actor lease or outside its Enemy Mount. Teacher and reprise retain separate actors; no ID-specific runtime branches.
- Scene120 reprise is separate from Scene60: 6HP, decay every3.5s, no dice, Victory-only, one required auto-dialogue sequence, enemy fade0.9s. Existing Teacher/Scene60 balance is unchanged. Final regression91/91 passed; evidence and visual limitations in Docs15.

#### Catch avoidance correction — 2026-08-28

54/54 `CombatEnemyRuntimeTests` passed (including six catch/alpha/pool/geometry checks), plus 1/1 Scene120 Play test sampling both fan/orbit prefab pools, deliberately overlapping the catch, asserting visible alpha and unchanged lease, then cancelling and checking input/dialogue/projectile cleanup. Evidence: `Temp/CatchAvoidance/tests_54_pass.xml`, `tests_play_pass.xml`, `fan-repel.png`, `orbit-repel.png` (1920x1080). No HP/TIME/dialogue/scene/Teacher/Scene60 edits in this correction. This focused pass does not claim the unrelated Teacher portrait importer issue is resolved.


<!-- END PRESERVED SOURCE -->
