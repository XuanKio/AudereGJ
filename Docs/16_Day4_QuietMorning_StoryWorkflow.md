# Ngày 4 — một buổi sáng không có lời nhắc

## Trạng thái câu chuyện

**Design Intent theo Xuân, đã dựng để duyệt:** sau khi Timor im lặng ở cuối Ngày 3, Audere tự làm các việc buổi sáng. Sự vắng mặt này không có nghĩa Audere hết lo âu. Em còn hoang mang, tự thu nhỏ việc cần làm thành hai câu: “Đánh răng… thay đồ… ăn sáng.” / “Ba việc thôi.” Không thêm lời tự tin tuyệt đối, lời Timor vô hình, hoặc bình luận về kết quả puzzle.

**Design Intent bổ sung theo Xuân:** Scene140 tiếp tục bằng cú ngã và combat Đám đông; xem `Docs/17_Day4_Crowd_StoryWorkflow.md`. Nội dung sau điểm đến buổi tối Scene150 vẫn **Unresolved**.

## Scene-first flow

- `120_D3_School_Teacher`: giữ toàn bộ Teacher/aftercare/Bianca reprise; thêm duy nhất `240_BeginDayFour` sau title kết thúc, qua `SceneLoadStep`/`GameScenes.Day4HomeMorning`.
- `130_D4_Home_Morning`: bản riêng từ Scene20. Một Player, PuzzleRuntime, hand/preview và coordinator; hai puzzle đánh răng/ăn sáng dùng art cũ. Không sửa source Scene20.
- `D4_HOME_WITHOUT_A_VOICE`: cover → chuẩn bị board/giữ tile dưới chân → title “Ngày 4” → fade in → khoảng lặng/nhìn trái/nhìn lại → độc thoại → đánh răng → collapse giữ goal → fade và nhịp “Thay đồ” → ăn sáng → fade đen → Scene140.
- Thay đồ được diễn tả bằng ellipsis/title dưới cover, **không có sprite thay quần áo hoặc puzzle thay đồ mới**.
- `140_D4_Classroom`: bản riêng từ Scene30; không còn StoryEvent Ngày1. Được mở rộng bằng flow Đám đông riêng cho Ngày4; không phát lại combat/thoại cũ. Debug world-mode hotkeys bị tắt.
- `Day4MorningSetupTool.CreateMissing()` chỉ tạo scene thiếu và nối tail; không tái author nội dung scene mới đã tồn tại. Không chạy broad builder Day3 để nối Day4.

## Thoại và hướng dẫn

- `Data/Dialogue/Day4/Dialogue_D4_THREE_THINGS.asset`: Audere bên trái, bên phải `None`; Scared ở nhịp hoang mang, Tired ở “Ba việc thôi.”.
- `DialogueController.TryResolveCharacter(None)` trả entry trống; slot có sẵn tự ẩn image khi portrait null. Không thêm nhân vật giả vào catalog; không đổi IDs6/7 hoặc logic `Line.CharacterOverride`.
- Bỏ StepTileTutorialGuide/UseAllPiecesTutorialGuide. PuzzleManager không bind HUD lời nhắc; tutorial labels tắt/rỗng. Goal Visual (mũi tên) tắt ở trạng thái authored nên reset không bật lại. Toothbrush/bread còn nổi nhẹ để nhận biết đích.
- Vẫn chọn mảnh/xoay/preview/drop như trước. Đi hụt, tới đích khi còn mảnh, hoặc hết mảnh mà chưa tới đích đều cho thử lại, không lời bình.

## Puzzle proof

Map runtime lấy từ BoardTiles trong scene. `PuzzleData` chỉ cấu hình piece list và `requireAllPathPieces=true`. JSON/spec và proof ở `Docs/Puzzles/Day4`; bản export gốc giữ tọa độ shared grid.

| Board | Start → Goal (Unity grid) | Mảnh trong hand | Số lời giải | Lượt đầu hợp lệ / dẫn tới lời giải |
|---|---|---|---:|---:|
| Washroom, 7 tile | (0,0) → (1,1) | L3, L4, Line2 | 3 | 4 / 1 |
| Breakfast, 10 tile | (1,1) → (0,2) | L4, Line3, L3, Line2 | 2 | 6 / 2 |

