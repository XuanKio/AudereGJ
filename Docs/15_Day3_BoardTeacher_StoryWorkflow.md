# Ngày 3 — vẽ bảng, mệt mỏi và lời hỏi han bị diễn giải thành áp lực

Status: **Design Intent**, triển khai theo yêu cầu Xuân ngày 2026-08-28. Cô giáo vẫn ôn hòa; lời tiêu cực trong combat là hình dung bị Timor bóp méo, không phải suy nghĩ đã được xác nhận của cô. Enemy visual và location art là **PLACEHOLDER**. Ontology combat, art chính thức, cân bằng cuối và hậu thoại sau trận vẫn **Unresolved**.

## Flow production

```text
90_D2_Home_Awakening
  → startle → fade đen → “Ngày 2 - Kết thúc” → load
100_D3_Home_Morning / D3_HOME_MORNING
  → “Ngày 3” → Timor gọi / Audere đã dậy
  → “Vậy đầu tiên—” → SchoolBell → fade → load
110_D3_School_Board / D3_SCHOOL_DECORATE_BOARD
  → 5 hop sang phải, song song với 5 câu Audere/Timor
  → Bianca quay lại → chào hỏi / phân việc
  → vẽ phấn → Hoàn thành → Bianca hop tại chỗ / khen
  → Audere kể mất ngủ, chóng mặt
  → Fatigue Sway + Bianca gọi tự động → fade → load
120_D3_School_Teacher / D3_TEACHER_CHECK_IN_PRESSURE
  → cô hỏi han → Timor diễn giải tiêu cực
  → Dreamy Disorientation → combat 12 HP / 120 TIME
  → Victory: neutral fade về cô; kết thúc event, chưa thêm hậu thoại
  → Defeat: Retry chung; không replay bảng vẽ
```

Scene90 vẫn giữ startle sau ác mộng. Phần kết ngày chỉ được nối sang Ngày3 sau yêu cầu mới này; không sửa lại thoại Ngày2.

## Staging và thoại

- Scene-first, direct references; sibling order của StoryStep là thứ tự thực thi. `StoryDirector.storyEventsRoot` phải trỏ vào STORY.
- Dùng Player/Bianca/Teacher prefab hiện có, body Player order5, shadow4. Actor chân ở tâm tile; hop shadow không đi theo Y. Camera chỉ follow X trong đoạn đi bộ, dừng follow và căn giữa hai bạn khi gặp nhau.
- Audere ở tile5, Bianca tile6. Bianca ban đầu nhìn phải, quay trái khi Audere tới cạnh. Startle khen tranh là `VerticalInPlace`, 0.19s, arc0.055; không dịch X/Z.
- Dialogue luôn Audere trái, đối thoại phải, tối đa42 ký tự/bubble. Timor dùng portrait có sẵn `TimorLolang` rồi `TimorLoLangKhongVui`; Audere `Audere_Tired` rồi `Audere_Scared`. Không thay enemy sprite theo dialogue portrait.
- Bianca khen nét vẽ và bảng lớp, sau đó chuyển sang quan tâm khi Audere không theo kịp. Audere: “Đêm qua tớ cứ tỉnh giấc.” / “Nhắm mắt lại là gặp giấc mơ đó.” / “Chỉ hơi chóng mặt thôi.”
- Cô giáo: “Audere, em tỉnh rồi à?” / “Cứ nằm nghỉ thêm một chút nhé.” / “Cô ở đây. Không cần vội.” Audere xin lỗi, cô đáp “Không sao đâu. Em cứ nghỉ đã.” Lời mở này theo bối cảnh Xuân chốt: Audere vừa ngất/tỉnh lại, chưa hồi sức; không phải tranh cãi khi đang cố làm bảng tiếp. Lượt sửa thoại không thay pose/staging actor.
- Timor: “Giờ cô cũng phải dừng việc lại vì cậu.” Audere phản hồi “Cô chỉ đang hỏi tớ thôi.”; câu cuối “Cậu có nghĩ cô ấy đang…” nối sang bark combat. Lời công kích hiện dưới hình ảnh cô giáo trong combat là projection, không phải lời cô nói ngoài đời.
- `AutoDialogueStep` tái dùng `DialogueController.PlayAuto`: không click/claim Dialogue input, không global pause; branch chờ kết thúc rồi mới load scene. Cancel đóng đúng playback đang sở hữu.

