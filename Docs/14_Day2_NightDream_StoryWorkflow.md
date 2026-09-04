# Day 2 — tan học, cuộc hỏi lại và giấc mơ

## Trạng thái và phạm vi

Implemented / QA 2026-08-28. Narrative mới là **Design Intent theo yêu cầu Xuân**, không tự xác nhận ontology của Timor/combat hoặc biến chữ trong mơ thành suy nghĩ thật của Bianca.

Chuỗi production: `60_D2_School_Morning → 70_D2_Home_Night → 80_D2_Dream → 90_D2_Home_Awakening`.

- Scene60 giữ combat, coop puzzle và hậu thoại cũ; chỉ nối thêm phần kết.
- Scene70/90 dùng Player prefab trên một tile giữa viewport, camera/mask theo Scene40. Không sao chép combat Timor sang phòng mới.
- Dream đi **sang phải**; chữ nền trôi về sau. Đây là cách giải quyết hai mô tả trái/phải khác nhau trong yêu cầu.
- Nhãn cuối giữ **“Ngày 2…”** đúng yêu cầu; ngày lịch tiếp theo và nội dung sau tỉnh giấc vẫn **Unresolved**.
- Art phòng/đồ vật và decor mơ là **PLACEHOLDER**, không phải art cuối.

## Scene60 — việc hôm nay đã xong

Sau `330_AudereQuestionsTimor` và `340_HoldAfterSmallBoundary`, tail của `D2_SCHOOL_WRONG_SUPPLIES` là:

```text
345_PreparationsAreDone
346_LetTomorrowStaySmall
347_BiancaSaysGoodbye
348_BiancaTurnsToLeave
348_EnableDepartureCover
349_SchoolBell
350_FadeOutAfterAnswer       (step cũ, 0.9 s)
360_GoHomeAfterBell
```

Bianca xác nhận đủ đồ cho ngày mai, hẹn trang trí bảng rồi chuẩn bị nốt với lớp. Audere ngập ngừng về việc làm với mọi người nhưng vẫn giữ lời muốn làm phần bảng. Bianca đáp bình thường, không reo mừng/cứu rỗi. Bianca quay sang phải; chuông dùng `AudioId.School_Bell` → `Assets/_Audere/Audio/SchoolBell.mp3`, không thêm audio ID/clip mới.

## Scene80 cinematic opening — revision requested by Xuân

**Design Intent**, not an additional waking-day event: the dream initially resembles an ordinary conversation. Audere and Bianca stand on two adjacent tiles; four nearby tiles hold desks from `AssetGame/Item/ban.aseprite`. There is no continuous classroom floor or added blackboard. Bianca discusses the remaining paper; Audere tentatively offers to finish cutting shapes. Both use their ordinary portraits, with Audere Left and Bianca Right.

The scene-first opening is `000_CoverDream → 005_ResetAtmosphereAndHideHand → 010_PrepareContinuousPath → 011_NormalClassroomUnderCover → 012_AudereFacesBianca → 014_RevealOrdinaryConversation → 016_BiancaOrdinaryConversation → 018_AQuietBeatBeforeTheCrack → 019_TheClassroomFractures`. The existing dream drift, five boards, collapse, Timor dialogue and wake-up handoff follow unchanged.

`019` directly references shared `WorldTransition_DreamFracture.asset`: 6.95 seconds, scenery swap at 4.8 seconds beneath the still-intact screenshot. Following Xuân's [BREAK your Screen reference](https://unitycodemonkey.com/video.php?v=RP1-PZD4Ab4), the source shakes, freezes at 0.8 seconds, then cracks advance from the top/left/right edges toward the center in discrete pulses; only afterward do secondary cracks spread outward to the corners. At 4.8 seconds the actual image pieces separate, rotate in three axes and fall with visible thickness, revealing the walking puzzle immediately behind them. They clear by 6.85 seconds; there is no black interlude. Charcoal/violet faces, darker backs and restrained cool edge highlights replace the bright wireframe appearance. This replaces the earlier fixed-region UV shifting. The runtime capture is never saved as a production asset. No scene-local timeline or added audio cue. Puzzle hand stays hidden until `020_BeginDreamDrift`.

Ten dream desks float gently with the existing scenery: horizontal drift, vertical bob and small tilt all begin at their authored pose. RGB fringes inherit parent motion instead of bobbing twice. Desk and floating-tile `SortingGroup` orders use their authored depth relative to Audere's constant ground plane; they do not change when the object bobs or Audere hops. Foreground objects can cover her feet, background objects remain behind her. Audere body/shadow stay `Player/5` and `Player/4`. Decor has no puzzle tiles or collision components.

Cancel before capture, during shard flight or after the glass swap restores the source active states and releases the screenshot, overlay and fullscreen material/feature. Dream cancellation restores prop positions, rotations, colors and the camera; replay starts under cover with the hand hidden. The existing create-missing-scenes author tool preserves this scoped scene revision.