Washroom đỏ `(0,1),(2,1)`. Một đường thắng: Line2 `(0,0)→(0,1)`; L4 `(0,1)→(0,0)→(1,0)→(2,0)`; L3 `(2,0)→(2,1)→(1,1)`. Các L3 đi thẳng tới toothbrush ở lượt đầu là bẫy còn mảnh.

Breakfast đỏ `(0,3),(2,1)`. Một đường thắng: L3 `(1,1)→(2,1)→(2,2)`; L4 `(2,2)→(2,3)→(1,3)→(0,3)`; Line3 `(0,3)→(0,2)→(0,1)`; Line2 `(0,1)→(0,2)`. Goal chỉ hoàn tất khi **kết thúc** path; đi qua goal giữa một path tuân theo runtime hiện có.

JSON breakfast dùng tọa độ chuẩn hóa với offset shared-grid `(-1,+1)`: start `(2,0)` tương đương cột3/hàng1, goal `(1,1)` tương đương cột2/hàng2. Không sửa luật để ép lời giải.

Washroom Goal world `(0.25,0.25,0)` = Breakfast PlayerStart world, delta0. Shared Player luôn active qua collapse/changing-clothes/reveal. Board mới đăng ký lại grid sau dịch root; tile bắt đầu thay anchor đúng vị trí cũ.

## Nhạc

`SceneMusicSpace` chỉ sở hữu music duck riêng của scene: 18s gain0.34, hạ trong3s xuống0.035, giữ8s, lên trong3s; chu kỳ32s, unscaled. Đây là cách để bản BGM có sẵn nhỏ và có khoảng lặng hơn, **không phải bản nhạc mới**. Không đổi clip/catalog, volume preference, SFX, combat music. Disable/unload giải phóng owner qua AudioService.

## Kiểm tra

`Day4MorningTests` kiểm tra data/hierarchy/anchor, không còn hướng dẫn/arrow kể cả reset, Day3 title→Day4, và đường chơi thật qua preview/drop: goal sớm → reset, fall → reset, cancel/replay → thắng hai board → Scene140. Test tạo SceneFlow/AudioService như Bootstrap; không có shortcut load scene trong story runtime. Thoại được kết thúc qua callback normal của controller để tập trung kiểm tra flow, không phải manual click QA.

Lượt cuối **4/4 Day4MorningTests passed**, XML `Temp/Day4/tests_4_pass.xml`, kết thúc `2026-08-28 16:21:27Z`. Suite trước đó đã pass30MusicPresentation +1Day3 reprise ending +3Day4; lỗi còn lại là expectation log Scene140 thừa trong test dừng ởScene130, đã sửa harness rồi chạy lại cả4test. Tổng35test liên quan đều đã có lượtpass; không tuyên bố toàn bộ suite portrait pass vì lỗi importer Teacher riêng ở dưới.

Ảnh 1920×1080: `Temp/Day4/opening.png`, `washroom.png`, `breakfast.png`, `classroom.png`. Không build executable, không QA ultrawide/mobile hoặc nghe loa thật trong lượt này.

### Lỗi ngoài phạm vi được phát hiện

Teacher Creepy importer đã chuyển từ Multiple sang Single sau lượt portrait migration trước đó. Catalog vẫn trỏ slice `Co_giao_Creepy_0` cũ nên regression Teacher portrait báo MissingReference. Giữ nguyên importer/catalog vì chưa xác định đây có phải thay đổi có chủ ý của Xuân; không chuyển reference sang IDSingle. Lỗi này không nằm trong assets Day4 và cần được xử lý ở lượt portrait riêng.


### Chỉnh nhịp kết ngày theo Xuân — 2026-08-28

Scene120 thêm `215_KeepTitleHidden` (alpha0, tức thì) trước fade1.1s và `225_HoldBlackBeforeTitle` giữ màn đen0.45s; sau đó `230_DayThreeEnds` mới fade-in chữ0.85s. Title không hiện trong lúc nền đang fade, kể cả replay với title cũ cònalpha1. Không đổi thoại hoặc transitionprofile. Test tail được mở rộng để kiểm tra ba mốc fade/black/title và tiếp tụcLoadScene130; **1/1passed**, XML `Temp/Day4/tests_day3_fade_before_title_pass.xml`, kết thúc16:26:01Z. Ảnh `day3-fading.png`, `day3-black-before-title.png`, `day3-title-after-black.png` xác nhận thứ tự render.
