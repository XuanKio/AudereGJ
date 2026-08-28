# Audere Combat Gameplay

Tài liệu này mô tả combat real-time dùng chung trong scene `20_D1_Home_Morning` và hand-off prototype ở
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

Scene40 là mẫu tách systems/presentation: controller trên `SYSTEMS/Combat Systems`,
board/actor dưới `WORLD/Combat Root`. Scene120 và tool tạo Day3 đã theo mẫu này;
reparent giữ component identity và direct references, không tái tạo enemy art/board.

### Required auto-dialogue tại shared HP threshold

`SharedHealthThresholds` tôn trọng `RequiredBeforePhaseAdvance`, và
`RequiredBeforeVictory` ở phase cuối. Damage clamp tới threshold authored như cũ
(Teacher là8/4/0);
nếu còn cue bắt buộc chưa resolve thì runtime giữ pending, không nhận damage thêm.
Combat-local TIME, projectile, dice và move cadence vẫn chạy. Khi cue resolve, tick
kế tiếp đi qua phase-break/Victory cleanup chung; không cần hit bổ sung và không
chuyển damage dư. Không có gate thì behavior cũ không đổi. Defeat/cancel vẫn ngắt
được; restart xóa pending, played/resolved cue state. Controller vẫn kiểm session
và phase version trước khi nhận callback; không check enemy ID.

Teacher Day3 dùng một auto sequence không lặp/không interrupt mỗi phase để Audere
phản kháng tăng dần. Chi tiết lời, portrait và QA ở `Docs/15_Day3_BoardTeacher_StoryWorkflow.md`.

Debug mode:

- `F1`: Puzzle.
- `F2`: Combat.
- `F3`: Story.
- Menu `Audere > Combat > Debug > Switch To Puzzle/Combat`.

## 2. Shared Battle Box

### Music ownership

`AudioService` dùng chung `Music_Combat` khi World đang ở Combat hoặc có combat session
đang chạy. `CombatController` chỉ acquire/release music owner trong lifecycle, không giữ
clip hay nhạc riêng theo enemy ID. Completion/cancel/retry không bật nhạc thường khi
World vẫn đang hiển thị Combat/Retry. Slot combat hiện để trống có chủ ý; gán clip vào
`AudioCatalog` sau sẽ áp dụng chung. Fade/transition không ảnh hưởng SFX, thoại hay TIME.
Xem `Docs/03_AudioSystem.md` cho setup và QA.

### Board presentation

Prefab chính:

```text
Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab
```

