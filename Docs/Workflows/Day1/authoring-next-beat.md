# Quy trình dựng beat và nối scene

[Mục lục nguồn](../../09_Day1_ProductionStoryWorkflow.md) · [Bản đồ docs](../../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
## 7. Workflow dựng beat production tiếp theo

### Pass 1 — xác định narrative

1. Xác định day, scene, event và beat đứng trước/sau.
2. Viết một câu nêu primary story job.
3. Xác định observable state kết thúc beat: actor tới đâu, object nào hiện, dialogue nào xong.
4. Gắn nhãn claim mới: Canon / Implied / Intent / Unresolved.
5. Đọc voice profile và relationship band của tất cả nhân vật tham gia.
6. Tách rõ UI instruction khỏi character dialogue.

### Pass 2 — chuẩn bị asset

1. Dialogue production đặt dưới `Dialogue/DayN/Location`.
2. Giữ `DialogueId` ổn định; không dùng filename hoặc sample làm story state.
3. Puzzle mới duplicate level prefab gần nhất rồi chỉnh trong Prefab Mode.
4. Dùng direct prefab/scene reference cho actor, target, controller, overlay và audio id.
5. Placeholder phải ghi rõ trong hierarchy và docs.

### Pass 3 — dựng Hierarchy

```text
STORY [StoryDirector]
└── D*_PRODUCTION_EVENT [StoryEvent]
    ├── 00_Normalize
    ├── 10_VisibleAction
    ├── 20_Dialogue
    ├── 30_Gameplay
    └── 40_Resolution
```

- Prefix số mô tả order rõ ràng.
- Một direct child chỉ có một `StoryStep`.
- Dùng step nhỏ sẵn có trước khi viết component mới.
- Không dùng `FindFirstObjectByType` để tiện authoring.
- Không dùng `SetActiveStep` để giả lập `PuzzleController.Play()` hoặc combat lifecycle.

### Pass 4 — cross-scene

```text
Source gameplay kết thúc sạch
→ ẩn gameplay UI/presentation
→ fade tới opaque
→ SceneLoadStep qua SceneFlow
→ destination bắt đầu dưới opaque overlay
→ normalize authored state
→ fade in
→ visible beat đầu tiên
```

Destination có `StoryDirector` riêng và `Starting Event` riêng. Không nối direct `Next Event`
qua scene.

### Pass 5 — QA

- Chạy từ beat ngay trước đó, không chỉ play event cô lập.
- Dialogue đúng speaker, không overflow và callback chỉ phát một lần.
- Puzzle/Combat chỉ nhận input sau `Play()`.
- Cancel giữa action không để callback, input token hoặc UI cũ tồn tại.
- Replay normalize về trạng thái scene-authored.
- Goal → PlayerStart không lệch, shared Player không nháy hoặc bị kéo khỏi Goal cuối scene.
- Fade che kín source và destination.
- Scene có trong Build Settings nếu load bằng tên.
- Unity compile thành công và Console không có error mới.
- Chỉ cập nhật canon ledger sau khi production flow đã chạy đúng.

<!-- END PRESERVED SOURCE -->
