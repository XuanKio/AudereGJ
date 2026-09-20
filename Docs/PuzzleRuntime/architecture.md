# Nguồn dữ liệu, hierarchy và runtime StepTile

[Mục lục nguồn](../04_PuzzleGameplay_SteptileArchitecture.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
# Audere Puzzle Gameplay — kiến trúc StepTile scene-first

> **Last updated:** 2026-08-27 · **Unity:** 6000.0.79f1

Tài liệu này là nguồn hướng dẫn hiện hành để dựng, nối và kiểm thử puzzle StepTile.

## 1. Trạng thái quyết định

- **Established Canon:** layout puzzle được author trực tiếp trong Scene/Prefab Mode.
- **Established Canon:** `PuzzleData` chỉ giữ config reusable và dữ liệu migration cũ; không phải nguồn layout runtime.
- **Established Canon:** mỗi location có đúng một shared Player, `PuzzleRuntime`, `PathPreview`, `PathPlacementController` và `Placed Path Root`.
- **Established Canon:** ô không đi được thì không có `BoardTile`; không tạo tile chặn chỉ để lấp hình.
- **Established Canon:** puzzle có `Require All Path Pieces` chỉ hoàn thành khi Player tới Goal sau khi đã dùng hết piece.
- **Design Intent:** Goal của puzzle trước trở thành đúng vị trí PlayerStart của puzzle kế tiếp để flow nhìn liền mạch.
- **Unresolved:** save/checkpoint dài hạn và graph/branching story chưa được xây.

## 2. Source of truth

### Layout — Scene/level prefab

Các object sau phải tồn tại thật và nhìn thấy được khi không Play:

- `StepTile Board` và từng `BoardTile`.
- `Goal Root` và Goal tile.
- `PlayerStart`.
- obstacle/interactive object nếu level có.
- prop/presentation riêng của level.

Các level prefab hiện tại:

```text
Assets/_Audere/Prefabs/Puzzle/Levels/
├── PZ_D1_WASHROOM.prefab
├── PZ_D1_BREAKFAST.prefab
└── PZ_D1_BUS_STOP.prefab
```

Sau khi bake, di chuyển/xóa/thêm tile trực tiếp trong prefab hoặc Scene View. Play Mode không được sinh lại board và không được ghi đè các chỉnh sửa này.

### Config — PuzzleData

`PuzzleData` hiện giữ:

- `Puzzle Id`.
- `Available Path Pieces`.
- `Require All Path Pieces`.
- layout cũ chỉ để `Materialize/Bake To Scene` một lần.

Không sửa map bằng `PuzzleData` sau khi level đã materialize, trừ khi chủ động muốn bake lại và chấp nhận thay toàn bộ tile hiện có.

## 3. Hierarchy chuẩn

```text
WORLD
└── Puzzle Root [GridSpace2D, PuzzleRootCoordinator]
    ├── Player                         shared, chỉ một instance
    ├── Puzzle Runtime                 shared
    │   ├── Path Placement Controller
    │   ├── Path Preview
    │   └── Placed Path Root
    ├── PZ_D1_WASHROOM                 level prefab
    │   ├── Puzzle Systems
    │   ├── StepTile Board
    │   ├── PlayerStart
    │   └── Goal Root
    ├── PZ_D1_BREAKFAST                level prefab
    │   └── ...
    └── PZ_D1_BUS_STOP                 level prefab
        └── ...
```

`Path Preview`, placement và path đã đặt không nằm trong từng `PZ_*`. Nếu mỗi level có một bản runtime riêng, board cũ rất dễ để lại preview/placed path chồng lên board mới.

Các prefab cấu trúc reusable:

```text
Assets/_Audere/Prefabs/Puzzle/Structure/
├── PuzzleSystems.prefab
├── StepTileBoard.prefab
├── PlayerStart.prefab
├── GoalRoot.prefab
└── PlacedPathRoot.prefab
```

Level prefab vẫn là một prefab hoàn chỉnh. Các node cấu trúc bên trong dùng prefab chung để sửa behavior/reference dùng chung tại một nơi, nhưng layout tile và cấu hình riêng vẫn nằm trên `PZ_*`.

## 4. Trách nhiệm runtime

| Component | Trách nhiệm |
| --- | --- |
| `PuzzleRootCoordinator` | Registry level trong location, normalize, chọn active puzzle, shared Player/runtime, Goal → PlayerStart hand-off và thứ tự reveal. |
| `PuzzleController` | Lifecycle `Play/Cancel/Reset`, input claim và flow state `Preparing/Revealing/Playing/...`. |
| `PuzzleManager` | Reset attempt, đặt shared Player tại PlayerStart, hand/piece, traversal và luật complete/fail. |
| `BoardManager` | Đăng ký các `BoardTile` đang tồn tại và lookup grid/runtime bounds. |
| `PuzzleRuntime` | Một placement controller, preview và placed-path root dùng chung cho location. |
| `GridPlayer` | Vị trí logical, di chuyển từng cell, landing và fall presentation. |

Lifecycle public:

```csharp
puzzleController.Play(result => { /* Completed, Cancelled, Failed */ });
puzzleController.Cancel();
puzzleController.ResetPuzzle();
```

`PuzzleController.Play()` mới cấp claim `GameplayInputMode.Puzzle`. Chỉ đổi `WorldMode` hoặc chỉ bật level root không làm puzzle nhận input.

### Shared Path Preview presentation

Path Piece trong thanh chọn dưới UI giữ presentation cũ: card `128 × 128 px`, gap `24 px`,
node `14 px` và logical spacing `24 px`. Không thay đổi presentation card khi chỉ polish
preview đang bám con trỏ trên board.

World `PathPreview` giữ endpoint `0.82` cell; ô connector nhỏ bằng `0.14` cell,
khoảng cách tâm bằng `0.25` cell. Các điểm nằm trên cùng một lưới chia tư mỗi đoạn;
vertex 90 độ có đúng một ô nối, không kéo mẫu lân cận về góc nên không còn dồn/chồng ô.
Clearance ở A/B có tính cả nửa kích thước connector. Path dài vượt budget giảm mật độ
đồng đều, tối đa 32 connector; không đổi `PathPieceData`, hình card hay luật đặt đường.

Connector mới/tái sử dụng được khởi tạo đúng kích thước trước frame hiển thị và dùng
chung scale presentation. Khi xoay/đổi hình, cả preview đổi hình cùng nhau; khi chỉ di
chuyển con trỏ, vẫn giữ nội suy vị trí. Hai prefab `PathPreviewWorld`, `PathPreviewUI`
và preview author trực tiếp trong Scene60 dùng cùng cấu hình.

Kiểm tra 2026-08-29: 14/14 test passed, gồm 13 presentation cases (góc, xoay, đảo hai đầu,
scale parent, pool tăng/giảm, retarget, clear) và production Day4 với actual preview/drop,
wrong-goal/fall/reset/cancel, thắng hai board rồi chuyển lớp. Evidence:
`Temp/PathPreviewPolish/tests_14_pass.xml`, `short-even.png`, `long-even.png`.
Hai ảnh là Play preview dùng prefab thật trên nền kiểm tra riêng; lượt Day4 kiểm gameplay.

<!-- END PRESERVED SOURCE -->