Revision verification: **39/39 passed** (`Day2NightDreamTests` 10 + `MusicPresentationTests` 29), result `Temp/DreamShatterQA/tests_39_pass.xml`, completed 2026-08-28 09:04:31Z. Includes 15 real preview/drops through Home→Dream→Awakening, source tiling at 16:9/4:3/21:9, edge-origin cracks with delayed outward branches, actual Canvas mesh, source brightness preservation, cancellation before capture/during flight/at scenery swap, replay and prop restoration. Play frames `Temp/DreamShatterQA/final-*.png` at 1920×1080 show edge→center→corner crack growth, dark glass colors and the walking puzzle visible directly behind falling shards. `final-capture-log.txt` records cover=0 throughout breakup and clean overlay/feature shutdown. Other aspect ratios received geometry tests, not a visual Play pass. Earlier QA caught a missing CanvasRenderer, screenshot gamma conversion and stale test expectations from the former black handoff; those are fixed and covered by the final run.

## Scene70 — D2_HOME_NIGHT_DOUBT

Audere đứng trên tile, ban đầu quay tránh rồi quay sang Timor sau câu trả lời. Body sorting `Player/5`, shadow `Player/4`; feet anchor ở tâm tile, không lấy tâm sprite làm chân.

| Beat | Nội dung / nhịp |
| --- | --- |
| 000–015 | Cover, reset pose, reveal 0.65 s, Audere quay tránh. |
| 020 | “Timor.” / “Ừ?” / “Lúc ở kho…” / “Cậu thật sự biết Bianca đang nghĩ gì à?” |
| 030–060 | Im 2.4 s. “Tớ biết cô ấy có thể nghĩ gì.” Audere nhìn sang, giữ 0.45 s. |
| 070 | “Có thể.” Timor giải thích chuẩn bị cho điều tệ nhất. Audere: “Nhưng hôm nay… nó không xảy ra.” Timor: “Hôm nay thôi.” |
| 080–100 | Im 1.6 s. Timor cảnh báo đừng vì hôm nay ổn mà quên những lần có thể không ổn. Im 1.1 s. |
| 110–150 | “…Tớ biết.” Audere chào ngủ ngon, quay đi, giữ 0.6 s, fade 1.35 s, load Dream. |

Timor không hét: từ lo lắng sang không vui nhưng vẫn tự coi mình là người bảo vệ. Audere hỏi lại một điều cụ thể, chưa biến thành tuyên bố chống lại Timor. Các câu dài được tách, mỗi bubble tối đa 42 ký tự; Audere luôn trái, Timor/Bianca phải.

Portrait có sẵn: `Audere_Tired`, `TimorLolang`, rồi `TimorLoLangKhongVui`. Không sửa enemy art hay tạo portrait mới.

## Scene80 — D2_DREAM_ONLY_ME

Scene-first: `WORLD/Dream Path - Scene Authored` chứa đúng một Player, Puzzle Runtime và năm `PZ_D2_DREAM_*`. Mỗi board có bốn tile (tính cả Start), ba card `PathPiece_Line_2`; mỗi card thêm một bước. Tổng cộng **15 bước**, không phải 30 bước.

- Các đoạn `(0,0)→(3,0)→(6,0)→(9,0)→(12,0)→(15,0)`.
- Goal cũ giữ hiện trong lúc đường mới reveal, trùng Start mới tuyệt đối; swap anchor cùng một frame. Player không bị disable giữa đoạn.
- Camera follow X, không follow Y của hop. Đường chính ổn định; 24 decor tile/RGB fringe lơ lửng không có BoardTile/collider nên không đi vào được.
- 20 dòng chữ scene-authored tái dùng bốn câu: “Lại phải sửa cho cậu.”, “Biết ngay rồi cũng sẽ nhờ thêm.”, “Ngày mai lại phải gặp.”, “Con nhỏ phiền phức”. Nhóm đặt tên **NOT Bianca Dialogue**.
- Chữ mờ, trôi ngược chuyển động camera, méo nhẹ bằng vertex; cuối mơ tăng wobble/opacity. Không thêm VHS hoặc thay shared fullscreen profile.
- `DreamAtmosphereView` chỉ sở hữu camera drift và decor/text presentation; không tạo board, di chuyển Player hoặc thay shadow. `DreamAtmosphereStep` điều khiển Begin/Collapse/Stop qua direct reference.
- Hết đoạn năm: giữ 0.55 s, ẩn PuzzleUI, tất cả path/decor fade 1.25 s. Audere còn lại giữa chữ.

Thoại cuối: Timor gọi “Audere.” / “Nhìn tớ.”; Audere giật mình tại chỗ. “Timor… đường đâu rồi?” Timor kéo sự chú ý về mình: “Đừng nhìn chỗ đó nữa.” / “Nhìn tớ thôi.” / “Chỉ có tớ giúp cậu an toàn thôi.” / “Chỉ mình tớ là bạn thật sự của cậu.” Audere: “…Đừng đi.” Timor: “Tớ ở đây.” / “Vậy cứ nghe tớ, Audere.”

