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

## 5. Dựng một map mới

1. Duplicate level prefab gần nhất trong `Assets/_Audere/Prefabs/Puzzle/Levels/`.
2. Đổi tên theo convention `PZ_<DAY>_<LOCATION_OR_BEAT>`.
3. Mở Prefab Mode; giữ `Puzzle Systems`, `StepTile Board`, `PlayerStart`, `Goal Root` đúng reference.
4. Dùng tile prefab thật dưới `StepTile Board`; đặt từng root tile đúng tâm grid.
5. Ô không đi được: xóa/không tạo tile đó.
6. Đặt một `PlayerStart` chồng đúng tile bắt đầu.
7. Đặt đúng một Goal tile trong `Goal Root`.
8. Tạo/chọn `PuzzleData`, gán tối đa `PuzzleContentConstants.Hand.MaxSlots = 4` path pieces.
9. Nếu luật yêu cầu dùng hết, bật `Require All Path Pieces`.
10. Register `PuzzleController` với `PuzzleRootCoordinator` (coordinator tự cache child nhưng vẫn phải chạy validation).
11. Dùng solver của skill `audere-puzzle-map-generator` để chứng minh có lời giải dùng hết piece.
12. Play-test complete, fail/fall, reset và replay.

### Migration map cũ

Mở:

```text
Audere > Puzzle > Map Editor
```

Map Editor hiện là công cụ migration. `Materialize/Bake To Scene` tạo các tile, Goal và PlayerStart thật. Nếu board đã có tile, tool hỏi xác nhận trước khi replace và hỗ trợ Undo.

## 6. Luật thiết kế map và piece

- Tọa độ Unity là zero-based `(x, y)`.
- Nếu brief dùng cột/hàng từ `1`, quy đổi thành `(column - 1, row - 1)`.
- `PathPieceData.OrderedLocalPath` là thứ tự Player thật sự đi.
- Một endpoint phải nối với `GridPlayer.GridPosition` hiện tại.
- Path được phép đi ra ngoài board; đó là fall, không phải placement invalid.
- Với `Require All Path Pieces`, chạm Goal sớm là một attempt sai có feedback rồi reset, không làm fail `StoryEvent`.
- Kiểm tra parity tổng số bước và độ lệch Start → Goal trước khi dựng art.
- Không tuyên bố puzzle có lời giải duy nhất nếu solver chưa xác nhận.

Path pieces hiện có:

| Stable ID | Asset |
| --- | --- |
| `line-2` | `PathPiece_Line_2.asset` |
| `line-3` | `PathPiece_Line_3.asset` |
| `line-4` | `PathPiece_Line_4.asset` |
| `l-corner` | `PathPiece_L_Corner.asset` |
| `l-corner-3` | `PathPiece_L_Corner_3.asset` |

Day 1 hiện dùng:

- Washroom: tutorial board nhỏ, dạy chọn/đặt rồi mới mở rotation.
- Breakfast: `Require All Path Pieces`, dạy không được tới Goal khi vẫn còn piece.
- Bus Stop: board `4 hàng × 7 cột`, PlayerStart `(0,0)`, Goal tile `Tile_23_BusStopGoal_6_3`, bốn pieces và bắt buộc dùng hết.

## 7. Shared Player và cảm giác di chuyển

Không đặt một Player riêng trong từng level prefab. Shared Player nằm trực tiếp dưới location `Puzzle Root`; mỗi puzzle chỉ sở hữu marker `PlayerStart`.

Khi Play/reset:

```text
BoardManager.RegisterExistingTiles
→ lấy cell từ PlayerStart world position
→ kiểm tra cell có BoardTile
→ GridPlayer.SetPosition(cell, cell world center)
```

Thông số Player prefab hiện tại:

| Field | Giá trị |
| --- | ---: |
| `Step Duration` | `0.32 s` |
| `Visual Scale` | `1.5` |
| `Step Arc Height` | `0.075` |
| `Landing Duration` | `0.10 s` |
| `Fall Duration` | `0.62 s` |

