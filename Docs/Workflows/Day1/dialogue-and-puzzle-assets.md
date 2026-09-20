# DialogueData và puzzle production

[Mục lục nguồn](../../09_Day1_ProductionStoryWorkflow.md) · [Bản đồ docs](../../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
## 5. DialogueData production

Dialogue được chia theo ngày rồi địa điểm:

```text
Assets/_Audere/Data/Dialogue/
├── DialogueCharacterCatalog.asset
├── Day1/
│   ├── Home/
│   │   ├── Dialogue_D1_HOME_MORNING.asset
│   │   ├── Dialogue_D1_AFTER_BRUSHING.asset
│   │   ├── Dialogue_D1_AFTER_BREAKFAST.asset
│   │   └── Dialogue_D1_LEAVING_CHECKLIST.asset
│   ├── BusStop/
│   │   ├── Dialogue_D1_BUS_STOP_APPROACH.asset
│   │   ├── Dialogue_D1_BUS_STOP_ARRIVAL.asset
│   │   └── Dialogue_D1_BUS_STOP_SAFE.asset
│   ├── Classroom/
│   │   ├── Dialogue_D1_CLASSROOM_*.asset
│   │   ├── Dialogue_D1_CLASSROOM_BIANCA_*.asset
│   │   ├── Dialogue_D1_TEACHER_*.asset
│   │   └── Combat/Dialogue_D1_COMBAT_TUTORIAL_*.asset
│   └── Evening/
│       ├── Dialogue_D1_HOME_NIGHT_*.asset
│       └── Dialogue_D1_TIMOR_NIGHT_PRESSURE_BARK_*.asset
└── Samples/
    └── Dialogue_Sample.asset
```

Không đặt movement, reveal, wait hoặc audio timing vào `DialogueController`. Khi cần staging
xen giữa các câu, tách dialogue thành asset nhỏ rồi đặt action thành StoryStep riêng.

## 6. Puzzle production workflow

Layout puzzle là scene/prefab-first:

```text
Puzzle Root
├── Player                 shared
├── Puzzle Runtime         shared
│   ├── Path Placement Controller
│   ├── Path Preview
│   └── Placed Path Root
├── PZ_D1_WASHROOM
├── PZ_D1_BREAKFAST
└── PZ_D1_BUS_STOP
```

Mỗi `PZ_*` giữ board, Goal, PlayerStart và config riêng; không giữ thêm một Player hoặc
PathPreview. `PuzzleData` không sinh lại layout khi Play.

Flow nối hai puzzle:

```text
Puzzle A Completed
→ hide board A, capture Goal A làm anchor
→ dialogue/wait nếu có
→ prepare Puzzle B với Align To Previous Goal
→ dịch level B để PlayerStart B trùng Goal A
→ reveal board B từ PlayerStart
→ Puzzle B Play
```

Puzzle cuối scene giữ Player trên Goal cho tới fade; không chuẩn bị một PlayerStart không tồn
tại trong scene kế tiếp.

<!-- END PRESERVED SOURCE -->
