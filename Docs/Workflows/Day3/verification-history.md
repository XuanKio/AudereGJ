# Các checkpoint kiểm tra Day3

[Mục lục nguồn](../../15_Day3_BoardTeacher_StoryWorkflow.md) · [Bản đồ docs](../../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
## QA đã thực hiện

### Vòng phấn, trail3.6s,15HP/90TIME và giữ co–dịch field — 2026-08-28

- C# compile thành công.68/68 EditMode tests PASS: RadialStunTrail, CombatEnemyRuntime, EnemyActorFloat và5 test data/binding/chalk Teacher; XML `Temp/RadialQA/tests.xml`. Có coverage trail3.59s còn chặn/3.61s hết chặn, pause, clip khi field dịch/co, cancel warning/flight/end, Shield ngừng sinh vệt, pool đúng prefab/lease và board disable.
- Play Scene120 qua CombatStep thật, data production15→7→4→0: Completed đúng1 lần sau69.81s; đầu trận90TIME. Dùng debug Attack/Heal để đo lifecycle, **không phải test độ khó/thắng bằng chuột**. Required cue vẫn giữ damage ở threshold trước khi chuyển phase.
- Ring phase2:12 viên, warning chưa có trail; flight sinh vệt, tất cả warning nằm trong viewport1920×1080 (mép thấp nhất6.3px). Đã xem ảnh `production-warning.png`, `production-flight.png`, `production-field-shift.png` trong `Temp/RadialQA`. Ảnh `ring-*` là mẫu isolated ban đầu, hai viên dưới bị cắt; không dùng làm hình QA cuối sau khi thu bán kính theo hướng phát.
- Toàn trận đo tối đa323 trail segment (<cap384), field co0.82, dịch X tối đa43.32 đơn vị local; vertical-control không bật. Cuối trận projectile0/trail0/dialoguefalse/input0. Báo cáo `Temp/RadialQA/production.txt`.
- Cancel giữa flight có128 trail/12 viên: clear hết, callback1,input0,width1. Replay15HP/90TIME/phase0; Defeat mở Retry sau khi clear projectile/trail/dialogue/input. Bấm đôi chỉ đổi session3→4 một lần; lượt mới15HP/90TIME/phase0,input1. Cancel hai lần vẫn dọn hết; `Temp/RadialQA/retry.txt`.
- Một lượt Play probe đầu bị forced synchronous domain reload giữa phase1; không tính là pass, chưa xác định nguồn reload. Đã stop/reload scene và chạy lại toàn bộ production/Retry thành công, không ghi file trong lúc chạy. Console cuối0error/warning, PlayOFF,compileOFF,Scene120dirty=false,startup=true,0missing/broken; không còn callback QA.
- Hash Scene120,Scene80,10 TeacherCombat DialogueData và CombatDiceConstants giữ nguyên so với đầu lượt. Shared CombatBoard chỉ thêm binding/root/material trail; scene override enemy art/size/position của Xuân được giữ. Không chạy lại builder production toàn scene.
- Chưa replay full100→110→120, chưa test balance90TIME bằng chuột,4:3/ultrawide hoặc build executable trong lượt này. Các mục QA phía dưới là lịch sử với data12HP/120TIME cũ, không thay cho thông số hiện tại.

### Phản kháng tăng dần và tách SYSTEMS — 2026-08-28

- Compile không có error/warning.14/14 EditMode tests PASS (`Temp/TeacherResistanceQA/tests_14_pass.xml`,05:39:53Z): gồm hai test mới shared-HP dialogue gates/restart/cancel, test data Teacher mới và regression policy/selector/Retry/chalk.
- Play Scene120 qua CombatStep: ép damage tới8/4/0 sớm, vẫn đọc đủ30 bubble theo thứ tự; ba phase kéo dài khoảng18.6/21.2/22.2s active, toàn lượt68.8s tính intro/transitions. MoveVersion tới4 mỗi phase, có đạn và TIME vẫn giảm. Dùng debug Heal để tránh chết trong phép đo — **không phải play-test cân bằng**.
- Victory Complete đúng1 lần sau câu cuối; dialogue=false,input0,projectile0,vertical constraint=false. Ảnh1920×1080 `Temp/TeacherResistance-final.png`: Audere Tired bên trái, Timor phải, câu cuối vừa ô và board vẫn hoạt động.
- Defeat trong lúc gate8HP đang chờ thoại: Retry mở sau cleanup input0/dialoguefalse/bullet0. Double-click tạo đúng attempt kế tiếp (session2→3), phase0/12HP/cue chưa resolve/input1. Cancel hai lần chỉ callback1; Retry owner/dialogue/input/bullet/vertical đều clear.
- MCP reparent controller hiện có sang SYSTEMS, không tạo lại controller/board; CombatStep reference giữ đúng.16 hash encounter/moves/actor prefab/EnemyBullet/Scene110 không đổi; enemy definition so JSON bỏ dialogueCues bằng nhau. Scene120 thay hierarchy có chủ đích.
- Chưa chạy full Scene110→120 theo input người chơi, chưa thử thắng bằng chuột/đánh giá balance120TIME, chưa kiểm lại4:3/ultrawide hoặc cancel tại mọi phase trong lượt này. Kiểm lời mở tỉnh lại bằng data/reference, không thay pose actor thành nằm.

### Tách đạn thường và phấn đặc biệt — 2026-08-28

- Compile chung với lượt enemy bob;54/54 tests pass, XML `Temp/EnemyFloat/tests_54_pass.xml`, kết thúc `05:22:36Z`. Gồm test mới `TeacherProjectiles_OrdinaryStreamUsesSharedBullet_ChalkOnlyForSpecials`, cùng2 test sẵn `ChalkMotion_TelegraphPauseCancelAndPoolReset` và `RotatedChalk_HitsOnlyItsOrientedRectangle`; phần còn lại là45 CombatEnemyRuntime và6 bob tests.
- Play QA isolated execution trên board Scene120:9 viên thường đều có SourcePrefab EnemyBullet/sprite dan;12 rod đều có SourcePrefab ChalkRod/sprite phấn; không tái sử dụng nhầm instance giữa hai pool. Cancel không bắn thêm; return toàn bộ trả active0/collision0/input0.
- Ảnh so sánh hai loại đã xem ở1920×1080: `Temp/TeacherProjectileQA/ordinary-and-special.png`. Đây là frame QA ghép hai execution để so hình, không phải thay đổi thứ tự đòn production. Chưa replay toàn trận Victory/Retry hoặc aspect khác trong lượt sửa reference này.
- Scene120 không lưu thay đổi QA: PlayOFF,compileOFF,dirty=false,startup=true,0missing script/broken prefab;Console0error/warning. Hash enemy definition/cues, Fence/Sweep và EnemyBullet chung giữ nguyên.

### Lượt portrait/cue cô giáo — 2026-08-28

- MCP author asset-only: 10 DialogueData /16 bubble,12 cue; enemy validation hợp lệ, sprite import đúng `Co_giao_Creepy_0`/`Co_giao_0`, Audere trái, mỗi dòng tối đa42 ký tự. Không thêm/sửa C# trong lượt này.
- Play Scene120 qua3phase bằng debug Attack để kiểm lifecycle: quan sát đủ các đoạn mở, creepy/normal portrait theo move, TIME vẫn giảm và projectile vẫn hoạt động; InputGate chỉ giữ1 claim Combat, không thêm claim Dialogue. Đây không phải kiểm tra thắng bằng chuột hoặc balance.
- Lượt đầu phát hiện move mở phase zero-lead-in không phát `MoveStarted`; đã bổ sung cue `PhaseEnter` riêng rồi chạy lại đủ3phase. Cancel sau phase3 trả combat/dialogue=false,claim0,projectile0,vertical-owner=false. Replay trở lại phase đầu và phát lại đoạn mở.
- Ảnh đã kiểm tra trực quan tại1920×1080: `Temp/TeacherProjectionQA/visible-Co_giao_Creepy_0.png`, portrait/ô thoại đúng bên phải, không che board. Ảnh lượt đầu bị Fade startup che đen vì QA bỏ qua story fade-in, không dùng làm bằng chứng visual; chỉ bỏ Fade trong Play QA, không lưu thay đổi vào scene. Normal portrait đã kiểm tra qua live binding, chưa có ảnh normal hợp lệ trong lượt này.
- Bàn giao PlayOFF,compileOFF,Scene120dirty=false,startup=true,0 missing scripts/broken prefabs; Console0 error/warning ở lượt kiểm cuối, mọi QA callback đã gỡ. Hash Scene60/120,enemy prefab,encounter và3 DialogueData trước combat giữ nguyên. Không rerun toàn bộ90test bên dưới, chưa test lại Retry/Victory/Attack-catch reply bằng chuột hay các aspect khác trong lượt portrait này.

### Lượt triển khai Day3 trước đó

- Unity6000.0.79f1 compile C# thành công; shader Chalk supported, ShaderUtil không báo lỗi.
- Suite **90/90 Passed**, XML kết thúc `2026-08-28 04:56:14Z`: Day3SchoolTests, CombatEnemyRuntimeTests, EveningNightPressureTests, MusicPresentationTests, BiancaProjectilePolishTests.
- Regression Day2NightDreamTests sau nối Scene90: **5/5 Passed**, XML kết thúc `2026-08-28 05:01:59Z`; vẫn đi đủ15 ô, collapse, wake/startle rồi tới end-Day2 title. MCP job báo timeout khởi tạo do reload nhưng Unity thực sự chạy và ghi đủ5 kết quả pass; dùng XML làm evidence.
- Production Play100→110→120: xác nhận SchoolBell AudioSource đang phát trước load, 5hop đạt X+1.25world, first-pointer raycast vào bảng trống, nét kéo, Complete double-click, auto calls không claim input, distortion sạch trước combat.
- Trận production qua12→8→4→0 và về Story; kết thúc không còn projectile/input/vertical-owner. Test dùng `DebugApplyDiceEffect(Attack)` để xác minh lifecycle, **không phải thắng bằng chơi tay hay kết luận balance**.
- Defeat→Retry double-click chỉ tạo attempt mới sạch12HP; cancel dọn projectile/dialogue/input. Test vertical warning/pull/cancel, modal cancel/owner destroyed, pool reset/paused motion, SAT góc rỗng.
- Rerun author giữ nguyên byte Scene60/90/100/110/120. Scene mới không missing script; dialogue left/right/length và data validation được kiểm tra.
- Visual bảng vẽ ổn định ở1920×1080,1440×1080,2520×1080; nút Complete là raycast trên cùng tại4:3/21:9. Ảnh tại `Temp/Day3QA/drawing-stable-16x9.png`, `drawing-4x3.png`, `drawing-21x9.png`. Các size QA tạm đã xóa khỏi GameView.
- Fatigue cancel hai lần: callback1 lần(false), feature inactive, transition=false,input0. Ảnh story/fatigue/chalk-fences/laser-stream cũng trong `Temp/Day3QA`.
- Lượt đầu tìm và sửa missing CanvasRenderer, thiếu StoryEventsRoot. Ảnh shader compile lần đầu có cyan Editor placeholder; ảnh ổn định sau compile hiển thị nét phấn đúng. Không coi ảnh warmup là kết quả cuối.
- Chưa build player executable, chưa nghe đánh giá loa thật, chưa chơi tay cân bằng đủ120s hoặc full story ở mọi aspect. Chưa viết hậu thoại sau trận cô giáo.
- Kiểm tra bàn giao: Scene20/30/60/90/100/110/120 đều0 missing scripts,0 broken prefabs,dirty=false; StoryDirector startup/reference còn đúng. Console không có error runtime mới, chỉ hai thông báo Test Runner save/cleanup. Dừng Play, mở Scene100; lượt Editor kế tiếp được nhả cho task âm thanh, không giữ QA callback.

<!-- END PRESERVED SOURCE -->
