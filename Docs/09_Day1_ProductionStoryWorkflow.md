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
**Auto-next:** tắt; sau Victory, event tự hoàn tất phần hậu combat rồi load `40_Evening` qua
`SceneLoadStep`. Defeat giữ event tại CombatStep bằng Retry UI.

**Primary story job:** đặt một lời mời xã hội nhỏ và có đường lui trước Audere, cho cô tự giành lại
một câu trả lời sau khi đối diện nỗi lo, rồi khép Day 1 lớp học bằng một lựa chọn nhỏ đã được thực hiện.

**Established Canon:**

- Sau khi giáo viên nói xong, scene fade nhẹ sang giờ nghỉ.
- Bianca tiến tới từ bên phải bằng các hop ngắn: tile trước mặt hiện, cô nhảy tới, tile phía
  sau mờ đi. Khoảng cách tile cùng nhịp với board StepTile hiện tại.
- Bianca gọi “Audere?” nhưng Audere chưa phản ứng. Bianca nhích gần một chút.
- Audere bật lên một nhịp giật mình tại chỗ rồi quay sang phải nhìn Bianca.
- Bianca xin lỗi, nói mình đang phụ trang trí và mời Audere cùng làm bảng một chút.
- Bianca không thúc ép; cô chủ động mở đường lui: “Không tiện cũng không sao.”
- Sau khoảng lặng của Bianca, Audere nhận ra tay mình run. Cô muốn trả lời nhưng trong đầu chỉ
  bật lên ý nghĩ “trốn đi”.
- Timor bảo cô đừng trả lời vội, nhìn cậu, rồi chỉ ra nỗi lo đang trả lời thay cô. Audere nói
  mình không muốn nó chọn thay nữa và tự quyết định sẽ đối diện với nó; Timor ở lại bên cô.
- Sequence giữ một nhịp sau câu của Timor rồi dùng shared profile `Dreamy Disorientation`:
  nghiêng/zoom nhẹ, wave rộng, scene trôi, radial bend và smear quanh Audere. Combat hiện qua
  lớp distortion đang hạ xuống; kết thúc prototype vẫn fade
  về đúng khung Story trước đó.
- **Design Intent:** prototype kỹ thuật dùng boss display name `Khoảng Lặng`, một phase `6 HP`
  và art `PLACEHOLDER`. Ba pattern Aimed Fan, Side Sweep, Rain chạy trong cùng phase. Tên/placement
  và framing nỗi lo không tự xác lập ontology cuối cùng của boss.
- **Unresolved:** ý nghĩa combat, voice/canon dialogue/portrait/art chính thức, tên hoặc ý nghĩa
  phase, final moveset/balance và kết quả narrative. Prototype không phải bằng chứng canon.

**Implemented Design Intent sau Victory:** Audere trả lời `Tớ muốn thử.` nhưng không hết run.
Bianca chỉ xác nhận đây là phần làm bảng, lùi ngắn sang phải về đúng tâm tile rồi nói `Được`;
không reo, ôm hoặc biến lời đồng ý thành một khoảnh khắc cứu rỗi. Audere tự ghi tên. Overlay
screen-space làm tối classroom, hiện `RegistrationSheet_PLACEHOLDER` màu trắng cùng caption
`Phiếu đăng ký hoàn thành`, và chỉ đóng khi người chơi click.

Bianca quay sang phải, hop qua ba anchor cách nhau đúng một tile; tile phía trước hiện trước khi
cô tới và tile phía sau mờ đi. `CharacterMotionStep` giữ shadow trên ground projection; fade actor
là `SpriteGroupFadeStep` riêng, không giấu trong motion. Sau khi Bianca rời đi, Audere thừa nhận tay
vẫn run nhưng mình đã nói được, rồi cảm ơn Timor. `School_Bell` phát trước neutral fade `0.85 s`;
fade che kín scene trước khi `SceneFlow` load `40_Evening`.

