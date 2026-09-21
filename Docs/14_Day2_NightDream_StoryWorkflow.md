# Day 2 — tan học, cuộc hỏi lại và giấc mơ

## Trạng thái và phạm vi

Implemented 2026-08-28; scene80 revised 2026-09-20. Narrative mới là **Design Intent theo yêu cầu Xuân**, không tự xác nhận ontology của Timor/combat hoặc biến chữ trong mơ thành suy nghĩ thật của Bianca.

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

The scene-first opening is `000_CoverDream → 005_ResetAtmosphereAndHideHand → 008_RestoreGroundedShadow → 009_AudereAtDreamStart → 011_NormalClassroomUnderCover → 012_AudereFacesBianca → 014_RevealOrdinaryConversation → 016_BiancaOrdinaryConversation → 018_AQuietBeatBeforeTheCrack → 019_TheClassroomFractures`. The automatic run/fall revision below follows this opening; the Timor dialogue and wake-up handoff are preserved.

`019` directly references shared `WorldTransition_DreamFracture.asset`: 6.95 seconds, scenery swap at 4.8 seconds beneath the still-intact screenshot. Following Xuân's [BREAK your Screen reference](https://unitycodemonkey.com/video.php?v=RP1-PZD4Ab4), the source shakes, freezes at 0.8 seconds, then cracks advance from the top/left/right edges toward the center in discrete pulses; only afterward do secondary cracks spread outward to the corners. At 4.8 seconds the actual image pieces separate, rotate in three axes and fall with visible thickness, revealing the automatic dream path immediately behind them. They clear by 6.85 seconds; there is no black interlude. Charcoal/violet faces, darker backs and restrained cool edge highlights replace the bright wireframe appearance. This replaces the earlier fixed-region UV shifting. The runtime capture is never saved as a production asset. No scene-local timeline or added audio cue. Puzzle hand stays hidden throughout the dream. Only eight opening murmurs are visible at scenery swap and at `020_BeginDreamDrift`.

Ten dream desks float gently with the existing scenery: horizontal drift, vertical bob and small tilt all begin at their authored pose. RGB fringes inherit parent motion instead of bobbing twice. Desk and floating-tile `SortingGroup` orders use their authored depth relative to Audere's constant ground plane; they do not change when the object bobs or Audere hops. Foreground objects can cover her feet, background objects remain behind her. Audere body/shadow stay `Player/5` and `Player/4`. Decor has no puzzle tiles or collision components.

Cancel before capture, during shard flight or after the glass swap restores the source active states and releases the screenshot, overlay and fullscreen material/feature. Dream cancellation restores prop positions, rotations, colors and the camera; replay starts under cover with the hand hidden. The existing create-missing-scenes author tool preserves this scoped scene revision.

Historical opening verification (2026-08-28): **39/39 passed** (`Day2NightDreamTests` 10 + `MusicPresentationTests` 29), result `Temp/DreamShatterQA/tests_39_pass.xml`, completed 2026-08-28 09:04:31Z. Includes 15 real preview/drops through Home→Dream→Awakening, source tiling at 16:9/4:3/21:9, edge-origin cracks with delayed outward branches, actual Canvas mesh, source brightness preservation, cancellation before capture/during flight/at scenery swap, replay and prop restoration. Play frames `Temp/DreamShatterQA/final-*.png` at 1920×1080 show edge→center→corner crack growth, dark glass colors and the walking puzzle visible directly behind falling shards. `final-capture-log.txt` records cover=0 throughout breakup and clean overlay/feature shutdown. Other aspect ratios received geometry tests, not a visual Play pass. Earlier QA caught a missing CanvasRenderer, screenshot gamma conversion and stale test expectations from the former black handoff; those are fixed and covered by the final run.

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

Revision 2026-09-20 — **Design Intent theo yêu cầu Xuân**: đoạn mơ tự chạy, không yêu cầu đặt khối puzzle. Giữ nguyên hội thoại mở đầu với Bianca, shared Dream Fracture profile và phần Timor → tỉnh giấc.