```text
CombatBoard
├── Frame
├── Dice Field                     RectMask2D; toàn bộ gameplay real-time
│   ├── Stun Zone Root             vùng chấm tím chặn thao tác cursor
│   ├── Projectile Mask            RectMask2D inset 14 px; hazards không vẽ đè lên viền
│   │   ├── Bullet Root            enemy bullets
│   │   └── Laser Root             telegraph/laser hazards theo session + phase
│   ├── Dice Root                  dice bắt/reroll bằng mouse
│   ├── Catch Cursor Root
│   │   └── Catch Cursor           vùng bắt/reroll đi theo mouse
│   │       └── Audere Heart Root  tâm nhận đạn của Catch Cursor
│   │           └── Heart Visual   nested prefab, sprite vuông placeholder
│   └── Feedback FX Root           root dự phòng cho board feedback, không tạo text khi catch
├── Airborne Dice Overlay          không mask; dice đang tung được vẽ trên viền board
├── Enemy                          status presentation ở phía trên
│   ├── Enemy Mount                giữ `CombatEnemyActor` scene-authored của encounter
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
                     → tạo session combat mới, một phase 6 HP và TIME đầy 45 s
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
shutdown tutorial actor/mechanic/move, return dice/projectile, rồi khởi tạo `Enemy_KhoangLang`
như một combat attempt mới. Attempt này dùng TIME `45 s`, một phase `6 HP` và tốc độ bình thường.
Moveset `OrderedLoop` chạy Aimed Fan → Side Sweep → Rain liên tục trong cùng thanh máu. Enemy move
cùng dice batch được schedule ngay khi state trở lại `Playing`; không có phase marker trên HUD.

### Production narrative pacing sau tutorial

Production phase dùng cùng `CombatDialogueCue` nhưng tách presentation khỏi trigger:

```text
PhaseEnter                       → AutoCombatDialogue qua DialogueUI chuẩn (combat vẫn chạy)
MoveStarted: Side Sweep          → AutoCombatDialogue qua DialogueUI chuẩn (combat vẫn chạy)
CueCompleted: Side Sweep         → Heart wobble → ModalDialogue (combat-local pause)
HealthAtOrBelow: 2               → BackgroundTextField (không lấy input)
```

`AutoCombatDialogue` đọc `DialogueData` bằng chính `DialogueController`: Audere luôn ở slot trái,
Khoảng Lặng ở slot phải, dùng portrait/bubble/typewriter chuẩn nhưng không claim
`GameplayInputMode.Dialogue`. Thời lượng mỗi câu là `max(1.4 s, characters / 20 + 0.55 s)` với
gap `0.18 s`. `BackgroundTextField` dùng pool `36` TMP label phủ theo grid có jitter, font
`28–46`, alpha `0.10–0.20`; mỗi line có ghost lệch để tạo smear và dao động rotation/scale mềm.
Layer không raycast, nằm sau gameplay và được clear cùng session.

Attack hit tiếp tục dùng `Assets/_Audere/AssetGame/Vfx/scratch.aseprite`. Vì enemy actor hiện nằm
trong World Space Combat Canvas, runtime đổi `100 PPU` của SpriteRenderer sang Canvas pixel scale
và đẩy cả SpriteRenderer/SortingGroup lên trên sorting order của Canvas trước khi phát animation.

Side Sweep riêng của Khoảng Lặng dùng `ConvergingSideCorridorMove`: hai mép phát projectile đối
xứng, bỏ trống hành lang giữa co từ `46%` xuống `24%` chiều cao box nhưng không nhỏ hơn `72 px`.
Projectile đứng ở mép `0.35 s` với collision tắt trước khi bay vào. Scene 20 vẫn dùng move mẫu cũ.

Cue `audere-timor-anchor` được đánh dấu `RequiredBeforeVictory`. Nếu người chơi gây lethal damage
trước khi cue resolve, runtime chỉ clamp lần lethal đó ở `1 HP`; non-lethal damage vẫn giữ nguyên,
không tạo phase phụ và damage dư không được tích sang sau dialogue. Retry tạo runtime mới nên cue,
auto-dialogue, text field và gate đều reset.

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
| `CompositeCombatMove.cs` | Chạy nhiều execution độc lập song song trong một phase, cancel toàn bộ theo cùng lifecycle. |
| `StunZonePressureMove.cs` / `CombatStunZoneView.cs` | Data pulse Hidden/Telegraph/Blocking/Fade và presentation/collision state của Stun Zone. |
| `CombatController.cs` | Session, TIME, input, dice batch, result và atomic phase hand-off; không chứa logic riêng của boss. |
| `CombatTutorialView.cs` | Text Scene-20-style, spotlight cutout theo target và preview trực tiếp prefab dice; unscaled fade, không nhận input. |
| `CombatBoardView.cs` | Shared Battle Box, actor mount, projectile pool theo prefab, dice/Heart/cursor/feedback. |
| `CombatCatchCursorView.cs` | Chuyển trạng thái viền cursor và phát feedback `X` khi thao tác bị Stun Zone chặn. |
| `CombatDieView.cs` | Chuyển động/bounce, reroll và capture. |

### Scene-authored enemy presentation

Mỗi production scene đặt prefab instance của enemy trực tiếp dưới `Combat Board/Enemy/Enemy Mount`
và bind instance đó vào `CombatBoardView.authoredEnemyActor`. Đây là actor thật được combat dùng,
không phải preview: runtime không clone actor, không copy transform và không chuẩn hóa scale. Vì vậy
sprite override, kích thước, offset, rotation và scale được chỉnh riêng trên scene sẽ được giữ nguyên.
Runtime chỉ bật/tắt instance và gọi lifecycle mechanic; khi cleanup actor scene-authored được shutdown
rồi ẩn, không bị destroy. `CombatEnemyDefinition.ActorPrefab` tiếp tục là nguồn để authoring tool tạo
instance ban đầu và là fallback cho scene/debug cũ chưa migrate, không phải nguồn presentation của
Scene 30/40 khi Play.
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

Attack dùng animation pixel `Assets/_Audere/AssetGame/Vfx/scratch.aseprite`: mỗi hit tạo một instance tại shared root `CombatBoard/Enemy/VFX`, root này được căn trùng tâm và kích thước với `Enemy Mount`. VFX không thuộc prefab actor và không bị thay reference khi đổi enemy; instance chạy đúng một vòng clip bằng unscaled time rồi tự hủy. Enemy rung mạnh trong phần đầu; các nhịp flash trắng được rải suốt clip và nhịp cuối kết thúc cùng scratch. Shield và Heal không tạo text hoặc projectile chữ.

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
  `d1-classroom-khoang-lang`, policy `PerPhaseHealth`, một phase `6 HP`. Phase dùng một moveset
  `OrderedLoop` chứa Aimed Fan, Side Sweep và Rain. Runtime nhiều phase vẫn là kiến trúc dùng chung;
  encounter D1 hiện cố ý dùng một thanh máu liền mạch.
- **Design Intent:** beat trước combat trình bày đây là nỗi lo đang giành quyền trả lời thay Audere;
  Audere chọn tự đối diện với nó. Điều này không tự xác lập boss là một thực thể literal.
- **Established implementation state:** Khoảng Lặng có các line/dialogue `PLACEHOLDER` do Xuân
  cung cấp; catalog tạm dùng portrait Audere nhưng vẫn hiển thị tên `Khoảng Lặng`.
- **Unresolved:** ontology và ý nghĩa tâm lý cuối cùng, final voice/dialogue của Khoảng Lặng,
  portrait/art chính thức, final moveset/balance, điều kiện thắng/thua canon và beat sau combat.
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

## 11. Captured-dice sequence và D1 Timor night pressure (2026-08-25)

`CombatPhasePolicy.CapturedDiceBatchSequence` là policy data-driven cho encounter mà phase tiến
theo việc người chơi bắt hết một batch authored, không tiến theo HP:

```text
Phase enter → move + AutoCombatDialogue + scripted dice batch
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
`AutoCombatDialogue` tiếp tục dùng DialogueUI chuẩn, không claim Dialogue input và không pause
combat. `DialogueData` hỗ trợ portrait override ở cấp left/right và từng line. Timor dùng art trong
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
[Audio System](03_AudioSystem.md#combat-volley-sfx--2026-08-28).

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
và trở về Story. Chi tiết và giới hạn QA ở [Audio System](03_AudioSystem.md).
**Unresolved:** cảm giác nhạc/độ khó qua một lượt chơi tay đầy đủ; chưa tuyên bố cân bằng cuối.

## Bianca supplies encounter — 2026-08-28

**Design Intent (Xuân):** Timor fills the silence with Audere's fear of Bianca's judgement. Combat barks are a projection, not evidence of the real Bianca's thoughts.

- Scene 60 binds the existing BiancaSupplies encounter and enemy GUIDs. Legacy `PLACEHOLDER` filenames remain for reference compatibility; encounter ID is now `d2-bianca-perceived-judgement`.
- Shared HP: 10; TIME: 90. Five phases use thresholds `6, 6, 2, 2, 0`. The two held-threshold phases set `advanceOnMoveComplete=true`, reject damage and stop normal batches.
- Normal batches contain three dice. `maximumAttacksPerBatch=2` reserves both live and already caught Attack dice; reroll excludes only its replaced die. Other encounters retain zero/unlimited default and the shared dice constants remain unchanged.
- Wrong Box: three evenly spaced stationary Attack/Shield/Heal choices. Each catch resolves once, hides all three, and has 60% explosion probability. Successes accumulate across failures; two successes resume ordinary combat at 6HP. Choices are not normal damage/heal/shield rewards.
- Returning special: at 2HP clear prior hazards/dice and run waves of 1, 2, then 3 spinning bullets. Bianca's authored `ReturningOrbitMove.horizontalTraversal=true` now travels horizontally across the field and reverses along the same lane, easing at both ends. Other assets retain the original orbit by default. Flight duration remains 4 → 2.8 seconds with a 0.65-second stationary/non-damaging telegraph. `Bullet_Bianca_Returning.prefab` is 69×69 (the former 46×46 enlarged 1.5×), uses the exact `dan_bianca` sprite, and its travel bounds leave room for the spinning corners inside the field. Damage begins after the telegraph; pause, fade and pool reuse reset/freeze the trajectory safely.
- Normal attacks compose existing narrative pressure patterns with horizontal Battle Box movement. The opening `Move_Bianca_0` also contains `Pattern_Bianca_OpeningBullets`: 3 aimed ordinary bullets per shot, 22° spread, speed 145, interval 1.15 seconds. Its lead-in is 0.01 seconds so the first active ticks already show bullets while retaining the existing MoveStarted bark. Other move lead-ins, cues, HP/TIME, Shield and dice constants are unchanged. Repeatable/interrupting combat cues avoid a stale speech queue; Audere's phase reply continues on later attacks.
- `DialogueLine.glitchPortraitTransition` briefly alternates old/new portraits and settles to the requested override; cancellation/disable restores the portrait transform. Bianca projection uses `Bianca_Creepy_0`, occasional calls use `Bianca_0`.
- Victory fades the combat enemy for 0.9 seconds before the Story hand-off. The board is locally active beneath Combat Root; only its parent owns mode visibility.
- Scoped authoring: `BiancaCombatAuthoring.AuthorLoadedScene()`. The generic school supplies builder preserves the dedicated event/encounter once authored.
- Focused projectile-only authoring: `Audere/Combat/Polish Bianca Projectiles Only`. It updates the two existing move assets, the returning bullet prefab and the shared opening shot asset idempotently, without rebuilding the scene, boss, dialogue or post-combat flow. The full Bianca author calls the same focused polish at the end.

### Projectile polish QA — 2026-08-28

- `CombatEnemyRuntime` now consumes only the elapsed portion of a move lead-in and ticks the move with the rest of that frame. This prevents a slow startup frame from stretching the 0.01-second opening wait. Paused combat still consumes neither lead-in nor move time.
- Final combined run: **63/63 passed** (`BiancaProjectilePolishTests`, `CombatEnemyRuntimeTests`, `EveningNightPressureTests`). The Scene60 UnityTest enters Play, calls the existing `Play(...)`, observes three ordinary projectiles during the first 0.078 seconds of combat-active time, and verifies single cancellation/input cleanup.
- The same live board/pool then runs the authored returning execution with a fixed seed: waves 1/2/3, 69×69 size, stationary telegraph, constant lane Y, full-width outbound/retrace and idempotent cleanup passed. This is targeted execution QA, not a new full boss victory/Retry playthrough.
- Horizontal trajectory geometry passed 16:9, 4:3 and 21:9 tests in both directions. Screenshots in `Temp/BiancaPolishQA` were visually checked at 1920×1080 only; other aspect ratios and full manual balance were not replayed.
- Unity compiled successfully; Scene60 validation found 0 missing scripts/broken prefabs. No unexpected Play logs. The Editor suite emits existing synthetic Test Board actor-fallback warnings and the Test Runner results-save message; these are not production runtime errors.
- Read-only Scene20/30 inspection also found 0 missing scripts/broken prefabs. Scene20's existing inactive debug CombatController has no board reference (`playOnStart=false`); it was not changed here. A full Scene20/30 gameplay replay was not part of this polish pass. Scene60 was left stopped, saved/clean, with StoryDirector startup enabled.

### Day3 teacher pressure — 2026-08-28

Scene120 adds a **Design Intent** encounter: 12 shared HP, thresholds8/4/0,120TIME,3dice/batch,max2Attack. Separate editable `Enemy_Teacher_PLACEHOLDER` actor; no boss-specific code in CombatController or changes to shared dice constants. New composable moves: ChalkFence, ChalkSweep, SineProjectileStream, VerticalPlayerImpulse; combine with existing laser/field-shift. Combat projections now use the teacher's right-side DialogueUI portrait `Co_giao_Creepy_0`, introduced by Timor's “Chắc cô đang nghĩ…”. Occasional caring calls glitch back to `Co_giao_0`, like Bianca in Scene60. These are distorted perceptions, not canon hostility from the real teacher; enemy art and pre-combat dialogue remain unchanged.

Teacher dialogue data lives in `Data/Dialogue/Day3/TeacherCombat`: six projection snippets, one Timor prefix, three Audere replies. Each phase has one opening `PhaseEnter` cue, two repeatable move-bound cues and one Attack-catch reply. All use non-modal `AutoCombatDialogue`; TIME/projectiles/input continue. A separate opening cue is necessary because the existing runtime intentionally considers the first zero-lead-in move already observed; no move timing or runtime behavior was changed for this dialogue pass.

Teacher ordinary bullets use the shared `EnemyBullet.prefab` / `dan.aseprite`, including the sine stream and the laser move's fallback projectile reference. Only the special top/bottom chalk fences and rotating sweeps use `Bullet_ChalkRod` / `phan.aseprite`; laser presentation stays shared and separate. Do not replace all of this enemy's bullets with chalk. The legacy `Move_ChalkSineStream` filename/GUID is retained; `Bullet_ChalkGrain` is no longer referenced by Teacher moves. Scoped binding, pooling and visual QA are recorded in the Day3 workflow.

`CombatBulletView` optionally owns a per-spawn `ICombatProjectileMotion`; Setup/Return/Fade cancel it, active combat delta alone advances it. `CombatRectCollision` uses oriented-rectangle SAT to avoid false hits in rotating chalk's empty bounding-box corners. `CombatBoardView` exposes owner-scoped vertical control (Y impulse while X stays steerable), released on move/session cleanup. Full authoring, timing and 90-test regression evidence: [Day3 workflow](15_Day3_BoardTeacher_StoryWorkflow.md).
