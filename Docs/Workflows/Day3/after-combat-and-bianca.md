# Hậu combat, Bianca trở lại và tile alignment

[Mục lục nguồn](../../15_Day3_BoardTeacher_StoryWorkflow.md) · [Bản đồ docs](../../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
## Hậu combat Teacher — bổ sung 2026-08-28

**Design Intent theo Xuân:** Audere nghe lời cô thật và tự chọn mức độ mình muốn nói ra; thắng không có nghĩa em hết mệt hoặc hết lo. Những câu xin Timor đừng nói thay cô và muốn nghe cô nói đã có trong cuối combat, không lặp lại toàn bộ tranh luận.

- `CombatEncounter_D3_TEACHER_PRESSURE.VictoryPresentation` giữ actor enemy đang có trong scene, dừng combat và fade hazard `.45s`, rồi phát ba câu cô trấn an qua DialogueUI bình thường. Portrait `Co_giao_0`, Audere `Audere_Tired_0`; không sửa sprite enemy do Xuân đã gán. Sau lời trấn an, enemy fade `.4s` và trả về neutral Story transition sẵn có.
- `Data/Dialogue/Day3/TeacherAfterCombat`: 7 DialogueData. Ba nhánh lo lắng / mệt vì lần đầu tham gia / nói mình chỉ thiếu ngủ, không điểm số hoặc timeout. Nhánh thiếu ngủ được cô đáp đúng lời em vừa nói, không ép em thú nhận thêm.
- `140_AudereChoosesHerAnswer` dùng `StoryChoiceBranchStep`, 3 StoryEvents dưới `STORY/TEACHER REPLY BRANCHES`, cùng hội tụ tại `150_NoNeedToSolveEverythingToday`. Choice UI tái sử dụng cách trình bày của Evening, không tạo framework mới.
- Hai tile cách nhau đúng một pitch `.25 world`, Audere trái và cô phải. Reset/facing nằm dưới cover; actor feet và shadow giữ baseline cũ. Sau `170_TeacherAsksPermission`, Audere nói `…Dạ.` rồi cô mới lại gần. Cử chỉ được diễn tả bằng sprite có sẵn nhích sát, Audere đáp nhẹ, giữ yên `1.6s`, sau đó cả hai trở về tâm tile. **Không có sprite/animation tay ôm mới.**
- Bốn `CharacterMotionStep` có direct shadow/anchor, không scale hoặc đổi màu bóng/tile; Audere chỉ lift `.004 world` ở nhịp đáp. Cancel/replay trả actor về ground pose hợp lệ.
- Apply lặp lại bằng `Audere/Story/Apply Teacher After Combat To Active Scene` trên Scene120 sạch. Lượt rerun kiểm tra byte scene không đổi; không chạy lại builder Day3 toàn bộ.

### Kiểm tra lượt hậu combat

- **20/20 Passed**, XML `Temp/TeacherAfterCombat/tests_20_pass.xml`, kết thúc `2026-08-28 14:27:17Z`: Day3SchoolTests + EveningNightPressureTests.
- Production test đi từ100→110→120, qua3phase tớiVictory rồi chọn reply và kết thúc. Harness dùng Debug Attack/Heal vì Heart đứng yên khi chờ thoại; đây là kiểm tra flow/lifecycle, **không phải thắng bằng chuột hoặc kết luận balance**. Lượt trước test đứng yên bị mắc trước hậu combat; đã bổ sung Heal trong harness, không sửa HP/TIME/đòn trong asset.
- Test riêng: giữ actor/hazard0/TIME đứng yên trong Victory dialogue; cancel rồi chạy lại và callback đúng1lần; cả3reply; choice double-click; hủy khi chọn và giữa chuyển động; replay; grounded shadow giữ scale/rotation/color; Defeat/Retry vẫn qua.
- Play visual1920×1080: `choices.png`, `victory-reassurance.png`, `visual-10.png`, `visual-14.png`, `embrace.png`. Probe visual bắt đầu tại seam Victory mô phỏng và tự tiến bubble để kiểm tra hình; không thay thế production test ở trên. Đã thấy đủ reassurance → reply lo lắng → lời hỗ trợ → xin ôm → đồng ý → gần lại. `visual-log.txt` kết thúc `claims=0 combat=False trails=0 choice=False dialogue=False`; callback tự unregister.
- 30 hash bảo vệ gồm Scene80, Teacher enemy/moves, audio catalog và dice constants không đổi trong lượt hậu combat. Atlas font động do Play sinh được trả về dữ liệu trước QA. Không build executable, chưa visual cả3nhánh ở mọi aspect.

## Bianca trở lại, Timor im lặng — 2026-08-28

**Design Intent theo Xuân:** Audere tự chọn nghe Bianca thật. Timor sợ mất vị trí bên Audere; sự im lặng của cậu không phải dấu hiệu Audere đã hết lo âu. Phần Ngày4 chưa được dựng trong lượt này, không thêm kết luận hoặc cờ chữa khỏi.

- `D3_TEACHER_CHECK_IN_PRESSURE` nối trực tiếp tới `D3_BIANCA_REPRISE_AND_SILENCE`. Sau một nhịp, Bianca đi3 bước từ phải trên đường tile. Timor vội diễn giải sự có mặt của Bianca rồi mở combat quen thuộc. Các motion có anchor/shadow trực tiếp; actor order5, shadow4; không đổi màu tile hoặc bóng. Chân Bianca căn theo **bounds của bóng được vẽ**, không theo pivot lệch của sprite bóng.
- Encounter riêng `Data/Combat/BiancaReprise/CombatEncounter_D3_BIANCA_REPRISE.asset`: 6HP tự giảm mỗi3.5s; không dice và không thua. Fan đạn thường xen boomerang `dan_bianca`; đạn đổi hướng/biến mất gần Heart và không có damage collision. HP giữ ở1 cho tới khi toàn bộ lời Audere cuối cue đã kết thúc, rồi mới về0 và fade enemy0.9s.
- 13 DialogueData tại `Data/Dialogue/Day3/BiancaReprise`. Timor và Bianca thật lần lượt nói; Bianca không hứa cứu Audere hay đọc được suy nghĩ của cô. Audere vẫn dùng `Audere_Tired`, nói ngắn: “Được rồi mà, Timor.” / “Cậu biết Bianca không phải người như thế.” Timor cuối chuyển từ buồn → giận → buồn; không dùng biểu cảm đắc ý. Bianca thật luôn portrait normal.
- Khi trở lại Story chỉ giữ Audere và Bianca. Audere bước sang tile bên cạnh rồi nhảy nhỏ `.035world/.22s`. Ba nhánh dùng chung ChoiceView với Teacher, lần lượt xin ở lại / cảm ơn / hẹn cùng về lớp. Nhánh thứ ba là câu nối được soạn theo yêu cầu3lựa chọn; không chấm điểm hoặc mặc định lựa chọn đúng.
- Sau câu Timor rút lui là1.4s không lời, fade đen1.1s rồi title “Ngày 3 - Kết thúc”. Dùng `CanvasFadeStep` với `DAY THREE STORY COVER` riêng, vì Fade của Scene Transition Overlay bị tắt khi scene bootstrap hoàn tất. Không sửa profile transition dùng chung hoặc Scene80. Title và cover reset khi replay main.
- Author lặp bằng `Audere/Story/Apply Bianca Reprise To Active Scene` trên Scene120 đã lưu; không rerun builder Day3 toàn bộ. Teacher CombatStep và reprise cùng board/controller nhưng có `EnemyActorOverride` riêng.

### Kiểm chứng

- Lượt đầu **64/64 passed**: CombatEnemyRuntimeTests + Day3SchoolTests, gồm production100→110→120 và test riêng reprise. Sau sửa visual, **91/91 passed**, XML `Temp/BiancaReprise/tests_91_pass.xml`, kết thúc `2026-08-28 15:38:47Z`: runtime, MusicPresentation, BiancaProjectilePolish và3test reprise.
- Reprise Play test không dùng Debug Attack: HP tự giảm tớiVictory, cố tình đặt đạn lên Heart, không dice/damage, cancel/replay, rồi chuyển actor về Teacher. Kiểm tra gate1HP, pause/long-frame/pool reset, đủ3reply/double-click/cancel/hop/title, author rerun không thay scene byte.
- Visual1920×1080 từ bước Teacher trở về chỗ đứng đi qua Director auto-chain tới hết ngày: `visual-log.txt` ghi đúng hai câu Audere cuối khiHP1, kết thúc combat/dialogue=false/claims0. Ảnh `visual-16.png` là lời Audere ởHP1; lượt đầu phát hiện lệch chân Bianca và inactive cover, đã sửa rồi chạy lại.
- Lượt visual cuối: `hop-apex.png`, `title-final.png`, `final-visual-log.txt`. 28 mẫu hop giữ shadowY=-0.1174375 trong khi actor lên/xuống; title cuối chỉ còn chữ trên nền đen, claims0. `choices-final.png` bắt lúc UI chuyển sang lời đáp nên không dùng làm ảnh kiểm layout choice; layout và3nhánh đã có test/ảnh ở lượt trước.
- Scene60, Scene80, Teacher enemy/encounter giữ nguyên hash qua tests; atlas Mynerve tạm được trả về baseline trước QA, giữ các font edit có sẵn. Không build executable, chưa nghe loa thật hoặc QA cả mạch mới ở4:3/ultrawide. Các probe đã tự unregister.

### Bianca tile-center alignment correction (2026-08-28)

Scene120 Bianca now uses the sprite foot midpoint (the same bottom-baseline convention as GridPlayer) to align to each tile center. Updated only her entry pose and four arrival anchors; preserved the prefab shadow offset, sorting, tile presentation, all dialogue and combat data. The reprise setup tool uses the same foot calculation so future authoring retains this placement.

2/2 focused Play tests passed: arrival across three tiles with apex cancel/replay and settled foot-center assertions; existing three-reply ending through the final title. Evidence: `Temp/BiancaCenter/tests_2_pass.xml`, `arrival-apex.png`, `centered-pair.png` (1920x1080). Scene diff for this correction is six coordinate lines; no broad scene builder was rerun.
<!-- END PRESERVED SOURCE -->
