# Audere Story System — scene-first hierarchy flow

> **Last updated:** 2026-08-22

Story của Audere được author trực tiếp trong scene bằng `StoryDirector → StoryEvent → StoryStep`. Sibling order trong Hierarchy là thứ tự chạy.

## 1. Trạng thái quyết định

- **Established Canon:** StoryStep là component trên GameObject, không phải ScriptableObject.
- **Established Canon:** `StoryEvent` chỉ đọc direct child đang active và yêu cầu mỗi child có đúng một `StoryStep`.
- **Established Canon:** một `StoryDirector` chỉ chạy một event chính tại một thời điểm.
- **Established Canon:** Dialogue, Puzzle và Combat đều có lifecycle callback để Story chờ kết quả.
- **Design Intent:** `EventId` dành cho debug/save/transition về sau; authoring bình thường dùng direct reference.
- **Unresolved:** Choice, conditional branching, StoryState, save/checkpoint và graph editor chưa có.

## 2. Hierarchy và thứ tự

```text
STORY [StoryDirector]
├── D1_HOME_MORNING [StoryEvent]
│   ├── 00_ResetMorningPresentation [SetActiveStep]
│   ├── 05_PreparePuzzleSequence    [PuzzleSequencePrepareStep]
│   ├── 10_MorningDialogue          [DialogueStep]
│   ├── 20_RevealWashroomBoard      [BoardTileTransitionStep]
│   ├── 30_WashroomTutorial         [PuzzleStep]
│   ├── 40_HideWashroomBoard        [BoardTileTransitionStep]
│   ├── 50_AfterBrushingDialogue    [DialogueStep]
│   ├── 60_RevealBreakfastBoard     [BoardTileTransitionStep]
│   ├── 70_PlayBreakfastPuzzle      [PuzzleStep]
│   ├── 80_HideBreakfastBoard       [BoardTileTransitionStep]
│   ├── 90_AfterBreakfastDialogue   [DialogueStep]
│   └── ...
└── D1_TO_BUS_STOP [StoryEvent]
    ├── 10_PrepareBusStopPuzzle     [PuzzleSequencePrepareStep]
    ├── 20_RevealBusStopBoard       [BoardTileTransitionStep]
    ├── 30_BusStopApproachDialogue  [DialogueStep]
    ├── 40_PlayBusStopPuzzle        [PuzzleStep]
    └── ...
```

Đổi sibling order là đổi flow. Child inactive được bỏ qua. Step nằm trong group con không được thu thập ngầm.

## 3. Lifecycle

`StoryStepState`:

```text
Idle → Running → Completed | Cancelled | Failed
```

`StoryEvent` mapping:

- Step `Completed`: chạy sibling kế tiếp.
- Step `Cancelled`: dừng event với `StoryEventResult.Cancelled`.
- Step `Failed`: dừng event với `StoryEventResult.Failed`.
- Disable/cancel khi đang chạy: cancel current step và phát kết quả đúng một lần.

Callback được xóa trước khi gọi. Mỗi Dialogue/Puzzle/Combat step có session/version ownership để callback cũ không ảnh hưởng replay.

## 4. StoryDirector và chaining

API chính:

```csharp
director.PlayEvent(eventReference, OnEnded);
director.PlayEventById("D1_HOME_MORNING", OnEnded);
director.CancelCurrentEvent();
```

`StoryDirector`:

- register các `StoryEvent` dưới `Story Events Root`;
- cảnh báo duplicate/empty `EventId`;
- từ chối event mới nếu event hiện tại còn chạy;
- clear `CurrentEvent` trước khi auto-play `Next Event`;
- defer auto-next sang frame sau và dùng generation/version để cancel deferred callback cũ;
- chỉ auto-next khi event trước `Completed`.

Flow Day 1 hiện tại:

```text
D1_HOME_MORNING Completed
→ frame kế tiếp
→ D1_TO_BUS_STOP
```

## 5. Step hiện có

| Step | Dùng để |
| --- | --- |
| `DialogueStep` | Play `DialogueData`, chờ Completed/Cancelled; fallback tới `GameplayUIRoot.Instance.Dialogue`. |
| `PuzzleSequencePrepareStep` | Normalize level chain, đặt/giữ shared Player và tùy chọn align PlayerStart với Goal trước. |
| `BoardTileTransitionStep` | Hide/reveal board theo wave và capture Goal transition anchor. |
| `PuzzleStep` | Play trực tiếp một scene-authored `PuzzleController`, chờ `PuzzleResult`. |
| `WorldModeStep` | Gọi `WorldModeController.SwitchTo` và tùy chọn chờ transition xong. |
| `CombatStep` | Play encounter và map Victory/Defeat/Special thành Complete/Fail/Retry/Cancel. |
| `WaitStep` | Chờ scaled/unscaled duration; mặc định unscaled. |
| `SetActiveStep` | Disable list trước, enable list sau; không rollback khi cancel. |
| `MoveActorStep` | Lerp actor tới direct Transform target; cancel dừng tại chỗ. |
| `DebugStoryStep` | Log/delay để kiểm tra runner, không dùng làm canon content. |

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

