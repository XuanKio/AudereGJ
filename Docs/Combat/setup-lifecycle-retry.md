# Setup, Story integration và Retry

[Mục lục nguồn](../06_CombatGameplay.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
## 8. Setup và debug QA

`Audere > Combat > Setup Combat Foundation` là tool idempotent: migrate/tạo prefab, cấu hình sample encounter, bind hierarchy và save scene.

Các lệnh Play Mode phục vụ kiểm thử:

- `Apply Attack Dice`, `Apply Shield Dice`, `Apply Heal Dice`.
- `Take Player Hit`.
- `Expire Timer`.
- `Preview Enemy White Flash`.

Lưu ý: `Audere > Dialogue > Preview Sample` chủ động pause `Time.timeScale` khi dialogue mở. Đây là hành vi của Dialogue overlay; đóng/skip dialogue trước khi đánh giá timer combat.

## 9. Lifecycle và Story integration (2026-08-22)

`CombatController.Play(encounterData, callback)` trả một trong:

```text
Victory, Defeat, Cancelled, Special
```

Controller không tự load scene, mở dialogue hoặc chọn ending. Callback one-shot, session cũ không sống sang lần Play mới; `Cancel()` và disable giữa chừng trả `Cancelled`, còn `ResetEncounter()` phục vụ replay/test.

`CombatStep` map kết quả bằng Inspector. Mặc định:

```text
Victory → Complete
Defeat  → Retry
Special → Complete
```

Với combat cốt truyện yêu cầu Audere thua, đặt `Defeat Behaviour = Complete`; Story chạy tiếp
mà không hiện retry. Với combat phải thắng, `Retry` giữ StoryEvent đứng tại CombatStep và dùng
`GameplayUIRoot/CombatRetryUI`. Overlay là nested Screen Space Overlay Canvas, sorting order
`1200`, có blocker toàn màn hình và nằm ngoài world-space board/fullscreen shader. `ForceHide()`
xóa owner/callback cũ khi scene load, Story cancel, retry mới hoặc root bị hủy.

### Shared Heartbreak Retry và Heart trắng — 2026-09-19

Toàn bộ chữ dưới `Retry Content` (lời nhắn và nút “Thử lại”) dùng `Assets/_Audere/AssetGame/Font/Mynerve-Regular SDF.asset` cùng material của font. Shared `GameplayUIRoot` và UI riêng Scene60 đã đồng bộ; các scene combat 30/40/120/140/150 kế thừa prefab chung. Authoring giữ font này cả khi Retry đã đủ references. Đã kiểm tra serialized bindings và build C# 0 lỗi; chưa xem lại font trong Play Mode.

`Assets/_Audere/Data/Combat/CombatRetryPresentation.asset` là nguồn dùng chung cho lời nhắn,
thời điểm tiếng rắc, chuyển động hai nửa Heart và tốc độ hiện chữ. Khi kết quả được map sang
`Retry`, Heart Visual trắng giữ vị trí, kích thước và góc trên màn hình tại thời điểm thua,
rồi tách sprite hiện có thành hai nửa theo đường nứt. Controller chụp `CombatHeartScreenPose`
trước khi cleanup có thể reset cursor/Battle Box. Với outcome cho phép Retry, controller
ẩn Heart/cursor gốc, chờ cuối frame để chụp nền combat còn nguyên rồi mới dọn runtime.
`CombatStep` chuyển quyền sở hữu texture sang Retry; nền chụp nằm dưới lớp đen và được
giải phóng khi đen hoàn toàn hoặc khi hide/cancel. Sau tiếng rắc 0,3 giây, lớp nền đen
bắt đầu fade trong 1,15 giây; hai nửa Heart tiếp tục rơi, mờ cùng nhịp và tắt hoàn toàn.
Timing nằm trong shared profile; fade dùng smoothstep và unscaled time.
Tiếng rắc dùng `AudioId.Player_HeartBreak = 5007`, tham chiếu
clip tự tạo `Assets/_Audere/Audio/HeartBreak.wav` qua `AudioCatalog`.

**Design Intent (Xuân):** lời nhắn UI là “Audere, cùng thử lại nào.\nMình tin cậu sẽ làm được mà.”
(xuống dòng sau câu đầu), với chữ “Thử lại” bên dưới. Người nói chưa được xác định; lời nhắn
không thiết lập thêm canon. Cả nhóm lời nhắn/nút nằm giữa màn hình; cỡ chữ52/48,
lời nhắn hiện dần sau khi nền đã đen và Heart ẩn hoàn toàn thêm0,25giây;
nút Retry nằm bên dưới lời nhắn và chỉ hiện, nhận input
khi toàn bộ lời nhắn đã hiện. Các kết quả thua theo cốt truyện vẫn do `CombatStep` và outcome
rules đã author quyết định; `Defeat Behaviour = Complete` tiếp tục Story như trước.

`CombatRetryView` giữ modal input và mute nhạc trong lúc overlay mở, dùng unscaled time
cho phần trình bày. Retry nhận callback một lần rồi dọn trước khi bắt đầu attempt mới.
Hide/cancel/disable hoặc mất owner đều xóa callback, trả input/music ownership, dừng tiếng
rắc và bỏ button selection; `ForceHide()` không tự gọi Retry.

Lượt sửa2026-09-20: **13/13 EditMode tests đạt**, gồm trình tự rơi/fade/ẩn, giải phóng
backdrop, input/callback và ảnh16:9,4:3,ultrawide. Probe trên combat Scene140 xác nhận
capture trước cleanup, nền còn nguyên trong Retry, Heart ẩn trước lúc sẵn sàng và ForceHide
dọn sạch; Console0error. Kết quả/ảnh: `Temp/CombatRetryQA/retry-fade-tests.json`,
`production-probe.txt`, `production-*.png`. Probe gây thua bằng debug, không phải lượt chơi tay.

Lệnh `Audere/Combat/Setup Heartbreak Retry` cập nhật shared UI, profile, audio và các direct
reference còn thiếu, đồng thời migrate Heart màu thường về trắng ở prefab và cả sáu scene
combat: `30_Classroom`, `40_Evening`, `60_D2_School_Morning`, `120_D3_School_Teacher`,
`140_D4_Classroom`, `150_D4_Home_Evening`. Migration bao gồm UI local của Scene60 và Heart
local của Scene150; đổi riêng prefab không đủ cho các bản local/override này.
`CombatPlayerView.normalColor` và màu `Image` của Heart đều trắng; hit feedback vẫn có màu
riêng và trở về trắng khi kết thúc.

<!-- END PRESERVED SOURCE -->
