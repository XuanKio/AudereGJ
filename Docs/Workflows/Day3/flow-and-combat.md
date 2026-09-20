# Flow, staging, vẽ phấn và Teacher combat

[Mục lục nguồn](../../15_Day3_BoardTeacher_StoryWorkflow.md) · [Bản đồ docs](../../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
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

<!-- END PRESERVED SOURCE -->
