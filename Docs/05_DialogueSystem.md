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
None, Audere, Timor, Teacher, Bianca, KhoangLang
```

Tên hiển thị và portrait không nằm trong từng đoạn thoại. Chúng được gắn một lần tại:

```text
Assets/_Audere/Data/Dialogue/DialogueCharacterCatalog.asset
```

Khi tạo `DialogueData`, designer chỉ cần chọn `Left Character` và `Right Character`; runtime tự lấy `Display Name` và `Portrait` từ catalog.

Hiện `Audere` và `Timor` đã có portrait. `Teacher` đã có constant và display name `Cô giáo`,
nhưng portrait/art cụ thể vẫn là **Unresolved** nên catalog đang để trống thay vì tự canon hóa
một thiết kế placeholder.

`Bianca` đã có constant và display name để phục vụ production event giờ nghỉ. Portrait chính
thức vẫn là **Unresolved**; prefab màu hồng hiện tại chỉ là presentation placeholder.

`KhoangLang = 5` là stable technical ID cho hook combat. Catalog có display name
`Khoảng Lặng` nhưng portrait để trống. Tên/placement là **Design Intent**; voice, canon
dialogue, ý nghĩa tâm lý và art chính thức vẫn **Unresolved**.

Voice hiện hành của `Teacher` được giữ tại
`.agents/skills/audere-dialogue-voice/references/characters/teacher.md`: ôn hòa, vui vẻ,
trưởng thành và tạo cảm giác chữa lành bằng cách giảm áp lực, đưa lựa chọn vừa sức; không dùng
ngôn ngữ trị liệu hoặc tự nói thẳng rằng cô đang “chữa lành”.

Khi thêm nhân vật mới:

1. Thêm một giá trị có số ổn định vào `DialogueCharacterId`.
2. Thêm entry cùng character vào `DialogueCharacterCatalog.asset`.
3. Gán `Display Name` và `Portrait` tại entry đó.

## 3. DialogueData

Folder convention:

```text
Assets/_Audere/Data/Dialogue/
├── DialogueCharacterCatalog.asset
├── Day1/
│   ├── Home/
│   ├── BusStop/
│   └── Classroom/
└── Samples/
```

Production dialogue is grouped by day first, then location. Shared catalogs stay at the
Dialogue root; sample/debug content stays outside production day folders.

Tạo asset bằng:

```text
Create > Audere > Dialogue > Dialogue Data
```

Mỗi asset gồm:

- `Dialogue Id`: id ổn định dùng cho `Trigger Once`;
- `Left Character` và `Right Character`: dropdown constant nhân vật;
- `Lines`: danh sách theo thứ tự, mỗi dòng chỉ chọn `Speaker` (`Left` hoặc `Right`) và nhập `Text`.

### Quy tắc độ dài bubble

- Một `Line` là một nhịp thoại có thể đọc độc lập, không phải một đoạn văn chờ TMP tự wrap.
- Với prefab hiện tại, mục tiêu là tối đa `42` ký tự hiển thị, tính cả khoảng trắng.
- Câu dài hơn phải được tách theo ý hoàn chỉnh hoặc được kiểm tra trực quan ở target resolution.
- Không tách giữa câu chỉ để dòng sau bắt đầu bằng một mệnh đề viết thường nếu có thể viết lại
  thành hai câu tự nhiên.
- Nhịp rất ngắn như `Xin lỗi!` được tách riêng khi nó tạo pause hoặc đổi thái độ; không tách
  máy móc nếu chỉ làm tăng số lần click.
- QA phải preview line dài nhất ở cả bubble trái và phải, gồm tên nhân vật và dấu tiếng Việt.

Sample:

```text
Assets/_Audere/Data/Dialogue/Samples/Dialogue_Sample.asset
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

Playback có hai mode:

- `GlobalTimePause` là mặc định của các overload cũ và giữ hành vi `Time.timeScale = 0`.
- `CallerOwnedPause` dành cho combat. Dialogue chỉ claim input/display; caller dừng combat-local
  TIME, move và input. Phase-break đã clear dice/projectile trước; mid-phase giữ nguyên Heart,
  dice, projectile và remaining move cadence.

Combat tutorial D1 lưu character dialogue tại
`Assets/_Audere/Data/Dialogue/Day1/Classroom/Combat/Dialogue_D1_COMBAT_TUTORIAL_*.asset`.
Các asset này chỉ có Audere/Timor; Khoảng Lặng không nói. `CombatDialogueCue` giữ direct
reference tới asset và phát bằng `CallerOwnedPause`. Câu điều khiển chính xác không nằm trong
bubble mà hiện trên `CombatTutorialUI` sau khi character dialogue đóng, tránh bắt Timor đọc
documentation giao diện. Mỗi line production hiện không quá `36` ký tự.

Trong khi phát thoại:

- gameplay pause bằng `Time.timeScale = 0` ở `GlobalTimePause`; `CallerOwnedPause` không đổi global time;
- typewriter dùng `Time.unscaledDeltaTime` nên vẫn chạy;
- hai portrait hiện cùng lúc bằng fade ngắn, không đổi kích thước;
- trước fade-in, controller đọc speaker của line đầu tiên; người sắp nói sáng ngay từ frame đầu,
  người còn lại vào đúng trạng thái inactive, nên không còn nháy cả hai portrait ở độ sáng active;