Portrait Audere chuyển sang `Audere_Scared`. Startle dùng `CharacterMotionStep.VerticalInPlace`, shadow giữ nguyên. Hold 0.7 s, fade 0.85 s rồi load Scene90.

## Scene90 — D2_HOME_WAKE_FROM_DREAM

Cover → reset pose → reveal 0.35 s → startle dọc 0.19 s / arc 0.09 → giữ 0.7 s. Theo yêu cầu Ngày3 tiếp theo: fade đen0.85s → title “Ngày 2 - Kết thúc” giữ2s → load `100_D3_Home_Morning`. Không thêm hậu thoại. Chi tiết phần nối và QA mới ở [Day3 workflow](15_Day3_BoardTeacher_StoryWorkflow.md); evidence5/5 bên dưới là checkpoint trước phần nối này.

## Ownership / authoring

- Menu `Audere/Story/Author Day 2 Night and Dream` tạo **scene còn thiếu**, không rebuild scene có sẵn; giữ DialogueData đã tồn tại. Guard Play mode và dirty scenes. Sửa visual/step trực tiếp ở scene sau lần dựng đầu.
- Mỗi direct child event có một StoryStep; sibling order là flow. Không hardcode hội thoại vào runtime presentation.
- SceneLoadStep dùng GameScenes/SceneFlow. Destination có cover active alpha 1. Neutral fade dùng contract sẵn có; BGM tiếp tục theo hook fade chung, không sửa music service.
- DialogueStep resolve UI qua persistent GameplayUIRoot, tránh giữ reference scene-local đã bị discard sau load.
- Cancel: PuzzleStep normalize board/Player và release input; DreamAtmosphereView nhận owner không còn playing hoặc OnDisable, khôi phục camera/text/decor. Replay reset chaos, card hand và cell 0.
- Không thay combat controller, encounter, Shield/dice, board sizing, hoặc các StoryStep cũ.

## Assets và nơi chỉnh

- Ba scene mới: `Assets/_Audere/Scenes/70_D2_Home_Night.unity`, `80_D2_Dream.unity`, `90_D2_Home_Awakening.unity`.
- Chín DialogueData: `Assets/_Audere/Data/Dialogue/Day2/NightDream/`.
- PuzzleData: `Assets/_Audere/Data/Puzzle/Day2/Puzzle_D2_Dream_ThreeSteps.asset`.
- Runtime mới: `Story/Presentation/DreamAtmosphereView.cs`, `Story/Steps/DreamAtmosphereStep.cs`.
- Author/tests: `Story/Editor/Day2NightDreamSetupTool.cs`, `Story/Editor/Tests/Day2NightDreamTests.cs`.
- [Tọa độ và solver proof](Puzzles/Day2Dream/README.md).

## Evidence / giới hạn QA

- Unity compile thành công (`scriptCompilationFailed=false`). Focused suite **5/5 Passed**, XML kết thúc `2026-08-28 04:29:48Z`.
- Kiểm thử Play dùng preview/drop thật đủ 15 lần qua năm board; hand mỗi đoạn ba card, một Player luôn active; Goal/Start delta <0.00001, feet/tile delta <0.002.
- Production SceneFlow chạy 70→80→90. Kiểm tra path alpha=0 tại collapse, title/wake hoàn tất, X/Z startle cố định, shadow pose/scale cố định, input claim cuối=0.
- Cancel khi Player di chuyển rồi replay: dừng movement, restore environment, input=0, bắt đầu lại cell0/hand3/chaos0.
- EditMode kiểm tra tail Scene60/bell binding, one-step-per-child, không missing script, dialogue side/length, và rerun author không thay byte của bốn scene.
- Visual QA tại 1920×1080: home dialogue, đường/RGB/text, collapse và wake title. Ảnh tại `Temp/Day2NightDreamQA` (không phải asset production).
- Đo bounds đường tại 16:9, 4:3, 21:9 đều trong viewport/mask; margin mask nhỏ nhất 0.1298 world units. Đây là **geometry check**, không phải ba lượt chơi visual.
- Console đọc lại sau Play: 0 error/warning. Test-run đầu có lỗi harness half-cell pointer/log expectation, đã sửa; không còn trong suite cuối.
- Kiểm tra cuối Scene60/70/80/90: mỗi scene 0 missing script, 0 broken prefab, dirty=false. Editor dừng Play và mở Scene70 để chỉnh trực tiếp.
- Chưa chơi lại toàn bộ combat/coop trước tail Scene60, chưa nghe xác nhận loa thật của SchoolBell, chưa full visual playthrough ở 4:3/ultrawide. Dialogue trong test flow được advance nhanh; visual home kiểm tra riêng ở tốc độ thường.