Movement dùng `SmootherStep`, hop nhẹ và landing squash. Không override `stepDuration` xuống giá trị cực nhỏ trong Scene; override cũ từng làm Audere chạy quá nhanh và mất nhịp cũ.

`GridPlayer.transform.position` là vị trí visual đã cộng offset để chân sprite đứng trên tâm tile. Khi kiểm tra alignment, so `GridPosition`, `PlayerStart`, Goal hoặc chân sprite; không so tâm sprite với tâm tile rồi kết luận bị lệch.

## 8. Chuyển tiếp Goal → PlayerStart

Flow chuẩn giữa hai puzzle:

```text
Puzzle A Completed
→ BoardTileTransitionStep hide dần board A, giữ Goal A
→ Goal visual/item được ẩn; tile nền trở thành transition anchor
→ dialogue/beat (Player vẫn đứng trên anchor)
→ PuzzleSequencePrepareStep cho B, Align To Previous Goal = true
→ dịch toàn bộ level B để PlayerStart B trùng world position Goal A
→ BoardTileTransitionStep reveal B từ PlayerStart
→ PuzzleStep.Play B
```

Quy tắc quan trọng:

- `goalToBecomeAnchor` phải trỏ đúng Goal của puzzle nguồn.
- Trong đoạn dialogue cần giữ tile dưới chân Player, không gán `rootToDisableAfterHide` cho source level.
- `PuzzleRootCoordinator` tự ẩn transition source cũ khi board kế tiếp bắt đầu reveal, tránh hai Goal/start tile cùng hiện.
- `PuzzleSequencePrepareStep.Align To Previous Goal` bật cho event/location tiếp theo như Breakfast → Bus Stop.
- `PuzzleController` giữ pose level đã prepare qua lần `Play()` kế tiếp; không chen một normalize khác làm restore authored root.
- Không đưa shared Player vào `SetActiveStep.Objects To Disable` giữa hai puzzle. Việc tắt ở cuối event rồi bật lại trong event kế tiếp tạo một frame nháy.
- `WorldModeStep` chỉ đổi world mode; không đặt Player, không Play puzzle và không cấp input.

Regression đã sửa ngày 2026-08-22:

- Goal Washroom cũ từng còn hiện lệch chéo sau Breakfast.
- Goal Breakfast từng bị tắt cùng root nên Player trông như đứng giữa không trung.
- `130_LeaveHouse` từng tắt Player rồi `PrepareBusStopPuzzle` bật lại, gây nháy.
- Sau sửa, Breakfast Goal → Bus PlayerStart có khoảng cách world `0`; tile reveal đầu tiên là `Tile_Street_0_0` và Player luôn active.

## 9. Animation hide/reveal board

`BoardTileTransitionStep` dùng style:

```text
fade nhẹ + nhô từ dưới + overshoot rất nhỏ
```

Thiết lập Day 1 hiện xoay quanh:

- transition mỗi tile `0.20–0.24 s`;
- cả reveal wave khoảng `0.95 s`;
- vertical offset khoảng `0.065–0.08`;
- overshoot khoảng `0.01–0.012`;
- unscaled time để không bị dialogue/timeScale làm treo.

Thứ tự reveal được tính từ `PlayerStart`. Trước khi sort, coordinator gọi `BoardManager.RegisterExistingTiles()` để grid position phản ánh pose level sau khi đã align.

`BoardTileTransitionStep` phát `Tile_Pop` khi tile bắt đầu biến mất hoặc vừa xuất hiện. Clip
được throttle tối thiểu `0.11 s`, nên board lớn vẫn có nhịp âm thanh theo wave mà không phát
chồng một one-shot cho mọi tile trong cùng frame. Khi Player bắt đầu rời tile an toàn để rơi,
`PuzzleManager.HandleFallStarted()` phát `Player_Fall`; sound không chờ tới lúc reset map.

