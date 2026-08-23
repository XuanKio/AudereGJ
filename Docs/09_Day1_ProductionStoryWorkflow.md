# Audere Day 1 — Production Story & Authoring Workflow

> **Last verified:** 2026-08-23  
> **Unity:** 6000.0.79f1  
> **Production scenes:** `20_Game`, `30_Classroom`

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
→ 20_Game (build 2)
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

## 4. Story Day 1 đã triển khai

### 4.1 D1_HOME_MORNING — routine buổi sáng

**Scene:** `20_Game`  
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

**Scene:** `20_Game`  
**Event:** `D1_TO_BUS_STOP`  
**Next scene:** `30_Classroom`

**Primary story job:** biến nguyên tắc “từng việc một” thành một thành công thật, củng cố cảm
giác Timor là nơi an toàn của Audere trước khi mặt hạn chế của sự bảo bọc xuất hiện.

**Established Canon:**

- Timor chỉ cho Audere mái che của bến xe và bảo cô tập trung vào đoạn ngay trước mặt.
- Bus Stop puzzle dùng board scene-first và yêu cầu dùng hết bốn path piece.
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

### 4.3 D1_CLASSROOM_ANNOUNCEMENT — cơ hội đầu tiên bị khép lại

**Scene:** `30_Classroom`  
**Event:** `D1_CLASSROOM_ANNOUNCEMENT`  
**Auto-next:** `D1_CLASSROOM_RECESS_BIANCA`.

**Primary story job:** cho người chơi thấy cùng kiểu “giúp Audere đỡ phải nghĩ” bắt đầu lấy
mất một lựa chọn nhỏ của cô, nhưng Timor vẫn nghe như đang quan tâm.

**Established Canon:**

- Scene bắt đầu dưới overlay đen, normalize presentation rồi đặt Audere tại chỗ ngồi cũ.
- Timor xác nhận chỗ ngồi quen thuộc và nói mọi thứ ổn.
- Giáo viên xuất hiện, ổn định lớp và thông báo buổi liên hoan cuối năm.
- Lớp sẽ cùng chuẩn bị trang trí, đồ ăn và trò chơi.
- Giáo viên cho mỗi học sinh chọn một việc vừa sức, không cần vội hoặc làm thật nhiều.
- Audere chú ý tới phần trang trí; interest tile hiện và cô nhích về phía trước.
- Timor hỏi liệu cô có thích không. Audere ban đầu không biết, rồi thừa nhận: “Chắc là có.”
- Timor công nhận thích phần đó cũng không sao, nhưng nói hai người chưa cần ghi tên ngay.
  Cậu nhận xét Audere chưa ngồi yên phút nào từ sáng, bảo cô nghỉ và để chuyện đó tính sau.
- Audere quay về chỗ cũ, interest tile biến mất và cô đáp: “…Ừm.”

**Dialogue polish:** announcement được chia thành bubble ngắn, mỗi bubble chỉ giữ một ý:
ổn định lớp → báo chuyện vui → liên hoan → các phần việc → lựa chọn vừa sức → không cần vội.
Không nhét toàn bộ lời dặn và reassurance vào cùng một line.

**Relationship movement:** từ `Trusted guidance` chạm sang `Protective pre-emption`. Timor
không đe dọa hay công khai áp đặt; cậu đóng lựa chọn bằng lý do nghỉ ngơi hợp lý.

Hierarchy đã xác nhận:

