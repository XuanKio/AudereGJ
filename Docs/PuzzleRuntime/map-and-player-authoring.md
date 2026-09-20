# Dựng map, luật piece và shared Player

[Mục lục nguồn](../04_PuzzleGameplay_SteptileArchitecture.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
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

<!-- END PRESERVED SOURCE -->
