# Lớp học: cơ hội phát biểu và Khoảng Lặng

[Mục lục nguồn](../../09_Day1_ProductionStoryWorkflow.md) · [Bản đồ docs](../../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
### 4.3 D1_CLASSROOM_ANNOUNCEMENT — cơ hội đầu tiên bị khép lại

**Scene:** `30_Classroom`  
**Event:** `D1_CLASSROOM_ANNOUNCEMENT`  
**Auto-next:** `D1_CLASSROOM_RECESS_BIANCA`.

**Primary story job:** cho người chơi thấy cùng kiểu “giúp Audere đỡ phải nghĩ” bắt đầu lấy
mất một lựa chọn nhỏ của cô, nhưng Timor vẫn nghe như đang quan tâm.

**Established Canon:**

- Scene bắt đầu dưới overlay đen, normalize presentation rồi đặt Audere tại chỗ ngồi cũ.
- Timor xác nhận chỗ ngồi quen thuộc và nói mọi thứ ổn.
- Giáo viên xuất hiện, ổn định lớp và thông báo buổi liên hoan cuối năm.
- Lớp sẽ cùng chuẩn bị trang trí, đồ ăn và trò chơi.
- Giáo viên cho mỗi học sinh chọn một việc vừa sức, không cần vội hoặc làm thật nhiều.
- Audere chú ý tới phần trang trí; interest tile hiện và cô nhích về phía trước.
- Timor hỏi liệu cô có thích không. Audere ban đầu không biết, rồi thừa nhận: “Chắc là có.”
- Timor công nhận thích phần đó cũng không sao, nhưng nói hai người chưa cần ghi tên ngay.
  Cậu nhận xét Audere chưa ngồi yên phút nào từ sáng, bảo cô nghỉ và để chuyện đó tính sau.
- Audere quay về chỗ cũ, interest tile biến mất và cô đáp: “…Ừm.”

**Dialogue polish:** announcement được chia thành bubble ngắn, mỗi bubble chỉ giữ một ý:
ổn định lớp → báo chuyện vui → liên hoan → các phần việc → lựa chọn vừa sức → không cần vội.
Không nhét toàn bộ lời dặn và reassurance vào cùng một line.

**Relationship movement:** từ `Trusted guidance` chạm sang `Protective pre-emption`. Timor
không đe dọa hay công khai áp đặt; cậu đóng lựa chọn bằng lý do nghỉ ngơi hợp lý.

Hierarchy đã xác nhận:

```text
D1_CLASSROOM_ANNOUNCEMENT [StoryEvent]
├── 00_CoverScene                   [CanvasFadeStep: instant opaque]
├── 05_NormalizePresentation       [SetActiveStep]
├── 08_PlaceAudereAtSeat           [MoveActorStep: snap]
├── 10_FadeIn                      [CanvasFadeStep: 0.65s]
├── 20_SeatDialogue                [DialogueStep]
├── 30_ShowTeacher                 [SetActiveStep]
├── 40_TeacherOpening              [DialogueStep]
├── 50_AnnouncementPause           [WaitStep: 0.5s]
├── 60_TeacherEvent                [DialogueStep]
├── 70_ClassMurmur                 [PlayAudioStep: Classroom_Murmur]
├── 80_MurmurBeat                  [WaitStep: 0.35s]
├── 90_TeacherDetails              [DialogueStep]
├── 95_ClassSettles                [WaitStep: 0.2s]
├── 100_NoticeDecoration           [DialogueStep]
├── 110_RevealInterestTile         [BoardTileTransitionStep]
├── 120_AudereLeansForward         [MoveActorStep: 0.16s]
├── 130_TimorAsks                  [DialogueStep]
├── 140_AudereSmallHop             [CharacterMotionStep: vertical in-place]
├── 160_AudereAdmits               [DialogueStep]
├── 170_TimorClosesChoice          [DialogueStep]
├── 180_AudereStops                [WaitStep: 0.28s]
├── 190_TimorProtects              [DialogueStep]
├── 200_ReturnToSeat               [MoveActorStep: 0.2s]
├── 210_HideInterestTile           [BoardTileTransitionStep]
└── 220_AudereYields               [DialogueStep]
```

Teacher hiện dùng prefab/visual placeholder. Tên riêng, portrait chính thức, tuổi và lịch sử
của cô vẫn là **Unresolved**.

<!-- END PRESERVED SOURCE -->
