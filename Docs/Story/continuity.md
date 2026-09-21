# Continuity — ranh giới bằng chứng và các điểm dễ bị bỏ sót

Đối chiếu 2026-09-20. Trang này sở hữu các khoảng trống; không giấu chúng trong profile nhân vật.
Tra scene tại [chronology](chronology.md), mục đích chung tại [plot](plot.md).

## Đã được thiết lập

| Điểm | Bằng chứng / giới hạn |
| --- | --- |
| Audere là tên hiện dùng | Enum, catalog, DialogueData và StoryEvent production |
| Buổi sáng D1 là đánh răng | `D1_HOME_MORNING`, vị bạc hà và item bàn chải khớp nhau |
| Bố chuẩn bị bánh mì | `D1_HOME_AFTER_BRUSHING`; chưa thêm lịch sử gia đình khác |
| Audere vẫn run sau lần đầu nói được | `D1_CLASSROOM_AFTER_BIANCA_DEPARTURE` |
| Timor đêm D1 thu hẹp câu trả lời | Scripted defeat → ba nhánh refusal/delay/silence |
| Audere có một lựa chọn mới ở D2 | Tình nguyện lấy đồ dù Timor bảo chưa lên tiếng |
| Bianca thật không xác nhận taunt | Dialogue hậu combat: lỗi nhỏ, có thể cùng sửa |
| Teacher chăm sóc có ranh giới | Choice chia sẻ, cho nghỉ, hỏi trước khi ôm |
| D4 im lặng có tiền đề | Timor tuyên bố rút hướng dẫn ở cuối scene120 |
| Ending là hòa giải có giới hạn | Audere vẫn cần Timor và muốn tự bước; Timor nói sẽ thử |

Các hàng là **Established Canon** về lời/hành động hiện có; không phải bảo đảm tương lai.

## Chưa được chốt

| Điểm | Nhãn | Quy tắc khi viết |
| --- | --- | --- |
| Timor là ai/cái gì, có hình dạng nào ngoài đời | Unresolved | Không tự gọi mèo/người/bạn tưởng tượng/chẩn đoán |
| Ai ngoài Audere nghe/nhìn Timor và combat | Unresolved | Không viết Bianca/cô giáo đối thoại với Timor như việc đã biết |
| Nguyên nhân Audere mất mẹ | Unresolved | Timor nhắc chuyện tin người và mất mẹ; chưa chứng minh quan hệ nhân quả |
| Timor kể đúng toàn bộ quá khứ không | Unresolved | Gắn lời quy kết với người nói, không chuyển sang giọng kể toàn tri |
| Tuổi, lịch sử cá nhân, tên riêng cô giáo | Unresolved | Không suy ra từ portrait hoặc câu Timor gọi cô đang nổi loạn |
| Hậu quả riêng của reply D1 | Unresolved | D2 nhắc tin nhắn trung tính, không mặc định gửi/từ chối trong mọi nhánh |
| Tất cả bạn trong lớp thực sự nghĩ gì | Unresolved | Giữ khác biệt giữa Audere sợ bị cười và Bianca nói chỉ thấy cô ngã |
| Ngày mai và mức ổn định sau ending | Unresolved | Không hứa mọi người luôn ở lại hoặc Audere sẽ không còn sợ |

## Phân biệt nhân vật thật và projection

**Established Canon:** `DialogueCharacterId` tách `Bianca`/`Teacher` khỏi
`BiancaDistorted`/`TeacherDistorted`; `CrowdDistorted` là một identity trình bày khác.
Đây là bằng chứng hệ thống phân biệt giọng thật với giọng bị bóp méo, không phải bản thể boss đã chốt.

Khi sửa một cue, đọc cả prefix Timor, câu méo mó, câu thật xen vào và reply Audere.
Không xóa mất sự đính chính vì nó lặp một phần từ vựng: đó có thể là trọng tâm scene.
Một số DialogueData dùng per-line portrait/glitch để đổi từ méo mó sang thật ngay trong một asset.
Giữ những override ấy khi rút câu; kiểm tra slot thay vì chỉ nhìn tên file.

