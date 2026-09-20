# UI, character catalog và vị trí portrait

[Mục lục nguồn](../05_DialogueSystem.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
# Audere Dialogue System

Tài liệu này mô tả hệ dialogue data-driven hiện tại, cách gán thoại vào map và quy tắc UI giữa các scene.

## 1. Cấu trúc UI

Dialogue không nằm trên Player và không được sinh bằng runtime code. Toàn bộ layout nằm trong prefab, lấy trực tiếp từ mẫu `Canvas/Left` đã setup trong scene:

```text
GameplayUIRoot                         Canvas + DontDestroyOnLoad
├── PuzzleUI
│   └── Path Piece Hand UI
│       └── Cards
├── DialogueUI
    ├── Left                          nested Left.prefab
    │   └── DialogueBubble            shared DialogueBubble.prefab
    │       ├── Dialogue Text (TMP)
    │       └── Character Name (TMP)
    └── Right                         nested Right.prefab
        └── DialogueBubble            shared DialogueBubble.prefab
            ├── Dialogue Text (TMP)
            └── Character Name (TMP)
├── CombatTutorialUI                  combat instruction; không block raycast
└── CombatRetryUI                     overlay Canvas order 1200; sibling cuối
```

Các prefab chính:

```text
Assets/_Audere/Prefabs/UI/GameplayUIRoot.prefab
Assets/_Audere/Prefabs/UI/Dialogue/Left.prefab
Assets/_Audere/Prefabs/UI/Dialogue/Right.prefab
Assets/_Audere/Prefabs/UI/Dialogue/DialogueBubble.prefab
```

`GameplayUIRoot` là singleton persistent:

- instance đầu tiên được giữ bằng `DontDestroyOnLoad` qua các gameplay scene;
- bản trùng bị hủy nếu scene gameplay khác cũng chứa prefab này;
- root tự đóng thoại, khôi phục `Time.timeScale` và tự hủy khi vào `10_MainMenu`;
- Main Menu tiếp tục dùng UI riêng;
- gameplay scene không tạo Canvas HUD thứ hai; `PuzzleUI` và `DialogueUI` cùng nằm trong root Canvas;
- `PuzzleManager` và `PathPlacementController` bind lại `Path Piece Hand UI`/Canvas persistent khi level được load.

## 2. Character constant và catalog

Nhân vật được chọn bằng dropdown `DialogueCharacterId`, hiện có:

```text
None, Audere, Timor, Teacher, Bianca, KhoangLang, BiancaDistorted, TeacherDistorted
```

Tên hiển thị và portrait mặc định được gắn một lần tại:

```text
Assets/_Audere/Data/Dialogue/DialogueCharacterCatalog.asset
```

Khi tạo `DialogueData`, designer chọn `Left Character` và `Right Character`; runtime tự lấy
`Display Name` và portrait mặc định từ catalog. Một scene cần biểu cảm riêng có thể author
`Left/Right Portrait Override`; từng line cũng có `Portrait Override` để đổi nét mặt từ line đó
trở đi. Portrait Override chỉ thay ảnh. Optional Character Override trên từng line đổi identity
của speaker, rồi lấy portrait mặc định của identity mới trước khi áp dụng Portrait Override.

### Quy ước vị trí Audere

- Trong mọi scene và mọi đoạn thoại có Audere, **Audere luôn ở slot trái**.
- Slot phải luôn là đối tượng đang nói chuyện với Audere: Timor, Teacher, Bianca, Khoảng Lặng…
- `Speaker` vẫn chỉ vị trí hiển thị: lời Audere là `Left`, lời của đối tượng là `Right`.
- `DialogueController` tự mirror các asset legacy đang author Audere ở phải để presentation không
  bị lệch, nhưng asset mới và tool authoring phải ghi đúng contract trái/phải ngay từ đầu.

Teacher và Bianca hiện có portrait PNG trong catalog. Tách rõ bốn lựa chọn authoring:

| Identity | ID ổn định | Portrait mặc định |
| --- | ---: | --- |
| Teacher | 3 | `Giáo viên/Co_giao.png → Co_giao_0` |
| Bianca | 4 | `Bianca/Bianca.png → Bianca_0` |
| BiancaDistorted | 6 | `Bianca/Bianca_Creepy.png → Bianca_Creepy_0` |
| TeacherDistorted | 7 | `Giáo viên/Co_giao_Creepy.png → Co_giao_Creepy_0` |

Đường dẫn ảnh tính từ `Assets/_Audere/AssetGame/`. Hai bản Distorted là **Design Intent**:
lời Audere nghe qua sự bóp méo của Timor, không xác nhận Bianca/cô giáo thật sự có ác ý.
Tên hiển thị vẫn là Bianca/Cô giáo. World actor và enemy prefab không dùng identity này để đổi art.
Timor nói trực tiếp vẫn chọn Timor, không đổi thành bản Distorted của người khác.

`KhoangLang = 5` là stable technical ID cho hook combat. Catalog có display name
`Khoảng Lặng` và tạm tái dùng portrait Audere theo yêu cầu prototype. Tên/placement và portrait
placeholder là **Design Intent**; voice, canon dialogue, ý nghĩa tâm lý và art chính thức vẫn
**Unresolved**.

Voice hiện hành của `Teacher` được giữ tại
`.agents/skills/audere-dialogue-voice/references/characters/teacher.md`: ôn hòa, vui vẻ,
trưởng thành và tạo cảm giác chữa lành bằng cách giảm áp lực, đưa lựa chọn vừa sức; không dùng
ngôn ngữ trị liệu hoặc tự nói thẳng rằng cô đang “chữa lành”.

Khi thêm nhân vật mới:

1. Thêm một giá trị có số ổn định vào `DialogueCharacterId`.
2. Thêm entry cùng character vào `DialogueCharacterCatalog.asset`.
3. Gán `Display Name` và `Portrait` tại entry đó.

<!-- END PRESERVED SOURCE -->
