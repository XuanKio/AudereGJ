# Quy tắc quản lý tài liệu Audere

Áp dụng cho toàn bộ `Docs/`. Giữ nguyên các skill bắt buộc trong [AGENTS gốc](../AGENTS.md).
Điểm vào: [README](README.md). Mỗi file Markdown **ít hơn 200 dòng**, kể cả index và bảng.

## Trách nhiệm theo loại tài liệu

| Chủ quản | Nội dung sở hữu | Khi cần phối hợp |
| --- | --- | --- |
| Narrative / dialogue | `Story/*`: plot, chronology, ý nghĩa boss, ending, giọng và revision | Kiểm tra skill `audere-dialogue-voice` và thoại production trước khi đổi canon |
| Runtime / hệ thống | `Audio/`, `PuzzleRuntime/`, `DialogueRuntime/`, `Combat/`, `StoryRuntime/` và doc kỹ thuật còn ngắn | Mô tả API, lifecycle, owner, scene/prefab binding; không tự viết lại plot |
| Scene authoring | `Workflows/*`, các workflow `12`–`17` chưa cần tách | Giữ direct reference, tên event/step, nguồn asset và thứ tự hierarchy; dùng skill scene tương ứng |
| Puzzle authoring | `Puzzles/*` và JSON/proof đi kèm | Layout, luật, lời giải và dữ liệu export phải khớp scene được kiểm tra |
| Docs manager / agent đang sửa | `README.md`, file index `00`–`17`, `Overview/*`, liên kết và giới hạn dòng | Định tuyến, kiểm tra chéo và ghi phạm vi xác minh; không đổi nội dung chuyên môn bằng suy đoán |

Đây là trách nhiệm trong task, không phải yêu cầu tạo thêm agent ở mỗi lần sửa.
Agent nhận task đọc index, tìm đúng trang chủ quản rồi cập nhật cả nguồn và tài liệu chịu ảnh hưởng.

## Nguồn bằng chứng và canon

Thứ tự ưu tiên: production `DialogueData` được `StoryEvent` tham chiếu → Scene/Hierarchy → runtime và
serialized config → docs → sample / `TEST_*` / placeholder / brainstorm. Khi có mâu thuẫn, ghi nguồn
và trạng thái; kiểm tra production trước khi kết luận. Không coi một câu trong tài liệu cũ là chứng cứ runtime mới.

| Nhãn | Khi được dùng |
| --- | --- |
| **Established Canon** | Đã được thoại / hành động production được tham chiếu thể hiện rõ |
| **Strongly Implied** | Nhiều bằng chứng đồng thuận nhưng game chưa nói thẳng |
| **Design Intent** | Xuân đã yêu cầu hoặc hướng thiết kế đã được ghi, chưa đủ kiểm chứng production |
| **Unresolved** | Thiếu bằng chứng, còn mâu thuẫn, placeholder hoặc chưa chốt |

Các snapshot và kết quả QA cũ giữ nguyên ngày, phạm vi và giới hạn. Chữ “hiện tại” ở bản lưu chỉ áp dụng
cho checkpoint gốc. Thông số mới phải lấy từ asset/config đang dùng; không lấy mặc định từ bảng cũ.
Không nâng kết quả kiểm tra dữ liệu thành kết quả Play, hoặc Play một cảnh thành full playthrough.

## Quy trình cập nhật một thay đổi

1. Tìm trang chủ quản qua [README](README.md), đọc skill chuyên môn bắt buộc và kiểm tra nguồn hiện tại.
2. Sửa đúng trách nhiệm: narrative ở `Story`, hợp đồng runtime ở hệ thống, event/asset/QA ở workflow.
3. Cập nhật đường dẫn từ index và những doc chịu ảnh hưởng; link tới nguồn chung thay vì chép lại quy tắc.
4. Ghi ngày, phạm vi sửa, bằng chứng đã chạy và phần chưa xác minh; giữ lịch sử QA dưới nhãn checkpoint.
5. Kiểm tra dưới 200 dòng, liên kết tương đối và heading đích; xác nhận không mất nội dung khi tách.

## Quy tắc tách và giữ liên kết

Tách theo nhiệm vụ / heading hoàn chỉnh, ưu tiên khoảng 80–170 dòng. Không cắt giữa code fence, bảng,
danh sách liên quan hoặc một điều kiện lifecycle. Một chủ đề ngắn có thể nằm riêng nếu trách nhiệm khác.
File gốc trở thành index ngắn, giữ các heading đã được link và trỏ tới trang con. Giữ đường dẫn tương đối
hợp lệ khi chuyển nội dung. Không tự xóa mô tả/QA cũ; đánh dấu lịch sử nếu nguồn production đã thay đổi.

Trang kỹ thuật không giữ bản sao thoại đầy đủ để chỉnh độc lập. Khi trích thoại làm bằng chứng, ghi asset / beat
và ngày; một revision thoại cần cập nhật hoặc gắn nhãn checkpoint cho các ví dụ cũ có thể gây hiểu nhầm.
Khi thay topology scene / số puzzle, cập nhật workflow lẫn `Puzzles/*` và chronology nếu ý nghĩa nhịp thay đổi.
Khi thay ý nghĩa boss / ending, cập nhật `Story/bosses-and-ending.md` cùng plot; balance không tự đổi canon.

## Giới hạn của lần sắp xếp 2026-09-20

Các trang lớn được tách nguyên nội dung; metadata, mốc cũ và nguồn kiểm tra vẫn được giữ.
Việc này không xác minh lại toàn bộ script inventory, Build Settings, balance hoặc các lần QA lịch sử.
Các trang `Overview/*` là snapshot 2026-08-23; tìm trạng thái mới qua trang hệ thống và dữ liệu production.
