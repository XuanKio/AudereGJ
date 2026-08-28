# Dream — năm đoạn liên tục, 15 bước

Scene source of truth: `Assets/_Audere/Scenes/80_D2_Dream.unity`.
Đã đọc lại tọa độ serialized qua Unity MCP, chạy `step_tile_solver.py` và Play preview/drop thật ngày 2026-08-28.

| Segment | Cells Unity (y=0) | Start | Goal | Goal world / next Start |
| --- | --- | --- | --- | --- |
| 1 | x=0,1,2,3 | (0,0) | (3,0) | (0.75,-0.04,0) |
| 2 | x=3,4,5,6 | (3,0) | (6,0) | (1.50,-0.04,0) |
| 3 | x=6,7,8,9 | (6,0) | (9,0) | (2.25,-0.04,0) |
| 4 | x=9,10,11,12 | (9,0) | (12,0) | (3.00,-0.04,0) |
| 5 | x=12,13,14,15 | (12,0) | (15,0) | (3.75,-0.04,0), kết thúc |

Hàng một theo cách đếm người chơi; cột 1 đến 16. Cell size world=0.25. Mỗi đoạn ba reference tới `Assets/_Audere/Data/Puzzle/PathPieces/PathPiece_Line_2.asset`, path `[(0,0),(1,0)]`, require-all=true.

Solution: lần lượt đặt ba card theo hướng phải, rotation0; không cần xoay. Mỗi segment có **6 nghiệm phân biệt danh tính card**, cùng một đường hình học (ba card giống nhau hoán vị 3!). Không tuyên bố một nghiệm theo solver. Tổng bước=3 mỗi đoạn, parity đúng, premature Goal hits=0, first-move traps=0; puzzle kể chuyện, không cố làm câu đố khó.

Các `segment-*.json` là spec tái hiện tọa độ đã đọc. Chạy từng file:

```powershell
python .agents/skills/audere-puzzle-map-generator/scripts/step_tile_solver.py --input Docs/Puzzles/Day2Dream/segment-1.json
```

Goal cũ là tile còn hiện qua reveal; tile Start mới cùng vị trí giữ ẩn đến swap. Bốn handoff có world delta=0. Player dùng chung luôn active; Play đã đi đủ 15 cell. RGB decor không có BoardTile/collider, không phải đường phụ.