- `100_AudereRunsThenSlows` (`DreamWalkStep`) đi qua 15 staging anchor trực tiếp; stride tăng từ 0.28 s lên 0.95 s. `GridPlayer`, puzzle managers/controllers/coordinator tắt, Puzzle Runtime và hand ẩn. Năm board cũ giữ như scene geometry; các PuzzleStep/board hand-off cũ inactive, không thực thi.
- 64 text scene-authored với 40 câu khác nhau, mở từ 8 câu thúc giục, tăng qua phàn nàn → chế giễu → xua đuổi. Ngưỡng pressure từng nhóm: 0 / 0.14 / 0.36 / 0.57 / 0.77. Opacity, xoay và vertex warp tăng liên tục theo tiến độ đi. Text thuộc `Dream Murmurs - NOT Bianca Dialogue`, không phải lời thật của Bianca.
- `005_ResetAtmosphereAndHideHand` capture/reset presentation ngay dưới cover, trước khi kính mở target. Chỉ 8 câu đầu hiện ở scenery swap; `020_BeginDreamDrift` giữ nguyên mức chữ đó. Pose/màu được cache khi text còn ẩn; mesh chỉ được copy sau Awake khi text active. TMP tạo lại mesh trước khi warp để rebuild màu ở PreRender không xóa distortion.
- Tile sụp ở cell 15 khi Audere cách tâm 0.015 world units (6% chiều dài bước). Tám tile decor phía sau giữ đường nhìn vẫn tiếp tục, không lộ đây là điểm kết thúc. Chúng không có BoardTile/collider.
- `250_LastTileShattersAudereFalls`: sprite tile thật tách mỗi tam giác thành 16 mảnh nhỏ; ít nhất 32 mảnh có texture, bay tách và rơi. Sau 0.07 s Audere rơi gia tốc xuống anchor 2.4 units trong 2.6 s, ngửa lưng 78°. Camera theo độ rơi, chữ co thành vòng xoáy quanh Audere, scenery/path tan đi. Shadow tắt riêng trước cú rơi; không đổi màu/scale của shadow.

Timor gọi ở step270 như cũ; step275 dựng đứng và step280 bật người đã inactive. Audere giữ góc ngửa 78° trong toàn bộ thoại đến fade; camera giữ cơ thể trong khung, chữ tiếp tục xoáy và gió chạy lên để diễn tả rơi liên tục. `FallingWindView` tham chiếu shared `FallingRoom_Classroom.asset` của cảnh trước boss Đám Đông, dùng 38 vệt ở hai mép bên trong aperture. Gió chạy bằng unscaled time cả khi chờ người chơi đọc thoại, dọn khi event kết thúc/hủy/disable. Opening normalize dùng `CharacterPoseStep` với anchor trực tiếp; cancel cả trong thoại cuối cũng bỏ góc nghiêng, dọn shard/gió và phục hồi atmosphere.

Shadow art có sprite bounds center Y=0.215 khác pivot. `Shadow_Start` bù offset này theo world scale để **tâm ellipse nhìn thấy** nằm ở Y=-0.04, ngang mặt tile và chân; không đặt Transform của bóng ở tâm tile. Khi bước, bóng chỉ theo ground trajectory, không nhảy theo body; màu/material/scale và sorting Player/4 giữ nguyên.

Follow-up float: từ 60% cú rơi, `DreamFallStep` tăng dần nhấp nhô ±0.025 world units, chu kỳ 2.8 s, chao nhẹ ±2.5° quanh pose ngả 78°. Nhịp tiếp tục bằng unscaled time suốt thoại Timor; camera chỉ theo độ rơi gốc, không bob theo cơ thể. Cancel/disable bỏ offset tạm tại vị trí rơi hiện tại; replay không giữ float cũ.

Thoại cuối: Timor gọi “Audere.” / “Nhìn tớ.” trong khi Audere tiếp tục rơi ngửa. “Timor… đường đâu rồi?” Timor kéo sự chú ý về mình: “Đừng nhìn chỗ đó nữa.” / “Nhìn tớ thôi.” / “Chỉ có tớ giúp cậu an toàn thôi.” / “Chỉ mình tớ là bạn thật sự của cậu.” Audere: “…Đừng đi.” Timor: “Tớ ở đây.” / “Vậy cứ nghe tớ, Audere.”

Portrait Audere chuyển sang `Audere_Scared`; cơ thể vẫn ngả sau, shadow ẩn vì không còn mặt đất. Hold 0.7 s, fade 0.85 s rồi load Scene90. Chỉ scene tỉnh giấc mới có startle tại chỗ.

## Scene90 — D2_HOME_WAKE_FROM_DREAM

Cover → reset pose → reveal 0.35 s → startle dọc 0.19 s / arc 0.09 → giữ 0.7 s. Theo yêu cầu Ngày3 tiếp theo: fade đen0.85s → title “Ngày 2 - Kết thúc” giữ2s → load `100_D3_Home_Morning`. Không thêm hậu thoại. Chi tiết phần nối và QA mới ở [Day3 workflow](15_Day3_BoardTeacher_StoryWorkflow.md); evidence5/5 bên dưới là checkpoint trước phần nối này.

## Ownership / authoring

