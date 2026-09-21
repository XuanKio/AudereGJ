# Story — bản đồ tài liệu nội dung

Trạng thái: đối chiếu scene production 20–150 và asset đang được tham chiếu, 2026-09-20.

## Đọc theo việc cần làm

| Việc | Nguồn chịu trách nhiệm |
| --- | --- |
| Hiểu câu chuyện, xung đột và kết luận | [plot.md](plot.md) |
| Xác định scene, bước ngoặt, nhịp sang cảnh kế | [chronology.md](chronology.md) |
| Hiểu công việc kể chuyện của từng battle và ending | [bosses-and-ending.md](bosses-and-ending.md) |
| Kiểm tra điều đã biết, chưa biết và lỗi liên tục | [continuity.md](continuity.md) |
| Xem phạm vi rút thoại và số đo trước/sau | [dialogue-revision.md](dialogue-revision.md) |

Đọc giọng nhân vật qua [skill dialogue voice](../../.agents/skills/audere-dialogue-voice/SKILL.md).
Đọc cấu trúc runtime, authoring và kiểm thử qua [Docs index](../README.md).
Story không giữ bản sao HP, thời gian encounter, collider, UI layout hay danh sách pattern.

## Ai quản lý gì

| Vai trò | Trách nhiệm |
| --- | --- |
| Agent quản lý docs | Giữ index, chủ sở hữu từng trang, liên kết và giới hạn dưới 200 dòng |
| Agent nội dung | Cập nhật plot/chronology/continuity sau khi đối chiếu production |
| Agent thoại | Đọc state/profile/nhịp cảm xúc; sửa DialogueData; ghi số đo và beat giữ lại ở dialogue-revision |
| Agent gameplay/scene | Giữ runtime/workflow ở tài liệu chuyên môn; báo đổi nhịp cho agent nội dung |
| Reviewer | Kiểm tra scene trước/sau, nhánh choice, ý nghĩa boss và điều không được suy diễn |

Đây là vai trò công việc, không phải các agent chạy nền tự động.
Một thay đổi có một nơi lưu chính; những trang khác trỏ tới nơi đó.
Không sao chép cả đoạn đặc tả sang nhiều trang để tránh những bản cập nhật lệch nhau.

## Bốn nhãn bằng chứng

- **Established Canon**: hành động/lời nói được thiết lập trong production hiện tại.
- **Strongly Implied**: cách hiểu được nhiều chi tiết hỗ trợ nhưng chưa nói thẳng.
- **Design Intent**: mục tiêu thiết kế hoặc cách đọc dự kiến, cần phân biệt với sự kiện.
- **Unresolved**: chưa có bằng chứng, còn mâu thuẫn hoặc chưa được chốt.

Lời Timor nói về một người là canon của **lời nói đó**, không tự chứng minh người đó nghĩ vậy.
Một scene đã implement có thể vẫn dùng art/presentation tạm; nhãn placeholder không phủ nhận
hành động đã diễn ra, cũng không xác nhận mọi diễn giải về thế giới truyện.

## Thứ tự đối chiếu

1. Lời Xuân đã chốt phạm vi và ý định; ghi nhãn nếu chưa có trong game.
2. Active StoryEvent/StoryStep và DialogueData được scene production sử dụng.
3. Encounter/cue/runtime được production tham chiếu; kiểm tra nhánh và gate thật.
4. Story bible và workflow docs tương ứng.
5. Tool authoring, sample, test, tên file và archive chỉ hỗ trợ truy vết.

Khi tool và scene khác nhau, báo rõ khác biệt; không chạy lại tool rộng để ép scene khớp doc.
Asset tồn tại hoặc xuất hiện trong GUID graph chưa đủ chứng minh nó thực sự được chạy:
phải kiểm tra active state, nhánh và thứ tự step.

## Vòng cập nhật bắt buộc

1. Xác định scene và câu chuyện thay đổi thế nào; đọc ít nhất nhịp trước/sau.
2. Sửa nguồn đúng vai trò, giữ các chi tiết chưa chốt trong continuity.
3. Cập nhật một trang Story sở hữu thông tin; thêm liên kết từ trang liên quan.
4. Kiểm tra mọi tài liệu vừa đổi dưới 200 dòng và không có liên kết nội bộ hỏng.
5. Ghi đúng mức xác minh: đọc serialized, compile, test hay chạy scene thực tế.

## Phạm vi phiên bản hiện tại

- Toàn bộ bốn ngày, bảy encounter production và hậu kết đều có trong bản đồ này.
- Puzzle bus Day 1/Day 2 và puzzle hợp tác đã rút; xem [puzzle index](../Puzzles/ShortBus/README.md).
- Scene80 dùng đoạn đi tự động/ác mộng; refine fall/wind/shadow thuộc workflow Dream.
- Thoại có thể ngắn hơn nhưng vẫn giữ input bắt buộc, causal turn, choice và khoảng lặng có tác dụng.
- Không biến mục tiêu giảm khoảng 20% chữ thành yêu cầu cắt đều 20% mọi scene.

## Ưu tiên cảm xúc khi rút thoại

Theo yêu cầu Xuân: câu chuyện vẫn cần đủ cảm động, các đoạn then chốt phải ra từ từ.
Tỷ lệ rút là mục tiêu mềm; nhịp người này nói → người kia tiếp nhận → quyết định có ưu tiên cao hơn.
Không giảm Wait, fade hoặc auto-dialogue minimum để đạt chỉ tiêu chữ/thời lượng.
Số câu ít hơn vẫn có thể khiến một nhịp quá nhanh dù timing field giữ nguyên;
review cả exchange và hành động trước/sau, đặc biệt lần nhận lời đầu, hỏi Bianca, chăm sóc và hòa giải.
Các beat được bảo vệ nằm trong [writing principles](../../.agents/skills/audere-dialogue-voice/references/writing-principles.md).

## Lịch sử và giới hạn

[Archive/README.md](Archive/README.md) giữ ledger thoại cũ để truy vết; không dùng làm canon hiện tại.
Những sự kiện cũ ghi “later unresolved” đã được đối chiếu lại với scene60–150.
Nguồn số đo và thay đổi đợt rút thoại nằm riêng để bible không trôi theo thống kê mỗi lần sửa.
