---
id: audere.visual_palette
archetype: knowledge
version: 1.0.0
schema_version: 1.0.0
cost_tier: S
summary: Shared camera, viewport-mask, transition, and contextual UI color rules for Audere production scenes.
---

# Audere — Visual Palette

> **Last updated:** 2026-08-23

Tài liệu này là nguồn chuẩn cho các màu nền hệ thống nhìn thấy khi load scene, đổi
gameplay mode hoặc khi content chưa phủ kín camera. Màu art của từng địa điểm vẫn có
thể khác nhau; không ép toàn bộ sprite/UI về cùng một màu.

## 1. Trạng thái và phạm vi

- **Established Canon:** tài liệu màu sắc này không thêm hoặc thay đổi story canon.
- **Design Intent:** mọi production camera dùng cùng fallback background để không đổi
  tông hoặc lóe màu giữa các scene.
- **Design Intent:** puzzle viewport và transition cover có màu dùng chung, được author
  từ prefab/overlay gốc thay vì override tùy scene.
- **Unresolved:** location art cuối cùng có thể thêm color grading riêng khi art direction
  được chốt; grading đó không được thay đổi các màu hệ thống bên dưới.

`SampleScene.unity` là scene template ngoài production nên không thuộc phạm vi đồng bộ.

## 2. Palette hệ thống

| Token | Hex | Unity RGBA (0–1) | Ownership | Dùng cho |
|-------|-----|------------------|-----------|----------|
| `CameraFallback` | `#160D1C` | `(0.08627451, 0.05098040, 0.10980393, 1)` | từng production `Main Camera` | Khoảng trống trong world, frame đầu khi load và nền phía sau UI. |
| `ViewportOutside` | `#0D0918` | `(0.04923587, 0.03693485, 0.09433961, 1)` | `PuzzleViewportMask.prefab` | Bốn cạnh ngoài vùng gameplay; instance không override màu. |
| `TransitionCover` | `#000000` | `(0, 0, 0, 1)` | transition `Image`; alpha do `CanvasGroup`/step điều khiển | Fade đổi mode và đổi scene. |
| `MenuNavy` | `#05142E` | `(0.02, 0.08, 0.18, variable alpha)` | Main Menu UI | Màu ngữ cảnh riêng của menu, không thay `CameraFallback`. |
| `MenuAccent` | `#298FFF` | `(0.16, 0.56, 1, 1)` | Main Menu UI | Focus/action accent của menu. |

Giá trị Hex là tên đọc nhanh; serialized Unity RGBA trong bảng là giá trị cần dùng khi
đối chiếu Inspector.

## 3. Quy tắc camera

Mọi production `Main Camera`:

- `Clear Flags`/`Clear Mode` dùng **Solid Color**, không dùng Skybox làm fallback;
- `Background` dùng `CameraFallback`;
- camera URP có `UniversalAdditionalCameraData` với cấu hình mặc định của project;
- orthographic size, position, camera follow và framing là scene-specific, không đồng bộ
  bằng palette pass;
- lighting và tint của art là scene-specific, miễn không làm đổi fallback khi content
  chưa phủ kín camera.

Main Menu được phép giữ UI xanh navy. Camera phía sau menu vẫn dùng `CameraFallback`
để scene load không lóe skybox xanh trước khi Canvas xuất hiện.

## 4. Puzzle viewport

`PuzzleViewportMask` phải là child của `Main Camera` và lấy màu từ:

`Assets/_Audere/Prefabs/Puzzle/Camera/PuzzleViewportMask.prefab`

Scene chỉ được override transform để giữ cùng tỷ lệ khung theo orthographic size. Không
override `SpriteRenderer.color` ở `Mask Top/Bottom/Left/Right`. Content trong viewport
dùng `CameraFallback` làm nền; phần ngoài dùng `ViewportOutside`.

## 5. Transition overlay

- `20_D1_Home_Morning/WORLD/World Transition Overlay/Transition Fade` dùng `TransitionCover`.
- `30_Classroom/Scene Transition Overlay/Fade` dùng `TransitionCover`.
- Alpha presentation thuộc `CanvasGroup` hoặc `CanvasFadeStep`; không sửa alpha gốc của
  `Image` để tạo biến thể màu.
- Destination scene bắt đầu được che kín, normalize presentation rồi mới fade in.

## 6. Production scene matrix

| Scene | Camera fallback | Viewport | Ghi chú |
|-------|-----------------|----------|---------|
| `00_Bootstrap` | Không có camera | Không | Chuyển ngay sang Main Menu. |
| `10_MainMenu` | `CameraFallback`, Solid Color | Không | UI giữ palette navy/blue riêng. |
| `20_D1_Home_Morning` | `CameraFallback`, Solid Color | `ViewportOutside` từ prefab | Nguồn tham chiếu gameplay hiện tại. |
| `30_Classroom` | `CameraFallback`, Solid Color | `ViewportOutside` từ prefab | Khung có cùng tỷ lệ màn hình với scene 20. |

## 7. Checklist khi thêm scene

1. Copy giá trị `CameraFallback` vào production `Main Camera` và dùng Solid Color.
2. Nếu có puzzle viewport, instantiate prefab chuẩn dưới camera và chỉ chỉnh transform.
3. Transition cover dùng Image đen nguyên alpha; animation đi qua CanvasGroup/StoryStep.
4. Chụp Game View ở target aspect ratio và kiểm tra không có dải màu lạ ngoài content.
5. Play từ scene trước đó để kiểm tra không lóe skybox/fallback trong lúc load.
6. Validate scene và xác nhận Console không có error trước khi cập nhật tài liệu.
