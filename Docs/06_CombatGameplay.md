# Audere Combat Gameplay

Tài liệu này mô tả combat real-time trong scene `20_Game`. Catch Cursor đi theo mouse để bắt/reroll dice; Audere Heart nằm đúng tâm Catch Cursor nên cũng né đạn bằng mouse trong cùng một Battle Box.

## 1. Scene hierarchy và lifecycle

```text
Main Camera
└── PuzzleViewportMask             camera-space; inactive trong Combat

WORLD                              WorldModeController
├── Puzzle Root
├── Combat Root
│   └── CombatBoard                prefab instance; world-space Canvas
└── World Transition Overlay       fade chuyển mode

SYSTEMS
├── Puzzle Systems
└── Combat Systems
    └── Combat Controller
```

`Puzzle Root` và `Combat Root` là hai mode ngang hàng dưới `WORLD`. `WorldModeController` là nơi duy nhất bật/tắt root, systems, Puzzle UI và camera bằng fade đen. Combat board thuộc lifecycle của `Combat Root`; `GameplayUIRoot` chỉ chứa UI dùng xuyên scene và Dialogue.

Debug mode:

- `F1`: Puzzle.
- `F2`: Combat.
- Menu `Audere > Combat > Debug > Switch To Puzzle/Combat`.

## 2. Shared Battle Box

Prefab chính:

```text
Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab
```

```text
CombatBoard
├── Frame
├── Dice Field                     RectMask2D; toàn bộ gameplay real-time
│   ├── Stun Zone Root             vùng chấm tím chặn thao tác cursor
│   ├── Bullet Root                enemy bullets
│   ├── Dice Root                  dice bắt/reroll bằng mouse
│   ├── Catch Cursor Root
│   │   └── Catch Cursor           vùng bắt/reroll đi theo mouse
│   │       └── Audere Heart Root  tâm nhận đạn của Catch Cursor
│   │           └── Heart Visual   nested prefab, sprite vuông placeholder
│   └── Feedback FX Root           root dự phòng cho board feedback, không tạo text khi catch
├── Airborne Dice Overlay          không mask; dice đang tung được vẽ trên viền board
├── Enemy                          actor duy nhất ở phía trên
│   ├── Name
│   │   └── Enemy Name             tên lấy từ CombatEncounterData
│   └── Vfx                        anchor VFX bám tâm enemy
└── Timer Track                    TIME-as-health; chưa có status text
```

Không dựng player portrait, dãy heart UI hoặc status text ở giai đoạn hiện tại. Audere chỉ được biểu diễn bằng Heart ở tâm Catch Cursor. Dice, Heart và bullet không nằm ở các khu riêng: mouse vừa chọn dice vừa điều khiển vị trí né đạn trong cùng một vùng nhìn.

## 3. Chu kỳ combat real-time

```text
Enemy intro + reset HP/armor/timer
            ↓
Spawn batch dice #1 + bắt đầu attack pattern
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

- `Enemy HP <= 0`: Victory.
- `TIME <= 0`: Defeat (`TIME UP`). TIME đồng thời là sinh lực của người chơi.

Khi combat chạy độc lập với `Play On Start`, có thể dùng flow debug/retry cũ. Khi combat do Story điều khiển, Defeat không tự chờ phím `R`; `CombatStep` quyết định Complete/Fail/Retry/Cancel.

## 4. Dice và hiệu ứng tức thì

| Dice | Hiệu ứng khi catch |
| --- | --- |
| Attack | Trừ Enemy HP ngay, spawn một vòng `scratch.aseprite` tại `CombatBoard/Vfx`, enemy flash trắng + shake và phát hit sound. Duration hit-feedback lấy trực tiếp từ clip scratch để hai animation kết thúc cùng lúc. |
| Armor | Cộng Armor ngay. Mỗi Armor chặn một lần bullet damage. |
| Heal | Cộng TIME ngay, không vượt quá thời lượng encounter ban đầu. |

Left click bắt dice đang overlap Catch Cursor. Right click reroll dice đang overlap. Nếu effect đổi loại, runtime thay dice hiện tại bằng instance từ pool của đúng prefab mới tại cùng vị trí và vận tốc; nhờ vậy icon author trực tiếp trên prefab luôn khớp effect.

Mỗi batch mặc định có 5 dice. Khi dice cuối bị bắt, batch kế tiếp xuất hiện sau `0.3 s`. Catch animation ngắn và không dừng chuyển động của dice khác, bullets hoặc timer.

Prefab riêng:

```text
Assets/_Audere/Prefabs/Combat/Dice/
├── Dice_Attack.prefab
├── Dice_Armor.prefab
└── Dice_Heal.prefab
```

Mỗi prefab có `Root > Shadow | Frame | Face > Icon`, cho phép thay art/màu/size riêng mà không sửa controller. Mỗi `Icon` chỉ giữ sprite Aseprite đúng với prefab của nó: `Dice_Attack → attack`, `Dice_Armor → gaurd`, `Dice_Heal → heal`. TMP `Symbol` chỉ là fallback tùy chọn khi prefab không có icon; `CombatDieView` không giữ một thư viện ba icon.

Dice có hai phase presentation:

1. `Airborne/Inactive`: khi vừa spawn hoặc reroll, shadow là ground projection và trượt ngang qua board; `Frame + Face/Icon` bay theo 2–3 cung parabol phía trên shadow. Mỗi lần chạm board có squash ngắn, độ cao/thời lượng nảy giảm dần. Shadow có alpha `100%`; shadow và icon dùng màu neutral `#23212D`; dice chưa thể catch/reroll.
2. `Landed/Active`: chỉ cú chạm cuối mới reveal icon theo chức năng, đặt shadow về alpha `0%` và mở input: Attack `#A83B44`, Armor `#B0ABB7`, Heal `#D8C097`. Sau đó dice tiếp tục chuyển động từ đúng velocity của quỹ đạo tung.

