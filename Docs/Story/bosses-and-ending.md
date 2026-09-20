# Boss và ending — công việc kể chuyện

Đối chiếu production 2026-09-20. Nguồn nhịp cảnh: [chronology](chronology.md).
Balance, phase threshold, collision và UI thuộc [combat docs](../06_CombatGameplay.md).
Trang này giữ trigger → áp lực → sự thay đổi trong lựa chọn của Audere.

## Quy tắc đọc combat

**Established Canon:** game trình bày những cuộc đối đầu trong luồng Story/Combat.
**Design Intent:** boss biểu hiện một dạng áp lực tâm lý cụ thể và làm thay đổi nhịp kể.
**Unresolved:** cơ chế thế giới khiến combat xuất hiện, người ngoài có thấy nó không,
và bản thể của mọi hình dạng méo mó. Không lấy sprite, số đạn hoặc tên asset làm lời giải.

## 1. Khoảng Lặng — scene30

- **Kích hoạt:** Bianca chờ câu trả lời; Audere muốn nói nhưng sợ phản ứng và muốn trốn.
- **Áp lực:** đã im quá lâu, nói lúc này sẽ kỳ, đồng ý sẽ phải làm nhiều hơn.
- **Established Canon:** Timor vẫn giúp trong tutorial; Audere giữ lại ý muốn thử.
- **Bước ngoặt:** sau Victory, cô trả lời Bianca và tự ghi tên trong khi tay vẫn run.
- **Design Intent:** tạo lần thành công nhỏ đầu tiên, đủ để người chơi thấy một lựa chọn có thể thuộc về cô.

Nguồn: `30_Classroom/D1_CLASSROOM_RECESS_BIANCA`, Day1/Combat và Guided DialogueData.
Không gọi tutorial/boss là chứng minh Audere đã hết lo hay Timor chưa từng ngăn cản.

## 2. Timor đêm đầu — scene40

- **Kích hoạt:** Audere muốn trả lời tin nhắn Bianca; Timor muốn ngăn lựa chọn đó.
- **Áp lực:** bảo vệ → bắt nghe lời → sợ bị thay thế → không cho trả lời.
- **Established Canon:** encounter dẫn đến Defeat có chủ ý, không mở Retry như battle thường.
- **Bước ngoặt:** sau khi kiệt sức, Audere chỉ còn chọn từ chối, hoãn hoặc im lặng.
- **Design Intent:** cho thấy cảm giác nhẹ đi có thể đi kèm việc nhường quyền quyết định.

Nguồn: `Data/Combat/TimorNightPressure`, `40_Evening/D1_HOME_NIGHT_MESSAGE`.
Timor dịu lại sau trận không có nghĩa xung đột đã được giải quyết.
Việc Timor nhắc mẹ chứng minh điều cậu ấy đã nói, chưa xác nhận nguyên nhân mất mẹ.

## 3. Bianca bị diễn giải — scene60

- **Kích hoạt:** Audere cùng lấy đồ nhưng mang nhầm hộp.
- **Áp lực:** một lỗi nhỏ bị diễn thành “phiền”, chậm việc và hối hận vì rủ cô đi.
- **Established Canon:** `BiancaDistorted` là identity trình bày khác `Bianca` thật.
- **Bước ngoặt:** Audere hỏi; Bianca không trách và đề nghị sửa cùng, khác điều Timor dự đoán.
- **Design Intent:** lần đầu Audere đối chiếu một suy đoán với chính người bị suy đoán.

Nguồn: `Data/Combat/BiancaSupplies`, Day2/School, Combat, PostCombat DialogueData.
Các phase là mức áp lực và phản ứng khác nhau, không biến Bianca thật thành người tấn công Audere.
Lời gọi tên thật chen vào suy nghĩ cần còn đọc được khi rút thoại.

## 4. Cô giáo bị diễn giải — scene120

- **Kích hoạt:** thiếu ngủ/chóng mặt khiến Audere phải nghỉ; cô giáo đến chăm sóc.
- **Áp lực:** được chăm sóc bị hiểu thành làm phiền, khiến lớp chờ, mất cơ hội tham gia lần sau.
- **Established Canon:** Timor gợi suy nghĩ; Audere nhắc lại trải nghiệm với Bianca để bác sự chắc chắn ấy.
- **Bước ngoặt:** cô muốn nghe cô giáo và tự chọn mức chia sẻ; giáo viên cho nghỉ, hỏi trước khi ôm.
- **Design Intent:** Audere có thể mệt, cần giúp và chưa kể hết mà vẫn được đối xử tử tế.

Nguồn: `Data/Combat/Teacher`, Day3/TeacherCombat và TeacherAfterCombat.
Không rút mất liên kết “hôm qua Bianca” vì đây là bằng chứng cho năng lực phân biệt mới hình thành.
Lời cô giáo thật nhẹ nhàng; lời projection đòi hỏi không phải tính cách thật của cô.

## 5. Bianca reprise — scene120, sau đoạn cô giáo

