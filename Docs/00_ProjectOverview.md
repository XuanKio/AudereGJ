---
id: audere.overview
archetype: state
version: 1.0.0
schema_version: 1.0.0
cost_tier: M
summary: Top-level map of the Audere project — what it is, the doc index, folder map, script inventory, decision log.
---

# Audere — Project Overview

> **Read this first.** It's the index + living map so a new session skips re-scanning the repo.
> **Last updated:** 2026-08-15 · **Engine:** Unity 6000.0.79f1 (URP, 2D)

## What Audere is

A 2D Unity 6 game-jam project. Concrete design is still being defined; the current
assets + audio ids imply the shape:

- **Exploration** via tiles you rotate / select / connect (`Tile_Rotate`, `Tile_Select`, `Tile_Connect`).
- **Combat** driven by dice (`Dice_Select`, `Dice_Roll`, `Dice_Hit`).
- Two characters: **Nilah** (step/hurt sfx) and **Timor** (a cat — meow/step).

> Treat the gameplay bullets as *inferred from assets*, not a locked GDD. Update when the design firms up.

## Documentation index

| Doc | Covers |
|-----|--------|
| `00_ProjectOverview.md` (this) | Index, folder map, script inventory, decision log. |
| [`01_ProjectSetup.md`](01_ProjectSetup.md) | Unity version, packages, how to open/run/build. |
| [`02_Bootstrap.md`](02_Bootstrap.md) | Entry point, services, scene flow, conventions. |
| [`03_AudioSystem.md`](03_AudioSystem.md) | Id-based audio (AudioId → catalog → clip). |
| [`04_PuzzleGameplay_SteptileArchitecture.md`](04_PuzzleGameplay_SteptileArchitecture.md) | PuzzleData, board/tile prefabs, placement, Map Editor. |
| [`05_DialogueSystem.md`](05_DialogueSystem.md) | Persistent gameplay UI, dialogue data, controller, animation và Dialogue tile. |

## Architecture at a glance

```
Unity Start → 00_Bootstrap → Bootstrapper
                              ├─ init services (IGameService): SceneFlow, AudioService
                              └─ SceneFlow.Load → 10_MainMenu → [New Game] → 20_Game
```

The Bootstrapper is a thin entry point; every real capability is its own service under a
persistent `Services` root. Details in [`02_Bootstrap.md`](02_Bootstrap.md).

## Folder map

```
D:\PJ\AudereGJ\
├── Assets/_Audere/          ← all first-party game content lives here
│   ├── Scripts/
│   │   ├── Core/            Global services + contracts   (namespace Audere.Core)
│   │   ├── UI/              UI controllers                 (Audere.UI)
│   │   ├── Audio/           Id-based audio system          (Audere.Audio)
│   │   ├── Puzzle/          Puzzle board, path, editor      (Audere.Puzzle)
│   │   └── Dialogue/        Dialogue data + persistent UI   (Audere.Dialogue)
│   ├── Scenes/              00_Bootstrap, 10_MainMenu, 20_Game (+ SampleScene leftover)
│   ├── Data/                Audio, Puzzle và Dialogue ScriptableObjects
│   ├── Audio/               Raw audio assets (empty)
│   ├── Prefabs/             Puzzle, world và UI prefabs
│   └── AssetGame/           Imported art: DiceCombat, Enemyy, Nilah, Timor, Step Tile
├── Packages/ ProjectSettings/
└── Docs/                    ← these docs (outside Assets, not imported by Unity)
```

`Assets/_Audere/` is the game root (underscore keeps it sorted to the top, away from
imported third-party assets).

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
| `MainMenuController.cs` | Auto-wires serialized `Button` refs in code. New Game → `SceneFlow.Load(GameScenes.Game)`. | Active |

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
| `GameplayUIRoot.cs` | Singleton UI độc lập, `DontDestroyOnLoad` giữa gameplay scenes; tự hủy khi vào Main Menu. | Active |
| `DialogueController.cs` | Left/right presentation, typewriter, emphasis, input và pause gameplay bằng unscaled time. | Active |
| `DialogueTileBehaviour.cs` | Nhận `DialogueData` theo từng `PuzzleTileData` cell và phát thoại khi Player bước vào. | Active |

### Deferred (documented, NOT built)
| Planned | Why deferred |
|---------|--------------|
| `SaveManager` (auto-save) | Save format depends on the not-yet-defined runtime data model. |
| `GameSettings` (volumes/quality/controls) | After `20_Game` core loop exists; will feed `AudioService`. |
| Music playback | `Music_*` ids exist; only one-shot SFX wired today. |

## Decision log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-08-11 | Bootstrap = single entry point; services under `Bootstrap › Services`, init order = sibling order. | Scales without editing Bootstrapper; avoids a God-Object entry point. |
| 2026-08-11 | Scene transitions only via `SceneFlow`; names via `GameScenes`. | Single choke-point + SSOT; no code/Build-Settings drift. |
| 2026-08-11 | Audio id-based: `AudioId` enum → `AudioCatalog` (SO) → clip; explicit permanent numeric ids. | Decouples gameplay from file names; designer swaps sounds in one asset; ids stable across reordering. |
| 2026-08-11 | `20_Game` before `GameSettings`; `SaveManager` = auto-save, deferred. | Core loop first; save needs the data model locked down. |
| 2026-08-11 | Docs live in repo-root `Docs/` (outside `Assets/`). | Keeps docs out of Unity's asset import (no `.meta` clutter); standard repo convention. |
| 2026-08-15 | Gameplay UI dùng prefab `GameplayUIRoot` độc lập và persistent; Main Menu giữ UI riêng. | Không gắn UI vào Player; tránh mất UI khi đổi gameplay scene và tránh kéo UI gameplay vào Main Menu. |
| 2026-08-15 | Dialogue dùng `DialogueCharacterId` + catalog trung tâm + `DialogueData`; trigger được gán theo từng cell trong `PuzzleData`. | Designer chỉ chọn constant nhân vật; tên/portrait tự resolve và không lặp theo từng đoạn thoại. |
| 2026-08-16 | Portrait Left/Right luôn giữ scale; người không nói dùng tint tối, bubble chuyển lượt bằng pop/fade/rise trước khi chạy typewriter. | Tránh viền do alpha/scale portrait và giúp lượt nói chuyển mượt, dễ nhận biết. |

## Maintenance

Update this file + the relevant topic doc when a folder/script/asset is added, moved, or
removed, or when an architectural decision is made (add a decision-log row).
