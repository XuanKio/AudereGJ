# Playback, input và animation

[Mục lục nguồn](../05_DialogueSystem.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
## 4. Runtime flow

```text
Player bước vào cell
→ BoardManager.NotifyPlayerEntered
→ BoardTile chuyển đầy đủ PuzzleTileData cho DialogueTileBehaviour
→ DialogueTileBehaviour gọi GameplayUIRoot.Dialogue.Play(data)
→ DialogueController hiển thị lần lượt Left/Right
```

Playback có ba mode:

- `GlobalTimePause` là mặc định của các overload cũ và giữ hành vi `Time.timeScale = 0`.
- `CallerOwnedPause` dành cho combat. Dialogue chỉ claim input/display; caller dừng combat-local
  TIME, move và input. Phase-break đã clear dice/projectile trước; mid-phase giữ nguyên Heart,
  dice, projectile và remaining move cadence.
- `AutoAdvanceNoInput` dùng chính `DialogueUI` chuẩn cho câu nói tự chạy trong combat: không claim
  input, không block raycast và tự chuyển line theo thời lượng authored. `CombatController` giữ
  combat-local pause trong lúc sequence hiện; mode playback không dùng `Time.timeScale`.

Combat tutorial D1 lưu character dialogue tại
`Assets/_Audere/Data/Dialogue/Day1/Classroom/Combat/Dialogue_D1_COMBAT_TUTORIAL_*.asset`.
Các asset tutorial chỉ có Audere/Timor. `CombatDialogueCue` giữ direct reference và phát bằng
`CallerOwnedPause`. Câu điều khiển chính xác không nằm trong bubble mà hiện trên
`CombatTutorialUI` sau khi character dialogue đóng, tránh bắt Timor đọc documentation giao diện.

Combat thật không có bark panel riêng. Khoảng Lặng phát bằng chính `DialogueController` và cặp
`Left.prefab`/`Right.prefab`: Audere ở trái, Khoảng Lặng ở phải. Mode `AutoAdvanceNoInput` tự chạy
line, không block raycast và không claim `GameplayInputMode.Dialogue`; caller tạm dừng TIME,
projectile, dice, Heart và enemy move cho tới khi sequence kết thúc.
Modal Audere–Timor giữa trận vẫn dùng `DialogueController`/`CallerOwnedPause`, nên caller giữ
nguyên Heart, dice, projectile và move cadence rồi resume. `BackgroundTextField` chỉ lấy raw line
từ `DialogueData` làm ambient presentation, không hiển thị portrait/name và không nhận input.
Mỗi line production hiện tuân thủ budget tối đa `42` ký tự.

Post-combat D1 tiếp tục giữ cùng contract: Audere ở `Left`, Bianca/Timor ở `Right`. Các asset tại
`Day1/Classroom/PostCombat` tách ở đúng điểm có staging. `StoryIllustrationStep` không phải dialogue:
nó mở `StoryRegistrationOverlay`, chặn click phía dưới và chờ đúng một click rồi mới trả flow cho
StoryEvent; cancel gọi `ForceHide` và không giữ callback cũ.

`D1_HOME_NIGHT_MESSAGE` giữ đúng contract này: Audere luôn ở `Left`, Bianca/Timor ở `Right`.
Story dialogue dùng playback click bình thường; mười một bark Timor dùng `AutoAdvanceNoInput`,
không claim Dialogue input và giữ combat-local pause trong lúc bark hiện. Cue phase 1–10 là
`RequiredBeforePhaseAdvance`; hai line phase 11 là `RequiredBeforePlayerDefeat`, nên callback
auto-dialogue phải resolve đúng session/phase trước khi progression hoặc lethal gate mở.
Pre-combat không cắt thẳng từ lời mời sang combat: Audere đưa ra cách hiểu bình thường của tin nhắn,
Timor mở rộng một câu trả lời thành kỳ vọng của Bianca, cả lớp chờ đợi, lỗi bị ghi nhớ và chuỗi câu
hỏi không dừng. Trong bark 2/3/6/7/9, Audere tiếp tục nêu một khả năng tích cực ngắn ở `Left`; Timor
ngay sau đó bẻ nó thành nghĩa vụ hoặc hậu quả xấu ở `Right`. Đây là narrative pacing trong
`DialogueData`, không phải logic combat theo enemy ID.

Trong khi phát thoại:

- gameplay pause bằng `Time.timeScale = 0` ở `GlobalTimePause`; `CallerOwnedPause` không đổi global time;
- typewriter dùng `Time.unscaledDeltaTime` nên vẫn chạy;
- hai portrait hiện cùng lúc bằng fade ngắn, không đổi kích thước;
- trước fade-in, controller đọc speaker của line đầu tiên; người sắp nói sáng ngay từ frame đầu,
  người còn lại vào đúng trạng thái inactive, nên không còn nháy cả hai portrait ở độ sáng active;
- trạng thái mặc định của slot là chưa nói; chỉ line hiện tại hoặc line đầu sắp phát mới được
  đánh dấu active;
- người không nói được tint tối nhưng giữ nguyên scale để không lộ viền;
- mỗi lượt chỉ có bubble của người nói: bubble cũ thu/fade xuống ngắn, bubble mới bắt đầu sát đầu nhân vật rồi pop + trượt lên nhẹ; sau đó text mới chạy typewriter;
- click, `Space` hoặc `Return` hoàn tất dòng/đi tiếp;
- `Escape` đóng toàn bộ đoạn thoại;
- khi đóng, time scale trước đó được khôi phục đúng giá trị.
- `Dialogue_Text` chạy bằng AudioSource 2D riêng chỉ trong lúc `TypeLine` đang reveal chữ;
  text hiện đủ tự nhiên hoặc do click, Escape, `ForceClose`, disable và scene transition đều
  dừng source ngay nên không có tiếng gõ kéo dài trong lúc chờ người chơi đọc.

`Trigger Once` được nhớ theo `Dialogue Id` trong lifetime của `GameplayUIRoot`, nên giữ qua các gameplay scene và reset khi quay lại Main Menu.

### Thông số animation hiện tại

| Thành phần | Giá trị | Nơi chỉnh |
| --- | ---: | --- |
| Character fade-in | `0.24 s` | `DialogueController.characterEntranceDuration` |
| Độ sáng người không nói | `0.34`, alpha `1`, scale `1` | `Left.prefab` và `Right.prefab` |
| Delay trước khi bubble xuất hiện | `0.06 s` | `DialogueController.bubbleDelay` |
| Bubble pop-in | `0.20 s` | `DialogueBubble.prefab` |
| Bubble start scale | `0.78` | `DialogueBubble.prefab` |
| Bubble overshoot scale | `1.06` | `DialogueBubble.prefab` |
| Bubble trượt lên | `22 px` | `DialogueBubble.prefab` |
| Bubble pop-out | `0.09 s` | `DialogueBubble.prefab` |
| Tốc độ typewriter | `42 ký tự/s` | `DialogueController.charactersPerSecond` |

Các animation dùng `Time.unscaledDeltaTime`. Portrait không được scale khi đổi speaker; chỉ `DialogueBubble` được scale để tạo hiệu ứng pop.

<!-- END PRESERVED SOURCE -->
