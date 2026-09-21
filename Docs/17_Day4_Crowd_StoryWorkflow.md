# Ngày4 — lớp học và Đám đông

## Ranh giới nội dung

**Design Intent của Xuân, đã author để duyệt:** Audere tự mang đồ lên lớp sau buổi sáng không có Timor. Cú ngã kéo lại nỗi sợ bị nhìn và bị cười; lời Đám đông là diễn giải trong suy nghĩ Audere, không xác nhận bạn học thực sự độc ác. Timor trở lại từ nỗi sợ mất quyền bảo vệ. Bianca hỏi một điều cụ thể về cơn đau; Audere vẫn sợ nhưng nghe được lời thật và cuối cùng xin cả lớp giúp.

**Unresolved:** phản ứng tiếp theo của cả lớp. Kết thúc Scene150 dưới đây là **Design Intent của Xuân**; không kết luận Audere đã hết lo âu.

## Combat polish — 2026-09-20

Scene140 dùng field/overlay720×360, frame760×440. Cú đâm phản đòn có fan3 viên mỗi bên; hàng tay sau đâm dày hơn nhưng giữ khe164/138. Thông số, cleanup và bằng chứng mới ở [Crowd combat](Combat/crowd-reactions-and-vfx.md#handcrowd-vùng-chơi-và-nhịp-đòn--2026-09-20).

## Flow và staging

`140_D4_Classroom/D4_CLASSROOM_CROWD`: fade-in → độc thoại đã mang được đồ → Audere nằm ngang trên tile → đau/lo đồ rơi/định nhặt lại → khoảng lặng → ngờ ngợ có tiếng cười → shared Dreamy Disorientation → Crowd combat → neutral cover → về lớp với Bianca bên phải → hỏi chỗ đau/xin phép vịn → Bianca nghiêng hỗ trợ, Audere đứng lên → cả hai hop cùng một lần → đối thoại ngắn → “Mọi người… giúp tớ được không?” → fade1.15s → `150_D4_Home_Evening`.

Tám desk tiles cách nhau hai ô lưới (pitch0.20 sau lượt fit mask, trước0.25); mỗi bàn `ban.aseprite` có chân/đáy giữa tile. Audere và Bianca dùng foot midpoint, không dùng tâm shadow để căn tile. Bàn hàng trước order8, giữa5, sau2; actor5/shadow4 trên Player. Anchor nằm trong scene, không lookup scene ở runtime. `CharacterPoseStep` giữ shadow ở anchor sàn khi xoay; cancel phục hồi pose ban đầu, replay có bước normalization dưới cover. Hai cặp parallel branches dùng StoryEvent trực tiếp: hỗ trợ đứng dậy và hop.

### Thu gọn stage trong PuzzleViewportMask — 2026-08-29

- Theo yêu cầu Xuân, tile/bàn và khoảng cách lưới còn80%, quanh midpoint hai tile nhân vật. Tile world scale0.25→0.20, bàn rộng0.215→0.172. Giữ nguyên scale actor, màu/layer của tile/bàn/shadow và nhịp động tác.
- Dịch actor và các anchor đứng/ngã/đỡ/shadow cùng delta của tile tương ứng. Audere/Bianca vẫn đứng giữa hai tile cạnh nhau. Cụm scenery còn dư ít nhất0.06 world unit với cả bốn mép mask; không đổi camera hoặc mask để che lỗi.
- Scoped menu `Audere/Story/Fit Active Day4 Classroom Stage Inside Mask` / `Day4CrowdSetupTool.FitStageActive()`. Chỉ sửa scenery/actor positions/anchors của Scene140; repeat không co thêm. Full author gọi ở cuối để giữ kích thước mới khi dựng lại.
- `Temp/Day4StageFit/tests_2_pass.xml`:2/2 content + production flow pass; kiểm bounds trong mask, chân actor sau ngã/đỡ/hop, rồi fade sang150. Đã xem ảnh Play đứng/ngã/hai nhân vật tại1920×1080 trong cùng thư mục. Không sửa combat, dialogue hoặc Scene150.

## Combat

- Encounter `Data/Combat/Crowd/CombatEncounter_D4_CROWD.asset`: **19 shared HP, 90 TIME**, 3 dice/batch, tối đa1 attack thường và1 rerolled attack/batch; shared dice constants không đổi.
- Non-Timor combat music dùng slot Music_Combat hiện có. SceneMusicSpace nằm dưới presentation lớp học, nên nhả duck khi vào Combat; không đổi AudioService/Catalog/SFX.
- Crowd dùng `SharedHealthThresholds`: HP chung, ba phase với mốc authored 10 HP → 3 HP → 0 HP (difficulty scaling vẫn dùng hệ chung). Cue lời thật phải chạy trước Victory. Không passive HP decay.
- Phase1 loop hiện tại: HandWaves → ClaspAndStab → RushingVoices → ShiftingPalms. Clasp có từng tay đuổi theo Heart; chỉ chạm thật mới giữ và kích hoạt các tay đâm. Né hết thì tay mờ đi, không bị giữ.
- Palms có2tay/nhịp, warning0.65s, một loạt10 viên với khoảng hở để lách; RushingVoices bắn3 viên/nhịp cách0.62s và cách nhau70 unit. Ghép `ShiftingBattleBoxMove` thay Width/X; Frame/Height/Y giữ nguyên.
- Khi HP chung xuống10: vào phase2, Enemy Mount lao xuống tách board. Mỗi hit gây damage trong phase2 kích hoạt một cú phản đòn như vậy, rồi trở lại `WatchingAisle`/`PressureCorridor`. Khi còn3 HP: hủy phản đòn, khép board, Bianca nói “Audere, cậu có bị đau không?” và phase3 dùng lại `UncertainHands`/`DistantVoices`.
- Enemy actor dùng IMG_1054.png, portrait ID8CrowdDistorted dùng Crowd.png, tay dùng IMG_1058.png. Không đổi importer PNG/Teacher catalog entries cũ.
- `UIWrithingHand` uốn shaft bằng UV, giữ lòng bàn tay/ngón cố định. Tay được tăng gấp2: visual108×480, palm hitbox46×54. Gốc fade từ UVy0.18→0.42 để không lộ mép ảnh; shaft mờ là presentation, chỉ palm gây hit. Sprite UV rect nằm trong material riêng; shader hỗ trợ UI clipping/stencil. Bullet pool/session/phase/lease và grouped SFX giữ hệ dùng chung.
- Các đòn tay trả lease khi đổi beat/cancel. Các viên từ lòng bàn tay tiếp tục bay đến khi ra board hoặc bị clear/phase/session invalidation. Forced control của Clasp chỉ bắt đầu sau va chạm, là owner-scoped và được clear cả phase/result/reset.

### Phản đòn tách board và cảnh báo — 2026-09-20

**Design Intent của Xuân, đã triển khai:** phase2 là nỗi sợ đám đông phản ứng khi Audere chống trả; đây là diễn giải thiết kế combat, không thêm canon hoặc lời thoại.

- `EnemyMountDiveMove` di chuyển chính Enemy Mount, giữ scale scene. Một cú kéo lên0.48s → lao0.34s → giữ0.18s → trở về0.48s → khép/nghỉ0.42s. Rainbow Echo là các snapshot silhouette có material riêng; bóng dư và đường trở về không gây hit. Board tách thành hai nửa có mép răng cưa, Heart/dice dùng biên của hai nửa. Thân đang lao dùng swept collision, luôn chừa hai phía để né.
- `damageReactionMove`/`damageReactionOnEnter` nằm trong phase data. Hit hợp lệ ngắt đòn cơ bản và dọn đạn trước khi phản đòn; hit đến khi đang lao xếp thêm một lượt, không giật enemy về vị trí đầu. Chuyển phase, Victory, Retry và Cancel xóa hàng chờ. Phase3 ở3 HP có ưu tiên hơn phản đòn.
- Các đòn cơ bản phase2 dùng `ProcessionGateMove`: hàng đạn từ trên xuống có khe164 unit dịch dần; hai bên ép vào với hành lang ngang138 unit. Theo chỉnh sửa tiếp theo của Xuân, dùng `Bullet_CrowdHand` bay, lane cách132 unit, cộng54 unit visual vào khoảng hở; nhịp2.3s/4.1s để các lượt tay dài không dồn lên nhau. Background opacity theo phase là0.55/0.60/0.18.
- Tay bắt có dấu `!` đỏ nhấp nháy0.4s bên trong board theo hướng xuất hiện, cách biên ngang22 unit và biên trên30 unit. Tay hiện0.14s, chỉ bám hướng0.28s rồi lao theo hướng khóa; tốc độ310 unit/s, xuất phát215 unit, toàn nhịp lao0.9s. Hụt thì rút28 unit và mờ0.38s, nghỉ0.3s trước tay sau. Bắt thật giữ1.02s và gọi3 tay đâm. Cú phản đòn cũng có `!` bên trong mép trên theo vị trí đáp trong toàn nhịp kéo lên. Warning dùng active time, không có hitbox, hủy/disable không để sót.
- Menu `Audere/Combat/Apply Crowd Mount Dive Phase Two` cập nhật riêng HP, ba phase, moveset, reaction và tay bắt. Giữ các cue hiện có; cue Bianca chuyển sang phase3. Full author giữ cùng cấu hình. QA trực tiếp qua MCP8080:75/75 runtime và12/12 Scene140 đạt, gồm production →3 HP/thoại Bianca →Victory →Scene150, Retry/catch, cảnh báo và disable cleanup. Đã xem ảnh cảnh báo/cú đâm/board tách; kiểm biên ở1280×720,960×720,1680×720. Bằng chứng và ảnh trong `Temp/CrowdPhase2QA`. Test Retry trước đó chờ bắt dice ngay trong thoại mở đầu; harness đã chờ hết pause trước khi thử bắt. Chưa đo cảm giác né bằng chuột thủ công.

### VFX bám visual và mép ngoài board — 2026-09-20

- VFX đánh trúng trở thành object con của visual enemy qua xử lý chung, bám cả chuyển động visual và Enemy Mount; giữ kích thước hiển thị đã cân. Áp dụng cho toàn bộ combat, gồm các prefab có VFX anchor cũ nằm cạnh visual.
- Khi board tách, biên ngang tính theo Heart thay vì vòng bắt lớn hơn; mở rộng mask của Dice Field theo độ tách và cập nhật cursor/dice sau chuyển động enemy trong cùng frame. Kết thúc/hủy trả mask và biên về ban đầu.
- Kiểm nhanh qua MCP8080: **18/18 EditMode tests đạt**, gồm cả6 prefab enemy, hai mép ngoài ở3 độ tách và các kiểm tra dive/pause/cancel hiện có. Kết quả: `Temp/CrowdPhase2QA/following-vfx-split-18-pass.json`. Chưa kiểm lại cảm giác né bằng chuột thủ công trong lượt này.

### Tay đuổi có thể né và cân phase — 2026-09-19

- **Design Intent của Xuân:** mỗi tay đuổi Heart lần lượt trên nền chơi; người chơi tự tìm khe né, không có safe box hay tín hiệu đánh dấu trước. Nếu tay chạm Heart, giữ ngắn rồi các tay từ bốn hướng đâm vào vị trí bắt được; né được thì tay mờ dần và hết đòn.
- `ConvergingHandsMove`: 3 lượt đuổi, mỗi lượt0.82s; tay xuất phát cách Heart170 unit, tốc độ390 unit/s, mờ0.3s, nghỉ0.18s. Chỉ palm va chạm mới kích hoạt giữ1.12s; bốn tay đâm cách0.12s. Dọn toàn bộ tay và nhả input khi kết thúc/hủy.
- Phase đầu đổi từ sóng tay sang truy đuổi, rồi đạn chữ thưa, cuối cùng palm/board chuyển động. Phase sau `UncertainHands`/`DistantVoices` giữ nhịp dịu hơn. HP/TIME, dice, nhạc và dialogue không thuộc lần cân này.
- Menu `Audere/Combat/Apply Crowd Dodge Phase Balance` cập nhật riêng các move asset và thứ tự moveset của Crowd; `PolishActive()` cũng áp cùng giá trị để lần dựng sau không hồi quy.
- QA bản sao Unity ở `Temp/CrowdChaseQAProject`: `clasp-tests.xml` đạt5/5; `crowd-tests.xml` đạt10/11, gồm content phase order và production Scene140. Test Retry/cancel còn lại vướng harness cũ: batch `-nographics` không có GameView; lượt có graphics lộ giả định TIME90 cố định dù Easy dùng72. Assertion đã đổi sang `ActiveMaximumTime` và C# build0 lỗi, nhưng chưa có lượt chạy lại đạt cho test này. Chưa kiểm thao tác né bằng chuột thủ công.

### Bỏ corner pull — 2026-08-29

- Gỡ `Move_Grasp` khỏi `MoveSet_Crowded` qua MCP, giữ nguyên bốn entry còn lại và thứ tự. Asset cũ không xóa, nhưng không còn được các phase của Crowd tham chiếu. Không tắt cơ chế tay chung hoặc đổi hành vi enemy khác.
- Full author và polish author không tạo/thêm lại corner pull. Không rerun broad builder để áp thay đổi; scene140/staging, scene150, dialogue,20HP/90TIME/dice/music và các move khác giữ nguyên.
- Mở rộng content test để kiểm đệ quy các composite: không có GraspingHandsMove với PullToCorners. Production test xác nhận vẫn có tay, đạn và clasp; lifecycle test bắt đầu bằng HandWaves, cancel trong clasp rồi Retry/catch các batch.
- `Temp/Day4CrowdNoPull/tests_2_pass.xml` có2/2 content+production pass; `tests_retry_1_pass.xml` có1/1 cancel/Retry pass. Chưa đo lại cân bằng độ khó bằng thao tác chuột thủ công.

## Scene150

`D4_EVENING_TIMOR_RETURNS` thay arrival trống bằng phần mở buổi tối. **Design Intent của Xuân:** sự nhẹ nhõm sau khi nhận giúp đỡ chạm vào nỗi sợ bị bỏ lại của Timor; trạng thái kết thúc beat là đấu trường Timor hoạt động và nhận input.

- Audere một mình giữa tile, PuzzleViewportMask bật; cover đầu alpha1 → chờ0.7s → reveal1.2s → yên0.9s. Không sao chép sự kiện Ngày2. Old inherited Fade vẫn0.
- Độc thoại: “Hôm nay không có Timor nhắc…” / “May là mọi chuyện rồi cũng ổn.” / “Lúc tớ ngã, mọi người đã giúp.” / “Tớ không phải làm hết một mình.” Cách viết giữ cảm giác nhẹ nhõm nhưng không phủ nhận cú ngã Scene140.
- Timor: “Vậy à…” → Audere quay trái/chờ0.65s → quay phải/chờ0.8s → “Timor?” → **3s không có câu trả lời** → “Tớ tưởng cậu không cần tớ nữa.” → giữ0.45s → bóng lớn lên.
- Facing dùng các SetActorFacingStep hiện có; actor và grounded shadow không đổi vị trí. Audere Tired→Scared; Timor Buon→LoLangKhongVui. Thoại có direct DialogueController references.
- Shared profile `WorldTransition_TimorShadow.asset`, shader `FullscreenTimorShadow.shader`: silhouette đúng `Enemyy/timor.png` hiện từ phía phải, uốn nhẹ và lớn dần, tối phủ cả phòng. Duration5.4s, cover kín4.0–4.4s, swap4.2s, sạch5.4s. Không sửa Dreamy/Fracture/Fatigue profiles. Cancel trước/sau swap trả Story/mask, hủy runtime material, không giữ input.
- Combat Root được sao chép qua Unity API từ setup Scene40 với visual Timor đã author; Combat Systems là sibling dưới SYSTEMS, không nằm trong presentation root. Encounter riêng `Data/Combat/TimorReturn/CombatEncounter_D4_TIMOR_RETURN.asset`, nhạc Music_TimorCombat/bossfightfull. Assets Ngày1 giữ nguyên.
- `Day4TimorEveningSetupTool.AuthorActive()` dựng phần mở Scene150. `Day4TimorFinalSetupTool.AuthorActiveEndingOnly()` cập nhật riêng ending trong scene sạch/EditMode; không rerun broad Day3/School/Teacher builder.

### Ending và after credit — Design Intent của Xuân, 2026-09-19

- Sau khi thắng Timor, ẩn Gameplay Canvas gồm combat UI/máu/thoại; cover trận đấu, trở về Story mode, tắt `PuzzleViewportMask` và cho camera bám Audere gần giữa khung (framingOffset y=-0.04). Camera giữ nền tối gốc ở đầu scene và combat; chỉ khi bật ending tile field sau thắng Timor mới đổi sang `#FCE1C2`, hủy/disable field khôi phục màu nền trước đó. Audere đứng dậy trên một tile, giữ 1.2 giây; hàng tile trắng mở dần sang phải trong 2.2 giây trước khi Audere bắt đầu đi. Sau đó Audere đi liên tục qua 18 anchor trong 12 giây, từ chậm tới nhanh; đường tiếp tục mở phía trước theo tiến độ di chuyển. Field chỉ còn một hàng 21 tile, tile hiện rồi giữ lại; không rung, nháy, lan sóng hoặc tự biến mất. Shader riêng `Audere/Ending Tile Ripple` giữ silhouette sprite và alpha theo từng tile. Nếu StoryEvent bị hủy, field tắt, camera, mask và Gameplay Canvas trở về trạng thái trước ending.
- Shared profile `TileRippleWalk_Ending.asset` điều khiển nhịp đứng chờ, mở hàng tile, chuyển động và sáng dần từ 70% thời lượng, khi Audere vẫn đang đi. Scene giữ direct references tới profile/actor/shadow/anchor/field. Ảnh `final_cutscene.png` (16:9) chỉ xuất hiện khi đã trắng hẳn; hiển thị nguyên tỉ lệ trên nền trắng cho các màn hình khác 16:9. Menu `Audere/Story/Update Scene150 Continuous Ripple Ending` chỉ cập nhật đoạn tile và camera; broader ending authoring cũng áp dụng cùng cấu hình.
- Credits cuộn từ trên xuống hết màn hình trong 11 giây, rung chữ 1.5 giây, rồi dùng `Heart Visual` của combat cho màn né. Màn né kéo dài 30 giây trên toàn bộ nền đen, không có khung Battle Box; chuột điều khiển tim. Dòng credit đầu bay tới sau 0.12 giây, tách thành từ rồi thành chữ cái; các mảnh tiếp tục đổi hướng đuổi theo tim. Va chạm chỉ báo bằng nhấp nháy và SFX để người chơi luôn xem được hết credit.
- Toàn bộ credit dùng `Music_Exploration`, tăng âm lượng nhạc nền lên 1.3 lần theo session; không dùng `Music_TimorCombat`. Hết 30 giây né, credits mờ đi và tự tải `10_MainMenu`.
- QA hàng tile mới: build C# thành công; đã kiểm tra scene có 21 tile, không mất reference và giữ màu camera ban đầu. Test đã cập nhật cho nhịp đứng chờ, tile không rung/ẩn và chuyển động tăng tốc; chưa chạy lại test, render hoặc Play Mode cho phiên bản này.

### Scene140: rơi trước combat — 2026-09-20

- **Design Intent của Xuân:** sau “Mọi người… đừng cười mà.”, tile vỡ và lớp học trôi lên, tạo cảm giác Audere rơi xuống vực cảm xúc. Đây là trình bày chủ quan, không xác nhận mọi người thực sự cười.
- `065_TheFloorFallsAway` dùng shared `FallingRoom_Classroom.asset`: camera/mask cùng đen, 10 tile tách mảnh, 8 bàn ghế trôi lên, 38 vệt pixel. Child event giữ rõ thứ tự: Audere hạ xuống 1.6 giây → nghỉ 0.5 giây → `Dialogue_D4_FALLING` → `070_TheRoomBecomesPressure` dùng Dreamy Disorientation như trước. Hiệu ứng tiếp tục trong lúc người chơi đọc thoại.
- `Audere > Story > Apply Day4 Falling Room` chỉ cập nhật đoạn này. Full author và polish gọi lại cùng setup; không tạo bản sao fullscreen profile. Hết/hủy đoạn rơi trả nguyên trạng lớp học, pose, bóng, màu camera/mask và sorting bàn ghế.
- Kiểm tra: build C# 0 lỗi; 4 ca NUnit gọi trực tiếp trong Unity đạt (MCP Test Runner timeout trước khi khởi chạy); Play production từ đầu tới combat đạt, hủy trước/sau mode swap và replay đạt. Console 0 lỗi; ảnh và kết quả tại `Temp/FallingRoomQA`. Chưa chơi lại toàn bộ trận hoặc hậu combat trong lượt này.

### QA Scene150 — 2026-08-29

- `Temp/Day4Timor/tests_4_pass.xml`: **4/4pass**, gồm production Crowd→fade→Scene150, direct references/content, production search/silence/shadow→Timor combat/cancel, và cancel trước/sau swap rồi replay. Lượt3/3 trước chỉnh sắc bóng cũng được giữ riêng.
- Đã xem ảnh GameView1920×1080: `150-alone.png`, `150-timor-reply.png`, `150-shadow-early.png`, `150-shadow-growing.png`, `150-shadow-swap.png`, `150-timor-combat.png`. Bóng được hạ sắc tím để tối hơn nền, không đọc như hình chiếu sáng. Chụp thoại đang typewriter; content test kiểm toàn bộ câu <=42ký tự.
- Bài test qua domain reload dùng coroutine runtime riêng; lượt đầu có lỗi harness đã sửa, không đổi production timing để làm test qua. Bộ lọc group+test ban đầu chọn0case không được tính là kiểm tra.
- Scene140 destination assertion giờ dừng ở độc thoại đầu Scene150 và cancel; không chờ cả trận cuối kết thúc. Test Crowd sử dụng debug effects để đi đủ flow, không chứng minh cân bằng thắng bằng chuột.
- Không thay Bootstrap, Teacher importer/catalog, scene140 nội dung, audio hoặc combat runtime dùng chung. Chưa build executable; kết quả trận cuối vẫn cần Xuân duyệt.

## Công cụ và kiểm tra

`Day4CrowdSetupTool.AuthorActive()` chỉ chạy trên Scene140 sạch/EditMode; author assets riêng Crowd, thêm một catalog entry và create-only Scene150. Không author Scene130/120/80 hoặc importer Teacher.

`CombatEnemyRuntimeTests` thêm mốc TIME qua ngưỡng, sharedHP/gate1HP/Heal không hồi phase/restart, và forced-control ownership/cleanup. `Day4CrowdTests` kiểm tra scene data; Play fall/cancel, pull/cancel, Defeat/Retry double-click, pha thật/thoại thật/Victory/hỗ trợ/hop/load buổi tối. Harness story dùng debug Attack/Heal để kiểm tra progression, không được coi là QA cân bằng bằng chuột của người chơi.

Kết quả và ảnh cuối lượt nằm dưới đây. Không build executable.

### Kiểm tra ghi nhận

- `Temp/Day4Crowd/tests_62_pass_mcp_warning.xml`: 62/63pass; lỗi duy nhất là cảnh báo timeout job của MCP tự phát trong Day4Morning, không phải assertion gameplay.
- Rerun cả3Day4Crowd + actual Day4Morning: **4/4pass**, `Temp/Day4Crowd/tests_final_4_pass.xml`, kết thúc2026-08-28 18:10:05Z. Cộng các lượt, đủ63test liên quan đã có kết quảpass.
- Lifecycle test bắt9dice thật qua cursor overlap, không sửa symbol/damage; tối đa3dice và2Attack/batch, Retry double-click/cancel sạch. Test full-story dùng debug damage để chạm gate và hoàn tất sau thoại; không phải bằng chứng cân bằng thắng bằng chuột.
- Ảnh thật1920×1080: classroom-standing/fallen, crowd-portrait, corner-grip, hands-and-volley, bianca-real-voice, bianca-beside-fallen-audere, both-standing, evening-arrival.
- QA aspect phát hiện UI cũ cắt lời bên phải ở4:3; thêm **CanvasScaler Expand chỉ trên GameplayUIRoot Scene140**, giữ prefabs/scene khác nguyên. Giữ cả ảnh trước/sau trong Temp/Day4Crowd.
- Đã xem ảnh sau sửa ở4:3 vàultrawide: portrait, board và lời thoại nằm trong khung. `combat-4x3-fit.png` chụp1440×1080; `combat-ultrawide-fit.png` là ảnh rộng. Rerun content sau sửa UI: **1/1pass**, `tests_ui_content_pass.xml`.
- Audit cuối: Scene140/150 saved sạch, startup=true, missingScript=0/brokenPrefab=0; PlayOFF/compileOFF, không test/probe đang chạy, Console0error/warning. GameView trở về preset ban đầu.
- Bootstrap được trả lại đúng thay đổi chưa lưu của Xuân: firstScene=Scene120, vẫn dirty=true có chủ ý, không ghi vào scene gốc. Hai snapshot trước/sau trong Temp/Day4Crowd so sánh không có khác biệt. Importer/portrait Teacher không sửa.
- Không build executable hoặc nghe loa thật. Cần Xuân duyệt cảm giác độ khó với thao tác chuột.

### Polish tay, nền và cú ngã — 2026-08-29 (lịch sử trước bản tay đuổi)

- `Day4CrowdSetupTool.PolishActive()` là lệnh scoped, không dựng lại cả scene. Full author cũng gọi nó ở cuối để không mất phần polish khi tạo mới. Giữ20HP/90TIME, mốc45TIME, dice/music và hậu combat.
- `OscillatingHandWallMove`: 8tay mỗi phía trên/dưới, warning0.8s, chu kỳ2.3s, duration7.3s. Hai hàng lệch độ sâu theo cùng sóng chạy ngang; hành lang giữa uốn theo nhưng không khép kín. Cả16 tay dùng pool/lease; pause giữ vị trí, cancel trả toàn bộ.
- `ConvergingHandsMove`: 3palms khép lại sau warning0.9s/close0.45s, giữ2.4s rồi mở0.55s, có recovery. Khi sát góc, tâm giữ lùi vào tối đa64unit để đủ chỗ cho ba lòng bàn tay. Các tay khác đâm lần lượt từ bốn phía cách0.34s, warning0.3s. Giữ/pull có protection; trả các tay đâm trước khi nhả input, không tạo hit bắt buộc. Catch dice vẫn giữ luật cũ.
- `DriftingSpriteField` là enemy mechanic module với direct scene reference, không kiểm tra enemyID. Canvas camera order-30 sau board, RawImage dùng đúng `Khong_Co_Tieu_e113_20260829003629.png`. Shader có hai lớp mật độ/tốc độ khác nhau, co/xiên/uốn UV. Material được clone theo session, clock dừng khi pause, phase0opacity0.55→phase1opacity0.18; Shutdown/disable hủy clone và ẩn nền.
- `storyUsesPuzzleViewportMask=true`; mask hiện ở Story, tắt ở Combat. Không thay shared fullscreen profile hay sorting actor/shadow.
- Thêm `052_TheFloorFirst`: “A… đau.” / “Đồ rơi hết rồi…” / “Tớ nhặt lại là được.”; chờ0.65s rồi “Sao tự nhiên im thế…” / “…Có ai vừa cười à?” / “Đừng nhìn tớ lúc này…”. Portrait chuyển Tired→Scared trong THOUGHT, không xác nhận lớp thực sự cười Audere.
- `Temp/Day4CrowdPolish/tests_10_pass.xml`: **10/10pass**, gồm production flow, Retry/cancel, wave geometry/pause,5mốc clasp và background lifecycle. Lượt đầu9/10 có test cancel pose bị frame đầu dài; harness đã kéo dài riêng pose runtime và ổn định frame. Không sửa thời gian pose production.
- Shader/content sau fade gốc: **1/1pass**, `tests_shader_content_pass.xml`. Ảnh production thật16:9,4:3,ultrawide nằm trong cùng thư mục; probe tự cancel, trả GameView preset, unregister và stop Play (`final_probe_done.txt`). Các ảnh `Move_*` là harness cô lập nên có nhãn board mặc định; ảnh `production-*` dùng encounter thật.
- Sau bổ sung inset khi giữ sát góc, rerun **5/5clasp cases pass**, gồm trường hợp cursor sát cạnh và không chống kéo bằng mouse (`tests_clasp_edge_5_pass.xml`). Xem thêm `final-inset-clasp.png`; probe đã tự nhả control và unregister (`inset_probe_done.txt`). Audit cuối Scene140 saved sạch/startup=true, 0missing/broken, PlayOFF/compileOFF/testinactive, Console0error/warning. Không sửa Bootstrap hoặc Teacher importer trong lượt polish này.
- QA visual có debug Heal để giữ phase trong lúc đổi aspect, không chứng minh độ khó thắng bằng chuột. Chưa build executable.
