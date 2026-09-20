# Goal hand-off, reveal và input ownership

[Mục lục nguồn](../04_PuzzleGameplay_SteptileArchitecture.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
## 8. Chuyển tiếp Goal → PlayerStart

Flow chuẩn giữa hai puzzle:

```text
Puzzle A Completed
→ BoardTileTransitionStep hide dần board A, giữ Goal A
→ Goal visual/item được ẩn; tile nền trở thành transition anchor
→ dialogue/beat (Player vẫn đứng trên anchor)
→ PuzzleSequencePrepareStep cho B, Align To Previous Goal = true
→ dịch toàn bộ level B để PlayerStart B trùng world position Goal A
→ BoardTileTransitionStep reveal B từ PlayerStart
→ PuzzleStep.Play B
```

Quy tắc quan trọng:

- `goalToBecomeAnchor` phải trỏ đúng Goal của puzzle nguồn.
- Trong đoạn dialogue cần giữ tile dưới chân Player, không gán `rootToDisableAfterHide` cho source level.
- `PuzzleRootCoordinator` tự ẩn transition source cũ khi board kế tiếp bắt đầu reveal, tránh hai Goal/start tile cùng hiện.
- `PuzzleSequencePrepareStep.Align To Previous Goal` bật cho event/location tiếp theo như Breakfast → Bus Stop.
- `PuzzleController` giữ pose level đã prepare qua lần `Play()` kế tiếp; không chen một normalize khác làm restore authored root.
- Không đưa shared Player vào `SetActiveStep.Objects To Disable` giữa hai puzzle. Việc tắt ở cuối event rồi bật lại trong event kế tiếp tạo một frame nháy.
- `WorldModeStep` chỉ đổi world mode; không đặt Player, không Play puzzle và không cấp input.

Regression đã sửa ngày 2026-08-22:

- Goal Washroom cũ từng còn hiện lệch chéo sau Breakfast.
- Goal Breakfast từng bị tắt cùng root nên Player trông như đứng giữa không trung.
- `130_LeaveHouse` từng tắt Player rồi `PrepareBusStopPuzzle` bật lại, gây nháy.
- Sau sửa, Breakfast Goal → Bus PlayerStart có khoảng cách world `0`; tile reveal đầu tiên là `Tile_Street_0_0` và Player luôn active.

## 9. Animation hide/reveal board

`BoardTileTransitionStep` dùng style:

```text
fade nhẹ + nhô từ dưới + overshoot rất nhỏ
```

Thiết lập Day 1 hiện xoay quanh:

- transition mỗi tile `0.20–0.24 s`;
- cả reveal wave khoảng `0.95 s`;
- vertical offset khoảng `0.065–0.08`;
- overshoot khoảng `0.01–0.012`;
- unscaled time để không bị dialogue/timeScale làm treo.

Thứ tự reveal được tính từ `PlayerStart`. Trước khi sort, coordinator gọi `BoardManager.RegisterExistingTiles()` để grid position phản ánh pose level sau khi đã align.

`BoardTileTransitionStep` phát `Tile_Pop` khi tile bắt đầu biến mất hoặc vừa xuất hiện. Clip
được throttle tối thiểu `0.11 s`, nên board lớn vẫn có nhịp âm thanh theo wave mà không phát
chồng một one-shot cho mọi tile trong cùng frame. Khi Player bắt đầu rời tile an toàn để rơi,
`PuzzleManager.HandleFallStarted()` phát `Player_Fall`; sound không chờ tới lúc reset map.

## 10. Input ownership

`GameplayInputGate` nằm dưới `GameplayUIRoot` và quản lý claim theo token/owner:

```text
None
PuzzleController.Play → Puzzle
DialogueController.Play trên Puzzle → Dialogue
Dialogue close → tự trở lại Puzzle
Puzzle complete/cancel/disable → None
```

Session cũ chỉ release token do chính nó tạo. Không dùng một biến mode global để callback cũ có thể tắt input của lượt replay mới.

`PuzzleStep` và `BoardTileTransitionStep` gọi `NormalizeAfterCancel()` khi hủy trong scene còn sống. Hàm này kiểm tra reference của toàn bộ chuỗi trước khi reset. Khi step/scene đang bị disable hoặc UI/Player đã bị hủy, chỉ cleanup ownership; không gọi normalize để dựng lại board. `NormalizeNow()` trong flow chuẩn vẫn báo lỗi nếu authoring thiếu reference.

Time Scale mặc định của project phải là `1`. Không dùng giá trị `0` lưu trong TimeManager để pause story; dialogue giữ và trả lại giá trị trước pause. Traversal dùng scaled time nên giá trị mặc định `0` sẽ làm puzzle đứng giữa bước đi.

<!-- END PRESERVED SOURCE -->
