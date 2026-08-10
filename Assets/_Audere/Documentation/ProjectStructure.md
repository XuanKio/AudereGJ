---
id: audere.project-structure
archetype: state
version: 1.0.0
schema_version: 1.0.0
cost_tier: M
summary: Living folder map + script/asset inventory so a new session can skip re-scanning the repo.
---

# Audere — Project Structure & Script Inventory

> **Purpose:** save a future session (human or AI) a full re-scan. Update this when a
> folder/script/data-asset is added, moved, or removed.
> **Companion doc:** [`Architecture.md`](Architecture.md) — the *why* (bootstrap, scene flow, audio).
> **Last updated:** 2026-08-11

## Folder map

```
Assets/_Audere/
├── Scripts/
│   ├── Core/          Global services + contracts (Audere.Core)
│   ├── UI/            UI controllers (Audere.UI)
│   └── Audio/         Id-based audio system (Audere.Audio)
├── Scenes/
│   ├── 00_Bootstrap.unity   Entry point (build index 0)
│   ├── 10_MainMenu.unity     Title + New Game (index 1)
│   ├── 20_Game.unity         Gameplay placeholder (index 2)
│   └── SampleScene.unity     Unity template leftover — NOT in Build Settings
├── Data/
│   └── Audio/
│       └── AudioCatalog.asset   AudioId → clip mapping (currently EMPTY)
├── Audio/             Raw audio assets (music/sfx) — empty for now
├── Prefab/            Prefabs — empty for now
├── AssetGame/         Imported/external art (DiceCombat, Enemyy, Nilah, Timor, Step Tile)
└── Documentation/     This doc + Architecture.md
```

## Script inventory

### Core services — `Scripts/Core/` (namespace `Audere.Core`)
| Script | Responsibility | Status |
|--------|----------------|--------|
| `Bootstrapper.cs` | Single entry point. `DontDestroyOnLoad`; discovers every `IGameService` under the services root and `Initialize()`s them in sibling order; then `SceneFlow.Load(firstScene)`. Holds NO gameplay logic. | Active |
| `IGameService.cs` | `Initialize()` contract implemented by every global service. | Active |
| `SceneFlow.cs` | Owns all scene load/unload. `Load(name)` async Single-mode; `IsBusy` guard; `SceneFlow.Instance`. | Active |
| `GameScenes.cs` | Scene-name constants (`Bootstrap`/`MainMenu`/`Game`) — SSOT, mirrors Build Settings. | Active |

### UI — `Scripts/UI/` (namespace `Audere.UI`)
| Script | Responsibility | Status |
|--------|----------------|--------|
| `MainMenuController.cs` | Auto-wires serialized `Button` refs in code (no Inspector OnClick). New Game → `SceneFlow.Load(GameScenes.Game)`. `quitButton` optional/null-guarded. | Active |

### Audio — `Scripts/Audio/` (namespace `Audere.Audio`)
| Script | Responsibility | Status |
|--------|----------------|--------|
| `AudioId.cs` | `enum` of stable, explicitly-numbered sound ids (UI 1000 / Nilah 2000 / Timor 3000 / Exploration 4000 / Combat 5000 / Music 9000). Ids are permanent, never reused. | Active |
| `AudioEntry.cs` | `[Serializable]` `{ id, clip, volume }` — one catalog row. | Active |
| `AudioCatalog.cs` | `ScriptableObject` `List<AudioEntry>` → `Dictionary` lookup. `TryGet(id, out entry)`. CreateAssetMenu: `Audere/Audio/Audio Catalog`. | Active |
| `AudioService.cs` | `IGameService`. `Play(AudioId)` → catalog → `AudioSource.PlayOneShot`. `AudioService.Instance`. 2D source (`spatialBlend = 0`). SFX only for now (music TODO). | Active |

### Deferred — documented, NOT built (see Architecture.md → "Deferred")
| Planned script | Responsibility | Why deferred |
|----------------|----------------|--------------|
| `SaveManager` | Auto-save (no manual slots). | Save format depends on the not-yet-defined runtime data model. |
| `GameSettings` | Global settings (volumes, quality, controls); feeds `AudioService`. | Comes after `20_Game` core loop exists. |
| Music playback | Looping music source + `PlayMusic` on `AudioService`. | `Music_*` ids exist but only one-shot SFX is wired today. |

## Scene contents (quick reference)

- **00_Bootstrap** — `Bootstrap` (Bootstrapper, DontDestroyOnLoad) › `Services` › `SceneFlow`, `AudioService` (+AudioSource, catalog assigned). No camera (transitions out immediately).
- **10_MainMenu** — `Main Camera` (+AudioListener), `EventSystem` (InputSystemUIInputModule), `Canvas` (overlay, scale-with-screen 1920×1080) › `Title` (TMP "AUDERE"), `NewGameButton` (Image+Button) › `Label` (TMP), `MainMenu` (MainMenuController → newGameButton).
- **20_Game** — `Main Camera` (+AudioListener), `Canvas` (overlay) › `GameLabel` (TMP "20_Game (placeholder)").

## Conventions (enforced)

- **Scene transitions:** always `SceneFlow.Load(GameScenes.X)`; never raw `SceneManager.LoadScene` or magic strings.
- **New global system:** implement `IGameService`, drop the component under `Bootstrap › Services`. Bootstrapper auto-discovers by sibling order — no Bootstrapper edit.
- **Audio:** gameplay calls `AudioService.Instance.Play(AudioId.X)`; never a file name, path, or `Resources.Load`. Swapping a sound = edit `AudioCatalog.asset`.
- **Namespaces:** `Audere.Core` (services/contracts) · `Audere.UI` (UI) · `Audere.Audio` (audio). New feature area → new `Audere.<Area>` namespace + `Scripts/<Area>/` folder.
- **Build Settings order** must match `GameScenes`: 0 `00_Bootstrap`, 1 `10_MainMenu`, 2 `20_Game`.

## Decision log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-08-11 | Bootstrap = single entry point; services under `Bootstrap › Services`, init order = sibling order. | Scales without editing `Bootstrapper`; each service owns its own init. Avoids a God-Object Bootstrap. |
| 2026-08-11 | Scene transitions only through `SceneFlow`; names via `GameScenes` constants. | Single choke-point + SSOT; prevents code/Build-Settings drift. |
| 2026-08-11 | Audio is id-based: `AudioId` enum → `AudioCatalog` (ScriptableObject) → clip. Explicit permanent numeric ids. | Decouples gameplay from asset file names; designer swaps sounds in one asset; ids stay stable across reordering. |
| 2026-08-11 | `20_Game` before `GameSettings`; `SaveManager` = auto-save, deferred. | Core loop first; save format needs the runtime data model locked down. |
