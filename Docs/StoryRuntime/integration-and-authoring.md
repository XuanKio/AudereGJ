# Tích hợp gameplay và quy tắc authoring

[Mục lục nguồn](../07_StorySystem_SceneFirst.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
## 6. Quy tắc tích hợp

### Dialogue

```text
DialogueResult.Completed → DialogueStep Completed
DialogueResult.Cancelled → DialogueStep Cancelled
```

Không overwrite dialogue đang chạy. Khi Story cancel, step chỉ `ForceClose` dialogue do chính session đó mở. Dialogue tạm phủ input Puzzle/Combat và khôi phục mode trước khi đóng.

### Puzzle

`PuzzleStep` tham chiếu trực tiếp `PuzzleController` trong scene/prefab. Nó không bake/generate layout. Nếu controller đã Play trước đó, step fail thay vì chiếm session.

Trong puzzle chain, dùng `PuzzleSequencePrepareStep` và `BoardTileTransitionStep`; không mô phỏng chuyển map bằng một chuỗi `SetActiveStep` rời rạc.

### Combat

Mặc định:

```text
Victory → Complete
Defeat  → Retry
Special → Complete
```

Combat cốt truyện bắt buộc Audere thua đặt `Defeat Behaviour = Complete`. Retry giữ StoryEvent đứng tại đúng `CombatStep`, không chạy lại các step trước combat.

### World mode và input

`WorldModeStep` chỉ đổi presentation/world root. Input chỉ được claim khi controller thực sự `Play()`:

```text
PuzzleController.Play → Puzzle input
CombatController.Play → Combat input
DialogueController.Play → Dialogue overlay input
```

Ba presentation mode dùng chung là `Puzzle`, `Combat`, `Story`. Giá trị enum cũ của Puzzle
và Combat không đổi để giữ serialized scene. `Story` có camera pose và story root riêng; mode
này không phải gameplay session và không tự cấp input.

`FullscreenWorldModeTransitionStep` cũng chỉ đổi presentation. Scene tham chiếu một shared
profile asset và focus renderer khi profile yêu cầu; step dùng unscaled time và chờ distortion
sạch hoàn toàn mới cho Story chạy tới
`CombatStep`. Cancel ở cả trước và sau mode swap phải tắt renderer feature, reset material và
khôi phục `sourceMode`; không được để Combat root active mà chưa có Combat input owner.

## 7. Quy tắc authoring flow puzzle liên tiếp

```text
PuzzleStep A
→ BoardTileTransitionStep (capture Goal A, giữ anchor)
→ Dialogue/Wait nếu cần
→ PuzzleSequencePrepareStep B (Align To Previous Goal)
→ BoardTileTransitionStep reveal B
→ PuzzleStep B
```

Lưu ý:

- Không tắt shared Player giữa hai event; `130_LeaveHouse` hiện không còn disable Player.
- Không tắt source root ngay sau hide nếu còn đoạn thoại cần tile anchor dưới chân Player.
- Coordinator tự ẩn source cũ lúc board mới reveal.
- Không thêm `Play Puzzle` vào `WorldModeStep` hoặc `SetActiveStep`.
- Khi replay/cancel, `normalizeOnCancel` phải trỏ về prepare step phù hợp để presentation không kẹt giữa tween.

## 8. Checklist khi thêm StoryEvent thật

- [ ] `EventId` duy nhất và có ý nghĩa.
- [ ] Mỗi direct child có đúng một `StoryStep`.
- [ ] Child name có prefix số để order đọc rõ trong Hierarchy.
- [ ] Tất cả reference dùng direct Inspector reference; không dùng global Find.
- [ ] Dialogue/Puzzle/Combat đang bận thì step fail có log, không overwrite.
- [ ] Cancel giữa step trả đúng một lần và không chạy step sau.
- [ ] Replay không nhận callback/token/session cũ.
- [ ] Auto-next chỉ bật khi `Next Event` hợp lệ và không tạo self/direct cycle.
- [ ] Puzzle hand-off giữ Player active và Goal trước trùng PlayerStart sau.
- [ ] Console không có error trong flow bình thường.

## 9. Chưa được phép suy thành canon

Các `TEST_*`, `DebugStoryStep`, sample dialogue/encounter và nội dung brainstorm chỉ là công cụ kiểm thử. Chúng không xác nhận cốt truyện, tính cách hay thứ tự canon nếu chưa được ghi rõ trong tài liệu narrative chính thức.

<!-- END PRESERVED SOURCE -->