Ba prefab dùng chung sprite khung `dice (1).aseprite`, nhưng vẫn giữ icon và `activeIconColor` riêng để chỉnh độc lập. Mỗi dice có launch delay ngẫu nhiên rất ngắn và 2–3 lần nảy nên cả batch không chuyển động đồng bộ. Trong phase tung, dice tạm được reparent từ `Dice Root` sang `Airborne Dice Overlay`, nằm ngoài `RectMask2D` và render sau `Frame`; vì vậy thân dice có thể phủ lên viền board. Cú đáp cuối đưa object về `Dice Root` để clipping trong arena hoạt động lại.

## 5. Enemy attack và Audere Heart

Enemy bắn liên tục qua danh sách pattern trong `CombatEncounterData`:

- `AimedFan`: fan bullet nhắm vị trí Audere Heart hiện tại.
- `SideSweep`: các hàng bullet luân phiên từ trái/phải.
- `Rain`: bullet rơi từ cạnh trên với góc lệch nhẹ.

Pattern tự đổi theo duration, không phụ thuộc batch dice. Bullet chạm Heart ở tâm Catch Cursor sẽ bị consume; Heart nhận một khoảng invulnerability ngắn để tránh nhiều bullet cùng frame cùng trừ TIME.

Nếu có Armor, hit tiêu thụ một Armor trước. Nếu không còn Armor, hit trừ TIME trực tiếp; TIME về `0` là thua. Catch Cursor và Heart được clamp hoàn toàn trong Battle Box, kể cả khi mouse đi ra ngoài khung.

`Stun Zone Root` chứa các vùng chấm tím đang hoạt động trong Battle Box. Khi Catch Cursor overlap một vùng stun, viền cursor chuyển từ trắng sang tím xỉn. Left click/right click trên dice lúc này đều bị chặn: dice không bị catch hoặc reroll. Sprite `Assets/_Audere/AssetGame/IconDice/X.aseprite` xuất phát rất nhỏ từ tâm cursor, xoay một vòng, nở có overshoot nhẹ, settle rồi fade. Rời vùng stun thì viền trở lại trắng và thao tác hoạt động ngay. Stun Zone không làm chậm, đổi màu hay thay đổi chuyển động của dice.

Heart visual được tách riêng để thay art mà không đụng vào movement/collision:

```text
Assets/_Audere/Prefabs/Combat/Player/HeartVisual.prefab
```

Prefab hiện chỉ có `RectTransform + Image` và sprite vuông placeholder. Khi có art Heart chính thức, chỉ cần thay `Source Image` trên prefab này.

Bullet prefab:

```text
Assets/_Audere/Prefabs/Combat/Bullets/EnemyBullet.prefab
```

## 6. Data và runtime code

Sample encounter:

```text
Assets/_Audere/Data/Combat/CombatEncounter_Sample.asset
```

