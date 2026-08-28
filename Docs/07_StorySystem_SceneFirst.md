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

## 10. Production flow hiện tại

- `20_D1_Home_Morning/STORY` chỉ giữ `D1_HOME_MORNING` và `D1_TO_BUS_STOP`; các `TEST_*` cũ đã được
  loại khỏi production scene.
- Bus-stop goal được giữ làm anchor trong lúc path tile và Puzzle UI biến mất. Sau một nhịp
  yên, SFX xe bus và hai đoạn thoại kết beat trước khi fade sang lớp học.
- Vì Bus Stop là puzzle cuối scene, shared Player đứng nguyên trên Goal cho tới lúc fade.
  Không dùng `MoveActorStep` để diễn tả pose/thả lỏng vì step đó dịch root gameplay; biểu cảm
  về sau phải nằm ở Animator hoặc visual child mà không đổi vị trí grid.
- `30_Classroom` author trực tiếp actor, tile, staging target và
  `D1_CLASSROOM_ANNOUNCEMENT` nối sang `D1_CLASSROOM_RECESS_BIANCA`. Interest tile xuất hiện, Audere nhích tới rồi quay về chỗ cũ
  theo `MoveActorStep`/`SetActiveStep`, không hardcode trong dialogue controller.
- Event giờ nghỉ dùng `CharacterMotionStep` cho Bianca nhảy từng tile và cho Audere giật mình
  tại chỗ rồi quay sang phải. Sau thoại Timor, event dùng
  `FullscreenWorldModeTransitionStep → CombatStep → WorldModeStep`: vào Combat bằng shared
  profile `Dreamy Disorientation`, rồi trở lại Story bằng fade đen và khôi phục nguyên bố cục.
- Combat prototype chỉ xác nhận contract kỹ thuật. Enemy, ý nghĩa narrative và kết quả canon
  vẫn `Unresolved`; không suy chúng từ art/name placeholder.
- Classroom staging trình bày ngang: Audere đứng bên trái, Teacher đứng bên phải trên cùng
  baseline và cả cặp được cân giữa khung hình. Hai Student placeholder/tile đã được bỏ khỏi
  production scene để beat chỉ tập trung vào Audere và Teacher. Teacher là prefab riêng tại
  `Assets/_Audere/Prefabs/Story/Characters/Teacher.prefab`; sprite hiện tại vẫn là placeholder
  và được thay trực tiếp trong prefab khi có art chính thức.
- Các object có hậu tố `PLACEHOLDER` là presentation tạm, không tự xác nhận thiết kế canon.
- `40_Evening` thay placeholder bằng `D1_HOME_NIGHT_MESSAGE`. Audere và Night Tile được author
  dưới `WORLD/Story Root`, cùng trục X; Story Root dùng cùng staging space với Scene 30 (`0.25`),
  Audere dùng scale `1.5`, body/shadow `Player 5/4`, còn tâm Night Tile khớp tâm camera.
  `PuzzleViewportMask` giữ transform prefab như Scene 20/30. Event dùng direct references theo thứ tự:
  dialogue → `Message_Arrive` → bật `dauchamthan` → `VerticalInPlace` startle → Audere nhận ra
  Bianca → ẩn alert → Bianca message → Timor biến một câu trả lời thành chuỗi hậu quả chắc chắn →
  Audere mất điểm tựa và Timor đóng lựa chọn → `Dreamy Disorientation` → Defeat-only
  CombatStep → hazard freeze/fade + hậu thoại Defeat → neutral `WorldModeStep` về Story →
  ba lựa chọn tin nhắn → nested branch → lights out → `Ngày 1 - Kết thúc`. Alert được normalize ẩn trước FadeIn và authoring
  tool bảo toàn chính scene object/transform do designer đặt.
  Scene cũng giữ một root prefab `GameplayUIRoot` như Scene 20/30; Bootstrap không sở hữu UI này.
- Encounter đêm không dùng `SetActiveStep` để giả lifecycle. `CombatController.Play()` vẫn là nơi
  claim input; `CombatStep` map Victory/Special thành Fail và Defeat thành Complete, nên không có
  Retry và event chỉ tiếp tục đúng một lần sau scripted defeat.
- Choice UI của Scene 40 là root Screen Space Overlay `NIGHT MESSAGE UI`, reference resolution
  `1920×1080`, sorting order `1300`. Text đặt ở vùng thấp giống khu path-piece; idle nhỏ/mờ,
  pointer hover thêm `> <` và trả lại scale/alpha đầy đủ. `StoryChoiceBranchStep` giữ ba branch
  thành nested `StoryEvent`, không đưa switch narrative vào `StoryEvent` runner.
- SFX bus/classroom hiện là clip placeholder được map qua `AudioCatalog`; có thể thay clip tại
  catalog mà không sửa StoryEvent.
- `50_D2_Home_Morning` sao chép presentation nhà/bến xe nhưng sở hữu StoryEvent
  `D2_HOME_MORNING → D2_TO_BUS_STOP`, DialogueData Day 2 và ba board scene-authored riêng.
  Washroom reveal đặt `25_OneUseTileTutorial` trước PuzzleStep; tile đỏ dùng traversal-rule
  component chung, không có nhánh theo scene hoặc puzzle ID. Scene kết thúc tại bến xe vì
  destination Day 2 tiếp theo còn `Unresolved` tại checkpoint này; production continuation hiện xem Docs13–15.

## Day3 extension

`90_D2_Home_Awakening → 100_D3_Home_Morning → 110_D3_School_Board → 120_D3_School_Teacher` giữ StoryDirector/Event/direct-step contract. Scene110 dùng ParallelStoryStep để vừa hop vừa thoại, `ChalkDrawingStep` chờ modal drawing completion, rồi `FullscreenPresentationStep` song song `AutoDialogueStep` cho đoạn chóng mặt. Không có narrative switch trong manager, không dùng SetActiveStep để thay combat lifecycle. Chi tiết scene, lời thoại, ownership và QA: [15_Day3_BoardTeacher_StoryWorkflow](15_Day3_BoardTeacher_StoryWorkflow.md).
