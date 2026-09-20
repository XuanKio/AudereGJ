# Gán scene, QA và story lifecycle

[Mục lục nguồn](../05_DialogueSystem.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
## 5. Gán thoại vào scene-first map

Sau khi puzzle đã materialize, `DialogueTileBehaviour` trên GameObject/prefab là source of truth:

1. Đặt `Dialogue.prefab` hoặc thêm behaviour vào tile scene-authored phù hợp.
2. Trong Inspector, gán direct `Dialogue Data`.
3. Chọn `Trigger Once` nếu tile chỉ được phát một lần trong session.
4. Lưu scene/level prefab và Play-test.

Custom Inspector hiển thị `Grid Position`, `Dialogue Data`, `Trigger Once` và runtime `Triggered`. Chỉnh Inspector không ghi ngược về `PuzzleData`.

`Audere > Puzzle > Map Editor` chỉ còn là đường migration cho data/map cũ. Sau `Materialize/Bake To Scene`, chỉnh tile trực tiếp trong Scene/Prefab Mode.

Nếu tile chỉ đóng vai trò trigger, visual có thể giống tile thường; gameplay phân biệt bằng component chứ không cần một sprite Dialogue đặc biệt.

## 6. Setup và QA

Menu editor:

```text
Audere > Dialogue > Setup From Scene Template
Audere > Dialogue > Preview Sample
```

Menu setup bootstrap các asset còn thiếu từ mẫu Left/Right và không ghi đè catalog, sample hoặc tile prefab đã tồn tại. `Preview Sample` vào Play Mode và phát sample ngay để kiểm tra nhịp animation. Không cần chạy setup mỗi lần sửa nội dung; thoại thường ngày chỉ sửa trên `DialogueData` và catalog.

### Script chịu trách nhiệm

| Script | Vai trò |
| --- | --- |
| `DialogueCharacterId.cs` | Constant nhân vật cho dropdown. |
| `DialogueCharacterCatalog.cs` | Resolve constant thành tên và portrait. |
| `DialogueData.cs` | Cặp nhân vật và thứ tự các line Left/Right. |
| `DialoguePresentationState.cs` | State identity/portrait riêng từng speaker, reset khi đổi identity và mỗi playback. |
| `DialogueCharacterSlotView.cs` | Portrait, tint người nói/không nói và visibility của slot. |
| `DialogueBubbleView.cs` | Nội dung bubble, pop-in, rise và pop-out. |
| `DialogueController.cs` | Điều phối thứ tự character → bubble → typewriter, input và pause. |
| `GameplayUIRoot.cs` | Singleton root Canvas chứa `PuzzleUI`, `DialogueUI`, direct reference `CombatTutorialUI` và `CombatRetryUI`, persistent giữa gameplay scene. |
| `DialogueTileBehaviour.cs` | Phát data được gán cho cell khi Player bước vào. |
| `DialogueTileBehaviourEditor.cs` | Chỉnh direct scene/prefab component; không ghi ngược về `PuzzleData`; có nút mở legacy Map Editor. |

QA lịch sử ngày 2026-08-16 (trước scene-first migration):

- Unity Console: `0` error sau compile và Play Mode test;
- `Assembly-CSharp` và `Assembly-CSharp-Editor`: `0 warning`, `0 error`;
- scene chỉ có một gameplay Canvas: `GameplayUIRoot/PuzzleUI|DialogueUI`; Main Menu vẫn dùng UI riêng;
- PuzzleUI sinh đúng 3 card và cả Puzzle Manager/placement bind vào UI persistent;
- Dialogue tile `(0,0)` hiện `Dialogue_Sample`, `Trigger Once = true` trong runtime Inspector;
- sample mở được, text tiếng Việt + tên Audere hiển thị đúng, typewriter chạy khi `timeScale = 0`;
- `ForceClose` trả `IsPlaying = false` và `Time.timeScale = 1`;
- `DialogueTileBehaviour` tại cell `(0,0)` đã gọi sample thành công;
- warning còn lại là Timor chưa có portrait nguồn.

### QA tách portrait người thật / Timor bóp méo (2026-08-28)

- Đối chiếu 175 DialogueData và dependency của14 scene: wording, speaker, dialogue ID, glitch flags
  và chuỗi portrait hiệu lực không đổi;12 asset được tách identity,9 asset có production reference.
- Hai PNG Creepy mới chứa thêm hình Timor ngoài sprite rect cũ. Mở rộng rect của đúng sprite
  `_0`, giữ asset GUID và subasset fileID; không sửa pixel của PNG hay art enemy/world actor.
- Unity compile thành công. `Temp/DialogueVariants/tests_60_pass.xml`:60/60 passed
  (50 CombatEnemyRuntime,9 DialoguePresentation,1 TeacherResistance).
- Real DialogueUI test: normal → distorted → normal; Audere giữ portrait riêng; auto không claim
  input/không đổi timeScale; ForceClose hai lần chỉ callback một lần; replay khởi tạo sạch.
- Visual1920×1080 đã xem `teacher-creepy-settled.png` và `bianca-creepy-settled.png` trong
  `Temp/DialogueVariants/`: đủ hình Timor phía sau, đúng slot phải, chữ không tràn bubble.
- Kiểm hash364 file (scene/prefab/combat data và hai PNG nguồn), không đổi so với preflight.
- Bàn giao Scene120 sạch, startup=true, PlayOFF/compileOFF,0 missing script, Console0 error/warning.
  MCP job cache bị cũ sau domain reload; kết quả pass lấy từ XML Unity, không suy từ cache.
- Chưa chạy lại toàn bộ story/gameplay của14 scene hoặc visual ở4:3/ultrawide trong lượt này.

## 7. Lifecycle và Story integration (2026-08-22)

`DialogueController` có completion contract:

```text
DialogueResult.Completed
DialogueResult.Cancelled
```

- Đi hết toàn bộ line trả `Completed` đúng một lần.
- Escape, `ForceClose`, disable hoặc scene transition giữa chừng trả `Cancelled` đúng một lần.
- Callback được xóa trước khi gọi và không tồn tại sang lần `Play` sau.
- `IsPlaying` và `Time.timeScale` được khôi phục theo đúng giá trị trước dialogue, kể cả `0.5`.
- Dialogue giữ claim `GameplayInputMode.Dialogue`; đóng thoại tự khôi phục claim Puzzle/Combat bên dưới.

`DialogueStep` tham chiếu `DialogueData` và optional `DialogueController`. Nếu controller không gán, step dùng `GameplayUIRoot.Instance.Dialogue`. Step không overwrite dialogue đang chạy và chỉ đóng session do chính nó mở khi Story bị cancel.

Dialogue tile không cần visual khác tile thường nếu vai trò chỉ là trigger hành động. Layout puzzle vẫn scene-first; không ghi chỉnh sửa runtime tile ngược về layout nếu không chủ động dùng tool migration.

Trong story staging, hãy tách `DialogueData` tại nơi cần chèn hành động giữa các câu. Ví dụ
`D1_CLASSROOM_ANNOUNCEMENT` dùng các asset nhỏ quanh `WaitStep`, `SetActiveStep` và
`MoveActorStep`, nhờ vậy thoại không phải tự điều khiển actor hoặc timing của scene.
<!-- END PRESERVED SOURCE -->
