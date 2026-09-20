# Các câu hỏi ở checkpoint cũ và nguồn tham chiếu

[Mục lục nguồn](../../09_Day1_ProductionStoryWorkflow.md) · [Bản đồ docs](../../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
## 8. Điểm chưa triển khai

- **Implemented prototype:** hand-off Story → `Khoảng Lặng` prototype một phase `6 HP` → Story
  sau câu Timor.
- **Design Intent:** tên `Khoảng Lặng` và placement ở D1 Classroom phục vụ prototype hiện tại.
- **Implemented Design Intent:** D1 dùng một `CombatTutorialData` và enemy tutorial một phase riêng
  (`99 HP`, `120 TIME`). Opening batch luôn có đúng Attack, Shield, Heal; card đầu preview cả ba,
  nói gọn luật bắt/gieo/TIME, sau đó mới spotlight Stun Zone và giới thiệu từng dice khi người chơi
  bắt nó. Instruction dùng một dòng cùng font Scene 20 và chỉ đóng bằng click trái/phải. Click đóng
  card bị consume; trong toàn bộ dialogue/highlight, TIME, dice, projectile và enemy move đều pause.
  Giữa các cue TIME chạy `0.25x`, damage có safety floor `1 s`, nên phần học không thể vô tình Defeat
  hoặc hạ boss thật. Sau câu kết của Timor, session tutorial bị shutdown và một session
  `Enemy_KhoangLang` mới được tạo với `45 s`, một phase `6 HP`, dice batch và moveset luân phiên
  Aimed Fan → converging Side Sweep → Rain. Không hiện phase marker.
- **Implemented Design Intent:** đoạn kết tutorial được thay bằng nhịp Audere giữ câu
  `Tớ muốn thử`. Trong combat thật, Khoảng Lặng dùng DialogueUI chuẩn tự chạy ở Aimed Fan/Side
  Sweep mà không khóa input; Audere luôn ở trái và Khoảng Lặng ở phải. Sau Side Sweep, Heart
  wobble nhẹ rồi dialogue Audere–Timor pause combat-local; ở `2 HP`, text lo lắng phủ dày background
  với ghost-smear và wobble mềm tới khi session dọn. Chỉ đòn lethal sớm mới bị
  giữ ở `1 HP` tới khi dialogue bắt buộc resolve, nên encounter vẫn là một phase.
- **Design Intent:** exact Khoảng Lặng wording và portrait Audere tái sử dụng là content
  `PLACEHOLDER` đã được production-wire theo yêu cầu của Xuân, chưa phải voice/portrait canon.
- **Unresolved:** ontology, final voice/dialogue/art/ý nghĩa của Khoảng Lặng, final moveset,
  balance và branch outcome ngoài Victory/Retry hiện tại.
- **Implemented Design Intent:** Victory tiếp tục bằng câu trả lời nhỏ với Bianca, registration
  overlay click-to-dismiss, ba hop rời lớp, dialogue Audere–Timor, School Bell và neutral fade sang
  scene build-listed `40_Evening`.
- **Implemented Design Intent:** `40_Evening` có `D1_HOME_NIGHT_MESSAGE`, Night Tile ở tâm camera
  và actor staging đồng tỷ lệ Scene 30 (`Story Root 0.25`, Audere `1.5`, body/shadow `Player 5/4`),
  message sound, Bianca text, Audere startle, Timor exchange và encounter scripted-defeat 11 nhịp.
  Defeat là result duy nhất; hazard freeze/fade và hậu thoại resolve trước neutral fade. Trong
  phòng, choice UI ba nhánh dẫn tới lights out và `Ngày 1 - Kết thúc`; không có Retry.
- **Unresolved:** art thật của phiếu đăng ký, room art buổi tối, combat ontology và kết quả
  narrative dài hạn của ba lựa chọn.
- **Unresolved:** StoryState, conditional branching, save/checkpoint và resume giữa event.
- **Unresolved:** Timor có được người khác nhìn/nghe thấy hay không.
- **Unresolved:** tên riêng/portrait chính thức của Teacher; portrait/art chính thức của Bianca
  và các NPC lớp học khác.
- **Unresolved:** cách gọi nhất quán cho washroom action nếu asset/thoại lại lệch giữa rửa mặt
  và đánh răng trong tương lai. Production hiện tại đã dùng “đánh răng”.

## 9. File tham chiếu chính

| Nội dung | File |
| --- | --- |
| Story runner và step catalog | `Docs/07_StorySystem_SceneFirst.md` |
| Puzzle scene-first và hand-off | `Docs/04_PuzzleGameplay_SteptileArchitecture.md` |
| Dialogue lifecycle/data | `Docs/05_DialogueSystem.md` |
| Character expression/motion contract | `Docs/10_CharacterExpressionAndMotion.md` |
| Bootstrap và scene loading | `Docs/02_Bootstrap.md` |
| Voice/canon ledger | `.agents/skills/audere-dialogue-voice/references/` |
| Production scene sáng Day 1 | `Assets/_Audere/Scenes/20_D1_Home_Morning.unity` |
| Production scene lớp học | `Assets/_Audere/Scenes/30_Classroom.unity` |
| Production scene buổi tối | `Assets/_Audere/Scenes/40_Evening.unity` |

Khi production scene và tài liệu này khác nhau, kiểm tra `DialogueData` và Hierarchy hiện tại
trước; sau khi QA, cập nhật lại doc thay vì đoán flow từ tên file.
<!-- END PRESERVED SOURCE -->