```text
D1_CLASSROOM_ANNOUNCEMENT [StoryEvent]
├── 00_CoverScene                   [CanvasFadeStep: instant opaque]
├── 05_NormalizePresentation       [SetActiveStep]
├── 08_PlaceAudereAtSeat           [MoveActorStep: snap]
├── 10_FadeIn                      [CanvasFadeStep: 0.65s]
├── 20_SeatDialogue                [DialogueStep]
├── 30_ShowTeacher                 [SetActiveStep]
├── 40_TeacherOpening              [DialogueStep]
├── 50_AnnouncementPause           [WaitStep: 0.5s]
├── 60_TeacherEvent                [DialogueStep]
├── 70_ClassMurmur                 [PlayAudioStep: Classroom_Murmur]
├── 80_MurmurBeat                  [WaitStep: 0.35s]
├── 90_TeacherDetails              [DialogueStep]
├── 95_ClassSettles                [WaitStep: 0.2s]
├── 100_NoticeDecoration           [DialogueStep]
├── 110_RevealInterestTile         [BoardTileTransitionStep]
├── 120_AudereLeansForward         [MoveActorStep: 0.16s]
├── 130_TimorAsks                  [DialogueStep]
├── 140_AudereSmallHop             [CharacterMotionStep: vertical in-place]
├── 160_AudereAdmits               [DialogueStep]
├── 170_TimorClosesChoice          [DialogueStep]
├── 180_AudereStops                [WaitStep: 0.28s]
├── 190_TimorProtects              [DialogueStep]
├── 200_ReturnToSeat               [MoveActorStep: 0.2s]
├── 210_HideInterestTile           [BoardTileTransitionStep]
└── 220_AudereYields               [DialogueStep]
```

Teacher hiện dùng prefab/visual placeholder. Tên riêng, portrait chính thức, tuổi và lịch sử
của cô vẫn là **Unresolved**.

### 4.4 D1_CLASSROOM_RECESS_BIANCA — lời mời bình thường bị chặn trước

**Scene:** `30_Classroom`  
**Event:** `D1_CLASSROOM_RECESS_BIANCA`  
**Auto-next:** tắt; event dừng sau khi combat prototype trả presentation về Story.

**Primary story job:** đặt một lời mời xã hội nhỏ và có đường lui trước Audere, rồi cho thấy
Timor tiến thêm một bước trong việc giành quyền định hướng phản ứng của cô.

**Established Canon:**

- Sau khi giáo viên nói xong, scene fade nhẹ sang giờ nghỉ.
- Bianca tiến tới từ bên phải bằng các hop ngắn: tile trước mặt hiện, cô nhảy tới, tile phía
  sau mờ đi. Khoảng cách tile cùng nhịp với board StepTile hiện tại.
- Bianca gọi “Audere?” nhưng Audere chưa phản ứng. Bianca nhích gần một chút.
- Audere bật lên một nhịp giật mình tại chỗ rồi quay sang phải nhìn Bianca.
- Bianca xin lỗi, nói mình đang phụ trang trí và mời Audere cùng làm bảng một chút.
- Bianca không thúc ép; cô chủ động mở đường lui: “Không tiện cũng không sao.”
- Audere im lặng. Timor bảo cô đừng trả lời vội, nhìn cậu và để cậu giúp.
- Sequence giữ một nhịp sau câu của Timor rồi dùng shared profile `Dreamy Disorientation`:
  nghiêng/zoom nhẹ, wave rộng, scene trôi, radial bend và smear quanh Audere. Combat hiện qua
  lớp distortion đang hạ xuống; kết thúc prototype vẫn fade
  về đúng khung Story trước đó.
- Enemy, ý nghĩa combat và kết quả narrative chính thức vẫn **Unresolved**. Tên/art prototype
  không được dùng làm bằng chứng canon.

**Relationship movement:** vẫn ở `Protective pre-emption`, nhưng rõ hơn beat trước: Bianca đã
cho Audere không gian lựa chọn, còn Timor chen vào đúng khoảng trống trước khi cô tự trả lời.
Cậu vẫn dùng ngôn ngữ giúp đỡ, không chuyển đột ngột sang đối đầu công khai.

**Presentation contract:** standing anchor ở giữa bàn chân của Audere, Teacher và Bianca
trùng tâm tile tương ứng. Không căn bằng pivot giữa thân của sprite. `TileHop` được phép đổi X/Y để tới tile kế; riêng
`StartleHop` dùng `VerticalInPlace`, khóa X/Z và chỉ tạo một cung nhảy trên trục Y.

**Dialogue rhythm:** Bianca gọi tên → pause không phản ứng → nhích gần → Audere giật mình →
`Xin lỗi!` thành một beat riêng → Bianca hỏi ngắn → nêu việc → mời trong phạm vi nhỏ → mở
đường lui. Timor chỉ chen vào sau khoảng im lặng của Audere.

Hierarchy đã xác nhận:

