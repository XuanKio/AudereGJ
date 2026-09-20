# Ranh giới bằng chứng và architecture — checkpoint Day1

[Mục lục nguồn](../../09_Day1_ProductionStoryWorkflow.md) · [Bản đồ docs](../../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
# Audere Day 1 — Production Story & Authoring Workflow

> **Last verified:** 2026-08-23  
> **Unity:** 6000.0.79f1  
> **Production scenes:** `20_D1_Home_Morning`, `30_Classroom`

Tài liệu này là bản handoff tổng hợp cho story production hiện tại: game đang kể tới đâu,
Hierarchy chạy thế nào, dữ liệu nằm ở đâu và quy trình chuẩn để dựng beat tiếp theo.

## 1. Ranh giới bằng chứng

Khi đọc hoặc cập nhật tài liệu này, dùng bốn nhãn:

- **Established Canon:** đã tồn tại trong `DialogueData` và production `StoryEvent` hiện tại.
- **Strongly Implied:** được hành vi và nhiều nguồn hiện tại cùng hỗ trợ nhưng chưa nói thẳng.
- **Design Intent:** hướng thiết kế đã được Xuân yêu cầu nhưng chưa được production scene xác nhận đầy đủ.
- **Unresolved:** thiếu dữ liệu, còn mâu thuẫn hoặc mới là placeholder/legacy.

Thứ tự ưu tiên khi nguồn mâu thuẫn:

```text
Production DialogueData đang được StoryEvent tham chiếu
→ production Scene/Hierarchy
→ runtime script và serialized config
→ Docs
→ sample, TEST_*, filename, placeholder và brainstorm
```

`TEST_*`, `Dialogue_Sample`, placeholder art và nội dung dự kiến không tự trở thành canon.

## 2. Runtime flow toàn game hiện tại

Các scene đều đã được bật trong Build Settings:

```text
00_Bootstrap (build 0)
→ khởi tạo SceneFlow + AudioService và giữ chúng bằng DontDestroyOnLoad
→ 10_MainMenu (build 1)
→ New Game
→ 20_D1_Home_Morning (build 2)
   ├── D1_HOME_MORNING
   └── D1_TO_BUS_STOP
→ fade đen + SceneLoadStep
→ 30_Classroom (build 3)
   ├── D1_CLASSROOM_ANNOUNCEMENT
   └── D1_CLASSROOM_RECESS_BIANCA
       └── Story → Combat prototype → Story
→ điểm kết thúc production story hiện tại
```

`GameplayUIRoot` giữ Dialogue UI và Puzzle UI xuyên các gameplay scene. Mỗi location scene
có `StoryDirector` riêng; không serialize direct `StoryEvent` reference qua ranh giới scene.

## 3. Story architecture đang dùng

```text
StoryDirector
└── StoryEvent
    ├── 00_FirstStep
    ├── 10_SecondStep
    └── 20_ThirdStep
```

Quy tắc execution:

- Sibling order của direct child là thứ tự chạy.
- Mỗi direct child active có đúng một `StoryStep`.
- Child inactive được bỏ qua.
- Step nested trong group con không tự chạy.
- `Completed` chạy step kế tiếp; `Cancelled` hoặc `Failed` dừng event.
- Một `StoryDirector` chỉ cho một event chạy tại một thời điểm.
- Auto-next chỉ chạy sau khi event cũ đã `Completed` và `CurrentEvent` đã được clear.

Lifecycle của hệ thống con:

```text
DialogueStep → DialogueController.Play → chờ Completed/Cancelled
PuzzleStep   → PuzzleController.Play   → chờ Completed/Cancelled/Failed
CombatStep   → CombatController.Play   → map Victory/Defeat/Special theo Inspector
FullscreenWorldModeTransitionStep → fullscreen presentation → swap mode → cleanup
```

Input chỉ được cấp khi controller gameplay thực sự `Play()`. `WorldModeStep` và
`SetActiveStep` không tự cấp input.

<!-- END PRESERVED SOURCE -->
