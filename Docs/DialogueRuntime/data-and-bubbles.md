# DialogueData, projection và độ dài bubble

[Mục lục nguồn](../05_DialogueSystem.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
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
- `Left/Right Portrait Override`: optional ảnh mở đầu riêng cho đoạn thoại;
- `Lines`: danh sách theo thứ tự, mỗi dòng chọn `Speaker`, nhập `Text`, optional
  `Character Override` và `Portrait Override`, áp dụng cho speaker từ line đó trở đi.

### Chuyển giữa người thật và lời bị bóp méo

- `Character Override = None` giữ identity hiện tại; `Portrait Override = null` giữ biểu cảm hiện tại.
- Đổi sang identity khác sẽ reset biểu cảm về catalog của identity mới. Nếu cùng line có Portrait
  Override thì ảnh này được áp dụng sau cùng. Chọn lại cùng identity không reset biểu cảm.
- Hai slot giữ state riêng trong mỗi playback; đổi bên phải không đổi portrait của Audere bên trái.
- Lời tiêu cực dùng BiancaDistorted/TeacherDistorted. Lời hỏi han thật xen giữa combat phải đổi
  Character Override về Bianca/Teacher, không chỉ để portrait trống vì trống có nghĩa là giữ ảnh.
- Ví dụ Teacher SMALL_TASK: mở đầu Teacher → line đầu TeacherDistorted + glitch → giữ bản bóp méo
  → câu “Audere, em nghe cô nói không?” đổi về Teacher + glitch. Không cần tách thêm StoryStep/cue.
- Dialogue mới/Retry khởi tạo lại từ default của asset; không kế thừa identity của lần phát trước.
- Scene60 có6 asset Bianca bóp méo; Scene120 dùng3 asset Teacher bóp méo. Ba draft Teacher cũ
  cũng đã tách identity nhưng không được thêm vào production flow. TeacherAfterCombat và
  BiancaReprise vẫn là người thật với portrait normal; các line Timor vẫn là Timor.

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

Thông thường để override trống để dùng catalog. Chỉ author override khi biểu cảm là một beat có chủ
đích; runtime giữ portrait override gần nhất của từng slot cho tới khi đổi identity hoặc dialogue kết thúc.

<!-- END PRESERVED SOURCE -->