## Minigame vẽ phấn

Prefab: `Assets/_Audere/Prefabs/Story/ChalkDrawingUI.prefab`, instance trong Scene110.

- Canvas Overlay order1100, scaler1920×1080 **match height**. Nền đen alpha0.78. Board Image1360×700 dùng `AssetGame/bang.aseprite`.
- `Drawable Interior Mask`: RectMask2D, inset trái/phải44, dưới64, trên44. Chỉnh vùng vẽ trực tiếp tại prefab; không hardcode kích thước trong input runtime.
- `ChalkDrawingSurface`: MaskableGraphic + CanvasRenderer, pointer trái kéo để vẽ. Rời vùng vẽ nhấc bút, phải bấm lại để nối nét mới, không kẻ xuyên khung. Mesh giới hạn9000 segments, không đọc/ghi texture mỗi frame.
- `Shaders/UIChalk.shader` + `Materials/UI/ChalkDrawing.mat`: hạt phấn cố định theo tọa độ bảng, alpha không đều và mép mềm. Đây là shader của nét vẽ, không áp lên cả sprite bảng.
- Nút Hoàn thành chỉ bật khi có ít nhất một nét. `ChalkDrawingView` giữ token InputGate trong modal; clear callback/token trước khi callback hoàn tất, double-click không tiếp tục story hai lần. Hide/cancel/owner destroyed đều nhả input.
- Tranh chỉ tồn tại trong session UI này, reset khi mở lại; **chưa có export/save tranh**. Không đánh giá nội dung tranh hoặc tính thắng/thua.

## Hiệu ứng choáng dùng chung

`Data/Transitions/WorldTransition_FatigueSway.asset`, ID `fatigue-sway`, 4.4s. Dùng material/shader Dreamy sẵn có nhưng nhẹ: tilt -0.6°→1.1°, zoom tối đa1.025, broad wave0.009, drift/radial/smear nhỏ, không veil đen.

`FullscreenPresentationStep` gọi `FullscreenTransitionController.PlayPresentation`: chỉ presentation, không đổi mode và không lấy quyền music/combat. `ParallelStoryStep` chạy cùng hai câu gọi của Bianca (PlayAuto). Cuối profile trả các giá trị về neutral và tắt renderer feature. Hủy cũng tắt feature/clear material/callback một lần.

Story→Combat tại Scene120 vẫn tham chiếu **Dreamy Disorientation** chung, không copy timeline vào scene. Scene-load và Combat→Story dùng CanvasFade/WorldMode contract hiện có. SchoolBell qua AudioId `School_Bell`/AudioCatalog; muốn nghe khi Play trực tiếp phải có service từ Bootstrap.

## Encounter và moves

Encounter: `Data/Combat/Teacher/CombatEncounter_D3_TEACHER_PRESSURE.asset`.
Enemy: `Data/Combat/Teacher/Enemy_Teacher_PLACEHOLDER.asset`.
Actor: `Prefabs/Combat/Enemies/Enemy_Teacher_PLACEHOLDER.prefab` — chỉnh Image/RectTransform trực tiếp hoặc override instance `WORLD/Combat Root/CombatBoard/.../Enemy_Teacher_PLACEHOLDER` trong Scene120. Runtime không tự normalize kích thước art.

