# Crowd phản đòn, split board và VFX

[Mục lục nguồn](../06_CombatGameplay.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
### Damage reactions and separated board halves — 2026-09-20

`CombatPhaseDefinition.damageReactionMove` optionally interrupts the normal move after positive, non-terminal damage. Entry reaction is opt-in. A hit during a reaction queues another execution; completion resumes the ordered basic moveset. Phase progression/result/restart/cancel clears queued counters. Existing phases with no reaction preserve their behavior; there are no enemy-ID branches.

`EnemyMountDiveMove` owns the actual enemy mount pose, alpha-only rainbow echo snapshots and `CombatSplitBoardGraphic`. Swept body collision is active only during the plunge. Heart and dice respect the separated halves; return/recovery are harmless. No actor scale is changed. Pause freezes move time/echo/warning, and cancellation restores authored mount position/rotation, frame/field visibility and normal bounds. `ProcessionGateMove` authors equally spaced ranks around wide, changing aisles, with per-lease cancellation.

`CombatBoardView.ShowAttackWarning` renders a pooled red pixel exclamation above the projectile mask, clamped inside the play area with 22-unit horizontal and 24/30-unit vertical padding. The owning move supplies active time for blinking and must hide the warning when the strike starts or cancels; board disable/whole-session cleanup also clears it. Crowd uses it before committed chasing hands and reactive mount dives. Production balance and QA are recorded in [Day4 Crowd workflow](../17_Day4_Crowd_StoryWorkflow.md).

### Following hit VFX and split outer bounds — 2026-09-20

Shared hit VFX now lives under the active enemy visual, centered on its authored VFX anchor. Legacy sibling anchors get a runtime child under `VisualRoot`; reparenting preserves the calibrated world size. Hit effects are excluded from enemy flash renderer collection and cleared with the actor. Every combat scene inherits this runtime behavior without scene overrides or actor scale changes.

While split, horizontal cursor limits use the visible Heart footprint instead of the larger catch circle. `Dice Field.RectMask2D` expands with separation and restores its authored padding on cleanup. Cursor and dice bounds update after enemy motion in the same frame. Focused verification through MCP8080: **18/18 EditMode tests passed**, including all six authored enemy prefabs, both outer edges at three separations, and existing dive/pause/cancel checks. Manual mouse feel has not been rechecked in this focused pass.
<!-- END PRESERVED SOURCE -->