```text
D1_CLASSROOM_RECESS_BIANCA [StoryEvent]
├── 00_FadeToRecess                  [CanvasFadeStep]
├── 05_NormalizeRecess              [SetActiveStep]
├── 08_PlaceAudereAtSeat            [MoveActorStep]
├── 10_PlaceBiancaAtStart           [MoveActorStep]
├── 15_FadeInRecess                 [CanvasFadeStep]
├── 20_RecessBeat                   [WaitStep]
├── 30_RevealBiancaMidTile          [BoardTileTransitionStep]
├── 40_BiancaHopsToMid              [CharacterMotionStep]
├── 50_HideBiancaStartTile          [BoardTileTransitionStep]
├── 60_RevealDecorationTile         [BoardTileTransitionStep]
├── 70_BiancaHopsTowardAudere       [CharacterMotionStep]
├── 80_HideBiancaMidTile            [BoardTileTransitionStep]
├── 90_BiancaCalls                  [DialogueStep]
├── 100_AudereDoesNotRespond        [WaitStep]
├── 110_BiancaNudgesCloser          [MoveActorStep]
├── 120_AudereStartlesAndTurns      [CharacterMotionStep]
├── 130_BiancaApologizes            [DialogueStep]
├── 140_BiancaInvites               [DialogueStep]
├── 150_BiancaWaits                 [WaitStep]
├── 160_BiancaLeavesRoom            [DialogueStep]
├── 170_AudereStaysSilent           [WaitStep]
├── 180_TimorIntervenes             [DialogueStep]
├── 190_HoldAfterTimor              [WaitStep]
├── 200_ClassroomIsConsumed         [FullscreenWorldModeTransitionStep]
├── 210_PlayCombatPrototype         [CombatStep]
├── 220_ReturnToStory               [WorldModeStep: Story]
└── 230_HoldAfterCombat             [WaitStep]
```

Bianca đang dùng prefab `Bianca_PLACEHOLDER`; portrait và art chính thức là **Unresolved**.
Motion contract và cách thay Animator sau này nằm tại `Docs/10_CharacterExpressionAndMotion.md`.
Fullscreen shader, timeline, cancel và replay contract nằm tại
`Docs/11_FullscreenWorldTransitions.md`.

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
│   └── Classroom/
│       ├── Dialogue_D1_CLASSROOM_*.asset
│       ├── Dialogue_D1_CLASSROOM_BIANCA_*.asset
│       └── Dialogue_D1_TEACHER_*.asset
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

## 8. Điểm chưa triển khai

- **Implemented prototype:** hand-off kỹ thuật Story → Combat → Story sau câu Timor.
- **Unresolved:** combat tutorial nội tâm, enemy/boss, ý nghĩa combat và result mapping canon.
- **Unresolved:** tin nhắn ban đêm, Choice UI và kết quả lựa chọn.
- **Unresolved:** StoryState, conditional branching, save/checkpoint và resume giữa event.
- **Unresolved:** Timor có được người khác nhìn/nghe thấy hay không.
- **Unresolved:** tên riêng/portrait chính thức của Teacher; portrait/art chính thức của Bianca
  và các NPC lớp học khác.
- **Unresolved:** cách gọi nhất quán cho washroom action nếu asset/thoại lại lệch giữa rửa mặt
  và đánh răng trong tương lai. Production hiện tại đã dùng “đánh răng”.

## 9. File tham chiếu chính

| Nội dung | File |
| --- | --- |
| Story runner và step catalog | `Docs/07_StorySystem_SceneFirst.md` |
| Puzzle scene-first và hand-off | `Docs/04_PuzzleGameplay_SteptileArchitecture.md` |
| Dialogue lifecycle/data | `Docs/05_DialogueSystem.md` |
| Character expression/motion contract | `Docs/10_CharacterExpressionAndMotion.md` |
| Bootstrap và scene loading | `Docs/02_Bootstrap.md` |
| Voice/canon ledger | `.agents/skills/audere-dialogue-voice/references/` |
| Production scene sáng Day 1 | `Assets/_Audere/Scenes/20_Game.unity` |
| Production scene lớp học | `Assets/_Audere/Scenes/30_Classroom.unity` |

Khi production scene và tài liệu này khác nhau, kiểm tra `DialogueData` và Hierarchy hiện tại
trước; sau khi QA, cập nhật lại doc thay vì đoán flow từ tên file.
