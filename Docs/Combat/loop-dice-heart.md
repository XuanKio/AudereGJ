# Vòng combat, dice, Heart và Stun Zone

[Mục lục nguồn](../06_CombatGameplay.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
## 3. Chu kỳ combat real-time

```text
Enemy actor intro + reset phase/TIME
            ↓
Spawn batch dice #1 + bắt đầu move của phase
            ↓
┌─────────────────────────────────────────┐
│ Timer, dice, bullets và Heart cùng chạy  │
│ Mouse: di chuyển Heart + catch/reroll    │
└─────────────────────────────────────────┘
            ↓
Catch dice → áp hiệu ứng NGAY
            ↓
ActiveDiceCount == 0
            ↓ 0.3 giây, không pause combat
Spawn batch tiếp theo
```

Không có turn, `End Turn` hay bước resolve cuối batch. Hết batch chỉ có một nhiệm vụ: yêu cầu batch mới. Trong khoảng delay, enemy bullet, timer và Heart vẫn tiếp tục đi theo mouse.

Điều kiện kết thúc:

- Policy enemy hoàn thành phase cuối: Victory.
- `TIME <= 0`: Defeat (`TIME UP`). TIME đồng thời là sinh lực của người chơi.

Khi combat chạy độc lập với `Play On Start`, có thể dùng flow debug/retry cũ. Khi combat do Story điều khiển, Defeat không tự chờ phím `R`; `CombatStep` quyết định Complete/Fail/Retry/Cancel.

## 4. Dice và hiệu ứng tức thì

| Dice | Hiệu ứng khi catch |
| --- | --- |
| Attack | Trừ Enemy HP ngay, spawn một vòng `scratch.aseprite` tại shared root `CombatBoard/Enemy/VFX`, chính giữa `Enemy Mount`; enemy flash trắng + shake và phát hit sound. Duration hit-feedback lấy trực tiếp từ clip scratch để hai animation kết thúc cùng lúc. |
| Shield | Xóa bullet trong bán kính chung quanh Audere Heart ngay lập tức; không tạo pulse, vùng màu hoặc spark phụ. |
| Heal | Cộng TIME ngay, không vượt quá thời lượng encounter ban đầu. |

Left click bắt dice đang overlap Catch Cursor. Right click reroll dice đang overlap. Nếu effect đổi loại, runtime thay dice hiện tại bằng instance từ pool của đúng prefab mới tại cùng vị trí và vận tốc; nhờ vậy icon author trực tiếp trên prefab luôn khớp effect.

Mỗi batch mặc định có 5 dice. Khi dice cuối bị bắt, batch kế tiếp xuất hiện sau `0.3 s`. Catch animation ngắn và không dừng chuyển động của dice khác, bullets hoặc timer.

Prefab riêng:

```text
Assets/_Audere/Prefabs/Combat/Dice/
├── Dice_Attack.prefab
├── Dice_Shield.prefab
└── Dice_Heal.prefab
```

Mỗi prefab có `Root > Shadow | Frame | Face > Icon`, cho phép thay art/màu/size riêng mà không sửa controller. Mỗi `Icon` chỉ giữ sprite Aseprite đúng với prefab của nó: `Dice_Attack → attack`, `Dice_Shield → gaurd`, `Dice_Heal → heal`. TMP `Symbol` chỉ là fallback tùy chọn khi prefab không có icon; `CombatDieView` không giữ một thư viện ba icon.

Dice có hai phase presentation:

1. `Airborne/Inactive`: khi vừa spawn hoặc reroll, shadow là ground projection và trượt ngang qua board; `Frame + Face/Icon` bay theo 2–3 cung parabol phía trên shadow. Mỗi lần chạm board có squash ngắn, độ cao/thời lượng nảy giảm dần. Shadow có alpha `100%`; shadow và icon dùng màu neutral `#23212D`; dice chưa thể catch/reroll.
2. `Landed/Active`: chỉ cú chạm cuối mới reveal icon theo chức năng, đặt shadow về alpha `0%` và mở input: Attack `#A83B44`, Shield `#B0ABB7`, Heal `#D8C097`. Sau đó dice tiếp tục chuyển động từ đúng velocity của quỹ đạo tung.

Ba prefab dùng chung sprite khung `dice (1).aseprite`, nhưng vẫn giữ icon và `activeIconColor` riêng để chỉnh độc lập. Mỗi dice có launch delay ngẫu nhiên rất ngắn và 2–3 lần nảy nên cả batch không chuyển động đồng bộ. Trong phase tung, dice tạm được reparent từ `Dice Root` sang `Airborne Dice Overlay`, nằm ngoài `RectMask2D` và render sau `Frame`; vì vậy thân dice có thể phủ lên viền board. Cú đáp cuối đưa object về `Dice Root` để clipping trong arena hoạt động lại.

## 5. Enemy attack và Audere Heart

Enemy bắn liên tục qua `CombatMoveSet` của phase hiện tại:

- `AimedFan`: fan bullet nhắm vị trí Audere Heart hiện tại.
- `SideSweep`: các hàng bullet luân phiên từ trái/phải.
- `Rain`: bullet rơi từ cạnh trên với góc lệch nhẹ.

Move tự đổi theo duration, không phụ thuộc batch dice. `LinearProjectilePatternMove` biểu diễn
ba prototype bằng spawn/target data dùng chung, không kiểm tra enemy ID. Bullet chạm Heart ở
tâm Catch Cursor sẽ bị consume; Heart nhận một khoảng invulnerability ngắn để tránh nhiều
bullet cùng frame cùng trừ TIME.

Với spawn mode `ActorAnchor`, vị trí projectile authored trên enemy được đổi sang local Battle Box
rồi clamp vào mép trong play area. Enemy visual có thể đứng ngoài Battle Box mà shot vẫn xuất hiện
trong vùng nhìn; Side Sweep và Rain tiếp tục dùng authored side/top distribution của chúng.

Bullet hit trừ TIME trực tiếp; TIME về `0` là thua. Shield chủ động dọn bullet gần Heart.
Catch Cursor và Heart được clamp hoàn toàn trong Battle Box, kể cả khi mouse đi ra ngoài khung.

`Stun Zone Root` chứa các vùng chấm tím đang được tutorial hoặc move của phase sở hữu. Zone không còn là decoration bật suốt encounter: ngoài owner hợp lệ nó ở trạng thái `Hidden`. `StunZonePressureMove` chạy lifecycle `Hidden → Telegraph → Blocking → Fade`; telegraph chỉ fade hình vào để người chơi đọc vị trí, chưa chặn catch. Khi chuyển sang `Blocking`, Catch Cursor overlap sẽ đổi viền từ trắng sang tím xỉn và **chỉ catch bằng chuột trái** bị chặn; chuột phải vẫn reroll dice đang overlap như bình thường. Khi thử catch, sprite `Assets/_Audere/AssetGame/IconDice/X.aseprite` xuất phát rất nhỏ từ tâm cursor, xoay một vòng, nở có overshoot nhẹ, settle rồi fade. Blocking kết thúc ngay khi bắt đầu fade-out, không đợi alpha về `0`. Stun Zone không làm chậm, đổi màu hoặc thay đổi chuyển động/reroll của dice, và luôn được dọn khi move/phase/session kết thúc.

<!-- END PRESERVED SOURCE -->
