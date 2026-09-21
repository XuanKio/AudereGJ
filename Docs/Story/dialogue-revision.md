# Biên tập thoại — 2026-09-20

Chủ quản: narrative/dialogue; điều hướng qua [Story index](README.md).
Nguồn truyện: [plot](plot.md), [chronology](chronology.md), [continuity](continuity.md).

## Phạm vi và kết quả

Đối chiếu 194 DialogueData thuộc Day 1–4 được graph tham chiếu từ scene production.
Biên tập trực tiếp 126 asset, giữ GUID để scene/encounter tiếp tục dùng đúng tài nguyên.
Graph tham chiếu có thể gồm nhánh không chạy và tutorial cũ; đây không phải số chữ của một lượt chơi.

| Số đo trên cùng 194 asset | Trước | Sau | Giảm |
| --- | ---: | ---: | ---: |
| Ký tự, gồm khoảng trắng | 13.187 | 10.888 | 17,43% |
| Cụm tách bằng khoảng trắng | 3.149 | 2.612 | 17,05% |
| Dòng thoại | 594 | 561 | 5,56% |

Tiếng Việt có từ gồm nhiều âm tiết; chỉ số khoảng trắng không phải bộ đếm từ ngữ nghĩa.
Mục tiêu khoảng 20% là mềm theo yêu cầu Xuân: ưu tiên cảm xúc và nhịp đến dần.
Không diễn giải giảm chữ thành giảm tương ứng thời lượng chơi.

## Cách rút

Rút phần nhắc lại, vòng giải thích và câu dài đã có hành động minh họa.
Giữ cách xưng hô, chi tiết thật và mức hiểu biết của từng nhân vật ở thời điểm đó.
Tham khảo `t1k:human-writing` / `t1k:human-reply` để kiểm tra lời nói tự nhiên và có căn cứ;
giọng nhân vật vẫn theo [voice skill](../../.agents/skills/audere-dialogue-voice/SKILL.md).

| Chỗ cần sửa | Vấn đề về giọng/nhịp | Cách xử lý |
| --- | --- | --- |
| Audere cuối game hỏi “thì bao giờ mới thử?” trong draft | Giống thắng một cuộc tranh luận | Trở lại lời lựa chọn dè dặt: “Nhưng tớ không muốn vì thế…” / “mà chẳng bao giờ thử.” |
| Gộp lời chúc ngủ ngon D2 trong draft | Mất phản hồi của Timor trước lúc Audere rút lui | Giữ riêng “...Tớ biết”, lời đi ngủ và “Ừ. Nghỉ đi.” như nguồn gốc |
| Audere khẳng định biết Bianca là người thế nào | Kết luận vượt quá điều đang được nghe | “Timor… để tớ nghe cậu ấy.” / “Bianca đang để tớ chọn mà.” |
| Chuyển chuyện đánh răng sang ăn sáng quá nhanh | Mất chút thân thuộc đầu ngày | Giữ lời trêu nhỏ “Ừ, mở được nửa rồi.” |

## Những nhịp được bảo vệ

- D1: “Tớ…” trước “Tớ muốn thử”; Bianca tiếp nhận; tay còn run; Timor đáp rồi mới đến lời cảm ơn.
- D1 tối → D2 sáng: sự dịu giọng, ép buộc và thất bại đi theo thứ tự; giữ chào hỏi dè dặt hôm sau.
- D2: gọi Bianca, được đáp, dám hỏi có làm phiền không, nghe câu trả lời rồi mới nghi ngờ lời Timor.
- D3: thừa nhận mệt/lo, cô giáo tiếp nhận, xin phép trước khi ôm; Bianca cho Audere quyền chọn có người ở bên.
- D4: giữ đủ chín dòng hòa giải cuối: biết ơn, xin được tin, nỗi sợ bị bỏ lại, tự bước và sự đồng ý thử.

Không giảm WaitStep, fade, auto-dialogue minimum, tốc độ hiện chữ hoặc thời gian motion.
Các choice entry quan trọng giữ nguyên để nhãn lựa chọn trong scene khớp lời đáp đầu tiên.
Rút câu vẫn có thể làm exchange nhanh hơn; đã đọc cả nhịp trước/sau, không chỉ đếm ký tự.