**Đã xác minh:** `d3-teacher-perceived-small-task` có rightCharacter Teacher nhưng dòng đầu
override thành TeacherDistorted + glitch; câu gọi thật cuối override về Teacher + glitch.
Đây là chuyển giọng có chủ đích. Khi gộp/xóa dòng, giữ đúng override ở hai đầu chuyển ấy.

## Cầu nối không được cắt mất

- D1 nhận lời làm bảng → tin nhắn chuẩn bị đồ → D2 đi lấy đồ → D3 thực hiện trang trí.
- Sai hộp → hỏi Bianca → tối hỏi Timor có biết không → Teacher battle nhắc phản chứng hôm qua.
- Dream rơi → giật tỉnh → thiếu ngủ ở110 → được chăm sóc ở120.
- Timor rút lời nhắc → sáng D4 tự làm → cú ngã → nhờ giúp → tối đặt giới hạn cuối.
- Câu hòa giải → bước đi không lời → ánh trắng → cutscene → credits; không đảo ảnh lên trước ánh sáng.

## Branch và gameplay

| Nơi | Ý nghĩa cần giữ |
| --- | --- |
| D1 night choice | Ba hình thức rút lui; không có “đáp án tốt” bí mật |
| D2 volunteer choice | Chọn cách nói cùng một quyết định đi giúp |
| D3 teacher choice | Audere tự chọn mức bộc lộ; hỗ trợ không phụ thuộc kể hết |
| D3 Bianca choice | Xin ở lại, cảm ơn hoặc hẹn cùng về đều có quyền chọn |
| Tutorial | UI giữ input/rule chính xác; thoại giữ cảm giác được hướng dẫn |

Việc rút thoại không đổi gate combat, defeat bắt buộc, portrait, branch hoặc input.
Không biến thời gian auto-dialogue thành cảm giác “khựng” bằng thêm nhiều câu ngắn vô nghĩa.
Khoảng lặng có chức năng trước khi hỏi, sau khi ngã, sau khi Timor rút đi cần còn đủ để đọc hành động.
Theo yêu cầu Xuân, mục tiêu khoảng 20% mềm hơn nhịp cảm xúc: giữ phản hồi của người nghe ở điểm ngoặt.
Không giảm Wait/fade/auto-dialogue minima. Kiểm cả exchange vì bỏ câu vẫn có thể làm tổng nhịp gấp lên.

## Những thông tin cũ đã được loại khỏi canon hiện tại

- Ledger từng dừng ở đầu D2; các câu “open conflict/later story unresolved” không còn đúng cho scene60–150.
- Mâu thuẫn “rửa mặt/đánh răng” đã được giải quyết trong asset D1, không hỏi lại Xuân.
- Scene140 không còn là arrival-only: đã có Crowd, Bianca hỗ trợ và lời nhờ cả lớp.
- Teacher không còn chỉ xuất hiện ở thông báo D1; quan hệ chăm sóc D3 đã có.
- Bianca không còn chỉ có lời mời D1; cùng làm, hỏi/đáp, reprise và hỗ trợ D4 đã có.

## Nội dung tồn tại nhưng không tự coi là đang phát

- `Dialogue_Sample` dùng Nhật Linh; audio cũ dùng Nilah: dữ liệu legacy, không phải tên trong truyện.
- Các barks hai puzzle COOP bị bỏ và các combat cue `UNREFERENCED` không làm bằng chứng chronology.
- `CONTINUE_TOGETHER`/`CONTINUE_STEP_*` chưa được tham chiếu ở ending hiện tại.
- Các object/step mơ cũ inactive có thể còn trong scene80; sibling tồn tại chưa đủ chứng minh được chạy.
- Authoring tool có chuỗi thoại cũ không tự có quyền ghi đè DialogueData đã biên tập.

## Quy trình cập nhật khoảng trống

Ghi rõ scene/asset/cue, điều thấy được, điều đang suy ra và bằng chứng còn thiếu.
Khi Xuân chốt ý định, dùng **Design Intent**; chỉ cập nhật **Established Canon** cho phần đã có nguồn.
Khi production thay đổi, sửa dòng liên quan ở đây rồi cập nhật trang sở hữu câu chuyện.
Ledger trước đợt tổ chức lại được giữ tại [Archive](Archive/README.md), chỉ để truy vết.
