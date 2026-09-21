# Crowd phản đòn, split board và VFX

[Mục lục nguồn](../06_CombatGameplay.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

## Hand/Crowd: vùng chơi và nhịp đòn — 2026-09-20

**Design Intent theo yêu cầu Xuân.** Scene140 author Dice Field và Airborne Dice Overlay720×360, Frame760×440 (trước đó field860×420); giữ pose/scale enemy. Hai nửa board khi đâm dùng bounds mới; Heart, dice, clipping và Retry lấy cùng kích thước scene.

- `EnemyMountDiveMove` có fan impact opt-in: ba viên mỗi bên, tốc độ140, fade báo0.18 giây sau khi chạm đáy. Chỉ phát một lần mỗi cú đâm; pause không phát/di chuyển. Đạn cũ được dọn trước chu kỳ đâm kế, cancel/phase/end dùng lease để không xóa nhầm viên tái sử dụng. Cả ba asset Counter/Alternating/Tracking được cấu hình; production phase2 dùng Counter.
- Sau đâm, WatchingAisle có5 hàng/10 giây, nhịp1.8 giây, spacing100, khe164. PressureCorridor có4 cặp hàng/13.2 giây, nhịp3.2 giây, spacing66, khe138, tốc độ235. Giữ khoảng trống theo footprint art108 để mật độ tay cao hơn không lấp khe Heart.
- Authoring tập trung: `Audere/Combat/Tune Crowd Pressure Rhythm` cập nhật move assets; `Resize Loaded Crowd Battle Box` chỉ đổi field/frame/overlay trong Scene140 đã lưu. Main scene authoring dùng cùng kích thước. Shared prefab, HP, dice constants và lời thoại không đổi.

QA 2026-09-20 10:51Z: **31/31** qua (EnemyMountDive25, TeacherSpiral5, Play smoke Scene1401). Hai đường né Heart qua hàng tay và hai phía tránh body/fan trên field720×360 không nhận hit; pause, frame dài, lease/cancel, phase, Retry và disable qua. Console không lỗi trước run và Play `NoUnexpectedReceived` qua. Đã xem impact/PressureCorridor/ảnh4:3; assert viewport cả1280×720,960×720,1680×720 qua. XML: `Temp/CrowdPressurePolishQA/tests.xml`; ảnh: `Temp/CrowdPhase2QA/`. Đây là kiểm tra kỹ thuật/ảnh, chưa thay thế người chơi thử độ khó.

## Phản đòn thuộc phase hiện tại — 2026-09-20

Khi qua mốc3HP, `CombatEnemyRuntime.EnterPhase()` phải xóa `queuedDamageReactions` và `playingDamageReaction` trước khi chọn chiêu phase3. `RetireActiveMove()` vẫn giữ pose để hồi chuyển động; hàng chờ phản đòn của phase2 không được chạy trong phase3, nơi `DamageReactionMove` là null.

Log bản Release xác nhận lỗi tại `StartDamageReaction → StartNextMove → EnterPhase → CompletePhaseBreak` khi bắt thêm Attack trong cú phản đòn rồi vượt ngưỡng trước lúc hàng chờ kết thúc. Bằng chứng và cách tái hiện: `Temp/CrowdPhase3QA/diagnosis.md`, `Temp/CrowdPhase3QA/Player-prev.log`.

Hai case `CrowdPhaseThreeDiscardsQueuedCountersAndReleasesVictoryAfterItsCue` đã đạt trong Unity Edit Mode: một/hai phản đòn đang chờ, qua3HP ngay, chạy đúng hai chiêu phase3 và chỉ Victory sau khi cue bắt buộc được resolve. Tổng lượt kiểm tra cùng hồi máu Timor: 52/52 lời gọi NUnit trực tiếp qua MCP; Console 0 error sau lượt cuối. Bằng chứng `Temp/CrowdPhase3QA/{existing-tests,timor-recovery-tests,policy-tests}.txt`. Chưa chơi tay xuyên Scene140/150.

<!-- BEGIN PRESERVED SOURCE -->
### Damage reactions and separated board halves — 2026-09-20

`CombatPhaseDefinition.damageReactionMove` optionally interrupts the normal move after positive, non-terminal damage. Entry reaction is opt-in. A hit during a reaction queues another execution; completion resumes the ordered basic moveset. Phase progression/result/restart/cancel clears queued counters. Existing phases with no reaction preserve their behavior; there are no enemy-ID branches.

`EnemyMountDiveMove` owns the actual enemy mount pose, alpha-only rainbow echo snapshots and `CombatSplitBoardGraphic`. Swept body collision is active only during the plunge. Heart and dice respect the separated halves; return/recovery are harmless. No actor scale is changed. Pause freezes move time/echo/warning, and cancellation restores authored mount position/rotation, frame/field visibility and normal bounds. `ProcessionGateMove` authors equally spaced ranks around wide, changing aisles, with per-lease cancellation.

`CombatBoardView.ShowAttackWarning` renders a pooled red pixel exclamation above the projectile mask, clamped inside the play area with 22-unit horizontal and 24/30-unit vertical padding. The owning move supplies active time for blinking and must hide the warning when the strike starts or cancels; board disable/whole-session cleanup also clears it. Crowd uses it before committed chasing hands and reactive mount dives. Production balance and QA are recorded in [Day4 Crowd workflow](../17_Day4_Crowd_StoryWorkflow.md).

### Following hit VFX and split outer bounds — 2026-09-20

Shared hit VFX now lives under the active enemy visual, centered on its authored VFX anchor. Legacy sibling anchors get a runtime child under `VisualRoot`; reparenting preserves the calibrated world size. Hit effects are excluded from enemy flash renderer collection and cleared with the actor. Every combat scene inherits this runtime behavior without scene overrides or actor scale changes.

While split, horizontal cursor limits use the visible Heart footprint instead of the larger catch circle. `Dice Field.RectMask2D` expands with separation and restores its authored padding on cleanup. Cursor and dice bounds update after enemy motion in the same frame. Focused verification through MCP8080: **18/18 EditMode tests passed**, including all six authored enemy prefabs, both outer edges at three separations, and existing dive/pause/cancel checks. Manual mouse feel has not been rechecked in this focused pass.
<!-- END PRESERVED SOURCE -->
