# Danh mục script và phần deferred — checkpoint 2026-08-23

[Mục lục nguồn](../00_ProjectOverview.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
## Script inventory

### Core — `Scripts/Core/` (`Audere.Core`)
| Script | Responsibility | Status |
|--------|----------------|--------|
| `Bootstrapper.cs` | Single entry point. `DontDestroyOnLoad`; finds every `IGameService` under the services root, `Initialize()`s them in sibling order, then `SceneFlow.Load(firstScene)`. No gameplay logic. | Active |
| `IGameService.cs` | `Initialize()` contract for every global service. | Active |
| `SceneFlow.cs` | Owns all scene load/unload. `Load(name)` async Single-mode; `IsBusy` guard; `SceneFlow.Instance`. | Active |
| `GameScenes.cs` | Scene-name constants — SSOT, mirrors Build Settings. | Active |

### UI — `Scripts/UI/` (`Audere.UI`)
| Script | Responsibility | Status |
|--------|----------------|--------|
| `MainMenuController.cs` | Auto-wires serialized `Button` refs in code. New Game → `SceneFlow.Load(GameScenes.Day1HomeMorning)`. | Active |

### Audio — `Scripts/Audio/` (`Audere.Audio`)
| Script | Responsibility | Status |
|--------|----------------|--------|
| `AudioId.cs` | Enum of stable, explicitly-numbered sound ids (UI 1000 / Nilah 2000 / Timor 3000 / Exploration 4000 / Combat 5000 / Music 9000). Ids permanent, never reused. | Active |
| `AudioEntry.cs` | `[Serializable] { id, clip, volume }`. | Active |
| `AudioCatalog.cs` | `ScriptableObject` `List<AudioEntry>` → dictionary lookup. `TryGet`. | Active |
| `AudioService.cs` | `IGameService`. `Play(AudioId)` → catalog → `AudioSource.PlayOneShot`. `AudioService.Instance`. 2D. | Active |

### Dialogue — `Scripts/Dialogue/` (`Audere.Dialogue`)
| Script | Responsibility | Status |
|--------|----------------|--------|
| `DialogueCharacterId.cs` | Constant nhân vật dùng làm dropdown ổn định trong dialogue data. | Active |
| `DialogueCharacterCatalog.cs` | Map character constant → tên hiển thị và portrait. | Active |
| `DialogueData.cs` | Data đoạn thoại: nhân vật Left/Right và danh sách line theo speaker. | Active |
| `GameplayUIRoot.cs` | Singleton root Canvas chứa `PuzzleUI` và `DialogueUI`, `DontDestroyOnLoad` giữa gameplay scenes; tự hủy khi vào Main Menu. | Active |
| `DialogueController.cs` | Left/right presentation, typewriter, emphasis, input và pause gameplay bằng unscaled time. | Active |
| `DialogueTileBehaviour.cs` | Scene/prefab component giữ `DialogueData` và phát thoại khi Player bước vào tile. | Active |

### Story — `Scripts/Story/` (`Audere.Story`)
| Script | Responsibility | Status |
|--------|----------------|--------|
| `StoryDirector.cs` | Registry StoryEvent trong scene, one-event-at-a-time, direct-reference/ID play và deferred auto-next. | Active |
| `StoryEvent.cs` | Chạy đúng một StoryStep trên mỗi direct child theo sibling order. | Active |
| `StoryStep.cs` | Base coroutine lifecycle `Running/Completed/Cancelled/Failed`, callback one-shot. | Active |
| `Steps/*.cs` | Dialogue, Puzzle, Combat, WorldMode, Wait, SetActive, MoveActor và board transition. | Active |

### Puzzle lifecycle/input — `Scripts/Puzzle/`, `Scripts/Input/`
| Script | Responsibility | Status |
|--------|----------------|--------|
| `PuzzleRootCoordinator.cs` | Shared Player/runtime, normalize level prefabs, Goal → PlayerStart hand-off và reveal ordering. | Active |
| `PuzzleController.cs` | Scene-first puzzle lifecycle callback và Puzzle input claim. | Active |
| `GameplayInputGate.cs` | Token/owner-safe mode stack cho Puzzle, Combat và Dialogue overlay. | Active |

### World — `Scripts/World/` (`Audere.World`)
| Script | Responsibility | Status |
|--------|----------------|--------|
| `WorldGameplayMode.cs` | Stable mode enum: Puzzle/Combat. | Active |
| `WorldModeController.cs` | Bật/tắt mode roots, systems, PuzzleUI và camera qua black-fade transition. | Active |

### Combat — `Scripts/Combat/` (`Audere.Combat`)
| Script | Responsibility | Status |
|--------|----------------|--------|
| `CombatSymbol.cs` | Stable dice faces: Attack, Armor, Heal. | Active |
| `CombatEncounterData.cs` | Encounter ScriptableObject: enemy HP, TIME-as-health, continuous batches, Heart hit tuning và attack patterns. | Active |
| `CombatController.cs` | Real-time loop: mouse-driven Heart/dice input, bullets, immediate effects và win/lose. | Active |
| `CombatBoardView.cs` | Shared Battle Box, mouse cursor/Heart, enemy name, timer và pool dice/bullets runtime. | Active |
| `CombatCatchCursorView.cs` | Cursor stun-state presentation và blocked-action `X` feedback. | Active |
| `CombatDieView.cs` | Dice movement, reroll và capture feedback. | Active |
| `CombatPlayerView.cs` | Heart visual ở tâm Catch Cursor, hit flash và invulnerability. | Active |
| `CombatBulletView.cs` | Enemy bullet velocity, bounds và pooling. | Active |

### Deferred (documented, NOT built)
| Planned | Why deferred |
|---------|--------------|
| `SaveManager` (auto-save) | Save format depends on the not-yet-defined runtime data model. |
| `GameSettings` (volumes/quality/controls) | After the home-morning core loop exists; will feed `AudioService`. |
| Music playback | `Music_*` ids exist; only one-shot SFX wired today. |

<!-- END PRESERVED SOURCE -->
