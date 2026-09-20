# Buổi sáng ở nhà và đường tới trạm xe

[Mục lục nguồn](../../09_Day1_ProductionStoryWorkflow.md) · [Bản đồ docs](../../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
## 4. Story Day 1 đã triển khai

### 4.1 D1_HOME_MORNING — routine buổi sáng

**Scene:** `20_D1_Home_Morning`  
**Event:** `D1_HOME_MORNING`  
**Auto-next:** `D1_TO_BUS_STOP`

**Primary story job:** làm người chơi tin rằng Timor hiểu Audere, giúp cô giảm quá tải bằng
cách chia buổi sáng thành từng việc nhỏ.

**Established Canon:**

- Audere thức dậy muộn và muốn nằm thêm một phút.
- Timor trêu nhẹ vì biết “một phút” của Audere thường kéo dài.
- Timor không bắt cô nghĩ hết cả buổi sáng; cậu chọn đánh răng làm việc đầu tiên và đi cùng.
- Washroom là StepTile tutorial đầu tiên: chọn, đặt rồi mới học xoay path piece.
- Sau khi đánh răng, Audere thấy vị bạc hà cay nhưng tỉnh hơn.
- Bố Audere đã chuẩn bị bánh mì. Timor chuyển cô sang việc ăn sáng.
- Breakfast puzzle dạy luật phải dùng hết toàn bộ path piece trước khi hoàn thành Goal.
- Sau bữa sáng, Audere nói bước ra khỏi nhà mới là phần khó.
- Timor chuyển sự chú ý sang checklist cặp, điện thoại và chìa khóa.
- Audere kiểm tra chìa khóa lại; Timor xác nhận nó vẫn ở đó và Audere đồng ý đi.

**Relationship movement:** `Trusted guidance`. Sự giúp đỡ của Timor là thật và hữu ích;
mầm kiểm soát chỉ xuất hiện qua việc cậu luôn chọn “việc tiếp theo” cho Audere.

Hierarchy đã xác nhận:

```text
D1_HOME_MORNING [StoryEvent]
├── 00_ResetMorningPresentation       [SetActiveStep]
├── 05_PreparePuzzleSequence          [PuzzleSequencePrepareStep]
├── 10_MorningDialogue                [DialogueStep]
├── 20_RevealWashroomBoard            [BoardTileTransitionStep]
├── 30_WashroomStepTileTutorial       [PuzzleStep]
├── 40_HideWashroomBoard              [BoardTileTransitionStep]
├── 50_AfterBrushingDialogue          [DialogueStep]
├── 60_RevealBreakfastBoard           [BoardTileTransitionStep]
├── 70_PlayBreakfastPuzzle            [PuzzleStep]
├── 80_HideBreakfastBoard             [BoardTileTransitionStep]
├── 90_AfterBreakfastDialogue         [DialogueStep]
├── 100_PrepareToLeaveBeat            [WaitStep: 0.35s]
├── 120_LeavingChecklistDialogue      [DialogueStep]
├── 130_LeaveHouse                    [SetActiveStep]
└── 140_LeaveHouseBeat                [WaitStep: 0.35s]
```

Puzzle flow:

```text
PZ_D1_WASHROOM
→ hide dần, giữ transition anchor cần thiết
→ thoại sau khi đánh răng
→ align PZ_D1_BREAKFAST.PlayerStart với Goal trước
→ reveal từ PlayerStart
→ PZ_D1_BREAKFAST
```

### 4.2 D1_TO_BUS_STOP — đi tới trạm xe

**Scene:** `20_D1_Home_Morning`  
**Event:** `D1_TO_BUS_STOP`  
**Next scene:** `30_Classroom`

**Primary story job:** biến nguyên tắc “từng việc một” thành một thành công thật, củng cố cảm
giác Timor là nơi an toàn của Audere trước khi mặt hạn chế của sự bảo bọc xuất hiện.

**Established Canon:**

- Timor chỉ cho Audere mái che của bến xe và bảo cô tập trung vào đoạn ngay trước mặt.
- Bus Stop puzzle dùng board scene-first và yêu cầu dùng hết ba path piece (bản rút gọn 2026-09-20; xem `docs/Puzzles/ShortBus/README.md`).
- Khi tới Goal, path và Puzzle UI biến mất nhưng shared Player đứng nguyên trên Goal.
- Cảnh giữ yên một giây rồi phát âm thanh xe bus tới gần.
- Audere nhẹ nhõm vì vẫn kịp. Timor nhắc lại nguyên tắc từng việc một.
- Audere cảm ơn Timor; Timor đáp: “Tớ ở đây mà.”
- Sau một nhịp ngắn, màn hình fade kín rồi scene lớp học được load.

**Strongly Implied:** “Tớ ở đây mà” tạo cảm giác an toàn và củng cố thói quen Audere dựa
vào Timor để định hướng.

**Design Intent:** chính cảm giác an toàn này sẽ khiến việc từ chối sự bảo vệ của Timor trở
nên khó hơn về sau.

Hierarchy đã xác nhận:

```text
D1_TO_BUS_STOP [StoryEvent]
├── 10_PrepareBusStopPuzzle          [PuzzleSequencePrepareStep]
├── 20_RevealBusStopBoard            [BoardTileTransitionStep]
├── 30_BusStopApproachDialogue       [DialogueStep]
├── 40_PlayBusStopPuzzle             [PuzzleStep]
├── 50_SettleAtBusStop               [BoardTileTransitionStep]
├── 60_HoldAtGoal                    [WaitStep: 1.0s]
├── 65_BusApproaches                 [PlayAudioStep: Bus_Approach]
├── 70_BusStopArrivalDialogue        [DialogueStep]
├── 90_BusStopSafetyDialogue         [DialogueStep]
├── 100_HoldOnSafety                 [WaitStep: 0.4s]
├── 110_FadeToClassroom              [CanvasFadeStep: 0.6s]
└── 120_LoadClassroom                [SceneLoadStep: 30_Classroom]
```

Bus Stop là puzzle cuối scene. Không di chuyển shared Player khỏi Goal và không tắt Player
trước fade. Biểu cảm như thả lỏng vai phải nằm ở Animator/visual child, không dịch root grid.

<!-- END PRESERVED SOURCE -->
