# Audere Dialogue System

Tài liệu này mô tả hệ dialogue data-driven hiện tại, cách gán thoại vào map và quy tắc UI giữa các scene.

## 1. Cấu trúc UI

Dialogue không nằm trên Player và không được sinh bằng runtime code. Toàn bộ layout nằm trong prefab, lấy trực tiếp từ mẫu `Canvas/Left` đã setup trong scene:

```text
GameplayUIRoot                         DontDestroyOnLoad
└── DialogueUI
    ├── Left                          nested Left.prefab
    │   └── DialogueBubble            shared DialogueBubble.prefab
    │       ├── Dialogue Text (TMP)
    │       └── Character Name (TMP)
    └── Right                         nested Right.prefab
        └── DialogueBubble            shared DialogueBubble.prefab
            ├── Dialogue Text (TMP)
            └── Character Name (TMP)
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
- `Canvas/Path Piece Hand UI` vẫn là UI riêng của scene vì có reference gameplay theo scene.

## 2. Character constant và catalog

Nhân vật được chọn bằng dropdown `DialogueCharacterId`, hiện có:

```text
None, Audere, Timor
```

Tên hiển thị và portrait không nằm trong từng đoạn thoại. Chúng được gắn một lần tại:

```text
Assets/_Audere/Data/Dialogue/DialogueCharacterCatalog.asset
```

Khi tạo `DialogueData`, designer chỉ cần chọn `Left Character` và `Right Character`; runtime tự lấy `Display Name` và `Portrait` từ catalog.

Hiện `Audere` đã có portrait. `Timor` đã có constant và tên nhưng chưa có portrait vì thư mục `Assets/_Audere/AssetGame/Timor` chưa chứa ảnh; gán sprite vào catalog khi art sẵn sàng.

Khi thêm nhân vật mới:

1. Thêm một giá trị có số ổn định vào `DialogueCharacterId`.
2. Thêm entry cùng character vào `DialogueCharacterCatalog.asset`.
3. Gán `Display Name` và `Portrait` tại entry đó.

## 3. DialogueData

Tạo asset bằng:

```text
Create > Audere > Dialogue > Dialogue Data
```

Mỗi asset gồm:

- `Dialogue Id`: id ổn định dùng cho `Trigger Once`;
- `Left Character` và `Right Character`: dropdown constant nhân vật;
- `Lines`: danh sách theo thứ tự, mỗi dòng chỉ chọn `Speaker` (`Left` hoặc `Right`) và nhập `Text`.

Sample:

```text
Assets/_Audere/Data/Dialogue/Dialogue_Sample.asset
```

Như vậy tên/ảnh không bị lặp trong từng line và một character có thể đổi portrait/tên tại một nơi duy nhất.

## 4. Runtime flow

```text
Player bước vào cell
→ BoardManager.NotifyPlayerEntered
→ BoardTile chuyển đầy đủ PuzzleTileData cho DialogueTileBehaviour
→ DialogueTileBehaviour gọi GameplayUIRoot.Dialogue.Play(data)
→ DialogueController hiển thị lần lượt Left/Right
```

Trong khi phát thoại:

- gameplay pause bằng `Time.timeScale = 0`;
- typewriter dùng `Time.unscaledDeltaTime` nên vẫn chạy;
- hai portrait hiện cùng lúc bằng fade ngắn, không đổi kích thước;
- người không nói được tint tối nhưng giữ nguyên scale để không lộ viền;
- mỗi lượt chỉ có bubble của người nói: bubble cũ thu/fade xuống ngắn, bubble mới bắt đầu sát đầu nhân vật rồi pop + trượt lên nhẹ; sau đó text mới chạy typewriter;
- click, `Space` hoặc `Return` hoàn tất dòng/đi tiếp;
- `Escape` đóng toàn bộ đoạn thoại;
- khi đóng, time scale trước đó được khôi phục đúng giá trị.

`Trigger Once` được nhớ theo `Dialogue Id` trong lifetime của `GameplayUIRoot`, nên giữ qua các gameplay scene và reset khi quay lại Main Menu.

### Thông số animation hiện tại

| Thành phần | Giá trị | Nơi chỉnh |
| --- | ---: | --- |
| Character fade-in | `0.24 s` | `DialogueController.characterEntranceDuration` |
| Độ sáng người không nói | `0.34`, alpha `1`, scale `1` | `Left.prefab` và `Right.prefab` |
| Delay trước khi bubble xuất hiện | `0.06 s` | `DialogueController.bubbleDelay` |
| Bubble pop-in | `0.20 s` | `DialogueBubble.prefab` |
| Bubble start scale | `0.78` | `DialogueBubble.prefab` |
| Bubble overshoot scale | `1.06` | `DialogueBubble.prefab` |
| Bubble trượt lên | `22 px` | `DialogueBubble.prefab` |
| Bubble pop-out | `0.09 s` | `DialogueBubble.prefab` |
| Tốc độ typewriter | `42 ký tự/s` | `DialogueController.charactersPerSecond` |

Các animation dùng `Time.unscaledDeltaTime`. Portrait không được scale khi đổi speaker; chỉ `DialogueBubble` được scale để tạo hiệu ứng pop.

## 5. Gán thoại vào map

Mở:

```text
Audere > Puzzle > Map Editor
```

Workflow:

1. Chọn `PuzzleData`.
2. Chọn Tile Type `Dialogue` và paint/click cell.
3. Tại `Selected Cell`, gán `Dialogue Data`.
4. Chọn hoặc bỏ `Trigger Once`.
5. `Save Data`, sau đó `Apply to Scene` hoặc `Apply & Play`.

Map Editor validation không cho save nếu Dialogue cell chưa có data. Mỗi cell giữ reference riêng, nên cùng một `Dialogue.prefab` có thể phát nhiều đoạn thoại khác nhau.

Sample map hiện đặt Dialogue tile tại `(0,0)`:

```text
Assets/_Audere/Data/Puzzle/Puzzle_MVP_01.asset
```

## 6. Setup và QA

Menu editor:

```text
Audere > Dialogue > Setup From Scene Template
Audere > Dialogue > Preview Sample
```

Menu setup bootstrap các asset còn thiếu từ mẫu Left/Right và không ghi đè catalog, sample hoặc tile prefab đã tồn tại. `Preview Sample` vào Play Mode và phát sample ngay để kiểm tra nhịp animation. Không cần chạy setup mỗi lần sửa nội dung; thoại thường ngày chỉ sửa trên `DialogueData` và catalog.

### Script chịu trách nhiệm

| Script | Vai trò |
| --- | --- |
| `DialogueCharacterId.cs` | Constant nhân vật cho dropdown. |
| `DialogueCharacterCatalog.cs` | Resolve constant thành tên và portrait. |
| `DialogueData.cs` | Cặp nhân vật và thứ tự các line Left/Right. |
| `DialogueCharacterSlotView.cs` | Portrait, tint người nói/không nói và visibility của slot. |
| `DialogueBubbleView.cs` | Nội dung bubble, pop-in, rise và pop-out. |
| `DialogueController.cs` | Điều phối thứ tự character → bubble → typewriter, input và pause. |
| `GameplayUIRoot.cs` | Singleton UI persistent giữa gameplay scene. |
| `DialogueTileBehaviour.cs` | Phát data được gán cho cell khi Player bước vào. |

QA gần nhất ngày 2026-08-16:

- Unity Console: `0` error sau compile và Play Mode test;
- `Assembly-CSharp` và `Assembly-CSharp-Editor`: `0 warning`, `0 error`;
- scene có root độc lập `GameplayUIRoot/DialogueUI/Left|Right` đúng vị trí mẫu;
- sample mở được, text tiếng Việt + tên Audere hiển thị đúng, typewriter chạy khi `timeScale = 0`;
- `ForceClose` trả `IsPlaying = false` và `Time.timeScale = 1`;
- `DialogueTileBehaviour` tại cell `(0,0)` đã gọi sample thành công;
- warning còn lại là Timor chưa có portrait nguồn.
