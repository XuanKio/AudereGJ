# Timor finale — chiêu, đường né và cao trào

[Combat](../06_CombatGameplay.md) · [TIME/Retry](timor-final-checkpoints.md) · [Ending](../Story/bosses-and-ending.md)

## Phạm vi 2026-09-20

**Design Intent của Xuân:** Timor can thiệp dice, phân thân ngoài viền như Bianca;
chỉ một Battle Box. Hành lang ngang kéo dài vượt cạnh màn hình, chữ tiêu cực lao
về Heart. Cô giáo, Bianca và mọi người động viên Audere sau khi vượt qua hình chiếu.
Không xác lập người ngoài nhìn thấy combat hay Timor.

Authoring: `Audere/Combat/Polish Timor Finale` (`TimorFinalPolishAuthoring`).
Chỉ cập nhật encounter TimorReturn, FinalMoves, prefab đạn riêng và support dialogue;
không dựng lại scene hoặc kết đường tile/credits. Full author gọi lại polish.

## Phân bổ mới

| Phase | Thứ tự OrderedLoop | Nhịp |
| --- | --- | --- |
| 1, 10 HP Easy | OpeningRhythm → TailThrow → CrossRhythm | Mưa có khe, đuôi, đổi hướng né |
| 2, 12 HP Easy | WordCorridor → ClockwiseClones → PressureWaves | Chạy hầm → vây bắn → khe dịch chuyển |
| 3, 6 HP Easy / 9 Hard | MemoryTeacher → MemoryBianca → MemoryCrowd → SupportedWaves | Vượt hình chiếu, nghe động viên, tự bước |

Hành lang chỉ xuất hiện phase 2. Các asset DiceBreak, ReleaseChoice, PeakClones và
SupportedCorridor cũ không còn được tham chiếu trong moveset production.
Lead-in mới 0,35s; recovery chung tối thiểu 0,4s. TIME author 120s: Easy 96,
Hard 78,72; hit mất 3s, bất tử sau hit 0,85s. Thoại và hồi giữa phase dừng TIME.
Phase 3 có chín phản đòn bắt buộc, đủ hạ HP cả hai độ khó; gate giữ HP1 đến đoạn cuối.

## Luật chiêu

- **Phá dice ngẫu nhiên:** mỗi lần bắt dice thường trong combat có xác suất 30%
  bị đuôi chặn trước khi nhận hiệu ứng. Không còn một lượt DiceBreak cố định.
  Mảnh vỡ không gây sát thương, giữ màu gốc; hủy/đổi phase dọn hiệu ứng.
  Heal hồi giữa phase và dice phản công thuộc chiêu riêng được bảo vệ.
- **ClockwiseClones (11,6s):** bốn Timor nguyên sprite/màu/kích thước gốc, bay
  ngoài field với dư ảnh đen/trắng như cô giáo. Field còn khoảng 50%×45%, có nhịp thở.
  Thứ tự 0→1→2→3 rồi 3→2→1→0; nghỉ đảo chiều 0,65s, không xóa đạn đang bay.
  Mỗi lượt khóa hướng, báo 0,65s, bắn hai fan 5 viên cách nhau 0,14s; góc kề 13°,
  tốc 265, đạn nguyên prefab 24×24. Cặp fan cùng hướng, không bám Heart sau khóa.
- **WordCorridor (16s):** field cao 82%, hai cạnh bên rời viewport; Timor lớn
  nguyên art bay phía trên. Heart kéo về vùng trái, vẫn rê lên/xuống tự do.
  Chữ Mynerve-Regular SDF trực tiếp, không nền; tốc 960, báo 0,6s, ba chữ/train.
  Train beat 0,34s; wave tối thiểu 1,9s và được giãn theo khoảng né an toàn thực tế.
  Chừa một trong ba lane; chỉ chuyển lane kề. Attack chờ tại Heart nếu bỏ lỡ.
  Tay móc chọn cạnh không chặn lane an toàn; báo trước rồi móc/rút.
  Dọn hazard trước khi thu field về, không ép người chơi vào đạn ở đoạn kết.