## Liên tục và thông tin thật

- D2 dùng “hộp này ghi lớp khác” và “lấy nhầm hộp” để lỗi nhỏ có đối tượng rõ ràng.
- D3 nhắc lại giấc mơ rơi; cô giáo đáp vào sự mệt mỏi thay vì đánh giá thành tích.
- D4 nhớ Bianca giúp đứng dậy và việc Audere đã nhờ cả lớp; không tự thêm phản ứng của cả lớp.
- Timor nhắc việc từng chỉ đường, không bịa thêm một sự kiện Audere bị lạc.
- Không thêm lời xác nhận bản thể Timor, nguyên nhân mất mẹ hay việc Audere đã hết sợ.

## Giữ bản biên tập khi dựng lại scene

`DialogueData` đang được scene tham chiếu là nơi lưu lời thoại và biểu cảm hiện hành.
Literal trong tool dựng scene là nội dung khởi tạo khi asset còn thiếu.
13 helper authoring đã thêm nhánh trả về asset có sẵn, trước khi ghi text/portrait.
Nhóm này gồm Classroom, Evening, Day2 home/Bianca, Teacher aftercare, Bianca reprise,
Day4 Crowd/Timor và tutorial/narrative combat.

Riêng Evening: bước gán portrait theo chỉ số chỉ chạy trên asset vừa được khởi tạo.
Nhờ vậy chạy lại setup không áp chỉ số câu cũ lên bản thoại đã rút.
Day2School, Day2NightDream và helper Day3School đã có cơ chế giữ asset sẵn từ trước.
Migration sửa nội dung có chủ đích vẫn cần kiểm tra diff riêng; không coi guard là khóa mọi đường ghi.

Khi cần sửa thoại: chỉnh đúng DialogueData, kiểm tra cả exchange và cập nhật doc chủ quản.
Không xóa asset để lấy lại literal mặc định; không chạy broad scene builder chỉ để đổi một câu.

## Xác minh

Snapshot đối chiếu gồm toàn bộ 223 DialogueData trước/sau; mặc định serialized 0 được chuẩn hóa.
Mỗi dòng giữ speaker, character override, glitch và portrait theo source line.
Một portrait transition trong D2 check-in được chuyển sang câu kế khi bỏ câu lặp.
Các câu của bản biên tập đều trong giới hạn 42 ký tự; GUID không đổi.

Hai test dữ liệu `AllDialogueAssets_ResolveIdentities_AndCreepyArtOnlyBelongsToDistortedSpeaker`
và `RepriseAndAftercare_KeepRealPeople_NotDistortedVariants` đã pass.
Test UI `RealDialogueUI_MixedPortraits_AutoPlaybackCancelAndReplay` pass ở lượt cuối,
XML kết thúc 2026-09-20 05:59:17Z. Test cũ so với portrait mặc định; đã sửa kỳ vọng để nhận
biểu cảm `Bianca_Worried_0` được asset chỉ định từ trước, giữ việc kiểm tra chuyển distorted → thật.
Câu dài nhất bên trái 40 ký tự, bên phải 36 ký tự: TMP không overflow, chiều cao yêu cầu
94,33 nằm trong ô cao 112 ở UI scene được probe. Không đổi scale/layout trong lượt biên tập.
Toàn bộ 223 asset khớp plan về text và metadata sau khi chuẩn hóa các field mặc định bị lược trong YAML.
Kiểm tra 99 trang Docs/skill/AGENTS: dài nhất 181 dòng, 495 liên kết tương đối không hỏng.
Không coi kiểm tra asset hoặc một đoạn UI là full playthrough toàn bộ bốn ngày.

## Quản lý tài liệu

Docs đã tách theo nhiệm vụ; [Docs index](../README.md) và [Docs AGENTS](../AGENTS.md) định tuyến người sửa.
Các trang gốc giữ vai trò index và heading đích; phần lịch sử giữ nhãn checkpoint.
Mỗi trang Markdown dưới 200 dòng. Archive giữ ledger cũ, không thay thế nguồn production hiện tại.