## 10. Input ownership

`GameplayInputGate` nằm dưới `GameplayUIRoot` và quản lý claim theo token/owner:

```text
None
PuzzleController.Play → Puzzle
DialogueController.Play trên Puzzle → Dialogue
Dialogue close → tự trở lại Puzzle
Puzzle complete/cancel/disable → None
```

Session cũ chỉ release token do chính nó tạo. Không dùng một biến mode global để callback cũ có thể tắt input của lượt replay mới.

`PuzzleStep` và `BoardTileTransitionStep` gọi `NormalizeAfterCancel()` khi hủy trong scene còn sống. Hàm này kiểm tra reference của toàn bộ chuỗi trước khi reset. Khi step/scene đang bị disable hoặc UI/Player đã bị hủy, chỉ cleanup ownership; không gọi normalize để dựng lại board. `NormalizeNow()` trong flow chuẩn vẫn báo lỗi nếu authoring thiếu reference.

Time Scale mặc định của project phải là `1`. Không dùng giá trị `0` lưu trong TimeManager để pause story; dialogue giữ và trả lại giá trị trước pause. Traversal dùng scaled time nên giá trị mặc định `0` sẽ làm puzzle đứng giữa bước đi.

## 11. Checklist QA cho mỗi puzzle

- [ ] Board/Goal/PlayerStart nhìn thấy và chỉnh được trong Prefab Mode.
- [ ] Không có tile ở cell không đi được.
- [ ] Đúng một Goal và PlayerStart chồng một BoardTile.
- [ ] Không có `PathPreview`, placement hoặc shared Player trùng trong level prefab.
- [ ] Solver có ít nhất một lời giải đúng luật; nếu require-all thì Goal chỉ tới sau piece cuối.
- [ ] `Play → Goal` trả `Completed` đúng một lần.
- [ ] Cancel/disable trả `Cancelled` đúng một lần.
- [ ] Reset/replay xóa path cũ, khôi phục piece và PlayerStart.
- [ ] Chỉnh tile/Goal trong Scene rồi Play dùng đúng vị trí mới.
- [ ] Chuyển puzzle: Goal cũ = PlayerStart mới, chỉ một anchor tile hiện, Player không nháy.
- [ ] Dialogue phủ puzzle thì click không đặt path.
- [ ] Console không có error sau flow bình thường.

## 12. Anti-patterns

- Không dùng `StartPuzzle(PuzzleData)` làm con đường duy nhất để dựng board.
- Không generate toàn bộ layout ở runtime.
- Không bake tự động khi Play.
- Không đặt runtime/preview/player vào từng `PZ_*`.
- Không tạo blocked tile chỉ để ô không đi được trông tối.
- Không dùng global `FindFirstObjectByType` để nối reference level nếu Inspector/child scope giải quyết được.
- Không dùng `SetActiveStep` để Play Puzzle/Combat hoặc tắt shared Player trong một puzzle chain.


## 13. Red tile exhaustion and reset — 2026-08-28

- `OneUseTileBehaviour` keeps its floor visible while occupied, then disables every tile renderer immediately on departure. It no longer leaves an alpha/scale remnant in any scene using the shared behaviour.
- `CooperativeRedTileBehaviour` keeps a tile visible while either carrier occupies it. With no holder and at least one spent visit, the tile can no longer be entered and its renderers turn off completely. Each actor still gets only one visit; a second visitor needs the first to hold the tile.
- Hidden tiles remain registered as authored `BoardTile` objects so reset can restore them. `BoardTile.ResetToAuthoredState` restores every renderer, authored color and scale; behaviour callbacks clear the visit/occupancy flags. No replacement tile is spawned.
- Co-op completion requires both actor-specific arrival flags and an empty shared hand. Falling or exhausting the hand first resets the whole attempt automatically; no Retry/actor-switch UI is added.
