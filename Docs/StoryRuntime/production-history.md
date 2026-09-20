# Các mốc tích hợp production được ghi nhận

[Mục lục nguồn](../07_StorySystem_SceneFirst.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
## 10. Production flow hiện tại

- `20_D1_Home_Morning/STORY` chỉ giữ `D1_HOME_MORNING` và `D1_TO_BUS_STOP`; các `TEST_*` cũ đã được
  loại khỏi production scene.
- Bus-stop goal được giữ làm anchor trong lúc path tile và Puzzle UI biến mất. Sau một nhịp
  yên, SFX xe bus và hai đoạn thoại kết beat trước khi fade sang lớp học.
- Vì Bus Stop là puzzle cuối scene, shared Player đứng nguyên trên Goal cho tới lúc fade.
  Không dùng `MoveActorStep` để diễn tả pose/thả lỏng vì step đó dịch root gameplay; biểu cảm
  về sau phải nằm ở Animator hoặc visual child mà không đổi vị trí grid.
- `30_Classroom` author trực tiếp actor, tile, staging target và
  `D1_CLASSROOM_ANNOUNCEMENT` nối sang `D1_CLASSROOM_RECESS_BIANCA`. Interest tile xuất hiện, Audere nhích tới rồi quay về chỗ cũ
  theo `MoveActorStep`/`SetActiveStep`, không hardcode trong dialogue controller.
- Event giờ nghỉ dùng `CharacterMotionStep` cho Bianca nhảy từng tile và cho Audere giật mình
  tại chỗ rồi quay sang phải. Sau thoại Timor, event dùng
  `FullscreenWorldModeTransitionStep → CombatStep → WorldModeStep`: vào Combat bằng shared
  profile `Dreamy Disorientation`, rồi trở lại Story bằng fade đen và khôi phục nguyên bố cục.
- Combat prototype chỉ xác nhận contract kỹ thuật. Enemy, ý nghĩa narrative và kết quả canon
  vẫn `Unresolved`; không suy chúng từ art/name placeholder.
- Classroom staging trình bày ngang: Audere đứng bên trái, Teacher đứng bên phải trên cùng
  baseline và cả cặp được cân giữa khung hình. Hai Student placeholder/tile đã được bỏ khỏi
  production scene để beat chỉ tập trung vào Audere và Teacher. Teacher là prefab riêng tại
  `Assets/_Audere/Prefabs/Story/Characters/Teacher.prefab`; sprite hiện tại vẫn là placeholder
  và được thay trực tiếp trong prefab khi có art chính thức.
- Các object có hậu tố `PLACEHOLDER` là presentation tạm, không tự xác nhận thiết kế canon.
- `40_Evening` thay placeholder bằng `D1_HOME_NIGHT_MESSAGE`. Audere và Night Tile được author
  dưới `WORLD/Story Root`, cùng trục X; Story Root dùng cùng staging space với Scene 30 (`0.25`),
  Audere dùng scale `1.5`, body/shadow `Player 5/4`, còn tâm Night Tile khớp tâm camera.
  `PuzzleViewportMask` giữ transform prefab như Scene 20/30. Event dùng direct references theo thứ tự:
  dialogue → `Message_Arrive` → bật `dauchamthan` → `VerticalInPlace` startle → Audere nhận ra
  Bianca → ẩn alert → Bianca message → Timor biến một câu trả lời thành chuỗi hậu quả chắc chắn →
  Audere mất điểm tựa và Timor đóng lựa chọn → `Dreamy Disorientation` → Defeat-only
  CombatStep → hazard freeze/fade + hậu thoại Defeat → neutral `WorldModeStep` về Story →
  ba lựa chọn tin nhắn → nested branch → lights out → `Ngày 1 - Kết thúc`. Alert được normalize ẩn trước FadeIn và authoring
  tool bảo toàn chính scene object/transform do designer đặt.
  Scene cũng giữ một root prefab `GameplayUIRoot` như Scene 20/30; Bootstrap không sở hữu UI này.
- Encounter đêm không dùng `SetActiveStep` để giả lifecycle. `CombatController.Play()` vẫn là nơi
  claim input; `CombatStep` map Victory/Special thành Fail và Defeat thành Complete, nên không có
  Retry và event chỉ tiếp tục đúng một lần sau scripted defeat.
- Choice UI của Scene 40 là root Screen Space Overlay `NIGHT MESSAGE UI`, reference resolution
  `1920×1080`, sorting order `1300`. Text đặt ở vùng thấp giống khu path-piece; idle nhỏ/mờ,
  pointer hover thêm `> <` và trả lại scale/alpha đầy đủ. `StoryChoiceBranchStep` giữ ba branch
  thành nested `StoryEvent`, không đưa switch narrative vào `StoryEvent` runner.
- SFX bus/classroom hiện là clip placeholder được map qua `AudioCatalog`; có thể thay clip tại
  catalog mà không sửa StoryEvent.
- `50_D2_Home_Morning` sao chép presentation nhà/bến xe nhưng sở hữu StoryEvent
  `D2_HOME_MORNING → D2_TO_BUS_STOP`, DialogueData Day 2 và ba board scene-authored riêng.
  Washroom reveal đặt `25_OneUseTileTutorial` trước PuzzleStep; tile đỏ dùng traversal-rule
  component chung, không có nhánh theo scene hoặc puzzle ID. Scene kết thúc tại bến xe vì
  destination Day 2 tiếp theo còn `Unresolved` tại checkpoint này; production continuation hiện xem Docs13–15.

## Day3 extension

`90_D2_Home_Awakening → 100_D3_Home_Morning → 110_D3_School_Board → 120_D3_School_Teacher` giữ StoryDirector/Event/direct-step contract. Scene110 dùng ParallelStoryStep để vừa hop vừa thoại, `ChalkDrawingStep` chờ modal drawing completion, rồi `FullscreenPresentationStep` song song `AutoDialogueStep` cho đoạn chóng mặt. Không có narrative switch trong manager, không dùng SetActiveStep để thay combat lifecycle. Chi tiết scene, lời thoại, ownership và QA: [15_Day3_BoardTeacher_StoryWorkflow](../15_Day3_BoardTeacher_StoryWorkflow.md).
<!-- END PRESERVED SOURCE -->