- **Kích hoạt:** Bianca đến hỏi Audere muốn có người ở lại hay muốn một mình.
- **Áp lực:** Timor gán việc cô đến cho nghĩa vụ hoặc thương hại.
- **Established Canon:** encounter không sinh dice, không cho người chơi Defeat;
  áp lực tự giảm và cue lời thật phải hoàn tất trước Victory.
- **Bước ngoặt:** Audere nghe lời mời có quyền chọn; ba nhánh đáp đều không ép cô phải thân mật hơn.
- **Design Intent:** tái dùng hình bóng quen thuộc để cho thấy cách nghe đã đổi, không phải trận thắng thứ hai nhờ đánh mạnh hơn.

Nguồn: `Data/Combat/BiancaReprise`, `D3_BIANCA_REPRISE_AND_SILENCE`.
Sau đó Timor nói bị bỏ lại và rút hướng dẫn ngày mai; sự im lặng ở130 có nguyên nhân cụ thể.

## 6. Đám Đông — scene140

- **Kích hoạt:** Audere ngã khi mang đồ, sợ mọi người đang nhìn và cười.
- **Áp lực:** ánh nhìn, nhiều bàn tay, bị giữ lại hoặc không còn khoảng trống để bước.
- **Established Canon:** Bianca hỏi cô có đau không; Audere nhận ra câu hỏi ấy khác lời buộc tội đang nghe.
- **Bước ngoặt:** sau trận, cô nhận giúp đỡ rồi tự hỏi cả lớp có thể giúp không.
- **Design Intent:** một lần tự làm không thành công vẫn có thể dẫn đến kết nối, không buộc phải quay về tránh né.

Nguồn: `Data/Combat/Crowd`, `140_D4_Classroom/D4_CLASSROOM_CROWD`.
Chừa lối né trong gameplay giữ ý “còn có cách đi”; pattern dày không có nghĩa câu chuyện khẳng định không có lối thoát.
Scene kết sau lời nhờ giúp; không tự thêm lời cả lớp trả lời, chế nhạo hoặc ăn mừng.

## 7. Timor cuối — scene150

- **Kích hoạt:** Audere kể mình vẫn làm được và đã nhờ giúp; Timor sợ cô không cần mình.
- **Áp lực:** nhắc công lao bảo vệ, cảnh báo thất bại, giữ/kéo cô, đưa lại bóng Bianca/cô giáo/đám đông.
- **Established Canon:** các lời đáp chuyển từ không thể biết trước sang vẫn muốn thử dù còn sợ.
- **Bước ngoặt:** Audere ghi nhận Timor nhưng đòi được tự bước; Timor nói sẽ thử tin.
- **Design Intent:** kết lại quyền chọn, không phải trục xuất một phần cảm xúc hay hạ một kẻ ác thuần túy.

Nguồn: `Data/Combat/TimorReturn`, Day4/TimorFinal và encounter của `160_TimorAgain`.
Lời hòa giải nằm trong victory presentation, trước khi Story mở đường kết.
Giữ cả hai vế: tình cảm còn đó và quyền quyết định phải thay đổi.

## Ending hiện tại

Nguồn: [scene150](../../Assets/_Audere/Scenes/150_D4_Home_Evening.unity),
[TileRippleWalk_Ending](../../Assets/_Audere/Data/Transitions/TileRippleWalk_Ending.asset),
`ContinuousTileWalkStep`, `CreditsPreludeStep`, `CreditsDodgeStep`.

1. **Established Canon:** Victory → ẩn gameplay UI → trở lại Story, tắt mask và bật camera follow.
2. Audere đứng lên, đi sang phải; bản production hiện tại mở **một hàng tile trắng** liên tục với ripple.
3. Ánh trắng tăng dần rồi mới chuyển ảnh cutscene; không chèn bài diễn văn vào đoạn bước đi.
4. Credits trôi từ trên xuống, dừng/rung khoảng 1,5 giây; Heart xuất hiện trên nền đen toàn màn hình.
5. Né bằng chuột khoảng 30 giây; dòng chữ truy đuổi, tách thành chữ/đạn; hết đoạn tự về `10_MainMenu`.

Credits dùng nhạc nền với boost được author, không mở một encounter chiến đấu mới.
**Design Intent:** đoạn chơi cuối giữ sự chuyển động và kết nối với ngôn ngữ combat sau khi xung đột đã khép.
Không diễn giải việc bị chữ credit chạm là Audere tái phát, thua hòa giải hay mất ending.

## Điều ending không xác nhận

- **Unresolved:** cuộc sống sau ngày4, Timor có giữ lời lâu dài không, người khác sẽ phản ứng thế nào lần tới.
- **Unresolved:** bản thể Timor và việc ai trong thế giới truyện nhìn thấy combat.
- Asset `CONTINUE_TOGETHER`/`CONTINUE_STEP_*` đang không tham gia ending production; không trích như thoại đã phát.
- **Design Intent:** “đi tiếp dù còn sợ” không phải lời hứa chữa khỏi hoàn toàn.
