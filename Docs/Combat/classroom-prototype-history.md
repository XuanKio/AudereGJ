# Lịch sử prototype lớp học và hướng mở rộng

[Mục lục nguồn](../06_CombatGameplay.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
## 10. Classroom prototype và hướng mở rộng (2026-08-23)

Scene `30_Classroom` hiện có một hand-off kỹ thuật sau thoại Timor:

```text
190_HoldAfterTimor
→ 200_ClassroomIsConsumed       [FullscreenWorldModeTransitionStep]
→ 210_PlayKhoangLangPrototype    [CombatStep]
→ 220_ReturnToStory              [WorldModeStep: Combat → Story]
→ 230_HoldAfterCombat
```

`WORLD` tham chiếu trực tiếp `CLASSROOM` làm `storyRoot`; `Combat Root` chứa cùng prefab
`CombatBoard` của scene 20. `SYSTEMS/Combat Systems` chỉ active ở mode Combat. Camera dùng
pose riêng cho Story và Combat; `PuzzleViewportMask` bật ở Puzzle/Story và tắt trong Combat.
`200_ClassroomIsConsumed` dùng shared profile `Dreamy Disorientation` trong `1.50 s`: camera
presentation nghiêng/zoom nhẹ, wide wave, UV drift, radial bend và smear quanh Audere; đổi
Story → Combat ở giây `1.10`, rồi distortion hạ xuống để Combat hiện rõ. Timeline nằm trong
`WorldTransition_DreamyDisorientation.asset`, không nằm riêng trong scene. Renderer Feature
inactive ngoài khoảng này nên
scene 20 và pixel art bình thường không chịu thêm full-screen blit. Chiều Combat → Story vẫn
dùng fade đen hiện tại.

Mode switch chỉ đổi presentation. `CombatController.Play()` vẫn là nơi duy nhất claim Combat
input, nên fullscreen transition, fade hoặc bật root không thể vô tình cấp input sớm. Contract
shader, timeline và cancel/replay nằm tại `Docs/11_FullscreenWorldTransitions.md`.

Encounter hiện tại:

```text
Assets/_Audere/Data/Combat/CombatEncounter_D1_CLASSROOM_KHOANG_LANG.asset
```

- **Design Intent:** prototype boss mang display name `Khoảng Lặng`, ID
  `d1-classroom-khoang-lang`, policy `PerPhaseHealth`, một phase `6 HP`. Phase dùng một moveset
  `OrderedLoop` chứa Aimed Fan, Side Sweep và Rain. Runtime nhiều phase vẫn là kiến trúc dùng chung;
  encounter D1 hiện cố ý dùng một thanh máu liền mạch.
- **Design Intent:** beat trước combat trình bày đây là nỗi lo đang giành quyền trả lời thay Audere;
  Audere chọn tự đối diện với nó. Điều này không tự xác lập boss là một thực thể literal.
- **Established implementation state:** Khoảng Lặng có các line/dialogue `PLACEHOLDER` do Xuân
  cung cấp; catalog tạm dùng portrait Audere nhưng vẫn hiển thị tên `Khoảng Lặng`.
- **Unresolved:** ontology và ý nghĩa tâm lý cuối cùng, final voice/dialogue của Khoảng Lặng,
  portrait/art chính thức, final moveset/balance, điều kiện thắng/thua canon và beat sau combat.
- Cả Victory và Defeat tạm map về `Complete` để QA được đường quay lại Story. Đây không phải
  quyết định kết quả cốt truyện.

Kiến trúc mở rộng hiện hành không nhét logic enemy vào StoryEvent hoặc controller:

```text
StoryEvent
→ CombatStep (chọn encounter + result mapping)
→ CombatEncounterData (TIME/batch/Heart/bullet tuning)
→ CombatEnemyDefinition (identity, actor prefab, policy, phases)
→ CombatEnemyRuntime (state theo attempt)
→ CombatEnemyActor + move execution + mechanic modules
→ CombatController (lifecycle và result, không biết story tiếp theo)
```

`PerPhaseHealth` reset HP và bỏ damage dư. `SharedHealthThresholds` clamp ở threshold hiện tại
để một hit không bỏ phase. `TimedSequence` chỉ đếm combat-active time và ẩn health bar.
Phase break chặn damage/input, dừng batch, clear dice/projectile phase cũ, pause TIME, chạy
phase-exit/dialogue hook, rồi mới enter phase mới và mở lại simulation. Mid-phase dialogue giữ
nguyên Heart, dice, projectile và move cadence.

<!-- END PRESERVED SOURCE -->
