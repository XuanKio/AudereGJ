# Story hierarchy, lifecycle, chaining và step catalog

[Mục lục nguồn](../07_StorySystem_SceneFirst.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
# Audere Story System — scene-first hierarchy flow

> **Last updated:** 2026-08-26

Story của Audere được author trực tiếp trong scene bằng `StoryDirector → StoryEvent → StoryStep`. Sibling order trong Hierarchy là thứ tự chạy.

## 1. Trạng thái quyết định

- **Established Canon:** StoryStep là component trên GameObject, không phải ScriptableObject.
- **Established Canon:** `StoryEvent` chỉ đọc direct child đang active và yêu cầu mỗi child có đúng một `StoryStep`.
- **Established Canon:** một `StoryDirector` chỉ chạy một event chính tại một thời điểm.
- **Established Canon:** Dialogue, Puzzle và Combat đều có lifecycle callback để Story chờ kết quả.
- **Design Intent:** `EventId` dành cho debug/save/transition về sau; authoring bình thường dùng direct reference.
- **Established implementation state:** choice cục bộ có `StoryChoiceBranchStep`; mỗi lựa chọn chạy
  một nested `StoryEvent` được bind trực tiếp rồi quay lại flow chung.
- **Unresolved:** StoryState bền vững, điều kiện xuyên event, save/checkpoint và graph editor chưa có.

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
    ├── 50_SettleAtBusStop          [BoardTileTransitionStep]
    ├── 60_HoldAtGoal               [WaitStep]
    ├── 65_BusApproaches            [PlayAudioStep]
    ├── 70_BusStopArrivalDialogue   [DialogueStep]
    ├── 90_BusStopSafetyDialogue    [DialogueStep]
    ├── 110_FadeToClassroom         [CanvasFadeStep]
    └── 120_LoadClassroom           [SceneLoadStep]
```

Đổi sibling order là đổi flow. Child inactive được bỏ qua. Step nằm trong group con không được thu thập ngầm.

### Tách logic khỏi presentation

Theo Scene40, gameplay runtime đặt ở root `SYSTEMS`: `Puzzle Systems` và/hoặc
`Combat Systems` tùy scene. Không đặt controller bên trong board hoặc actor art.
`WORLD` giữ Puzzle/Story/Combat presentation; `STORY` giữ runner và các step.
`WorldModeController`/fullscreen controller vẫn ở WORLD như Scene40, bind systems root
và presentation bằng direct reference. Bật presentation không tự cấp gameplay input.
Scene120 đã theo cấu trúc này; không tạo hệ thống Puzzle rỗng cho scene không có puzzle.

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
→ fade + SceneFlow.Load(GameScenes.Classroom)
→ D1_CLASSROOM_ANNOUNCEMENT trong scene `30_Classroom`
→ D1_CLASSROOM_RECESS_BIANCA
→ Story → Combat prototype → Story
→ dừng sau `230_HoldAfterCombat`
```

Cross-scene flow không dùng direct `Next Event`, vì reference đó không tồn tại sau Single
scene load. Mỗi scene sở hữu `StoryDirector` riêng; source event kết thúc bằng
`SceneLoadStep`, còn destination scene tự khởi động event đã serialize của nó.

## 5. Step hiện có

| Step | Dùng để |
| --- | --- |
| `DialogueStep` | Play `DialogueData`, chờ Completed/Cancelled; fallback tới `GameplayUIRoot.Instance.Dialogue`. |
| `PuzzleSequencePrepareStep` | Normalize level chain, đặt/giữ shared Player và tùy chọn align PlayerStart với Goal trước. |
| `BoardTileTransitionStep` | Hide/reveal board theo wave và capture Goal transition anchor. |
| `PuzzleStep` | Play trực tiếp một scene-authored `PuzzleController`, chờ `PuzzleResult`. |
| `WorldModeStep` | Gọi `WorldModeController.SwitchTo` và tùy chọn chờ transition xong. |
| `FullscreenWorldModeTransitionStep` | Chạy shared `FullscreenTransitionProfile`, swap mode ở mốc profile rồi mới Complete sau cleanup. |
| `CombatStep` | Play encounter và map Victory/Defeat/Special thành Complete/Fail/Retry/Cancel. |
| `WaitStep` | Chờ scaled/unscaled duration; mặc định unscaled. |
| `SetActiveStep` | Disable list trước, enable list sau; không rollback khi cancel. |
| `MoveActorStep` | Lerp actor tới direct Transform target; cancel dừng tại chỗ. |
| `CharacterMotionStep` | Hop/squash/landing và facing cho actor story. `TravelToTarget` dùng cho locomotion; `VerticalInPlace` khóa X/Z và trở về baseline cho phản xạ giật mình. |
| `SetActorFacingStep` | Lật trực tiếp một actor renderer ở một beat riêng, không tạo hop hoặc dịch root. |
| `SpriteGroupFadeStep` | Fade một nhóm SpriteRenderer theo authored alpha; reset/cancel không để lại alpha tạm. |
| `StoryIllustrationStep` | Mở overlay illustration screen-space bằng direct reference và chờ một click dismiss; cancel xóa owner/callback. |
| `StoryChoiceBranchStep` | Hiện choice text screen-space, chờ một click rồi chạy đúng nested `StoryEvent`; cancel đóng view và cancel branch đang chạy. |
| `StoryMessageStatusStep` | Fade status tin nhắn ngắn bằng unscaled time, dùng direct CanvasGroup/TMP reference. |
| `StoryTitleCardStep` | Fade title card screen-space và có thể giữ opaque sau khi event hoàn tất. |
| `CanvasFadeStep` | Fade một `CanvasGroup` bằng unscaled time; dùng cho source/destination scene transition. |
| `SceneLoadStep` | Load scene qua `SceneFlow`, không gọi raw `SceneManager.LoadScene`. |
| `PlayAudioStep` | Phát một `AudioId`; có thể cho phép placeholder/missing service chỉ warning rồi tiếp tục. |
| `DebugStoryStep` | Log/delay để kiểm tra runner, không dùng làm canon content. |

<!-- END PRESERVED SOURCE -->
