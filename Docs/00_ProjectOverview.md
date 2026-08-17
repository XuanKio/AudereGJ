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
> **Last updated:** 2026-08-16 · **Engine:** Unity 6000.0.79f1 (URP, 2D)

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
| [`06_CombatGameplay.md`](06_CombatGameplay.md) | WORLD mode switching, Combat Root, dice-catching loop, encounter data và board presentation. |

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
│   │   ├── Combat/          Dice combat runtime             (Audere.Combat)
│   │   ├── World/           Puzzle/Combat mode coordinator  (Audere.World)
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
| `GameplayUIRoot.cs` | Singleton root Canvas chứa `PuzzleUI` và `DialogueUI`, `DontDestroyOnLoad` giữa gameplay scenes; tự hủy khi vào Main Menu. | Active |
| `DialogueController.cs` | Left/right presentation, typewriter, emphasis, input và pause gameplay bằng unscaled time. | Active |
| `DialogueTileBehaviour.cs` | Nhận `DialogueData` theo từng `PuzzleTileData` cell và phát thoại khi Player bước vào. | Active |

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
| 2026-08-16 | Gộp gameplay HUD và dialogue vào một Canvas `GameplayUIRoot`, chia child `PuzzleUI`/`DialogueUI`. | Tránh hai root Canvas trùng trách nhiệm; UI gameplay giữ xuyên scene và scene mới chỉ cần rebind systems. |
| 2026-08-15 | Dialogue dùng `DialogueCharacterId` + catalog trung tâm + `DialogueData`; trigger được gán theo từng cell trong `PuzzleData`. | Designer chỉ chọn constant nhân vật; tên/portrait tự resolve và không lặp theo từng đoạn thoại. |
| 2026-08-16 | Portrait Left/Right luôn giữ scale; người không nói dùng tint tối, bubble chuyển lượt bằng pop/fade/rise trước khi chạy typewriter. | Tránh viền do alpha/scale portrait và giúp lượt nói chuyển mượt, dễ nhận biết. |
| 2026-08-16 | Combat dùng `WORLD/Combat Root`, ngang hàng với `Puzzle Root`; board là world-space Canvas prefab, không nằm trong `GameplayUIRoot`. | Combat board thuộc lifecycle của gameplay mode và đi theo camera/world; `GameplayUIRoot` chỉ giữ UI xuyên scene. |
| 2026-08-16 | `WORLD` sở hữu `WorldModeController`; logic được nhóm trong `Puzzle Systems`/`Combat Systems` và chuyển mode bằng fade đen. | Một nơi quản lý lifecycle, camera và UI; parent group là switch duy nhất nên child controller không bị kẹt inactive. |
| 2026-08-16 | Combat chuyển sang real-time hoàn toàn: Attack/Armor/Heal áp ngay khi catch; hết batch chỉ spawn batch mới sau 0.3 giây. | Không còn turn hoặc resolve cuối batch; timer, bullets, player và dice luôn chạy đồng thời. |
| 2026-08-16 | `PuzzleViewportMask` là child của `Main Camera`, không nằm trong `Puzzle Root`; `WorldModeController` bật/tắt mask theo mode. | Mask mô tả viewport nên phải giữ cố định theo camera follow, không trôi theo tọa độ map. |
| 2026-08-16 | Attack/Armor/Heal dùng ba dice prefab riêng; dice mặc định không xoay; enemy hit dùng white-silhouette shader. | Bám motion reference của Dice Catcher và cho phép chỉnh từng mặt dice/art feedback độc lập. |
| 2026-08-16 | Dice, enemy bullets và mouse-controlled Audere Heart nằm chung trong Battle Box; Heart là tâm của Catch Cursor. | Một input mouse vừa xử lý dice vừa quyết định vị trí né đạn, giữ toàn bộ áp lực trong cùng không gian. |
| 2026-08-16 | Stun Zone là vùng chấm tím chặn catch/reroll theo vị trí cursor; cursor đổi viền tím và pop dấu `X`, còn dice vẫn di chuyển bình thường. | Khớp frame reference: vùng stun vô hiệu hóa công cụ bắt chứ không tác động vật lý hoặc presentation của dice. |
| 2026-08-16 | TIME là sinh lực duy nhất của player: Heal cộng TIME, bullet trừ TIME, Armor chặn hit; không còn Player HP riêng. | Gộp áp lực sống sót và giới hạn encounter vào cùng một tài nguyên dễ đọc liên tục. |
| 2026-08-16 | `HeartVisual.prefab` chỉ chứa một sprite placeholder; Timer Fill co `RectTransform` từ trái thay vì dựa vào `Image.fillAmount`. | Heart art thay độc lập; timer vẫn hiển thị đúng kể cả khi Image chưa có sprite. |
| 2026-08-16 | Player damage làm TIME fill giảm ngay, để lại white damage-trail co trễ và rung camera ngắn; Armor block không phát damage feedback này. | Lượng TIME vừa mất đọc được tức thì, đồng thời tạo phản hồi va chạm rõ mà không che gameplay real-time. |
| 2026-08-16 | Ba dice prefab dùng icon Aseprite riêng: Attack=`attack`, Armor=`gaurd`, Heal=`heal`; TMP label chỉ là fallback inactive. | Art được author trực tiếp trên đúng prefab để chỉnh độc lập; `CombatDieView` không giữ một thư viện ba sprite. |
| 2026-08-16 | Dice có phase tung neutral `#23212D`: ground shadow trượt qua board, thân dice nảy parabol 2–3 lần rồi cú chạm cuối mới reveal màu Attack `#A83B44`, Armor `#B0ABB7`, Heal `#D8C097`. | Tạo chiều sâu giả 3D như reference, tránh batch đồng bộ và chỉ mở input khi dice thật sự ổn định. |
| 2026-08-16 | Dice đang tung chuyển sang `Airborne Dice Overlay` ngoài `Dice Field/RectMask2D`, render trên `Frame`; landed mới trả về `Dice Root`. | Dice có thể phủ lên mép board như vật thể đang bay thay vì bị mask hoặc viền đè lên. |

## Maintenance

Update this file + the relevant topic doc when a folder/script/asset is added, moved, or
removed, or when an architectural decision is made (add a decision-log row).