Thông số sample hiện tại: enemy `Con bò` có 12 HP, TIME tối đa 40 giây, Heal `+3 s`, bullet hit `-3 s`, 5 dice/batch, respawn delay `0.3 s`, dice speed `115–185`, weight Attack/Armor/Heal `5/3/2`.

`Timer Fill` giảm bằng cách thay `RectTransform.anchorMax.x` từ `1 → 0`, neo cố định ở mép trái. Không dùng `Image.fillAmount` làm nguồn hiển thị vì sprite-less Image của UGUI bỏ qua fill và luôn vẽ full quad.

Khi bullet gây damage thật (không bị Armor chặn), `Timer Fill` màu chính giảm ngay để người chơi đọc được lượng TIME còn lại. Phần TIME vừa mất được giữ lại bằng `Timer Damage Fill` màu trắng trong `0.12 s`, sau đó co mượt về mức mới trong `0.34 s`. Cùng lúc camera rung mạnh ở đầu rồi tắt dần trong `0.20 s`. Heal hủy trail damage đang chạy và đồng bộ cả hai fill lên mức TIME mới.

| Script | Trách nhiệm |
| --- | --- |
| `CombatSymbol.cs` | Enum ổn định: Attack, Armor, Heal. |
| `CombatEncounterData.cs` | Enemy HP, TIME-as-health, dice batch, immediate effect, Heart tuning và attack-pattern data. |
| `CombatController.cs` | Đồng hồ real-time, input song song, batch liên tục, pattern, immediate effect và win/lose. |
| `CombatBoardView.cs` | Shared Battle Box, enemy name, timer, spawn/pool dice+bullet, Heart, cursor và feedback. |
| `CombatCatchCursorView.cs` | Chuyển trạng thái viền cursor và phát feedback `X` khi thao tác bị Stun Zone chặn. |
| `CombatDieView.cs` | Chuyển động/bounce, reroll và capture. |
| `CombatPlayerView.cs` | Heart visual ở tâm Catch Cursor, hit flash và invulnerability. |
| `CombatBulletView.cs` | Bullet velocity, despawn bounds và pooling. |

## 7. Motion reference từ GIF `ry0CXX (1).gif`

GIF có 444 frame, đa số 30 ms/frame. Các điểm đã dùng làm chuẩn:

- Dice giữ khung vuông, không quay; mỗi dice trượt theo vector riêng và bounce ở biên.
- Không có collision dice-với-dice.
- Trong phase tung, shadow chạy trên mặt board còn khung/icon tạo độ cao bằng cung parabol; shadow thu nhỏ nhẹ ở đỉnh cung để tăng cảm giác giả 3D.
- Dice chạm board 2–3 lần với biên độ giảm dần rồi mới active; trong toàn bộ phase này dice không thể catch/reroll.
- Stun Zone là dải nền chấm tím; dice đi xuyên qua mà không đổi tốc độ hay màu.
- Cursor độc lập, không hút dice; chỉ catch khi overlap và cursor không nằm trong Stun Zone.
- Khi cursor đi vào Stun Zone, viền trắng chuyển tím xỉn; thử catch/reroll phát sprite `X.aseprite` xoay và nở từ tâm rồi fade, còn dice vẫn tồn tại.
- Catch làm dice biến mất ngay; không sinh text `ATK`, `ARM` hoặc `HEAL`.
- Một dice bị bắt không dừng các dice khác.

Attack dùng animation pixel `Assets/_Audere/AssetGame/Vfx/scratch.aseprite`: mỗi hit tạo một instance tại node `CombatBoard/Vfx`, chạy đúng một vòng clip rồi tự hủy. Enemy rung mạnh trong phần đầu; các nhịp flash trắng được rải suốt clip và nhịp cuối kết thúc cùng scratch. Armor và Heal không tạo text hoặc projectile chữ.

## 8. Setup và debug QA

`Audere > Combat > Setup Combat Foundation` là tool idempotent: migrate/tạo prefab, cấu hình sample encounter, bind hierarchy và save scene.

Các lệnh Play Mode phục vụ kiểm thử:

- `Apply Attack Dice`, `Apply Armor Dice`, `Apply Heal Dice`.
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

Với combat cốt truyện yêu cầu Audere thua, đặt `Defeat Behaviour = Complete`; Story chạy tiếp mà không hiện retry. Với combat phải thắng, `Retry` giữ StoryEvent đứng tại CombatStep, dùng retry panel của combat presentation và tạo một combat session mới sạch cho mỗi lần thử.