**Relationship movement:** vẫn ở `Protective pre-emption`: Bianca đã cho Audere không gian lựa
chọn, còn Timor chen vào đúng khoảng trống và bảo cô chưa trả lời. Tuy vậy, beat mới trả lại cho
Audere một bước agency nhỏ: chính cô nói mình không muốn nỗi lo chọn thay và quyết định đối diện.
Timor giữ vai trò định hướng rồi ở bên cạnh, không tuyên bố chiến đấu thay cô.

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
├── 06_ResetBiancaVisibility        [SpriteGroupFadeStep: instant restore]
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
├── 210_PlayKhoangLangPrototype     [CombatStep]
├── 220_ReturnToStory               [WorldModeStep: Story]
├── 230_HoldAfterCombat             [WaitStep]
├── 240_AudereAnswersBianca         [DialogueStep]
├── 250_BiancaSettlesRight          [MoveActorStep]
├── 260_BiancaAccepts               [DialogueStep]
├── 270_SignupExchange              [DialogueStep]
├── 280_ShowRegistrationSheet       [StoryIllustrationStep]
├── 290_BiancaTurnsAway             [SetActorFacingStep]
├── 300_RevealDepartureTile1        [BoardTileTransitionStep]
├── 310_BiancaHopsDeparture1        [CharacterMotionStep]
├── 320_HideDecorationTile          [BoardTileTransitionStep]
├── 330_RevealDepartureTile2        [BoardTileTransitionStep]
├── 340_BiancaHopsDeparture2        [CharacterMotionStep]
├── 350_HideDepartureTile1          [BoardTileTransitionStep]
├── 360_RevealDepartureTile3        [BoardTileTransitionStep]
├── 370_BiancaHopsDeparture3        [CharacterMotionStep]
├── 380_HideDepartureTile2          [BoardTileTransitionStep]
├── 390_BiancaFadesOut              [SpriteGroupFadeStep]
├── 400_HideDepartureTile3          [BoardTileTransitionStep]
├── 410_AudereThanksTimor           [DialogueStep]
├── 420_PlaySchoolBell              [PlayAudioStep]
├── 430_FadeToEvening               [CanvasFadeStep]
└── 440_LoadEvening                 [SceneLoadStep]
```

Bianca đang dùng prefab `Bianca_PLACEHOLDER`; portrait và art chính thức là **Unresolved**.
Motion contract và cách thay Animator sau này nằm tại `Docs/10_CharacterExpressionAndMotion.md`.
Fullscreen shader, timeline, cancel và replay contract nằm tại
`Docs/11_FullscreenWorldTransitions.md`.

### 4.5 D1_HOME_NIGHT_MESSAGE — lời mời thứ hai và scripted defeat

**Scene:** `40_Evening`

**Event:** `D1_HOME_NIGHT_MESSAGE`
**Primary story job:** cho thấy khi Audere muốn tự trả lời một lời mời bình thường, nỗi sợ mất cô
khiến Timor chuyển từ cảnh báo sang bắt cô phải nghe lời; combat là hành động khóa lựa chọn đó.

```text
00_NormalizeMessageAlert
→ 10_FadeIn
→ 20_AudereAfterLongDay
→ 30_PlayMessageArrival
→ 35_HoldForMessage
→ 40_ShowMessageAlert           [dauchamthan]
→ 45_HoldMessageAlert
→ 50_AudereStartles             [VerticalInPlace: 0.19 s, arc 0.09]
→ 55_HoldAfterStartle
→ 60_AudereRecognizesBianca     [“Bianca nhắn cho tớ này.”]
→ 65_HideMessageAlert
→ 70_BiancaNightMessage
→ 80_TimorQuestionsHer
→ 90_KeepSilence
→ 100_AudereAndTimorConclude
→ 110_HoldBeforePressure
→ 120_EnterNightPressure        [Dreamy Disorientation]
→ 130_PlayTimorNightPressure    [Defeat-only CombatStep]
→ 140_ReturnToEvening           [neutral fade]
→ 145_HoldAfterReturn
→ 150_TimorNarrowsTheReply
→ 160_ChooseBiancaReply         [3 nested StoryEvent branches]
→ 170_HoldAfterReply
→ 180_LightsOut
→ 190_DayOneEnds                [“Ngày 1 - Kết thúc”]
```

Night Tile nằm đúng tâm camera; Audere cùng trục X với tile và dùng staging scale giống Scene 30.
Body giữ sorting order `5`, shadow `4`. `dauchamthan` là child presentation của Audere, mặc định
ẩn và dùng `Player/6`; authoring tool bảo toàn transform/art đã đặt trong scene. `MessCome.mp3`
được map bằng stable `AudioId.Message_Arrive`, sau đó alert hiện, Audere giật mình theo Y, nhận ra
Bianca và chỉ khi ấy nội dung tin nhắn mới mở. Audere luôn ở dialogue slot trái; Bianca/Timor ở phải.

Nhịp trước combat phải giữ quan hệ nhân-quả nhìn thấy được:

```text
Bianca đưa một lời mời bình thường
→ Timor lo Bianca chỉ tìm Audere để nhờ vả
→ nỗi sợ hiện ra trực tiếp và Timor viện chuyện mẹ Audere như bằng chứng
→ Audere tách hai chuyện ra và nói cô vẫn muốn trả lời
→ Timor chuyển từ khuyên sang cấm, rồi yêu cầu Audere phải nghe lời
→ Audere miễn cưỡng nhưng nói “Lần này, để tớ tự trả lời.”
→ Timor đáp “Tớ không thể để cậu làm vậy.”
→ Dreamy Disorientation biến việc ngăn Audere trả lời thành combat
```

Combat dùng policy `CapturedDiceBatchSequence`: phase 1–10 mỗi phase đúng một batch Attack,
Shield, Heal và chỉ tiến khi cả ba được catch cùng bark bắt buộc đã resolve. Timor có `36 shared
HP`; damage vẫn có feedback nhưng không tạo Victory. Player bắt đầu `66 TIME`, không thể Defeat
trước phase 11. Finale không có dice, nâng TIME còn lại lên floor `30 s`, kéo Heart mềm về tâm,
đợi hai câu cuối tự chạy xong rồi volley thật mới được phép kết liễu. Encounter chỉ cho Defeat và
tắt Retry; CombatStep map Defeat thành Complete, Victory/Special thành Fail.

TIME về `0` không trả Story ngay. Bullet/laser dừng collision và vận tốc, fade `0.62 s`, enemy
actor Timor còn lại trên board trong đoạn `Audere: … → Timor: Thấy chưa → ... → Audere: …Ừ`.
Portrait Sad và nhịp câu ngắn giữ `Thấy chưa` như một kết luận lo buồn, không phải chiến thắng.
Sau callback dialogue, runtime mới cleanup và neutral fade đưa cảnh về phòng.

Trong phòng, Timor nói `Không cần ép mình` rồi thu lựa chọn của Audere về “câu dễ nhất”. Choice UI
ở vùng path-piece có ba dòng; idle mờ/nhỏ, hover thêm `> <` và sáng/đủ scale. Ba nested branch:

1. Tránh hẳn: Timor xác nhận ngày mai không phải lo; `Đã gửi` hiện trước câu `…Ừ` của Audere.
2. Trì hoãn: `Đã gửi`, Timor bảo để mai nếu thấy ổn thì tính tiếp.
3. Không trả lời: giữ im lặng lâu hơn, Timor coi im lặng là câu trả lời; Audere vẫn hỏi Bianca sẽ
   nghĩ gì, nhưng Timor khép lại bằng `Ngày mai rồi tính` và `Nghỉ thôi`.

Mọi branch quay lại cùng `LightsOut`; overlay đen sau đó hiện `Ngày 1 - Kết thúc`. Branch là flow
cục bộ trong scene, chưa tạo StoryState/save flag xuyên scene.

Mười một bark tăng sức ép theo bốn tầng: (1) bảo vệ và yêu cầu đứng yên; (2) phủ nhận khả năng Audere
tự đặt giới hạn; (3) biến lựa chọn khác Timor thành không tin hoặc bỏ rơi Timor; (4) khóa câu trả lời.
Audere vẫn có các câu ngắn ở slot trái để người chơi nghe được sự chống lại của cô. Nhịp 2 là laser
dọc board, nhịp 8 là laser quét, nhịp 10 là laser con lắc; tất cả telegraph trước khi có collision.
Projectile nằm dưới mask inset của board nên không vẽ đè lên viền.

Portrait Timor là presentation của DialogueUI, không phải enemy sprite. Question bắt đầu bằng
`TimorLolang`, chuyển sang `TimorLoLangKhongVui` khi Timor thừa nhận sợ; conclusion chuyển sang
`TimorTucGian` tại `Không được đâu, Audere`. Bark 1–3 dùng Worried, 4–6 dùng WorriedUneasy,
7–10 dùng Angry và bark 11 dùng Sad. Enemy prefab vẫn giữ visual placeholder riêng.

**Design Intent:** đây là điểm đầu tiên Timor mất bình tĩnh vì Audere không làm theo. Cậu vẫn tin
mình đang bảo vệ cô, nhưng nỗi sợ chuyển thành cấm đoán và bắt phục tùng. Chi tiết mẹ Audere từng
tin người khác rồi Audere mất bà đang được dùng theo yêu cầu của scene này ở mức `Design Intent`,
chưa tự động trở thành `Established Canon` cho các scene khác. Đây là vòng lo cụ thể của Audere,
không phải mô tả lâm sàng áp cho mọi người có rối loạn lo âu. **Unresolved:** ontology combat,
final art của phòng, final moveset/balance và nghĩa tâm lý cuối cùng.

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

- **Implemented prototype:** hand-off Story → `Khoảng Lặng` prototype một phase `6 HP` → Story
  sau câu Timor.
- **Design Intent:** tên `Khoảng Lặng` và placement ở D1 Classroom phục vụ prototype hiện tại.
- **Implemented Design Intent:** D1 dùng một `CombatTutorialData` và enemy tutorial một phase riêng
  (`99 HP`, `120 TIME`). Opening batch luôn có đúng Attack, Shield, Heal; card đầu preview cả ba,
  nói gọn luật bắt/gieo/TIME, sau đó mới spotlight Stun Zone và giới thiệu từng dice khi người chơi
  bắt nó. Instruction dùng một dòng cùng font Scene 20 và chỉ đóng bằng click trái/phải. Click đóng
  card bị consume; trong toàn bộ dialogue/highlight, TIME, dice, projectile và enemy move đều pause.
  Giữa các cue TIME chạy `0.25x`, damage có safety floor `1 s`, nên phần học không thể vô tình Defeat
  hoặc hạ boss thật. Sau câu kết của Timor, session tutorial bị shutdown và một session
  `Enemy_KhoangLang` mới được tạo với `45 s`, một phase `6 HP`, dice batch và moveset luân phiên
  Aimed Fan → converging Side Sweep → Rain. Không hiện phase marker.
- **Implemented Design Intent:** đoạn kết tutorial được thay bằng nhịp Audere giữ câu
  `Tớ muốn thử`. Trong combat thật, Khoảng Lặng dùng DialogueUI chuẩn tự chạy ở Aimed Fan/Side
  Sweep mà không khóa input; Audere luôn ở trái và Khoảng Lặng ở phải. Sau Side Sweep, Heart
  wobble nhẹ rồi dialogue Audere–Timor pause combat-local; ở `2 HP`, text lo lắng phủ dày background
  với ghost-smear và wobble mềm tới khi session dọn. Chỉ đòn lethal sớm mới bị
  giữ ở `1 HP` tới khi dialogue bắt buộc resolve, nên encounter vẫn là một phase.
- **Design Intent:** exact Khoảng Lặng wording và portrait Audere tái sử dụng là content
  `PLACEHOLDER` đã được production-wire theo yêu cầu của Xuân, chưa phải voice/portrait canon.
- **Unresolved:** ontology, final voice/dialogue/art/ý nghĩa của Khoảng Lặng, final moveset,
  balance và branch outcome ngoài Victory/Retry hiện tại.
- **Implemented Design Intent:** Victory tiếp tục bằng câu trả lời nhỏ với Bianca, registration
  overlay click-to-dismiss, ba hop rời lớp, dialogue Audere–Timor, School Bell và neutral fade sang
  scene build-listed `40_Evening`.
- **Implemented Design Intent:** `40_Evening` có `D1_HOME_NIGHT_MESSAGE`, Night Tile ở tâm camera
  và actor staging đồng tỷ lệ Scene 30 (`Story Root 0.25`, Audere `1.5`, body/shadow `Player 5/4`),
  message sound, Bianca text, Audere startle, Timor exchange và encounter scripted-defeat 11 nhịp.
  Defeat là result duy nhất; hazard freeze/fade và hậu thoại resolve trước neutral fade. Trong
  phòng, choice UI ba nhánh dẫn tới lights out và `Ngày 1 - Kết thúc`; không có Retry.
- **Unresolved:** art thật của phiếu đăng ký, room art buổi tối, combat ontology và kết quả
  narrative dài hạn của ba lựa chọn.
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
| Production scene sáng Day 1 | `Assets/_Audere/Scenes/20_D1_Home_Morning.unity` |
| Production scene lớp học | `Assets/_Audere/Scenes/30_Classroom.unity` |
| Production scene buổi tối | `Assets/_Audere/Scenes/40_Evening.unity` |

Khi production scene và tài liệu này khác nhau, kiểm tra `DialogueData` và Hierarchy hiện tại
trước; sau khi QA, cập nhật lại doc thay vì đoán flow từ tên file.
