# World lifecycle, music ownership và Battle Box

[Mục lục nguồn](../06_CombatGameplay.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
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
(Teacher là7/4/0, tổng15HP);
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
- `L` ×5 (mỗi lần cách nhau tối đa 1.5 s): bỏ qua đoạn hiện tại để vào bước combat sắp tới trong scene. Dùng `StoryDirector` và các `CombatStep`/world controller đã được gán; không thêm encounter vào scene không có trận. Khi đang combat hoặc chờ Retry, phím này không khởi động lại trận. Shortcut có trong runtime build, không chỉ Editor.
- Menu `Audere > Combat > Debug > Switch To Puzzle/Combat`.

Shortcut `L` giữ `CombatStep` dưới quyền sở hữu của StoryEvent để dùng luồng kết quả/Retry hiện có; đóng bước đang chạy, gỡ cover và chuyển presentation/camera sang Combat trước khi bắt đầu. Kiểm tra 2026-09-20: biên dịch không lỗi; kiểm tra tham chiếu ở 6 scene combat đạt; kiểm tra Play ở scene60 xác nhận 4 lần chưa nhảy, lần thứ 5 vào trận Bianca từ COOP, khoảng nghỉ reset bộ đếm và nhấn tiếp không restart trận. **Kiểm tra toàn luồng sau Victory còn chưa hoàn tất:** lần chạy gần nhất dừng ở setup vì `GameplayUIRoot.Instance` chưa được khởi tạo sau `EnterPlayMode`; không tính là pass toàn bộ suite `CombatShortcutTests`.

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
│   ├── Stun Trail Root            RectMask2D; vệt phấn chỉ chặn catch trong field
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
├── Exterior Projectile Root       opt-in cho vòng phấn ngoài field, không dùng cho đạn thường
├── Enemy                          status presentation ở phía trên
│   ├── Enemy Mount                giữ `CombatEnemyActor` scene-authored của encounter
│   ├── Name
│   │   ├── Image                  khung tên `420 × 120`
│   │   └── Enemy Name             TMP cỡ `57`, căn giữa và wrap trong Image
└── Timer Track                    TIME-as-health; chưa có status text
```

Không dựng player portrait, dãy heart UI hoặc status text ở giai đoạn hiện tại. Audere chỉ được biểu diễn bằng Heart ở tâm Catch Cursor. Dice, Heart và bullet không nằm ở các khu riêng: mouse vừa chọn dice vừa điều khiển vị trí né đạn trong cùng một vùng nhìn.

<!-- END PRESERVED SOURCE -->
