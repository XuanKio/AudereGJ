# Audere Puzzle Gameplay — kiến trúc StepTile scene-first

> Tách theo trách nhiệm ngày 2026-09-20. Đường dẫn và các heading cũ được giữ để không mất liên kết.
> Nội dung gốc, giới hạn QA và ngày checkpoint nằm trong các trang con; không xem số liệu cũ là trạng thái mới đã xác minh.

[Bản đồ tài liệu](README.md) · [Quy tắc cập nhật](AGENTS.md) · [Cốt truyện](Story/README.md)

## Tài liệu theo nhiệm vụ

| Nhiệm vụ | Trang cần đọc / sửa |
| --- | --- |
| Nguồn dữ liệu, hierarchy và runtime StepTile | [architecture](PuzzleRuntime/architecture.md) |
| Dựng map, luật piece và shared Player | [map-and-player-authoring](PuzzleRuntime/map-and-player-authoring.md) |
| Goal hand-off, reveal và input ownership | [handoff-and-input](PuzzleRuntime/handoff-and-input.md) |
| Checklist, anti-pattern và reset red tile | [verification-and-reset](PuzzleRuntime/verification-and-reset.md) |

## Heading từ tài liệu trước khi tách

## 1. Trạng thái quyết định

[Xem phần này](PuzzleRuntime/architecture.md#1-trạng-thái-quyết-định)

## 2. Source of truth

[Xem phần này](PuzzleRuntime/architecture.md#2-source-of-truth)

### Layout — Scene/level prefab

[Xem phần này](PuzzleRuntime/architecture.md#layout--scenelevel-prefab)

### Config — PuzzleData

[Xem phần này](PuzzleRuntime/architecture.md#config--puzzledata)

## 3. Hierarchy chuẩn

[Xem phần này](PuzzleRuntime/architecture.md#3-hierarchy-chuẩn)

## 4. Trách nhiệm runtime

[Xem phần này](PuzzleRuntime/architecture.md#4-trách-nhiệm-runtime)

### Shared Path Preview presentation

[Xem phần này](PuzzleRuntime/architecture.md#shared-path-preview-presentation)

## 5. Dựng một map mới

[Xem phần này](PuzzleRuntime/map-and-player-authoring.md#5-dựng-một-map-mới)

### Migration map cũ

[Xem phần này](PuzzleRuntime/map-and-player-authoring.md#migration-map-cũ)

## 6. Luật thiết kế map và piece

[Xem phần này](PuzzleRuntime/map-and-player-authoring.md#6-luật-thiết-kế-map-và-piece)

## 7. Shared Player và cảm giác di chuyển

[Xem phần này](PuzzleRuntime/map-and-player-authoring.md#7-shared-player-và-cảm-giác-di-chuyển)

## 8. Chuyển tiếp Goal → PlayerStart

[Xem phần này](PuzzleRuntime/handoff-and-input.md#8-chuyển-tiếp-goal--playerstart)

## 9. Animation hide/reveal board

[Xem phần này](PuzzleRuntime/handoff-and-input.md#9-animation-hidereveal-board)

## 10. Input ownership

[Xem phần này](PuzzleRuntime/handoff-and-input.md#10-input-ownership)

## 11. Checklist QA cho mỗi puzzle

[Xem phần này](PuzzleRuntime/verification-and-reset.md#11-checklist-qa-cho-mỗi-puzzle)

## 12. Anti-patterns

[Xem phần này](PuzzleRuntime/verification-and-reset.md#12-anti-patterns)

## 13. Red tile exhaustion and reset — 2026-08-28

[Xem phần này](PuzzleRuntime/verification-and-reset.md#13-red-tile-exhaustion-and-reset--2026-08-28)
