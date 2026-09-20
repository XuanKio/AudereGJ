# Audere — bản đồ tài liệu

**Điểm vào cho mọi task.** Mỗi tài liệu Markdown dưới 200 dòng; đọc đúng phần cần làm thay vì nạp toàn bộ docs.
Quy tắc chủ quản và cập nhật: [AGENTS](AGENTS.md). Cập nhật cấu trúc: 2026-09-20.

## 1. Chọn tài liệu theo câu hỏi

| Cần biết / cần sửa | Điểm vào | Trách nhiệm |
| --- | --- | --- |
| Câu chuyện muốn kể, quan hệ nhân vật, plot | [Story](Story/README.md) → [Plot](Story/plot.md) | Ý nghĩa câu chuyện; không chứa chi tiết API |
| Thứ tự cảnh, setup → payoff, nhịp nối | [Chronology](Story/chronology.md) | Chuỗi sự kiện và nguyên nhân chuyển cảnh |
| Boss đại diện điều gì; ending trả lời gì | [Bosses and ending](Story/bosses-and-ending.md) | Quan hệ giữa gameplay và narrative |
| Rút gọn thoại, giữ giọng và mục tiêu từng beat | [Dialogue revision](Story/dialogue-revision.md) | Quy tắc, phạm vi và bằng chứng sửa thoại |
| Code, prefab, scene, authoring hoặc QA | Bảng kỹ thuật / workflow bên dưới | Hợp đồng triển khai và các checkpoint đã ghi |

## 2. Hệ thống kỹ thuật

| Hệ thống | Tài liệu chủ quản |
| --- | --- |
| Tổng quan project, inventory, lịch sử quyết định | [Overview](00_ProjectOverview.md) |
| Cài project / chạy game / build | [Project Setup](01_ProjectSetup.md) |
| Bootstrap, services và SceneFlow | [Bootstrap](02_Bootstrap.md) |
| AudioId, SFX, BGM, quyền sở hữu nhạc | [Audio](03_AudioSystem.md) |
| StepTile, map, shared Player, hand-off, input | [Puzzle runtime](04_PuzzleGameplay_SteptileArchitecture.md) |
| DialogueData, UI, portrait, playback, lifecycle | [Dialogue runtime](05_DialogueSystem.md) |
| Battle Box, dice, TIME, enemy, tutorial, Retry | [Combat](06_CombatGameplay.md) |
| StoryDirector/Event/Step, hierarchy và tích hợp | [Story runtime](07_StorySystem_SceneFirst.md) |
| Palette, camera fallback, PuzzleViewportMask | [Visual palette](08_VisualPalette.md) |
| Actor, portrait, motion và transform ownership | [Character expression and motion](10_CharacterExpressionAndMotion.md) |
| Transition profile, mode swap, cancel/replay | [Fullscreen transitions](11_FullscreenWorldTransitions.md) |

## 3. Scene và workflow sản xuất

| Phạm vi | Tài liệu chủ quản |
| --- | --- |
| Day1: scene20 → scene30 → scene40 | [Day1 workflow](09_Day1_ProductionStoryWorkflow.md) |
| Day2: sáng ở nhà, scene50 | [Day2 morning](12_Day2_ProductionStoryWorkflow.md) |
| Day2: Bianca, supplies, co-op, scene60 | [Day2 school](13_Day2_School_StoryWorkflow.md) |
| Day2: tối → giấc mơ → tỉnh giấc, scene70–90 | [Day2 night / dream](14_Day2_NightDream_StoryWorkflow.md) |
| Day3: sáng → vẽ bảng → cô giáo, scene100–120 | [Day3 workflow](15_Day3_BoardTeacher_StoryWorkflow.md) |
| Day4: buổi sáng yên lặng, scene130 | [Day4 morning](16_Day4_QuietMorning_StoryWorkflow.md) |
| Day4: Đám đông → Timor / ending, scene140–150 | [Day4 crowd / ending](17_Day4_Crowd_StoryWorkflow.md) |

## 4. Puzzle layout và solver proof

| Phạm vi | Tài liệu / dữ liệu |
| --- | --- |
| Hai đường tới trạm xe Day1 / Day2 đã rút gọn | [ShortBus](Puzzles/ShortBus/README.md) |
| Một puzzle Audere–Bianca dùng chung | [Day2School](Puzzles/Day2School/README.md) |
| Đường đi giấc mơ Day2 | [Day2Dream](Puzzles/Day2Dream/README.md) |

## 5. Cách đọc bằng chứng

Các doc kỹ thuật/workflow chứa nhiều checkpoint khác ngày. Từ “hiện tại”, HP/TIME, số puzzle,
test pass hoặc “chưa triển khai” trong một checkpoint chỉ nói về mốc đó; không chứng minh trạng thái mới.
Một lần tách doc không đồng nghĩa đã chạy lại QA. Các trang `Overview/*` là snapshot 2026-08-23.

Ưu tiên `DialogueData` được production `StoryEvent` tham chiếu → Scene/Hierarchy → runtime/config → docs.
Thông số boss, tên file và hình art không tự xác lập canon. Dùng bốn nhãn **Established Canon**,
**Strongly Implied**, **Design Intent**, **Unresolved** như [quy tắc quản lý docs](AGENTS.md).

Đường dẫn `00`–`17` được giữ làm điểm vào. Các trang lớn đã tách theo chủ đề; heading cũ dẫn tới đúng
trang con. Khi sửa, cập nhật trang chủ quản và liên kết từ các nơi liên quan, tránh sao chép cùng một quy tắc.
