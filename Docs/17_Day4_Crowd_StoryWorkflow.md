# Ngày4 — lớp học và Đám đông

## Ranh giới nội dung

**Design Intent của Xuân, đã author để duyệt:** Audere tự mang đồ lên lớp sau buổi sáng không có Timor. Cú ngã kéo lại nỗi sợ bị nhìn và bị cười; lời Đám đông là diễn giải trong suy nghĩ Audere, không xác nhận bạn học thực sự độc ác. Timor trở lại từ nỗi sợ mất quyền bảo vệ. Bianca hỏi một điều cụ thể về cơn đau; Audere vẫn sợ nhưng nghe được lời thật và cuối cùng xin cả lớp giúp.

**Unresolved:** phản ứng tiếp theo của cả lớp và kết quả trận Timor cuối. Phần mở Scene150 đã author theo yêu cầu mới bên dưới; không kết luận Audere đã hết lo âu.

## Flow và staging

`140_D4_Classroom/D4_CLASSROOM_CROWD`: fade-in → độc thoại đã mang được đồ → Audere nằm ngang trên tile → đau/lo đồ rơi/định nhặt lại → khoảng lặng → ngờ ngợ có tiếng cười → shared Dreamy Disorientation → Crowd combat → neutral cover → về lớp với Bianca bên phải → hỏi chỗ đau/xin phép vịn → Bianca nghiêng hỗ trợ, Audere đứng lên → cả hai hop cùng một lần → đối thoại ngắn → “Mọi người… giúp tớ được không?” → fade1.15s → `150_D4_Home_Evening`.

Tám desk tiles cách nhau hai ô lưới (pitch0.20 sau lượt fit mask, trước0.25); mỗi bàn `ban.aseprite` có chân/đáy giữa tile. Audere và Bianca dùng foot midpoint, không dùng tâm shadow để căn tile. Bàn hàng trước order8, giữa5, sau2; actor5/shadow4 trên Player. Anchor nằm trong scene, không lookup scene ở runtime. `CharacterPoseStep` giữ shadow ở anchor sàn khi xoay; cancel phục hồi pose ban đầu, replay có bước normalization dưới cover. Hai cặp parallel branches dùng StoryEvent trực tiếp: hỗ trợ đứng dậy và hop.

### Thu gọn stage trong PuzzleViewportMask — 2026-08-29

- Theo yêu cầu Xuân, tile/bàn và khoảng cách lưới còn80%, quanh midpoint hai tile nhân vật. Tile world scale0.25→0.20, bàn rộng0.215→0.172. Giữ nguyên scale actor, màu/layer của tile/bàn/shadow và nhịp động tác.
- Dịch actor và các anchor đứng/ngã/đỡ/shadow cùng delta của tile tương ứng. Audere/Bianca vẫn đứng giữa hai tile cạnh nhau. Cụm scenery còn dư ít nhất0.06 world unit với cả bốn mép mask; không đổi camera hoặc mask để che lỗi.
- Scoped menu `Audere/Story/Fit Active Day4 Classroom Stage Inside Mask` / `Day4CrowdSetupTool.FitStageActive()`. Chỉ sửa scenery/actor positions/anchors của Scene140; repeat không co thêm. Full author gọi ở cuối để giữ kích thước mới khi dựng lại.
- `Temp/Day4StageFit/tests_2_pass.xml`:2/2 content + production flow pass; kiểm bounds trong mask, chân actor sau ngã/đỡ/hop, rồi fade sang150. Đã xem ảnh Play đứng/ngã/hai nhân vật tại1920×1080 trong cùng thư mục. Không sửa combat, dialogue hoặc Scene150.

## Combat

- Encounter `Data/Combat/Crowd/CombatEncounter_D4_CROWD.asset`: **20sharedHP,90TIME**,3dice/batch,max2Attack; shared dice constants không đổi.
- Non-Timor combat music dùng slot Music_Combat hiện có. SceneMusicSpace nằm dưới presentation lớp học, nên nhả duck khi vào Combat; không đổi AudioService/Catalog/SFX.
- `SharedHealthPlayerTime` là policy dùng lại được: HP chung, transition khi TIME/maximum <= threshold. Phase đầu threshold0.5, phase cuối kết thúcHP0. Không so enemyHP với50%. Heal không quay ngược phase. HP tối thiểu1 trước mốc thoại; cuối phase giữ1 đến hết cue lời thật. Không passiveHP decay.
- Phase1 loop: HandWaves → ClaspAndStab → ShiftingPalms → RushingVoices. Theo yêu cầu mới của Xuân, đã bỏ đòn kéo người chơi ra góc. Ba bàn tay chụm giữ trong ClaspAndStab vẫn giữ; protection và grace sau nhả không đổi.
- Palms có2tay, warning0.65s, đâm/xoay, bắn3loạt cách0.35s; vòng có khoảng trống hai viên. Ghép `ShiftingBattleBoxMove` thay Width/X; Frame/Height/Y giữ nguyên.
- Khi TIME<=45: hủy move cũ, dọn hazards/dice theo phase lifecycle, Bianca nói “Audere, cậu có bị đau không?”. Phase2 không kéo và không co board; tay chậm1.1s/1tay, warning1.2s, ít volley và đạn thường1.1s/nhịp so0.35s trước đó.
- Enemy actor dùng IMG_1054.png, portrait ID8CrowdDistorted dùng Crowd.png, tay dùng IMG_1058.png. Không đổi importer PNG/Teacher catalog entries cũ.
- `UIWrithingHand` uốn shaft bằng UV, giữ lòng bàn tay/ngón cố định. Tay được tăng gấp2: visual108×480, palm hitbox46×54. Gốc fade từ UVy0.18→0.42 để không lộ mép ảnh; shaft mờ là presentation, chỉ palm gây hit. Sprite UV rect nằm trong material riêng; shader hỗ trợ UI clipping/stencil. Bullet pool/session/phase/lease và grouped SFX giữ hệ dùng chung.
- Các đòn tay trả lease khi đổi beat/cancel. Các viên từ lòng bàn tay tiếp tục bay đến khi ra board hoặc bị clear/phase/session invalidation. Forced control của đòn chụm giữ là owner-scoped và được clear cả phase/result/reset.

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
- Combat Root được sao chép qua Unity API từ setup Scene40 với visual Timor đã author; Combat Systems là sibling dưới SYSTEMS, không nằm trong presentation root. Encounter riêng `Data/Combat/TimorReturn/CombatEncounter_D4_TIMOR_RETURN.asset`, nhạc Music_TimorCombat/bossfightfull. Giữ moves/dice/36HP/66TIME và luật **Defeat-only** của trận cũ trong lúc chờ Xuân chốt kết quả; không tự quyết định kết thúc câu chuyện. Đã bỏ các cue và defeat dialogue riêng Ngày1 khỏi bản Day4; assets Ngày1 giữ nguyên. Chưa author hậu combat hoặc ending.
- Author riêng `Day4TimorEveningSetupTool.AuthorActive()` chỉ chạy trên Scene150 sạch/EditMode. Không rerun broad Day3/School/Teacher builder. Khi chốt lại balance/outcome, cập nhật author cùng asset để rerun không phục hồi luật cũ.

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

### Polish tay, nền và cú ngã — 2026-08-29

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