- **PressureWaves:** bốn hàng mỗi cụm, field đổi kích thước và dao động nhẹ;
  khe nối tiếp có thể theo được. Opening 8,4s, Cross 8,4s, Pressure 9,5s.
  SupportedWaves 10,5s nới khe và hiện lời động viên, chỉ một Battle Box.
- **Ba hình chiếu:** cô giáo bên trái chạy trọn GeometrySketch + SpiralSketch;
  Bianca bên phải chạy trọn RibbonFanSweep; đám đông phía trên chạy trọn
  MountDiveCounter + DiagonalHands. Dùng trực tiếp asset chiêu phase 2 của boss gốc.
  Mỗi hình chiếu cần đúng ba Attack; đánh đủ sớm không cắt ngang bài đòn.
  Với đòn exclusive của cô giáo, dice phản công đến sau bài né. Hình chiếu chờ
  người chơi bắt đủ, mờ đi 0,75s rồi mới phát cue động viên hiện có.

Timor tắt flash đổi màu khi trúng đòn (`suppressHitFlash`); phản hồi chuyển động
và damage còn hoạt động. Đuôi không có rim tím hoặc nhấp nháy alpha báo đòn.

## Ownership và cue

`CombatMoveStage` sở hữu root và lease đạn; Cancel/phase break/board disable dọn chúng.
Projection dùng actor tạm cùng enemy mount để chiêu gốc chuyển động đúng; tạm ẩn
Timor và khôi phục khi hủy/kết thúc. Không clone Battle Box hoặc controller.
Delta0 không chuyển động, sinh đạn hay dice. Projection không tự hết hạn nếu chưa đủ hit.

`MoveCompleted` chỉ phát khi hoàn tất tự nhiên, không phát khi cancel/reaction.
Phase2 chờ PressureWaves. Ba cue support phase3 gắn với completion hình chiếu tương ứng;
Victory chờ cả ba và SupportedWaves. Burst damage giữ HP1 và tự đi tiếp khi gate mở.

## Narrative

SUPPORT_TEACHER / SUPPORT_BIANCA / SUPPORT_TOGETHER giữ direct DialogueData references.
Hai cue đầu gọi lại lời chăm sóc D3/D4; cue cuối là Audere nhớ việc nhờ cả lớp.
“Các bạn: Tụi mình cùng làm nhé.” là Design Intent của support move, chưa xác lập
một bạn cụ thể đã nói trong Scene140. Giữ hòa giải sau Victory và ranh giới tự bước.

## Kiểm chứng bản sửa

29/29 kiểm tra gọi trực tiếp bằng NUnit trong Unity Edit Mode đã đạt sau sửa hình chiếu.
Kết quả `Temp/TimorPolishQA/revision20-tests.txt`: đủ ba hit, chạy trọn signature,
không hết hạn khi chưa phản đòn, Cancel khôi phục actor; phase-only corridor;
hồi TIME theo từng dice và clamp; đuôi 30%; hai fan năm viên; gate Victory.
Bốn route mưa qua nhiều hàng chồng nhau đạt không hit. Preview ba hình chiếu và clone:
`Temp/TimorPolishQA/revision20-*.png`. Đây là preview riêng, không phải chơi xuyên trận.
Corridor bản cuối đạt 13/13, gồm sáu route né thực, mật độ/arrival, pause/cancel/exit
và font Mynerve đúng material. Chữ chỉ hiện lúc bắt đầu bay để tránh chồng lúc báo đòn.
Kết quả: `Temp/TimorPolishQA/revision20-corridor-tests.txt`. Tổng hai nhóm: 42/42.
Kết quả/ảnh trước revision20 không thay thế bằng chứng của bản này.

Windows64 Release: `D:/PJ/AudereBuilds/Audere-Timor-Phases-20260920/Audere.exe`.
Build `build-d9548c8555` succeeded, 91,14s, 0 errors; BuildReport Development=false.
Một warning về pending Editor compilation xuất hiện sau refresh dữ liệu PerformanceTest.
Đã đọc DLL trong chính bản build bằng Cecil: có hồi mượt, sabotage, projection với
CanvasGroup null fix, fan mới và BeginTrain cộng warningDuration trước phát chữ;
bằng chứng `Temp/TimorPolishQA/release-method-proof.txt`. Không ghi nhận Play toàn ending.
