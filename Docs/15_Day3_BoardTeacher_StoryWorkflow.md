# Ngày 3 — vẽ bảng, mệt mỏi và lời hỏi han bị diễn giải thành áp lực

Status: **Design Intent**, triển khai theo yêu cầu Xuân ngày 2026-08-28. Cô giáo vẫn ôn hòa; lời tiêu cực trong combat là hình dung bị Timor bóp méo, không phải suy nghĩ đã được xác nhận của cô. Enemy visual và location art là **PLACEHOLDER**. Hậu thoại đã triển khai theo yêu cầu Xuân; ontology combat, art chính thức và cân bằng cuối vẫn **Unresolved**.

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
  → Dreamy Disorientation → combat 15 HP / 90 TIME
  → Victory: giữ hình enemy khi cô trấn an → neutral fade về hai tile cạnh nhau → chọn một trong ba câu trả lời → cô xin phép ôm → Audere đồng ý → cử chỉ gần lại và giữ yên
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

- ID `d3-teacher-perceived-pressure`; display “Cô giáo”. **SharedHealthThresholds**, tổng15HP, chuyển ở7/4/0; không hồi đầy HP mỗi phase. Damage dư qua threshold bị bỏ theo contract chung. HP nguyên nên mốc dưới nửa máu (7.5) là7HP.
- `CombatController` nằm ở `SYSTEMS/Combat Systems`, tách khỏi `WORLD/Combat Root/CombatBoard` như Scene40. `CombatStep.combatController`, controller.boardView và WorldModeController.combatSystemsRoot vẫn direct reference. Scene120 không có puzzle nên không tạo Puzzle Systems rỗng. Tool tạo Day3 mới cũng theo bố cục này; không rebuild scene cũ.
- Audere90TIME (1 phút30 giây),3dice/batch,max2Attack/batch kể cả reroll/caught budget. Giữ nguyên CombatDiceConstants, Shield clear và physics dice.
- Phase1: `ChalkFenceMove` hàng phấn từ trên/dưới, chừa hành lang giữa và một cột trống luân phiên; sau đó `ChalkSweepMove` phấn xoay băng ngang.
- Phase2 (7→4HP): `RadialInwardTrailMove` mở đầu, rồi sweep, rồi fence, OrderedLoop.12 thanh phấn chia đều trên một vòng tròn ngoài Dice Field, hướng vào tâm; alpha0→1 trong1.1s, sau đó đồng loạt lao xuyên tâm trong2.6s. Đòn dài8.2s để đọc vệt và có khoảng thoát. Bán kính tính theo hướng phát thực tế, kích thước field và chiều dài phấn, không đẩy vòng báo trước ra khỏi màn hình chỉ để bao các góc không dùng.
- Phase3: `SineProjectileStreamMove` tạo luồng đạn thường uốn lượn từ trên xuống + laser columns có telegraph0.85s. Luân phiên field-shift + sweep; vùng Dice Field co còn82%, dịch trong Frame, rồi trả về.
- Đạn thường dùng nguyên `Prefabs/Combat/Bullets/EnemyBullet.prefab` chung: sprite `AssetGame/Item/dan.aseprite`,24×24,không tint riêng. `Move_ChalkSineStream` giữ tên/GUID cũ nhưng reference đạn đã đổi sang EnemyBullet; reference dự phòng của `Move_TeacherLaserColumns` cũng dùng EnemyBullet. Laser vẫn là presentation riêng của CombatLaserView, không biến thành viên phấn.
- Chỉ các đòn phấn đặc biệt `ChalkFenceMove`, `ChalkSweepMove`, `RadialInwardTrailMove` dùng `Bullet_ChalkRod`120×19 với `AssetGame/Item/phan.aseprite`. Cả ba bật `stunTrail`: vệt chấm tím nằm trong Dice Field, chặn catch3.6 giây combat-active rồi fade0.3s không còn chặn. Reroll và di chuyển vẫn dùng được. Shield clear projectile thì ngừng tạo vệt mới; vệt đã tạo tự hết hạn hoặc được dọn cùng lifecycle phase/move/session. Không đổi prefab đạn thường, dice constants hay thoại.
- `ParametricProjectileMotion` giữ elapsed riêng mỗi projectile, được tick bằng combat-local delta; pause giữ nguyên vị trí. Pool Setup/Return/Fade hủy motion cũ và reset ownership/rotation/collision. Phase/session cleanup vẫn do board/controller chung sở hữu.
- Va chạm rectangle xoay dùng SAT trong `CombatRectCollision`, tránh vùng góc rỗng của AABB gây hit giả. **Teacher không còn dùng vertical impulse/hất người chơi. Co–dịch width/X của Dice Field trong Frame vẫn giữ nguyên.** Asset impulse cũ được giữ để tái sử dụng, không nằm trong moveset production Teacher.

Menu `Audere/Combat/Author Teacher Radial Trails` cập nhật riêng data và hai root trên shared CombatBoard prefab, không rebuild Scene120/actor. `Exterior Projectile Root` cho phép vòng phấn xuất hiện ngoài field; đạn thường/laser vẫn ở Projectile Mask. `Stun Trail Root` có RectMask2D trong field. Chi tiết contract/pool/cleanup tại `Docs/06_CombatGameplay.md`.