- trạng thái mặc định của slot là chưa nói; chỉ line hiện tại hoặc line đầu sắp phát mới được
  đánh dấu active;
- người không nói được tint tối nhưng giữ nguyên scale để không lộ viền;
- mỗi lượt chỉ có bubble của người nói: bubble cũ thu/fade xuống ngắn, bubble mới bắt đầu sát đầu nhân vật rồi pop + trượt lên nhẹ; sau đó text mới chạy typewriter;
- click, `Space` hoặc `Return` hoàn tất dòng/đi tiếp;
- `Escape` đóng toàn bộ đoạn thoại;
- khi đóng, time scale trước đó được khôi phục đúng giá trị.
- `Dialogue_Text` chạy bằng AudioSource 2D riêng chỉ trong lúc `TypeLine` đang reveal chữ;
  text hiện đủ tự nhiên hoặc do click, Escape, `ForceClose`, disable và scene transition đều
  dừng source ngay nên không có tiếng gõ kéo dài trong lúc chờ người chơi đọc.

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

## 5. Gán thoại vào scene-first map

Sau khi puzzle đã materialize, `DialogueTileBehaviour` trên GameObject/prefab là source of truth:

1. Đặt `Dialogue.prefab` hoặc thêm behaviour vào tile scene-authored phù hợp.
2. Trong Inspector, gán direct `Dialogue Data`.
3. Chọn `Trigger Once` nếu tile chỉ được phát một lần trong session.
4. Lưu scene/level prefab và Play-test.

Custom Inspector hiển thị `Grid Position`, `Dialogue Data`, `Trigger Once` và runtime `Triggered`. Chỉnh Inspector không ghi ngược về `PuzzleData`.

`Audere > Puzzle > Map Editor` chỉ còn là đường migration cho data/map cũ. Sau `Materialize/Bake To Scene`, chỉnh tile trực tiếp trong Scene/Prefab Mode.

Nếu tile chỉ đóng vai trò trigger, visual có thể giống tile thường; gameplay phân biệt bằng component chứ không cần một sprite Dialogue đặc biệt.

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
| `GameplayUIRoot.cs` | Singleton root Canvas chứa `PuzzleUI`, `DialogueUI`, direct reference `CombatTutorialUI` và `CombatRetryUI`, persistent giữa gameplay scene. |
| `DialogueTileBehaviour.cs` | Phát data được gán cho cell khi Player bước vào. |
| `DialogueTileBehaviourEditor.cs` | Chỉnh direct scene/prefab component; không ghi ngược về `PuzzleData`; có nút mở legacy Map Editor. |

QA lịch sử ngày 2026-08-16 (trước scene-first migration):

- Unity Console: `0` error sau compile và Play Mode test;
- `Assembly-CSharp` và `Assembly-CSharp-Editor`: `0 warning`, `0 error`;
- scene chỉ có một gameplay Canvas: `GameplayUIRoot/PuzzleUI|DialogueUI`; Main Menu vẫn dùng UI riêng;
- PuzzleUI sinh đúng 3 card và cả Puzzle Manager/placement bind vào UI persistent;
- Dialogue tile `(0,0)` hiện `Dialogue_Sample`, `Trigger Once = true` trong runtime Inspector;
- sample mở được, text tiếng Việt + tên Audere hiển thị đúng, typewriter chạy khi `timeScale = 0`;
- `ForceClose` trả `IsPlaying = false` và `Time.timeScale = 1`;
- `DialogueTileBehaviour` tại cell `(0,0)` đã gọi sample thành công;
- warning còn lại là Timor chưa có portrait nguồn.

## 7. Lifecycle và Story integration (2026-08-22)

`DialogueController` có completion contract:

```text
DialogueResult.Completed
DialogueResult.Cancelled
```

- Đi hết toàn bộ line trả `Completed` đúng một lần.
- Escape, `ForceClose`, disable hoặc scene transition giữa chừng trả `Cancelled` đúng một lần.
- Callback được xóa trước khi gọi và không tồn tại sang lần `Play` sau.
- `IsPlaying` và `Time.timeScale` được khôi phục theo đúng giá trị trước dialogue, kể cả `0.5`.
- Dialogue giữ claim `GameplayInputMode.Dialogue`; đóng thoại tự khôi phục claim Puzzle/Combat bên dưới.

`DialogueStep` tham chiếu `DialogueData` và optional `DialogueController`. Nếu controller không gán, step dùng `GameplayUIRoot.Instance.Dialogue`. Step không overwrite dialogue đang chạy và chỉ đóng session do chính nó mở khi Story bị cancel.

Dialogue tile không cần visual khác tile thường nếu vai trò chỉ là trigger hành động. Layout puzzle vẫn scene-first; không ghi chỉnh sửa runtime tile ngược về layout nếu không chủ động dùng tool migration.

Trong story staging, hãy tách `DialogueData` tại nơi cần chèn hành động giữa các câu. Ví dụ
`D1_CLASSROOM_ANNOUNCEMENT` dùng các asset nhỏ quanh `WaitStep`, `SetActiveStep` và
`MoveActorStep`, nhờ vậy thoại không phải tự điều khiển actor hoặc timing của scene.
