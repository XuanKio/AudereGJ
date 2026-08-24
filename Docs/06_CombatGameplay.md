# Audere Combat Gameplay

Tài liệu này mô tả combat real-time dùng chung trong scene `20_Game` và hand-off prototype ở
`30_Classroom`. Catch Cursor đi theo mouse để bắt/reroll dice; Audere Heart nằm đúng tâm
Catch Cursor nên cũng né đạn bằng mouse trong cùng một Battle Box.

## 1. Scene hierarchy và lifecycle

```text
Main Camera
└── PuzzleViewportMask             camera-space; inactive trong Combat

WORLD                              WorldModeController
├── Puzzle Root
├── Combat Root
│   └── CombatBoard                prefab instance; world-space Canvas
├── Story Root / direct Story ref  presentation của location; có thể nằm ngoài WORLD
└── World Transition Overlay       fade chuyển mode

SYSTEMS
├── Puzzle Systems
└── Combat Systems
    └── Combat Controller
```

`Puzzle Root` và `Combat Root` là hai mode ngang hàng dưới `WORLD`. `WorldModeController` là
nơi duy nhất bật/tắt root, systems, Puzzle UI và camera. Chuyển mode thông thường dùng fade
đen; riêng Story → Combat có thể gọi `ApplyModeImmediate` ở giữa một fullscreen transition
đang che kín hình. Combat board thuộc lifecycle của `Combat Root`; `GameplayUIRoot` chứa UI
xuyên scene, Dialogue và `CombatRetryUI` screen-space độc lập với camera/world shader.

`WorldGameplayMode` giữ thứ tự serialize ổn định: `Puzzle = 0`, `Combat = 1`, `Story = 2`.
`Story` là presentation mode, không phải một gameplay controller và không tự claim input.

Debug mode:

- `F1`: Puzzle.
- `F2`: Combat.
- `F3`: Story.
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
├── Enemy                          status presentation ở phía trên
│   ├── Enemy Mount                spawn `CombatEnemyActor` từ definition
│   ├── Name
│   │   ├── Image                  khung tên `420 × 120`
│   │   └── Enemy Name             TMP cỡ `57`, căn giữa và wrap trong Image
└── Timer Track                    TIME-as-health; chưa có status text
```

Không dựng player portrait, dãy heart UI hoặc status text ở giai đoạn hiện tại. Audere chỉ được biểu diễn bằng Heart ở tâm Catch Cursor. Dice, Heart và bullet không nằm ở các khu riêng: mouse vừa chọn dice vừa điều khiển vị trí né đạn trong cùng một vùng nhìn.

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
| Attack | Trừ Enemy HP ngay, spawn một vòng `scratch.aseprite` tại `CombatBoard/Vfx`, enemy flash trắng + shake và phát hit sound. Duration hit-feedback lấy trực tiếp từ clip scratch để hai animation kết thúc cùng lúc. |
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

`Stun Zone Root` chứa các vùng chấm tím đang hoạt động trong Battle Box. Khi Catch Cursor overlap một vùng stun, viền cursor chuyển từ trắng sang tím xỉn. Stun Zone **chỉ chặn catch bằng chuột trái**; chuột phải vẫn reroll dice đang overlap như bình thường. Khi thử catch, sprite `Assets/_Audere/AssetGame/IconDice/X.aseprite` xuất phát rất nhỏ từ tâm cursor, xoay một vòng, nở có overshoot nhẹ, settle rồi fade. Rời vùng stun thì viền trở lại trắng và catch hoạt động ngay. Stun Zone không làm chậm, đổi màu hay thay đổi chuyển động/reroll của dice.

### Tutorial combat D1 Classroom

Tutorial và trận đánh thật là hai runtime tách biệt. `CombatEncounterData` chỉ tham chiếu optional
`CombatTutorialData`; asset tutorial chọn một `CombatEnemyDefinition` một phase riêng, TIME an toàn,
opening batch cố định và các cue hướng dẫn. Controller không kiểm tra boss ID. Retry tạo lại toàn bộ
attempt nên tutorial bắt đầu sạch.

```text
Tutorial runtime   → 1 phase `tutorial-only-placeholder`, 99 HP, 120 TIME
Opening batch      → đúng 3 dice: Attack, Shield, Heal
Dice Batch Ready   → dim nền + preview cả ba loại cùng lúc
                     → giới thiệu bắt, gieo lại và TIME ngắn gọn