- Menu `Audere/Story/Author Day 2 Night and Dream` tạo **scene còn thiếu**, không rebuild scene có sẵn; giữ DialogueData đã tồn tại. Guard Play mode và dirty scenes. Sửa visual/step trực tiếp ở scene sau lần dựng đầu.
- Mỗi direct child event có một StoryStep; sibling order là flow. Không hardcode hội thoại vào runtime presentation.
- SceneLoadStep dùng GameScenes/SceneFlow. Destination có cover active alpha 1. Neutral fade dùng contract sẵn có; BGM tiếp tục theo hook fade chung, không sửa music service.
- DialogueStep resolve UI qua persistent GameplayUIRoot, tránh giữ reference scene-local đã bị discard sau load.
- Cancel: story motion dừng và bỏ lift/rotation tạm; fragment view dọn mesh/material khi event kết thúc. DreamAtmosphereView khôi phục camera/text/decor. Replay normalize actor/shadow dưới cover; không cấp input puzzle.
- Không thay combat controller, encounter, Shield/dice, board sizing, hoặc các StoryStep cũ.

## Assets và nơi chỉnh

- Ba scene mới: `Assets/_Audere/Scenes/70_D2_Home_Night.unity`, `80_D2_Dream.unity`, `90_D2_Home_Awakening.unity`.
- Chín DialogueData: `Assets/_Audere/Data/Dialogue/Day2/NightDream/`.
- PuzzleData: `Assets/_Audere/Data/Puzzle/Day2/Puzzle_D2_Dream_ThreeSteps.asset`.
- Runtime: `DreamAtmosphereView`, `DreamAtmosphereStep`, `DreamWalkStep`, `DreamFallStep`, `DreamTileFragments`; tái dùng `CharacterPoseStep` cho normalize/recover.
- Author/tests: `Day2NightDreamSetupTool` (tạo scene thiếu), `Day2DreamAutomaticAuthoring` (revision có thể chạy lại), `Day2NightDreamTests`.
- [Tọa độ và solver proof](Puzzles/Day2Dream/README.md).

## QA revision 2026-09-20

- Initial automatic traversal suite: **8/8 passed**, `Temp/DreamAutomaticQA/results.xml`.
- Float follow-up: **2/2 Play tests passed**, `Temp/DreamAutomaticQA/float-final-play.xml` (2026-09-20 06:05:29Z). Quan sát trọn chu kỳ ở thoại270/300: biên độ Y trong 0.035–0.055 units, góc chao 2–5.1°, luôn ngả >75°, camera/scale cố định; cancel trong lúc rơi và thoại loại bỏ offset rồi replay sạch. Đã xem `float-high.png`/`float-low.png` tại 875×498; Console0error, scene80 saved clean, rerun author byte-identical.
- Follow-up giữ ngả sau/gió/shadow: **3/3 passed**, `Temp/DreamAutomaticQA/held-fall-wind-shadow.xml` (2026-09-20 05:34:04Z). Production 70→80→90, visible shadow center đúng mặt tile suốt bước, feet chỉ lift theo stride; giữ thoại270/300 vẫn nghiêng >75° và gió tiếp tục chạy; cancel khi đi, đang rơi và trong thoại cuối đều dọn sạch rồi replay. Ảnh `run.png`, `fall-timor-call.png`, `fall-only-me.png` đã xem ở 875×498. Compile/Console 0 error, 0 missing script, rerun author byte-identical. Chưa lặp visual ở aspect ratio khác.
- After Xuân's center-impact/backward-fall/text-flash refinement: scene structure and unchanged opening checks passed; final two Play tests **2/2 passed**, `Temp/DreamAutomaticQA/refinement-final-play.xml` (2026-09-20 05:16:11Z). Covers Home→Dream→Awakening without any puzzle drop; exactly eight visible murmurs at glass swap and walk start; center approach <0.025 units; ≥32 tile fragments; body lean >65° while falling; hidden grounded shadow; cancel while walking/falling; replay restores upright pose and removes fragments/input claims.
- The first refinement run caught TMP mesh arrays not yet initialized on hidden text. Presentation colors/poses now capture before reveal, while mesh cache waits until text is active after Awake. The final rerun passes with that correction.
- Visual frames inspected: `Temp/DreamAutomaticQA/glass-reveal.png`, `run.png`, `shatter.png`, `fall.png`; the row remains visible beyond the breaking tile and the body falls backward. Captures are at the current Game View size (875×498); this revision did not repeat a full 1080p/ultrawide playthrough. Shared glass geometry at 16:9/4:3/21:9 passed in the initial eight-test run; the shared fullscreen profile was not changed.
- Compile successful; final Console contains zero errors. Scene80 saved and reopened in Edit mode. All three scene DialogueData references are unchanged from the task-start scene backup.

## Evidence / giới hạn QA — checkpoint cũ 2026-08-28

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
