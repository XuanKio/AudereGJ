# Lịch sử quyết định và bảo trì

[Mục lục nguồn](../00_ProjectOverview.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
## Decision log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-08-11 | Bootstrap = single entry point; services under `Bootstrap › Services`, init order = sibling order. | Scales without editing Bootstrapper; avoids a God-Object entry point. |
| 2026-08-11 | Scene transitions only via `SceneFlow`; names via `GameScenes`. | Single choke-point + SSOT; no code/Build-Settings drift. |
| 2026-08-11 | Audio id-based: `AudioId` enum → `AudioCatalog` (SO) → clip; explicit permanent numeric ids. | Decouples gameplay from file names; designer swaps sounds in one asset; ids stable across reordering. |
| 2026-08-11 | Home-morning gameplay before `GameSettings`; `SaveManager` = auto-save, deferred. | Core loop first; save needs the data model locked down. |
| 2026-08-11 | Docs live in repo-root `Docs/` (outside `Assets/`). | Keeps docs out of Unity's asset import (no `.meta` clutter); standard repo convention. |
| 2026-08-15 | Gameplay UI dùng prefab `GameplayUIRoot` độc lập và persistent; Main Menu giữ UI riêng. | Không gắn UI vào Player; tránh mất UI khi đổi gameplay scene và tránh kéo UI gameplay vào Main Menu. |
| 2026-08-16 | Gộp gameplay HUD và dialogue vào một Canvas `GameplayUIRoot`, chia child `PuzzleUI`/`DialogueUI`. | Tránh hai root Canvas trùng trách nhiệm; UI gameplay giữ xuyên scene và scene mới chỉ cần rebind systems. |
| 2026-08-15 | Dialogue dùng `DialogueCharacterId` + catalog trung tâm + `DialogueData`; trigger được gán theo từng cell trong `PuzzleData`. | Designer chỉ chọn constant nhân vật; tên/portrait tự resolve và không lặp theo từng đoạn thoại. |
| 2026-08-16 | Portrait Left/Right luôn giữ scale; người không nói dùng tint tối, bubble chuyển lượt bằng pop/fade/rise trước khi chạy typewriter. | Tránh viền do alpha/scale portrait và giúp lượt nói chuyển mượt, dễ nhận biết. |
| 2026-08-16 | Combat dùng `WORLD/Combat Root`, ngang hàng với `Puzzle Root`; board là world-space Canvas prefab, không nằm trong `GameplayUIRoot`. | Combat board thuộc lifecycle của gameplay mode và đi theo camera/world; `GameplayUIRoot` chỉ giữ UI xuyên scene. |
| 2026-08-16 | `WORLD` sở hữu `WorldModeController`; logic được nhóm trong `Puzzle Systems`/`Combat Systems` và chuyển mode bằng fade đen. | Một nơi quản lý lifecycle, camera và UI; parent group là switch duy nhất nên child controller không bị kẹt inactive. |
| 2026-08-16 | Combat chuyển sang real-time hoàn toàn: Attack/Armor/Heal áp ngay khi catch; hết batch chỉ spawn batch mới sau 0.3 giây. | Không còn turn hoặc resolve cuối batch; timer, bullets, player và dice luôn chạy đồng thời. |
| 2026-08-16 | `PuzzleViewportMask` là child của `Main Camera`, không nằm trong `Puzzle Root`; `WorldModeController` bật/tắt mask theo mode. | Mask mô tả viewport nên phải giữ cố định theo camera follow, không trôi theo tọa độ map. |
| 2026-08-16 | Attack/Armor/Heal dùng ba dice prefab riêng; dice mặc định không xoay; enemy hit dùng white-silhouette shader. | Bám motion reference của Dice Catcher và cho phép chỉnh từng mặt dice/art feedback độc lập. |
| 2026-08-16 | Dice, enemy bullets và mouse-controlled Audere Heart nằm chung trong Battle Box; Heart là tâm của Catch Cursor. | Một input mouse vừa xử lý dice vừa quyết định vị trí né đạn, giữ toàn bộ áp lực trong cùng không gian. |
| 2026-08-16 | Stun Zone là vùng chấm tím chặn catch/reroll theo vị trí cursor; cursor đổi viền tím và pop dấu `X`, còn dice vẫn di chuyển bình thường. | Khớp frame reference: vùng stun vô hiệu hóa công cụ bắt chứ không tác động vật lý hoặc presentation của dice. |
| 2026-08-16 | TIME là sinh lực duy nhất của player: Heal cộng TIME, bullet trừ TIME, Armor chặn hit; không còn Player HP riêng. | Gộp áp lực sống sót và giới hạn encounter vào cùng một tài nguyên dễ đọc liên tục. |
| 2026-08-16 | `HeartVisual.prefab` chỉ chứa một sprite placeholder; Timer Fill co `RectTransform` từ trái thay vì dựa vào `Image.fillAmount`. | Heart art thay độc lập; timer vẫn hiển thị đúng kể cả khi Image chưa có sprite. |
| 2026-08-16 | Player damage làm TIME fill giảm ngay, để lại white damage-trail co trễ và rung camera ngắn; Armor block không phát damage feedback này. | Lượng TIME vừa mất đọc được tức thì, đồng thời tạo phản hồi va chạm rõ mà không che gameplay real-time. |
| 2026-08-16 | Ba dice prefab dùng icon Aseprite riêng: Attack=`attack`, Armor=`gaurd`, Heal=`heal`; TMP label chỉ là fallback inactive. | Art được author trực tiếp trên đúng prefab để chỉnh độc lập; `CombatDieView` không giữ một thư viện ba sprite. |
| 2026-08-16 | Dice có phase tung neutral `#23212D`: ground shadow trượt qua board, thân dice nảy parabol 2–3 lần rồi cú chạm cuối mới reveal màu Attack `#A83B44`, Armor `#B0ABB7`, Heal `#D8C097`. | Tạo chiều sâu giả 3D như reference, tránh batch đồng bộ và chỉ mở input khi dice thật sự ổn định. |
| 2026-08-16 | Dice đang tung chuyển sang `Airborne Dice Overlay` ngoài `Dice Field/RectMask2D`, render trên `Frame`; landed mới trả về `Dice Root`. | Dice có thể phủ lên mép board như vật thể đang bay thay vì bị mask hoặc viền đè lên. |
| 2026-08-22 | Puzzle layout chuyển sang scene-first; level prefab/Scene là source of truth, `PuzzleData` chỉ giữ config/migration. | Designer nhìn và chỉnh trực tiếp board, Goal, PlayerStart và interactive object khi không Play. |
| 2026-08-22 | Một location chỉ có một Player/PuzzleRuntime/PathPreview/PlacedPathRoot dùng chung; từng `PZ_*` chỉ giữ level content. | Tránh duplicate preview/path/player và lỗi state khi đổi puzzle. |
| 2026-08-22 | Puzzle hand-off dùng Goal trước làm world anchor cho PlayerStart sau; Player không bị tắt giữa event. | Giữ chuyển cảnh liền mạch và tránh nháy/lệch tile. |
| 2026-08-22 | Story author bằng `StoryDirector → StoryEvent → direct-child StoryStep`, sibling order là execution order. | Flow đọc/chỉnh trực tiếp trong Hierarchy và không hardcode story trong manager. |
| 2026-08-23 | Production story chuyển `20_D1_Home_Morning → 30_Classroom` qua fade + `SceneLoadStep`; mỗi scene có StoryDirector riêng. | Direct StoryEvent reference không sống qua Single scene load; flow vẫn đọc được tại từng Hierarchy và đi qua SceneFlow. |
| 2026-08-23 | Placeholder classroom actors/art được đặt tên rõ; Teacher id có catalog entry nhưng portrait để trống. | Cho phép dựng và kiểm tra staging mà không tự biến art tạm hoặc suy luận nhân vật thành canon. |
| 2026-08-23 | Classroom staging chỉ giữ Audere trái / Teacher phải, cân giữa cùng baseline; Teacher dùng prefab riêng. Dialogue entrance resolve first speaker trước fade. | Tập trung beat vào hai nhân vật, dễ thay art Teacher tại một chỗ và loại nháy active-state của cả hai portrait khi bắt đầu thoại. |
| 2026-08-23 | Production camera fallback dùng `#160D1C`; `PuzzleViewportMask` dùng `#0D0918` từ prefab; transition cover dùng đen. Main Menu giữ màu UI xanh riêng nhưng camera vẫn dùng fallback chung. | Tránh đổi tông/lóe skybox giữa scene, đồng thời không xóa màu ngữ cảnh của UI và location art. |

## Maintenance

Update this file + the relevant topic doc when a folder/script/asset is added, moved, or
removed, or when an architectural decision is made (add a decision-log row).
<!-- END PRESERVED SOURCE -->
