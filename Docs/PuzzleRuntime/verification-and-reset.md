# Checklist, anti-pattern và reset red tile

[Mục lục nguồn](../04_PuzzleGameplay_SteptileArchitecture.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
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
<!-- END PRESERVED SOURCE -->
