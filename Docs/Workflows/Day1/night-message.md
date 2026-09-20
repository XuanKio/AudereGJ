# Tin nhắn buổi tối và scripted defeat

[Mục lục nguồn](../../09_Day1_ProductionStoryWorkflow.md) · [Bản đồ docs](../../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
### 4.5 D1_HOME_NIGHT_MESSAGE — lời mời thứ hai và scripted defeat

**Scene:** `40_Evening`

**Event:** `D1_HOME_NIGHT_MESSAGE`
**Primary story job:** cho thấy khi Audere muốn tự trả lời một lời mời bình thường, nỗi sợ mất cô
khiến Timor chuyển từ cảnh báo sang bắt cô phải nghe lời; combat là hành động khóa lựa chọn đó.

```text
00_NormalizeMessageAlert
→ 10_FadeIn
→ 20_AudereAfterLongDay
→ 30_PlayMessageArrival
→ 35_HoldForMessage
→ 40_ShowMessageAlert           [dauchamthan]
→ 45_HoldMessageAlert
→ 50_AudereStartles             [VerticalInPlace: 0.19 s, arc 0.09]
→ 55_HoldAfterStartle
→ 60_AudereRecognizesBianca     [“Bianca nhắn cho tớ này.”]
→ 65_HideMessageAlert
→ 70_BiancaNightMessage
→ 80_TimorQuestionsHer
→ 90_KeepSilence
→ 100_AudereAndTimorConclude
→ 110_HoldBeforePressure
→ 120_EnterNightPressure        [Dreamy Disorientation]
→ 130_PlayTimorNightPressure    [Defeat-only CombatStep]
→ 140_ReturnToEvening           [neutral fade]
→ 145_HoldAfterReturn
→ 150_TimorNarrowsTheReply
→ 160_ChooseBiancaReply         [3 nested StoryEvent branches]
→ 170_HoldAfterReply
→ 180_LightsOut
→ 190_DayOneEnds                [“Ngày 1 - Kết thúc”]
```

Night Tile nằm đúng tâm camera; Audere cùng trục X với tile và dùng staging scale giống Scene 30.
Body giữ sorting order `5`, shadow `4`. `dauchamthan` là child presentation của Audere, mặc định
ẩn và dùng `Player/6`; authoring tool bảo toàn transform/art đã đặt trong scene. `MessCome.mp3`
được map bằng stable `AudioId.Message_Arrive`, sau đó alert hiện, Audere giật mình theo Y, nhận ra
Bianca và chỉ khi ấy nội dung tin nhắn mới mở. Audere luôn ở dialogue slot trái; Bianca/Timor ở phải.

Nhịp trước combat phải giữ quan hệ nhân-quả nhìn thấy được:

```text
Bianca đưa một lời mời bình thường
→ Timor lo Bianca chỉ tìm Audere để nhờ vả
→ nỗi sợ hiện ra trực tiếp và Timor viện chuyện mẹ Audere như bằng chứng
→ Audere tách hai chuyện ra và nói cô vẫn muốn trả lời
→ Timor chuyển từ khuyên sang cấm, rồi yêu cầu Audere phải nghe lời
→ Audere miễn cưỡng nhưng nói “Lần này, để tớ tự trả lời.”
→ Timor đáp “Tớ không thể để cậu làm vậy.”
→ Dreamy Disorientation biến việc ngăn Audere trả lời thành combat
```

Combat dùng policy `CapturedDiceBatchSequence`: phase 1–10 mỗi phase đúng một batch Attack,
Shield, Heal và chỉ tiến khi cả ba được catch cùng bark bắt buộc đã resolve. Timor có `36 shared
HP`; damage vẫn có feedback nhưng không tạo Victory. Player bắt đầu `66 TIME`, không thể Defeat
trước phase 11. Finale không có dice, nâng TIME còn lại lên floor `30 s`, kéo Heart mềm về tâm,
đợi hai câu cuối tự chạy xong rồi volley thật mới được phép kết liễu. Encounter chỉ cho Defeat và
tắt Retry; CombatStep map Defeat thành Complete, Victory/Special thành Fail.

TIME về `0` không trả Story ngay. Bullet/laser dừng collision và vận tốc, fade `0.62 s`, enemy
actor Timor còn lại trên board trong đoạn `Audere: … → Timor: Thấy chưa → ... → Audere: …Ừ`.
Portrait Sad và nhịp câu ngắn giữ `Thấy chưa` như một kết luận lo buồn, không phải chiến thắng.
Sau callback dialogue, runtime mới cleanup và neutral fade đưa cảnh về phòng.

Trong phòng, Timor nói `Không cần ép mình` rồi thu lựa chọn của Audere về “câu dễ nhất”. Choice UI
ở vùng path-piece có ba dòng; idle mờ/nhỏ, hover thêm `> <` và sáng/đủ scale. Ba nested branch:

1. Tránh hẳn: Timor xác nhận ngày mai không phải lo; `Đã gửi` hiện trước câu `…Ừ` của Audere.
2. Trì hoãn: `Đã gửi`, Timor bảo để mai nếu thấy ổn thì tính tiếp.
3. Không trả lời: giữ im lặng lâu hơn, Timor coi im lặng là câu trả lời; Audere vẫn hỏi Bianca sẽ
   nghĩ gì, nhưng Timor khép lại bằng `Ngày mai rồi tính` và `Nghỉ thôi`.

Mọi branch quay lại cùng `LightsOut`; overlay đen sau đó hiện `Ngày 1 - Kết thúc`. Branch là flow
cục bộ trong scene, chưa tạo StoryState/save flag xuyên scene.

Mười một bark tăng sức ép theo bốn tầng: (1) bảo vệ và yêu cầu đứng yên; (2) phủ nhận khả năng Audere
tự đặt giới hạn; (3) biến lựa chọn khác Timor thành không tin hoặc bỏ rơi Timor; (4) khóa câu trả lời.
Audere vẫn có các câu ngắn ở slot trái để người chơi nghe được sự chống lại của cô. Nhịp 2 là laser
dọc board, nhịp 8 là laser quét, nhịp 10 là laser con lắc; tất cả telegraph trước khi có collision.
Projectile nằm dưới mask inset của board nên không vẽ đè lên viền.

Portrait Timor là presentation của DialogueUI, không phải enemy sprite. Question bắt đầu bằng
`TimorLolang`, chuyển sang `TimorLoLangKhongVui` khi Timor thừa nhận sợ; conclusion chuyển sang
`TimorTucGian` tại `Không được đâu, Audere`. Bark 1–3 dùng Worried, 4–6 dùng WorriedUneasy,
7–10 dùng Angry và bark 11 dùng Sad. Enemy prefab vẫn giữ visual placeholder riêng.

**Design Intent:** đây là điểm đầu tiên Timor mất bình tĩnh vì Audere không làm theo. Cậu vẫn tin
mình đang bảo vệ cô, nhưng nỗi sợ chuyển thành cấm đoán và bắt phục tùng. Chi tiết mẹ Audere từng
tin người khác rồi Audere mất bà đang được dùng theo yêu cầu của scene này ở mức `Design Intent`,
chưa tự động trở thành `Established Canon` cho các scene khác. Đây là vòng lo cụ thể của Audere,
không phải mô tả lâm sàng áp cho mọi người có rối loạn lo âu. **Unresolved:** ontology combat,
final art của phòng, final moveset/balance và nghĩa tâm lý cuối cùng.

<!-- END PRESERVED SOURCE -->