- ID `d3-teacher-perceived-pressure`; display “Cô giáo”. **SharedHealthThresholds**, tổng12HP, chuyển ở8/4/0; không hồi đầy HP mỗi phase. Damage dư qua threshold bị bỏ theo contract chung.
- `CombatController` nằm ở `SYSTEMS/Combat Systems`, tách khỏi `WORLD/Combat Root/CombatBoard` như Scene40. `CombatStep.combatController`, controller.boardView và WorldModeController.combatSystemsRoot vẫn direct reference. Scene120 không có puzzle nên không tạo Puzzle Systems rỗng. Tool tạo Day3 mới cũng theo bố cục này; không rebuild scene cũ.
- Audere120TIME,3dice/batch,max2Attack/batch kể cả reroll/caught budget. Giữ nguyên CombatDiceConstants, Shield clear và physics dice.
- Phase1: `ChalkFenceMove` hàng phấn từ trên/dưới, chừa hành lang giữa và một cột trống luân phiên; sau đó `ChalkSweepMove` phấn xoay băng ngang.
- Phase2: sweep + `VerticalPlayerImpulseMove`. Cảnh báo0.9s rồi kéo Y0.65s, nghỉ1.6s. X vẫn nhận chuột. Luân phiên lên/xuống, không khóa người chơi vào sát viền. Sau đó fence.
- Phase3: `SineProjectileStreamMove` tạo luồng đạn thường uốn lượn từ trên xuống + laser columns có telegraph0.85s. Luân phiên field-shift + sweep; vùng Dice Field co còn82%, dịch trong Frame, rồi trả về.
- Đạn thường dùng nguyên `Prefabs/Combat/Bullets/EnemyBullet.prefab` chung: sprite `AssetGame/Item/dan.aseprite`,24×24,không tint riêng. `Move_ChalkSineStream` giữ tên/GUID cũ nhưng reference đạn đã đổi sang EnemyBullet; reference dự phòng của `Move_TeacherLaserColumns` cũng dùng EnemyBullet. Laser vẫn là presentation riêng của CombatLaserView, không biến thành viên phấn.
- Chỉ `ChalkFenceMove` (hàng trên/dưới) và `ChalkSweepMove` (thanh xoay/quét) dùng `Bullet_ChalkRod`120×19 với `AssetGame/Item/phan.aseprite`. `Bullet_ChalkGrain`33×8 cũ được giữ nhưng không còn dùng trong move Teacher. Tool dựng Day3 lấy EnemyBullet chung cho luồng đạn, không tạo grain mới. Không đổi tốc độ/interval/HP/TIME/dice/cue hoặc prefab đạn chung. Các asset move nằm trong `Data/Combat/Teacher/Moves`; không check enemyID trong runtime.
- `ParametricProjectileMotion` giữ elapsed riêng mỗi projectile, được tick bằng combat-local delta; pause giữ nguyên vị trí. Pool Setup/Return/Fade hủy motion cũ và reset ownership/rotation/collision. Phase/session cleanup vẫn do board/controller chung sở hữu.
- Va chạm rectangle xoay dùng SAT trong `CombatRectCollision`, tránh vùng góc rỗng của AABB gây hit giả. `CombatBoardView` thêm owner handle cho vertical impulse, release khi move cancel/complete hoặc board cleanup.

## Chỉnh sửa / rerun author

### Portrait và lời combat bị bóp méo

- `Data/Dialogue/Day3/TeacherCombat/`: giữ 10 asset/GUID, hiện tham chiếu 7 asset trong ba sequence tổng30 bubble (prefix Timor được dùng ở cả ba mốc). Ba draft `LET_ME_DO_IT`, `THE_CLASS_WAITS`, `DONT_ADD_TROUBLE` được giữ nhưng không tham chiếu trong encounter. Audere luôn trái; Timor/cô giáo bên phải. Không đổi catalog, enemy visual hoặc scene art.
- Mỗi sequence: Timor “Chắc cô đang nghĩ…” → lời cô giáo bị bóp méo với `Co_giao_Creepy_0` → một câu chăm sóc bằng `Co_giao_0` → Audere đối thoại trực tiếp với Timor. Glitch portrait giữ cơ chế hiện có. Đây là **Design Intent về sự diễn giải của Timor**, không xác nhận cô giáo thật sự trách Audere.
- Mỗi phase chỉ có **một `PhaseEnter` cue**, auto/non-modal, không repeat, không interrupt. Bỏ trigger theo mỗi move/catch để câu lập luận không bị cắt hoặc quay về mức phản kháng trước. Min1.4s,30 ký tự/s,gap0.12s; không click, không claim Dialogue input, không pause TIME/đạn.
- Phase1/2 dùng `RequiredBeforePhaseAdvance`, phase3 dùng `RequiredBeforeVictory`. Nếu damage đạt8/4/0 trước khi nói xong, giữ phase hiện tại, không nhận thêm damage/không chuyển damage dư, tiếp tục dodge/heal/moves; hết sequence thì tick kế tiếp chuyển phase/Victory, không yêu cầu hit bổ sung. HP có thể hiển thị0 trong phần cuối sequence trước Victory. Defeat/cancel vẫn được phép ngắt. HP12,TIME120,3dice,max2Attack và moves không đổi; đây không phải ba phase hồi HP.
- Audere giữ `Audere_Scared_0` qua hai mốc đầu. Ở câu “Nhưng tớ đang mệt thật.” đổi sang `Audere_Tired_0`, giữ tới cuối: cô dám nhận giúp đỡ nhưng vẫn kiệt sức, không chuyển sang cười/đắc thắng. Timor vẫn `TimorLoLangKhongVui_0`, lời bảo vệ thu hẹp lựa chọn của Audere; không cần tăng thành quát tháo.
- Ba asset cũ `Dialogue_D3_COMBAT_PROJECTION_01..03` được giữ nguyên, không còn được enemy này tham chiếu; không xóa draft của người dùng.