## Chỉnh sửa / rerun author

### Portrait và lời combat bị bóp méo

- `Data/Dialogue/Day3/TeacherCombat/`: giữ 10 asset/GUID, hiện tham chiếu 7 asset trong ba sequence tổng30 bubble (prefix Timor được dùng ở cả ba mốc). Ba draft `LET_ME_DO_IT`, `THE_CLASS_WAITS`, `DONT_ADD_TROUBLE` được giữ nhưng không tham chiếu trong encounter. Audere luôn trái; Timor/cô giáo bên phải. Enemy visual và scene art không đổi.
- Catalog tách `Teacher = 3` (người thật, `Co_giao_0`) và `TeacherDistorted = 7` (Timor bóp méo, `Co_giao_Creepy_0`). Dùng `Line.CharacterOverride` để trở về Teacher ở câu chăm sóc thật; không đổi nội dung, thời lượng hay thứ tự cue. TeacherAfterCombat giữ Teacher normal, BiancaReprise giữ Timor trực tiếp/Bianca thật. Quy ước chung và QA ở `Docs/05_DialogueSystem.md`.
- Mỗi sequence: Timor “Chắc cô đang nghĩ…” → lời cô giáo bị bóp méo với `Co_giao_Creepy_0` → một câu chăm sóc bằng `Co_giao_0` → Audere đối thoại trực tiếp với Timor. Glitch portrait giữ cơ chế hiện có. Đây là **Design Intent về sự diễn giải của Timor**, không xác nhận cô giáo thật sự trách Audere.
- Mỗi phase chỉ có **một `PhaseEnter` cue**, auto/non-click, không repeat, không interrupt. Bỏ trigger theo mỗi move/catch để câu lập luận không bị cắt hoặc quay về mức phản kháng trước. Min1.4s,30 ký tự/s,gap0.12s; không click, không claim Dialogue input; TIME/đạn/Heart/move pause cục bộ tới khi sequence kết thúc.
- Phase1/2 dùng `RequiredBeforePhaseAdvance`, phase3 dùng `RequiredBeforeVictory`. Nếu damage đạt7/4/0 trước khi nói xong, giữ phase hiện tại, không nhận thêm damage/không chuyển damage dư, tiếp tục dodge/heal/moves; hết sequence thì tick kế tiếp chuyển phase/Victory, không yêu cầu hit bổ sung. HP có thể hiển thị0 trong phần cuối sequence trước Victory. Defeat/cancel vẫn được phép ngắt. Đây không phải ba phase hồi HP; lượt cập nhật15HP/90TIME không sửa lời/cue.
- Audere giữ `Audere_Scared_0` qua hai mốc đầu. Ở câu “Nhưng tớ đang mệt thật.” đổi sang `Audere_Tired_0`, giữ tới cuối: cô dám nhận giúp đỡ nhưng vẫn kiệt sức, không chuyển sang cười/đắc thắng. Timor vẫn `TimorLoLangKhongVui_0`, lời bảo vệ thu hẹp lựa chọn của Audere; không cần tăng thành quát tháo.
- Ba asset cũ `Dialogue_D3_COMBAT_PROJECTION_01..03` được giữ nguyên, không còn được enemy này tham chiếu; không xóa draft của người dùng.

### Nhịp phản kháng trong combat — 2026-08-28

Mỗi mốc bắt đầu bằng prefix Timor nêu trên. Các dòng bên dưới là thứ tự sau prefix, không phải lời nói ngoài đời của cô giáo:

**15 → 7 HP: Audere bắt đầu nghi ngờ cách Timor diễn giải.**

- Cô giáo (bóp méo): “Cô phải bỏ việc để trông em.” / “Em làm mọi người cuống cả lên.”
- Cô giáo (bình thường): “Audere, em nghe cô nói không?”
- Audere: “Cô… đang hỏi tớ mà.”
- Timor: “Vì cô đang phải lo cho cậu đấy.”
- Audere: “Tớ biết. Nhưng cô chưa trách tớ.”
- Timor: “Cô không cần nói ra.”
- Audere: “…Cậu đâu biết chắc.”

**7 → 4 HP: Audere viện vào trải nghiệm thật với Bianca hôm qua.**

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

Victory chỉ kết thúc lớp diễn giải áp lực, không chứng minh Audere đã khỏi lo âu hoặc biến Timor thành kẻ xấu tuyệt đối. Phần hậu thoại bổ sung sau lượt combat nằm ở mục dưới; không thay lời phản kháng đã có trong ba phase.

Menu `Audere/Story/Author Day 3 Board and Teacher` chỉ tạo scene/data/prefab còn thiếu và nối Scene90 một lần. Guard Play/dirty scene; không rebuild nội dung đã được Xuân chỉnh. Scene100/110/120 đã có trong Build Settings và GameScenes; GameplayUIRoot tắt PuzzleUI tại các scene này.

DialogueData mới nằm ở `Data/Dialogue/Day3/`. Chỉnh DialogueData thay vì chạy author để ghi đè lời. Giữ rõ Design Intent; không biến placeholder asset thành canon.

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
