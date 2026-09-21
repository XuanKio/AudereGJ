# Timor finale: hồi máu, TIME và checkpoint phase

[Combat index](../06_CombatGameplay.md) · [Story context](../Story/README.md)

## Production config — Scene150

`150_D4_Home_Evening` tham chiếu
`Assets/_Audere/Data/Combat/TimorReturn/CombatEncounter_D4_TIMOR_RETURN.asset` và
`Enemy_TimorReturn.asset`. Đây là encounter duy nhất đang bật
`CombatEncounterData.phaseRecovery`; các encounter khác giữ phase break và Retry cũ.

| Phase | HP Easy | Nhịp chính |
| --- | ---: | --- |
| 1 — protection | 10 | Mưa có khe, đuôi, đổi hướng né |
| 2 — control | 12 | Hành lang chữ, Timor phân thân ngoài box, mưa có khe |
| 3 — uncertainty | 6 | Ba hình chiếu boss, ba phản đòn mỗi người, nhận giúp |

TIME tối đa author là 120 giây mỗi phase: Easy 96 giây, Hard 78,72 giây theo
`GameplayDifficultySettings`. Hard vẫn nhân HP với hệ số 1,36. Đây là **Design Intent**
về độ khó tăng dần; cần chơi tay để chốt cân bằng. Chiêu và completion gate mới ở
[Timor finale moves](timor-finale-moves.md). Checkpoint QA bên dưới thuộc phiên bản trước polish.

## Handoff giữa các phase

Khi HP phase về 0 và cue bắt buộc đã hoàn tất, controller dừng TIME và input đánh,
thu dice cũ, hủy hazard của phase và chờ board hồi pose. Sau exit cue, Timor thả
liên tục dice Heal từ dải hẹp phía trên giữa Dice Field. Tối đa bốn dice cùng lúc;
die mới xuất hiện mỗi 0,48 giây khi còn chỗ. Theo xác nhận của Xuân ngày 2026-09-20,
**mỗi lần thả thành công hồi 1 HP cho Timor**, hướng tới HP tối đa của phase kế
(có hệ số độ khó). Thanh HP tăng theo từng die; không tăng thêm khi người chơi bắt.
Chỉ bắt Heal bằng cursor mới hồi 6 giây TIME cho Audere, dần trong 0,24 giây và clamp tại tối đa.
Timor đầy HP thì ngừng thả, kể cả Audere chưa đầy TIME; Audere đầy trước cũng không dừng Timor hồi.
Die bỏ lỡ tồn tại 3 giây, mờ trong 0,4 giây cuối rồi trả pool để không kẹt giới hạn bốn viên.
Khi Timor đầy, dice đã thả vẫn được bắt cho đến khi hết hạn; hết dice và lượng hồi TIME
đang chờ thì chuyển phase. Các dice này không gây damage, không kích hoạt batch/cue hoặc reroll.
TIME không hao và không có hazard gây sát thương trong khoảng hồi.

`CombatStep` giữ index phase đã mở khóa trong cùng lần chạy StoryStep. Chỉ sau khi
hồi máu Timor hoàn tất, xử lý hết dice còn lại và vào phase mới, checkpoint mới cập nhật. Nếu thua, Retry tạo
runtime/session mới ở phase đã mở khóa, với TIME và HP phase đầy; cue của phase đó
được chạy lại. Cancel hoặc kết thúc StoryStep xóa checkpoint. Không ghi PlayerPrefs
hay giữ checkpoint qua lần vào scene mới.

Scene150 mở rộng riêng `Enemy Name` từ 220,2 lên 264 đơn vị và dịch tâm sang phải
để giữ mép trái. Vùng chữ sau padding rộng 240; TMP đo `TIMOR` cần 200,77 ở
cỡ font hiện tại. Author tool giữ đúng layout này khi chạy lại.

## Checkpoint QA cũ — trước bản hình chiếu/hồi mượt

Unity compile sạch, Console 0 error sau thay đổi scene/authoring. Hai EditMode test
`Runtime_PerPhaseDialogueGateStopsAtOneThenAdvances` và
`Runtime_CheckpointStartsAtReachedPhaseWithFreshHealth` đạt 2/2. Probe Play Scene150
đã vào phase 1 với UI, board, ba dice, 10 HP và TIME tối đa 84 giây; debug damage
đã đưa enemy vào `TransitioningPhase`. Probe bị ngắt trước khi bắt Heal, nên chưa
xác nhận đường hồi TIME, Retry checkpoint, hoặc pha tiếp theo trong Play.

Test tổng Scene150 cũ hiện dừng ở phần ending vì còn kỳ vọng năm
`CharacterMotionStep` sau khi production đổi sang một bước đi liên tục. Play test
`FinalBoss_TailMemoriesVictoryAndCreditsPlayThrough` cũng bị Unity Test Runner
ngắt khi vào Play Mode trước khi tới combat, nên không tính là bằng chứng runtime.
Cần kiểm tra Scene150: hết phase 1, nhặt Heal đến đầy, qua phase 2, chết rồi
Retry ở phase 2; lặp lại cho phase 3; Cancel trong lúc hồi và sau Retry phải
sạch dice, hazard, input claim.

## Checkpoint hồi mượt cũ — trước thay đổi điều kiện kết thúc

Heal giữa phase không chịu xác suất phá dice 30%. Lượng hồi đang chờ được dọn khi
Cancel/Retry; bản cũ chỉ cập nhật checkpoint sau khi TIME thực sự đầy. Điều kiện này
đã được thay bằng HP Timor như mô tả handoff ở trên. Kiểm chứng bản chiêu ở
[Timor finale moves](timor-finale-moves.md), không dùng checkpoint Play cũ làm bằng chứng.

## Kiểm chứng hồi máu khi thả dice — 2026-09-20

`TimorPhaseRecoveryTests` (trong `TimorMemoryRevisionTests.cs`) đạt 11/11 lời gọi
NUnit trực tiếp trong Unity Edit Mode: hai lần chuyển phase, Easy/Hard, Audere đầy/chưa
đầy TIME, không bắt dice vẫn kết thúc, dừng thả tại HP tối đa, bắt không hồi Timor lần hai,
TIME hồi mượt và clamp, Cancel khi đang hồi hoặc chờ dice cuối. Fixture bind actor
trực tiếp như scene production; Console 0 error ở lượt cuối. Thêm 37 test Crowd/Timor
hiện có và 4 test phase policy đạt: tổng 52/52. Báo cáo `Temp/CrowdPhase3QA/`.
Chưa xác minh bằng chơi tay xuyên Scene150 hoặc ending trong lượt sửa này.

Windows64 Release: `D:/PJ/AudereBuilds/Audere-CrowdP3-TimorHeal-20260920/Audere.exe`.
Build `build-d8779d3dfb` thành công trong 65,37 giây, Development=false, 0 error.
Một warning pending Editor code (fixture test vừa sửa); đã kiểm DLL trong chính build
có reset hàng phản đòn và hồi HP khi thả dice: `Temp/CrowdPhase3QA/release-proof.json`.