### Nhịp phản kháng trong combat — 2026-08-28

Mỗi mốc bắt đầu bằng prefix Timor nêu trên. Các dòng bên dưới là thứ tự sau prefix, không phải lời nói ngoài đời của cô giáo:

**12 → 8 HP: Audere bắt đầu nghi ngờ cách Timor diễn giải.**

- Cô giáo (bóp méo): “Cô phải bỏ việc để trông em.” / “Em làm mọi người cuống cả lên.”
- Cô giáo (bình thường): “Audere, em nghe cô nói không?”
- Audere: “Cô… đang hỏi tớ mà.”
- Timor: “Vì cô đang phải lo cho cậu đấy.”
- Audere: “Tớ biết. Nhưng cô chưa trách tớ.”
- Timor: “Cô không cần nói ra.”
- Audere: “…Cậu đâu biết chắc.”

**8 → 4 HP: Audere viện vào trải nghiệm thật với Bianca hôm qua.**

- Cô giáo (bóp méo): “Cả lớp còn đang chờ em đấy.” / “Em định nằm đây đến bao giờ?”
- Cô giáo (bình thường): “Em cứ nghỉ đã.”
- Audere: “Cô bảo tớ nghỉ một chút.”
- Timor: “Cậu nghĩ cô không thấy phiền à?”
- Audere: “Hôm qua, cậu cũng nói thế về Bianca.” / “Nhưng cậu ấy đã cùng tớ sửa lại.”
- Timor: “Lần này có thể khác.”
- Audere: “Có thể… đâu phải là chắc chắn.”

**4 → 0 HP: đặt một ranh giới nhỏ dù vẫn sợ.**

- Cô giáo (bóp méo): “Lần sau em cứ ngồi ngoài nhé.” / “Đừng để mọi người phải lo thêm.”
- Cô giáo (bình thường): “Audere, cô ở đây mà.”
- Audere: “Timor, đừng nói thay cô nữa.”
- Timor: “Tớ chỉ không muốn cậu bị tổn thương.”
- Audere: “Tớ biết… Tớ vẫn sợ.” / “Nhưng tớ đang mệt thật.” / “Tớ muốn nghe cô nói.”
- Timor: “Audere—”
- Audere: “Để tớ tự trả lời.”

Victory chỉ kết thúc lớp diễn giải áp lực, không chứng minh Audere đã khỏi lo âu hoặc biến Timor thành kẻ xấu tuyệt đối. Hậu thoại thực tế với cô giáo chưa được viết trong lượt này, vẫn **Unresolved**.

Menu `Audere/Story/Author Day 3 Board and Teacher` chỉ tạo scene/data/prefab còn thiếu và nối Scene90 một lần. Guard Play/dirty scene; không rebuild nội dung đã được Xuân chỉnh. Scene100/110/120 đã có trong Build Settings và GameScenes; GameplayUIRoot tắt PuzzleUI tại các scene này.

DialogueData mới nằm ở `Data/Dialogue/Day3/`. Chỉnh DialogueData thay vì chạy author để ghi đè lời. Giữ rõ Design Intent; không biến placeholder asset thành canon.

## QA đã thực hiện

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