Ngay sau overview  → cutout Stun Zone; chỉ catch bị chặn
Catch Attack đầu   → dim nền + preview Attack gây 1 damage
Catch Shield đầu   → dim nền + preview Shield dọn đạn gần Heart
Catch Heal đầu     → dim nền + preview Heal hồi 3 giây
Reroll/Player hit  → cue xác nhận rule tương ứng, mỗi cue một lần
Đã bắt đủ 3 loại   → Timor kết thúc phần làm quen
                     → hủy tutorial session, xóa actor/dice/projectile/callback cũ
                     → tạo session combat mới, phase 1 có 2 HP và TIME đầy 45 s
```

Character dialogue chỉ giúp Audere chia nhỏ việc cần làm. Input/rule chính xác nằm trong
`GameplayUIRoot/CombatTutorialUI`. Instruction là đúng một dòng text trắng dùng cùng
`Mynerve-Regular SDF`, cỡ `40`, Bold như tutorial Scene 20; không có card/background riêng.
Cue focus TIME/Stun Zone dùng bốn panel đen mờ tạo một cutout quanh `RectTransform` thật.
`DiceAll` mở vùng showcase `520 × 180` và đặt ba preview cùng hàng; cue riêng lẻ chỉ hiện đúng prefab
dice vừa được bắt. Không có icon copy hoặc tuning gameplay riêng trong tutorial.

Dialogue và instruction cùng giữ controller ở `DialoguePause`. Instruction không tự hết hạn; nó chỉ
đóng bằng cú click trái/phải tiếp theo, và cú click đóng card bị consume nên không đồng thời bắt/gieo
dice. Khi card đang mở, controller không tick TIME, Heart feedback, dice movement/collision, enemy
move hoặc bullet. Dice stagger/batch delay cũng dùng combat-local clock: pause giữ nguyên vị
trí trong coroutine và resume tiếp, không cắt cụt batch đang spawn. Giữa các cue, tutorial TIME chạy
ở `0.25x`; bullet hit vẫn dùng đúng penalty nhưng
có safety floor `1 s`. Enemy tutorial có `99 HP` và người chơi có `120 s`, nên ba thao tác học không
thể vô tình kết thúc boss hoặc làm tutorial thua giữa chừng.

Khi Timor chốt phần làm quen, controller không gọi reset trên runtime cũ. Nó tăng session version,
shutdown tutorial actor/mechanic/move, return dice/projectile, rồi khởi tạo `Enemy_KhoangLang` ba
phase như một combat attempt mới. Attempt này dùng TIME `45 s`, HP `2` mỗi phase và tốc độ bình
thường. Enemy move cùng dice batch được schedule ngay khi state trở lại `Playing`; không có marker
`NHỊP n / 3`, vì phase chỉ là cấu trúc nội bộ của enemy runtime.

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

Sample dùng `Enemy_Sample` một phase, 12 HP, TIME tối đa 40 giây, Heal `+3 s`, bullet hit
`-3 s`, 5 dice/batch, respawn delay `0.3 s`, dice speed `115–185`. Weight và hiệu ứng
Attack/Shield/Heal lấy duy nhất từ `CombatDiceConstants`.

`Timer Fill` giảm bằng cách thay `RectTransform.anchorMax.x` từ `1 → 0`, neo cố định ở mép trái. Không dùng `Image.fillAmount` làm nguồn hiển thị vì sprite-less Image của UGUI bỏ qua fill và luôn vẽ full quad.

Khi bullet gây damage, `Timer Fill` màu chính giảm ngay để người chơi đọc được lượng TIME còn lại. Phần TIME vừa mất được giữ lại bằng `Timer Damage Fill` màu trắng trong `0.12 s`, sau đó co mượt về mức mới trong `0.34 s`. Cùng lúc camera rung mạnh ở đầu rồi tắt dần trong `0.20 s`. Heal hủy trail damage đang chạy và đồng bộ cả hai fill lên mức TIME mới.

| Script | Trách nhiệm |
| --- | --- |
| `CombatSymbol.cs` / `CombatDiceConstants.cs` | Enum và tuning chung: Attack, Shield, Heal. |
| `CombatEncounterData.cs` | TIME, dice batch pacing, Heart/bullet tuning, enemy definition và optional tutorial data. |
| `CombatTutorialData.cs` | Tutorial ID, enemy một phase riêng, TIME an toàn, opening dice cố định và cue hướng dẫn. |
| `CombatEnemyDefinition.cs` | Stable enemy ID, actor prefab, phase policy và authored phases. |
| `CombatEnemyRuntime.cs` | State/phase/HP/timer/move/cue/mechanic lifecycle theo mỗi `Play()`. |
| `CombatMoveSet.cs` / `CombatMoveDefinition.cs` | Ordered/weighted selection và immutable authored move data. |
| `CombatController.cs` | Session, TIME, input, dice batch, result và atomic phase hand-off; không chứa logic riêng của boss. |
| `CombatTutorialView.cs` | Text Scene-20-style, spotlight cutout theo target và preview trực tiếp prefab dice; unscaled fade, không nhận input. |
| `CombatBoardView.cs` | Shared Battle Box, actor mount, projectile pool theo prefab, dice/Heart/cursor/feedback. |
| `CombatCatchCursorView.cs` | Chuyển trạng thái viền cursor và phát feedback `X` khi thao tác bị Stun Zone chặn. |
| `CombatDieView.cs` | Chuyển động/bounce, reroll và capture. |
| `CombatPlayerView.cs` | Heart visual ở tâm Catch Cursor, hit flash và invulnerability. |
| `CombatBulletView.cs` | Bullet velocity, source prefab, session/phase ownership, collision reset và pooling. |

## 7. Motion reference từ GIF `ry0CXX (1).gif`

GIF có 444 frame, đa số 30 ms/frame. Các điểm đã dùng làm chuẩn:

- Dice giữ khung vuông, không quay; mỗi dice trượt theo vector riêng và bounce ở biên.
- Không có collision dice-với-dice.
- Trong phase tung, shadow chạy trên mặt board còn khung/icon tạo độ cao bằng cung parabol; shadow thu nhỏ nhẹ ở đỉnh cung để tăng cảm giác giả 3D.
- Dice chạm board 2–3 lần với biên độ giảm dần rồi mới active; trong toàn bộ phase này dice không thể catch/reroll.
- Stun Zone là dải nền chấm tím; dice đi xuyên qua mà không đổi tốc độ hay màu.
- Cursor độc lập, không hút dice; chỉ catch khi overlap và cursor không nằm trong Stun Zone.
- Khi cursor đi vào Stun Zone, viền trắng chuyển tím xỉn; thử catch phát sprite `X.aseprite` xoay và nở từ tâm rồi fade, còn chuột phải vẫn reroll bình thường.
- Catch làm dice biến mất ngay; không sinh text `ATK`, `ARM` hoặc `HEAL`.
- Một dice bị bắt không dừng các dice khác.

Attack dùng animation pixel `Assets/_Audere/AssetGame/Vfx/scratch.aseprite`: mỗi hit tạo một instance tại VFX anchor của actor, chạy đúng một vòng clip rồi tự hủy. Enemy rung mạnh trong phần đầu; các nhịp flash trắng được rải suốt clip và nhịp cuối kết thúc cùng scratch. Shield và Heal không tạo text hoặc projectile chữ.

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

## 10. Classroom prototype và hướng mở rộng (2026-08-23)

Scene `30_Classroom` hiện có một hand-off kỹ thuật sau thoại Timor:

```text
190_HoldAfterTimor
→ 200_ClassroomIsConsumed       [FullscreenWorldModeTransitionStep]
→ 210_PlayKhoangLangPrototype    [CombatStep]
→ 220_ReturnToStory              [WorldModeStep: Combat → Story]
→ 230_HoldAfterCombat
```

`WORLD` tham chiếu trực tiếp `CLASSROOM` làm `storyRoot`; `Combat Root` chứa cùng prefab
`CombatBoard` của scene 20. `SYSTEMS/Combat Systems` chỉ active ở mode Combat. Camera dùng
pose riêng cho Story và Combat; `PuzzleViewportMask` bật ở Puzzle/Story và tắt trong Combat.
`200_ClassroomIsConsumed` dùng shared profile `Dreamy Disorientation` trong `1.50 s`: camera
presentation nghiêng/zoom nhẹ, wide wave, UV drift, radial bend và smear quanh Audere; đổi
Story → Combat ở giây `1.10`, rồi distortion hạ xuống để Combat hiện rõ. Timeline nằm trong
`WorldTransition_DreamyDisorientation.asset`, không nằm riêng trong scene. Renderer Feature
inactive ngoài khoảng này nên
scene 20 và pixel art bình thường không chịu thêm full-screen blit. Chiều Combat → Story vẫn
dùng fade đen hiện tại.

Mode switch chỉ đổi presentation. `CombatController.Play()` vẫn là nơi duy nhất claim Combat
input, nên fullscreen transition, fade hoặc bật root không thể vô tình cấp input sớm. Contract
shader, timeline và cancel/replay nằm tại `Docs/11_FullscreenWorldTransitions.md`.

Encounter hiện tại:

```text
Assets/_Audere/Data/Combat/CombatEncounter_D1_CLASSROOM_KHOANG_LANG.asset
```

- **Design Intent:** prototype boss mang display name `Khoảng Lặng`, ID
  `d1-classroom-khoang-lang`, policy `PerPhaseHealth`, ba phase `2 HP`: Aimed Fan, Side Sweep,
  Rain. Đây là data/presentation prototype để kiểm tra runtime nhiều phase.
- **Unresolved:** ý nghĩa tâm lý cuối cùng, voice, canon dialogue, portrait/art chính thức,
  tên/ý nghĩa phase, final moveset/balance, điều kiện thắng/thua canon và beat sau combat.
- Cả Victory và Defeat tạm map về `Complete` để QA được đường quay lại Story. Đây không phải
  quyết định kết quả cốt truyện.

Kiến trúc mở rộng hiện hành không nhét logic enemy vào StoryEvent hoặc controller:

```text
StoryEvent
→ CombatStep (chọn encounter + result mapping)
→ CombatEncounterData (TIME/batch/Heart/bullet tuning)
→ CombatEnemyDefinition (identity, actor prefab, policy, phases)
→ CombatEnemyRuntime (state theo attempt)
→ CombatEnemyActor + move execution + mechanic modules
→ CombatController (lifecycle và result, không biết story tiếp theo)
```

`PerPhaseHealth` reset HP và bỏ damage dư. `SharedHealthThresholds` clamp ở threshold hiện tại
để một hit không bỏ phase. `TimedSequence` chỉ đếm combat-active time và ẩn health bar.
Phase break chặn damage/input, dừng batch, clear dice/projectile phase cũ, pause TIME, chạy
phase-exit/dialogue hook, rồi mới enter phase mới và mở lại simulation. Mid-phase dialogue giữ
nguyên Heart, dice, projectile và move cadence.
