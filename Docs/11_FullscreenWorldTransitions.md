# Audere Fullscreen World Transitions

> **Last updated:** 2026-08-28

Tài liệu này định nghĩa runtime contract cho transition giữa các world presentation. Việc
chọn transition theo story job và registry scene nằm tại
`.agents/skills/audere-world-transition-authoring/references/transition-catalog.md`.

## 1. Trạng thái quyết định

- **Established implementation state:** transition fullscreen dùng
  `FullscreenTransitionProfile` asset chung. Scene chỉ tham chiếu profile; duration, mode-swap
  time, material và shader curves không serialize riêng trong scene.
- **Established implementation state:** `30_Classroom` dùng profile
  `Dreamy Disorientation` dài `1.50 s`, đổi Story → Combat ở `1.10 s`, xoay/méo nhẹ quanh
  Audere rồi để Combat hiện dần khi distortion hạ xuống.
- **Established implementation state:** Combat → Story tiếp tục dùng neutral fade của
  `WorldModeStep`.
- **Design Intent:** sửa một shared profile/material/shader sẽ áp dụng cho mọi scene đang
  tham chiếu nó. Chỉ tạo profile khác khi scene cần được tune độc lập hoặc có visual grammar
  khác về bản chất.
- **Unresolved:** audio low-pass/cue, enemy, ý nghĩa in-world/psychological của combat, kết
  quả canon và các mechanic/enemy module sau này.

## 2. Kiến trúc dùng chung

```text
Renderer2D.asset
└── Audere Fullscreen World Transition
    [FullScreenPassRendererFeature, inactive mặc định]

Assets/_Audere/Data/Transitions
└── WorldTransition_*.asset
    └── FullscreenTransitionProfile
        ├── shared material
        ├── duration + modeSwapTime
        ├── optional focus requirement
        └── shader float AnimationCurves

WORLD
├── WorldModeController
└── FullscreenTransitionController
    ├── world Camera              direct scene reference
    └── Renderer Feature          direct sub-asset reference

STORY/D*_EVENT
└── *_TransitionStep
    └── FullscreenWorldModeTransitionStep
        ├── shared profile        direct asset reference
        ├── controllers           direct scene references
        └── focus Renderer        only when profile requires it
```

Renderer Feature dùng `AfterRenderingPostProcessing`, fetch color buffer, requirements
`None`, không bind depth-stencil và inactive khi idle. Controller clone material được profile
chọn ở runtime, bind clone vào feature, rồi restore/destroy clone khi complete/cancel. Vì vậy
timeline runtime không ghi ngược vào material asset.

`Screen Space Overlay` UI không đi qua camera shader; dialogue phải đóng trước fullscreen
step. Profile đổi presentation nhưng không sở hữu gameplay input.

## 3. Shared profile contract

`FullscreenTransitionProfile` chứa:

- `profileId` và display name ổn định cho catalog/log.
- Một shared material.
- `duration` và `modeSwapTime`.
- `usesFocusRenderer`.
- Tên property thời gian, mặc định `_EffectTime`.
- Danh sách shader float property + `AnimationCurve` theo thời gian thực.

Scene không giữ bản sao curve. Muốn đổi toàn bộ consumer, sửa profile hoặc shared shader/
material. Khi đổi tên shader property, phải update profile và setup tool trong cùng change.

## 4. Profile Dreamy Disorientation

Shader xử lý theo thứ tự:

1. Lấy focus từ `Audere SpriteRenderer.bounds.center`, đổi sang viewport UV và aspect-correct.
2. Rotate toàn frame rất nhẹ quanh focus và zoom tối đa khoảng `1.06`.
3. Thêm wide low-frequency wave như hình nổi trên mặt nước.
4. Drift UV chậm theo X/Y để tạo cảm giác background trôi lệch khỏi cơ thể.
5. Radial bend vài pixel quanh Audere; không dùng vortex hoặc hút hình về tâm.
6. Multi-sample dọc một hướng để tạo smear/after-image mềm.
7. Chromatic drift rất nhỏ; không dùng VHS tearing hoặc static noise.
8. Dreamy veil tối tăng ở mode swap, rồi giảm để Combat hiện xuyên qua distortion.

