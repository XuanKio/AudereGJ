# Audere — Character Expression & Motion Contract

> **Last verified:** 2026-08-23  
> **Current production example:** `30_Classroom/D1_CLASSROOM_RECESS_BIANCA`

Tài liệu này ghi lại cách biểu đạt chuyển động nhân vật trong story scene để animation có thể
được thay sau mà không phá staging, direct reference hoặc nhịp `StoryEvent`.

## 1. Ranh giới bằng chứng

- **Established implementation:** `CharacterMotionStep` đang điều khiển hop, squash/stretch,
  landing và hướng nhìn bằng direct scene reference.
- **Design Intent:** các motion beat bên dưới mô tả cảm giác cần giữ khi thay bằng Animator.
- **Unresolved:** sprite sheet, clip, rig và thông số animation cuối cùng của từng nhân vật.
- Placeholder motion và placeholder art không tự xác nhận ngoại hình hay phong cách animation canon.

## 2. Ownership của Transform

- Actor story có thể di chuyển root giữa các staging target do scene sở hữu.
- Khi actor đang đứng trên tile, **standing anchor ở giữa bàn chân** phải trùng tâm tile.
  Không dùng actor pivot làm chuẩn vì sprite hiện tại đặt pivot gần giữa thân.
- Tool có thể tính offset authoring từ `SpriteRenderer.bounds`: tâm X của bounds trùng tâm X
  tile và `bounds.min.y` trùng tâm Y tile. Sau đó mọi staging target dùng actor baseline này.
- Khi thay art/animation, ưu tiên đặt một visual/feet anchor rõ trong prefab; không hardcode
  một offset Y dùng chung cho các sprite có chiều cao khác nhau.
- Shared gameplay `GridPlayer` không được dịch root chỉ để diễn biểu cảm; animation biểu cảm
  phải nằm ở visual child hoặc Animator để không làm lệch cell/Goal.
- Mọi step giữ direct reference tới actor, target và renderer; không tìm actor toàn cục ở runtime.
- Khi thay placeholder bằng animation thật, giữ nguyên vị trí kết thúc và thời điểm step báo
  `Completed` để các dialogue/action kế tiếp không đổi nhịp.

## 3. Motion beat dùng lại

| Beat | Mục đích | Contract hiện tại | Trạng thái |
| --- | --- | --- | --- |
| `TileHop` | Nhân vật story đi từng tile | Hop tới target; `0.32s`, arc `0.075` world, landing `0.10s`; hướng nhìn theo chiều đi | Established implementation |
| `StartleHop` | Giật mình tại chỗ | `VerticalInPlace`: khóa X/Z, chỉ Y tăng theo arc rồi trở về standing baseline (bàn chân ở tâm tile); `0.19s`, arc `0.09`, landing `0.10s`; đổi hướng sau cú hop | Established implementation, visual là placeholder |
| `Nudge` | Nhích gần rất nhẹ | `MoveActorStep` ngắn khoảng `0.14s`, không thêm bounce lớn | Established implementation |
| `LeanInterest` | Audere bị thu hút bởi một lựa chọn | Nhích nhẹ về phía đối tượng quan tâm rồi có thể quay lại anchor | Established implementation |

`StartleHop` không phải cú nhảy vui mừng. Silhouette cần đọc là phản xạ ngắn: bật lên một
nhịp theo trục Y, đáp xuống đúng tọa độ bắt đầu, rồi mới quay sang nguồn tiếng gọi. Trong suốt
motion, X/Z không được lerp về một target khác. Cancel cũng phải trả actor về vị trí bắt đầu.

## 4. Bianca trong giờ nghỉ

Sequence hiện tại dùng nhịp di chuyển đồng bộ với StepTile:

```text
tile trước mặt hiện bằng BoardTileTransitionStep
→ Bianca TileHop tới tile đó
→ tile phía sau mờ đi
→ lặp lại cho tới Tile_DecorationInterest
```

Bianca dừng bên phải Audere, gọi tên cô, rồi chỉ nhích gần một khoảng nhỏ khi Audere chưa
phản ứng. Audere `StartleHop` tại chỗ và lật sang phải để nhìn Bianca.

## 5. Quy tắc thay animation sau này

1. Giữ tên motion beat và observable end state; thay implementation visual, không viết lại story.
2. Không đổi actor root nếu beat chỉ là biểu cảm.
3. Nếu clip có root motion, tắt root motion hoặc bù về đúng staging target trước khi complete.
4. Hướng nhìn phải ổn định trước dialogue kế tiếp để tránh nháy/lật muộn.
5. Test cả play từ event trước và replay event hiện tại; cancel không được để actor kẹt ở scale tạm.
6. Đối chiếu actor/tile bằng standing anchor: giữa bàn chân phải trùng tâm tile. Actor root chỉ
   trùng tâm tile nếu chính prefab đã author pivot tại bàn chân.
