# Giờ nghỉ: lời mời của Bianca

[Mục lục nguồn](../../09_Day1_ProductionStoryWorkflow.md) · [Bản đồ docs](../../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
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

<!-- END PRESERVED SOURCE -->