| Thời gian | Presentation |
| --- | --- |
| `0.00` | Hình bình thường. |
| `0.30` | Zoom `1.03`, rotation `-0.7°`; Audere bắt đầu mất thăng bằng. |
| `0.70` | Rotation đổi chậm sang `+1°`; wide wave và drift đã đọc được. |
| `0.90` | Radial bend/smear tăng nhưng scene vẫn nhận ra rõ. |
| `1.10` | Distortion và veil đạt đỉnh; gọi `ApplyModeImmediate(Combat)`. |
| `1.25` | Combat đã hiện, còn after-image/tilt nhẹ. |
| `1.50` | Mọi curve về neutral, feature tắt; Combat rõ hoàn toàn. |

Nếu frame dài nhảy qua `modeSwapTime`, controller vẫn apply đúng profile state ở mốc swap và
render ít nhất một frame trước khi tiếp tục cleanup.

## 5. Story và input lifecycle

```text
190_HoldAfterTimor
→ 200_ClassroomIsConsumed       [Dreamy Disorientation; swap ở 1.10]
→ cleanup hoàn tất ở 1.50
→ 210_PlayCombatPrototype       [CombatController.Play claim input]
→ 220_ReturnToStory             [neutral fade]
→ 230_HoldAfterCombat
```

`ApplyModeImmediate(Combat)` chỉ bật presentation/root/system. Combat input chỉ được cấp khi
`CombatStep` gọi `CombatController.Play()` sau khi fullscreen step Complete.

## 6. Cancel, replay và propagation

- Cancel trước/sau swap: dừng coroutine, feature inactive, runtime material bị destroy và
  StoryStep khôi phục `sourceMode`.
- Callback dùng transition version; callback cũ không Complete replay mới.
- Replay tạo runtime material mới từ profile asset, không giữ curve state của lần trước.
- Scene 20 và scene không chạy fullscreen transition không chịu full-screen blit.
- Chỉnh `WorldTransition_DreamyDisorientation.asset` áp dụng cho tất cả scene tham chiếu cùng
  GUID; không cần chạy lại scene authoring chỉ để thay curve.

## 7. Authoring và QA

### BGM dùng chung

Từ 2026-08-28, `AudioService` giảm BGM theo tiến độ từ đầu transition tới `ModeSwapTime`,
giữ im đến khi fullscreen effect hoàn tất, rồi phục hồi nhạc theo presentation đích.
Đổi vào Combat chọn `Music_Combat` (slot trống hiện tại = im lặng); quay về Story/Puzzle
chọn `Music_Exploration`. Cancel giải phóng owner audio riêng và để step khôi phục source
mode; không ghi đè âm lượng Settings. Neutral fade và scene-load có cover/owner riêng,
nên một transition kết thúc không thể mở nhạc khi màn đen khác vẫn còn.

Đây là contract BGM bên ngoài shader profile; không thêm cue, low-pass hay timeline riêng
trong scene. Chi tiết setup và giới hạn QA nằm ở `Docs/03_AudioSystem.md`.

### Setup và checklist hình ảnh

Setup idempotent riêng cho Scene 30:

```text
Audere > Story > Setup Classroom Combat Transition
```

Menu bind shared profile, controller, Camera, Audere focus renderer và renderer feature; nó
không sửa `DialogueData`.

Checklist:

- Feature inactive ngoài transition.
- Profile reference không null; focus renderer có mặt nếu profile yêu cầu.
- Capture early/peak/swap/target reveal/clean target ở 16:9, kiểm tra thêm 4:3 và ultrawide.
- Mode swap không lộ một clean cut không chủ ý.
- Combat chưa claim input trước khi fullscreen step Complete.
- Cancel trước/sau swap và replay đều trả state sạch.
- C#/shader compile sạch; Console 0 error; scene không missing script/reference.

## Presentation-only use: Day3 fatigue

`FullscreenTransitionController.PlayPresentation(profile, focusRenderer, onEnded)` dùng shared material/profile nhưng **không gọi WorldModeController** và không claim music duck. `FullscreenPresentationStep` sở hữu callback/cancel; normal completion và cancellation đều disable feature/reset runtime material. Public mode-transition API không đổi.

Consumer: Scene110, `170_TheRoomDriftsWhileBiancaCalls/WorldSway`, profile `WorldTransition_FatigueSway.asset` (4.4s, tiny tilt/zoom/wave/drift, không veil). Bianca gọi qua PlayAuto ở parallel branch; scene chỉ load khi cả hai branch xong. Scene120 vào combat vẫn dùng Dreamy Disorientation chung, về Story dùng neutral fade. Catalog trong skill ghi riêng hai mục để không lấy profile choáng không-che-màn làm mode swap. Xem [Day3 workflow](15_Day3_BoardTeacher_StoryWorkflow.md).
