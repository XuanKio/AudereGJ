# Bản đồ project và kiến trúc — checkpoint 2026-08-23

[Mục lục nguồn](../00_ProjectOverview.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
---
id: audere.overview
archetype: state
version: 1.2.0
schema_version: 1.0.0
cost_tier: M
summary: Top-level map of the Audere project — what it is, the doc index, folder map, script inventory, decision log.
---

# Audere — Project Overview

> **Read this first.** It's the index + living map so a new session skips re-scanning the repo.
> **Last updated:** 2026-08-23 · **Engine:** Unity 6000.0.79f1 (URP, 2D)

## What Audere is

- **Established Canon:** game narrative 2D với nhân vật chính Audere và Timor trong vai trò quan trọng của Day 1.
- **Established Canon:** gameplay hiện có Dialogue, puzzle StepTile scene-first và combat DiceCatcher real-time.
- **Established Canon:** story flow được author trong Unity Hierarchy bằng StoryEvent/StoryStep.
- **Design Intent:** Choice sẽ phục vụ các đoạn hội thoại/quyết định về sau.
- **Unresolved:** Choice UI, StoryState, save/checkpoint và branching chưa được implement.

Asset/sample/debug content không tự trở thành canon. Xem quy tắc tại [`07_StorySystem_SceneFirst.md`](../07_StorySystem_SceneFirst.md).

## Documentation index

| Doc | Covers |
|-----|--------|
| `00_ProjectOverview.md` (this) | Index, folder map, script inventory, decision log. |
| [`01_ProjectSetup.md`](../01_ProjectSetup.md) | Unity version, packages, how to open/run/build. |
| [`02_Bootstrap.md`](../02_Bootstrap.md) | Entry point, services, scene flow, conventions. |
| [`03_AudioSystem.md`](../03_AudioSystem.md) | Id-based audio (AudioId → catalog → clip). |
| [`04_PuzzleGameplay_SteptileArchitecture.md`](../04_PuzzleGameplay_SteptileArchitecture.md) | Scene-first level prefabs, shared runtime/Player, map rules và Goal → PlayerStart hand-off. |
| [`05_DialogueSystem.md`](../05_DialogueSystem.md) | Persistent gameplay UI, dialogue data, controller, animation và Dialogue tile. |
| [`06_CombatGameplay.md`](../06_CombatGameplay.md) | WORLD mode switching, Combat Root, dice-catching loop, encounter data và board presentation. |
| [`07_StorySystem_SceneFirst.md`](../07_StorySystem_SceneFirst.md) | StoryDirector/Event/Step, hierarchy order, chaining và integration Dialogue/Puzzle/Combat. |
| [`08_VisualPalette.md`](../08_VisualPalette.md) | Shared camera fallback, PuzzleViewportMask, transition cover và ranh giới với màu UI/location-specific. |
| [`09_Day1_ProductionStoryWorkflow.md`](../09_Day1_ProductionStoryWorkflow.md) | Current Day 1 canon, exact production event hierarchy và workflow dựng beat/scene tiếp theo. |
| [`14_Day2_NightDream_StoryWorkflow.md`](../14_Day2_NightDream_StoryWorkflow.md) | Scene60 closure → Day2 home question → 15-cell Dream → awakening; staging, assets and QA. |
| [`15_Day3_BoardTeacher_StoryWorkflow.md`](../15_Day3_BoardTeacher_StoryWorkflow.md) | Day2 ending → Day3 home/school, chalk drawing, fatigue sway and 12HP teacher-pressure encounter; bindings and QA. |

## Architecture at a glance

```
Unity Start → 00_Bootstrap → Bootstrapper
                              ├─ init services (IGameService): SceneFlow, AudioService
                              └─ SceneFlow.Load → 10_MainMenu → [New Game] → 20_D1_Home_Morning → 30_Classroom
```

The Bootstrapper is a thin entry point; every real capability is its own service under a
persistent `Services` root. Details in [`02_Bootstrap.md`](../02_Bootstrap.md).

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
│   │   ├── Dialogue/        Dialogue data + persistent UI   (Audere.Dialogue)
│   │   ├── Input/           Owner-safe gameplay input gate  (Audere.GameplayInput)
│   │   └── Story/           Scene-first story runner        (Audere.Story)
│   ├── Scenes/              00_Bootstrap, 10_MainMenu, 20_D1_Home_Morning, 30_Classroom, 40_Evening, 50_D2_Home_Morning
│   ├── Data/                Audio, Puzzle và Dialogue ScriptableObjects
│   ├── Audio/               Raw audio assets (empty)
│   ├── Prefabs/             Puzzle, world và UI prefabs
│   └── AssetGame/           Imported art: DiceCombat, Enemyy, Nilah, Timor, Step Tile
├── Packages/ ProjectSettings/
└── Docs/                    ← these docs (outside Assets, not imported by Unity)
```

`Assets/_Audere/` is the game root (underscore keeps it sorted to the top, away from
imported third-party assets).

<!-- END PRESERVED SOURCE -->
