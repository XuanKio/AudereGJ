# Capture sequence, Timor ban đêm và nhạc

[Mục lục nguồn](../06_CombatGameplay.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
## 11. Captured-dice sequence và D1 Timor night pressure (2026-08-25)

`CombatPhasePolicy.CapturedDiceBatchSequence` là policy data-driven cho encounter mà phase tiến
theo việc người chơi bắt hết một batch authored, không tiến theo HP:

```text
Phase enter → combat-local pause + AutoCombatDialogue → move/scripted dice batch tiếp tục
→ catch đủ toàn bộ dice của batch
→ chờ mọi cue RequiredBeforePhaseAdvance resolve
→ atomic phase break → phase kế
```

- HP dùng chung xuyên phase; Attack vẫn trừ HP và phát scratch/hit feedback nhưng bị clamp ở
  `1 HP`, không thể phát Victory.
- `CombatDiceBatchDefinition` author đúng symbol, vị trí chuẩn hóa, hướng, tốc độ và delay. Reroll
  không tính là resolve batch; chỉ catch đủ toàn bộ dice mới tiến.
- `CombatEncounterOutcomeRules` giới hạn result dùng lại được. Encounter đêm chỉ cho `Defeat`,
  tắt Retry và dùng `CurrentPhaseAndRequiredCues` để chặn thua sớm.
- `RequiredBeforePhaseAdvance` giữ phase cũ cho tới khi bark tự chạy xong. Phase cuối dùng
  `RequiredBeforePlayerDefeat`; TIME và bullet vẫn tick nhưng safety floor là `1 s` cho tới khi
  hai câu cuối resolve.
- `minimumPlayerTimeOnEnter` là phase-level floor dùng chung. Finale đêm nâng TIME còn lại lên
  tối thiểu `30 s` trước khi mở lethal gate, để volley thật — không conditional theo enemy ID —
  kết liễu Audere.
- `CombatBoardView` cung cấp player-constraint handle; `ClosingFinale` kéo Heart mềm về tâm và
  `Cancel()`/Defeat/unload luôn release constraint cùng bullet, laser và session cũ.
- `Projectile Mask` là child inset `14 px` bên trong `Dice Field`; chỉ `Bullet Root` và `Laser Root`
  nằm dưới mask nên Heart, dice và cursor vẫn dùng đủ play area nhưng hazard không ló lên viền.
- `CombatEncounterData.DefeatPresentation` là contract tùy chọn, không phụ thuộc enemy ID. Khi
  configured, TIME về `0` chuyển state sang Defeat ngay, dừng move/dice/input, giữ enemy actor,
  khóa collision và velocity của mọi bullet/laser, fade hazard bằng unscaled time, rồi phát
  DialogueUI `CallerOwnedPause`. Chỉ sau dialogue controller mới clear actor/session và trả Defeat
  cho `CombatStep`; cancel giữa fade hoặc dialogue vẫn cleanup một lần.

Asset production:

```text
Assets/_Audere/Data/Combat/TimorNightPressure/
├── CombatEncounter_D1_TIMOR_NIGHT_PRESSURE.asset   66 TIME; Defeat-only; no Retry
├── Enemy_TimorNightPressure.asset                   36 shared HP; 11 phases
├── DiceBatches/DiceBatch_TimorNightPressure_01..10 exactly 3 dice each
└── Moves/MoveSet_TimorNightPressure_01..11

Assets/_Audere/Prefabs/Combat/Enemies/
└── Enemy_TimorNightPressure_PLACEHOLDER.prefab
```

Mười move dùng các pattern authored riêng; phase 11 không spawn dice và dùng `ClosingFinale`.
Nhịp 2 dùng `VerticalLaserColumns` bắn dọc board với hai lane trống thay đổi. Nhịp 6 dùng
`ShiftingBattleBoxMove`: `Frame` đứng yên; move chỉ tween `Dice Field.Width` và `Pos X` qua ba pose
ngang được author bằng `Width Fraction` + `Normalized X`. Board tính Pos X tối đa từ phần chiều rộng
còn dư nên hai mép Dice Field không thể vượt khỏi Frame. Projectile mask, dice, Stun Zone và Catch
Cursor co theo field; Heart giữ nguyên vị trí nếu còn hợp lệ và chỉ bị clamp vào trong khi biên co qua
nó. Move không spawn thêm bullet, không đổi Height/Pos Y, và luôn trả `Dice Field` cùng
`Airborne Dice Overlay` về authored Width/Pos khi hoàn tất, phase break, cancel, defeat hoặc retry.
Nhịp 8 dùng
`SweepingLaser` quét ngang cả box, và nhịp 10 dùng `PendulumLaser`. Finale trộn
vertical laser vào volley bốn phía. Mỗi laser telegraph trước, giữ ownership session/phase và được
clear cùng projectile cũ. Telegraph màu trắng, bắt đầu alpha `0` và bề ngang `8%`, rồi smoothstep
lên đủ alpha/bề rộng để đọc rõ trên nền tối. Khi bắn, tia chuyển sang đỏ hồng cùng tông bullet và
collision chỉ bật sau khi telegraph nở xong.
`NarrativePressurePatternMove` chỉ switch theo loại pattern data, không
biết Timor/enemy ID.

Stun Zone là mechanic composition độc lập, không nằm trong switch pattern và không kiểm tra Timor ID.
Nhịp 4 ghép `MovingGapWall` với hai dải dọc trái/phải luân phiên; nhịp 6 ghép
`ShiftingBattleBoxMove` với dải trái/phải/giữa theo field hiện tại; nhịp 9 ghép
`RotatingBlades` với dải dọc giữa rồi hai dải ngang trên/dưới. Ba moveset tham chiếu
`CompositeCombatMove`, còn geometry/timing nằm trong asset `Move_*_StunZone`. `Cancel()` của composite
dọn cả primary pattern lẫn zone; `CombatBoardView.ClearCombatRuntime`, `PrepareEncounter` và disable
cũng ép zone về Hidden và xóa cursor stunned. Tutorial D1 chủ động gọi authored fixed zone trong session
tutorial, rồi cleanup trước khi tạo real-combat session.
`AutoCombatDialogue` tiếp tục dùng DialogueUI chuẩn và không claim Dialogue input; controller giữ
combat-local pause cho tới khi sequence kết thúc. `DialogueData` hỗ trợ portrait override ở cấp left/right và từng line. Timor dùng art trong
`Assets/_Audere/AssetGame/Timor` theo đường cong Worried → WorriedUneasy → Angry → Sad; portrait
đổi đúng ở DialogueUI và không được điều khiển bởi enemy phase/actor runtime.

Bark progression là data narrative, không nằm trong move execution: nhịp 1–3 chuyển từ bảo vệ sang
ra lệnh đứng yên; nhịp 4–7 dùng nỗi sợ mất Audere để phủ nhận lựa chọn; nhịp 8–10 chuyển thành yêu
cầu nhìn/nghe lời và độc quyền sự gần gũi; nhịp 11 khóa câu trả lời. Các bark có
Audere vẫn dùng `Left`, Timor dùng `Right`; mọi bubble giữ tối đa `42` ký tự.

Khi finale rút TIME về `0`, encounter đêm dùng defeat presentation `0.62 s`: toàn bộ hazard đứng
lại rồi tan, Timor vẫn còn trên board, và portrait Sad phát `Thấy chưa` như một kết luận buồn chứ
không phải lời đắc thắng. Đoạn thoại hoàn tất trước neutral fade về phòng.

**Design Intent:** nhịp đêm là lần đầu Timor mất bình tĩnh khi Audere phản đối; cơn giận đi ra từ
nỗi sợ mất cô và biến bảo vệ thành yêu cầu phục tùng, không phải thú vui làm hại cô. Chi tiết mẹ
Audere từng tin người khác và Audere sau đó mất bà chỉ được khóa cho beat này ở mức Design Intent;
chưa dùng làm Established Canon cho cảnh khác. **Unresolved:** ontology combat, ý nghĩa tâm lý
cuối cùng và final moveset/balance.

### Shared attack SFX — 2026-08-28

All board-spawned bullets use `Enemy_BulletVolley` (`dan.wav`); lasers use
`Enemy_LaserVolley` (`laze.mp3`). Sounds start on activation, not during telegraph.
`CombatVolleyAudio` groups simultaneous requests and limits rapid bullet requests to one
per 0.25 s (laser: 0.12 s). Each kind owns only one reusable source; new beats replace old
tails. These Inspector values affect sound only, not projectile timing, geometry or damage.
Pause, phase clear, hazard fade, Retry/cancel and board disable/destroy clean up audio;
old-version cleanup cannot reset a newer phase. Detailed 89-test and Play evidence is in
[Audio System](../03_AudioSystem.md#combat-volley-sfx--2026-08-28).

### Timor music grid — 2026-08-28

**Design Intent:** Xuân chọn `bossfightfull.mp3` cho Timor và muốn đạn sát nhạc hơn.
Phân tích PCM của clip 54.596 giây dùng spectral flux/RMS; lưới thực dụng được chọn là
**110 BPM**, offset **0.013 s**. Đây là lựa chọn author từ các ứng viên nhịp/harmonic,
không phải khẳng định tempo âm nhạc duy nhất. Không thay các phase theo tiến độ bắt xúc xắc,
66 TIME, defeat gate, dialogue, Shield hoặc dice constants.

- `CombatEncounterData.Music = Music_TimorCombat`; các encounter khác mặc định `Music_Combat`.
- `NarrativePressurePatternMove` có optional `rhythmMusic`, `rhythmBpm`, `rhythmBeatOffset`,
  `waveBeats`. Đồng hồ dùng `AudioSource.timeSamples`, không tích lũy thời gian giả theo phase.
- Nhịp 1/3/4/7: 2 phách mỗi đợt; 5/9: 1.5 phách; laser 2/8: 4 phách; pendulum 10: 2 phách;
  finale: 0.5 phách. Nhịp 6 vẫn là ShiftingBattleBox với Stun Zone, giữ nguyên geometry/timing.
- Đạn báo trước 0.5 phách (~0.273 s); laser thường báo trước 1 phách (~0.545 s).
  Launch được đặt sớm để thời điểm kích hoạt rơi trên lưới; không bỏ telegraph/collision gate.
  Pendulum dùng nhịp đều trên music grid; nhịp nghỉ lệch cũ chỉ còn ở local fallback.
- Clock mới cho mỗi move; bỏ các nhịp đã lỡ khi pause/seek, căn lại khi clip loop, không xả
  một chuỗi đạn bù. Không có AudioService hoặc slot đã chọn trống thì dùng pacing local.
  Trong lúc service đang đổi/loading track thì đợi clock đúng. Mute volume không dừng clock.
- Chỉ chỉnh asset Timor và nhánh Timor của author tool; không chạy lại scene builder.
  Các enemy khác không bật music grid nên giữ cadence cũ.

Verification: **79/79 tests pass** (audio/runtime/Evening); Play lấy mẫu 9 emissions,
sai lệch lớn nhất ~32 ms, qua loop thật, telegraph/active và cleanup; đúng nhạc khi cancel
và trở về Story. Chi tiết và giới hạn QA ở [Audio System](../03_AudioSystem.md).
**Unresolved:** cảm giác nhạc/độ khó qua một lượt chơi tay đầy đủ; chưa tuyên bố cân bằng cuối.

<!-- END PRESERVED SOURCE -->
