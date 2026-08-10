# Audere — Bootstrap & Scene Architecture

> Status: living doc. Updated as the core systems evolve.

## Goal

`00_Bootstrap` is the **single entry point** of the game. It initializes the global
systems, then hands scene transitions off to a dedicated `SceneFlow` service — so the
Bootstrapper never becomes a "God Object". Each responsibility lives in its own service.

## Roles

```
00_Bootstrap (scene)
│
├── Bootstrapper      → entry point: keeps services alive, initializes them in order,
│                        hands the first scene load to SceneFlow. NOTHING else.
│
├── SceneFlow         → owns all scene load / unload
│
├── AudioService      → global audio: Play(AudioId) → AudioCatalog → clip
│
├── SaveManager       ← LATER (see "Deferred" below)
│
└── GameSettings      ← LATER (see "Deferred" below)
```

Hierarchy inside `00_Bootstrap.unity`:

```
Bootstrap            (Bootstrapper)          ← DontDestroyOnLoad root
└── Services         (servicesRoot)
    ├── SceneFlow    (SceneFlow    : IGameService)
    └── AudioService (AudioService : IGameService, + AudioSource, catalog ref)
```

- The whole `Bootstrap` root is marked `DontDestroyOnLoad`, so every service survives
  the Single-mode scene loads that swap out gameplay scenes.
- **Init order = sibling order** under `Services`. Add a new global service by dropping
  its component under `Services` and implementing `IGameService` — no change to
  `Bootstrapper` is needed.

## Startup flow

```
Unity Start
   ↓
00_Bootstrap
   ↓
Bootstrapper.Awake()  → DontDestroyOnLoad + Initialize() every IGameService
   ↓
Bootstrapper.Start()  → SceneFlow.Load("10_MainMenu")
   ↓
10_MainMenu
   ↓  (New Game button → MainMenuController.NewGame)
   ↓
SceneFlow.Load("20_Game")
   ↓
20_Game
```

## Scenes (Build Settings order)

| Index | Scene          | Purpose                                      |
|-------|----------------|----------------------------------------------|
| 0     | `00_Bootstrap` | Entry point. Inits services, loads MainMenu. |
| 1     | `10_MainMenu`  | Title + New Game button.                     |
| 2     | `20_Game`      | Gameplay (placeholder for now).              |

Scene names are centralized in `Audere.Core.GameScenes` — reference those constants,
never magic strings. Keep the constants in sync with Build Settings.

## Scripts (namespaces `Audere.Core`, `Audere.UI`, `Audere.Audio`)

- `IGameService` — `Initialize()` contract; discovered + called by the Bootstrapper.
- `GameScenes` — scene-name constants (SSOT).
- `Bootstrapper` — entry point / init orchestrator / first-scene handoff.
- `SceneFlow` — `Load(sceneName)` async Single-mode load; `IsBusy` guard. All scene
  transitions go through here.
- `AudioService` (`Audere.Audio`) — `Play(AudioId)`. Resolves the id via `AudioCatalog`
  then `AudioSource.PlayOneShot`. `AudioService.Instance` is the global access point.
- `MainMenuController` (`Audere.UI`) — auto-wires its buttons in code (serialized
  `Button` refs) and routes New Game → `SceneFlow.Load(GameScenes.Game)`.

## Audio — id-based, data-driven

Gameplay refers to sounds by a **stable id**, never by file name/path:

```
Gameplay
   ↓  AudioService.Instance.Play(AudioId.UI_Click)
AudioService
   ↓  catalog.TryGet(id)
AudioCatalog (.asset)  →  AudioEntry { id, clip, volume }
   ↓
AudioSource.PlayOneShot(clip, volume)   // Unity 6: many overlapping one-shots on one source
```

- `AudioId` (enum) — permanent identity per sound, with **explicit numeric ids**
  (`UI_Click = 1001`) grouped by category (UI 1000 / Nilah 2000 / Timor 3000 /
  Exploration 4000 / Combat 5000 / Music 9000). An id, once used, is **never reused** for
  a different sound even if removed — reordering/adding entries never shifts existing ids.
- `AudioEntry` — `{ id, clip, volume }`. One row of the mapping.
- `AudioCatalog` — `ScriptableObject` holding `List<AudioEntry>`, built into a
  `Dictionary` lookup at load. Lives as a standalone asset:
  `Assets/_Audere/Data/Audio/AudioCatalog.asset`. Add sounds via the Inspector; Unity
  keeps clip links by GUID/fileID (in the `.meta`), so renaming/moving clips is safe.
- **Swapping a sound = edit the catalog asset, zero gameplay code change.**
- The `AudioService` source is 2D (`spatialBlend = 0`) since Audere's UI/SFX are
  position-independent.
- **Not built yet:** looping **music** (the `Music_*` ids exist but `Play` only fires
  one-shots for now) — add a dedicated looping music source + `PlayMusic` when needed.
- **Populate the catalog:** the `AudioCatalog.asset` ships empty; add an `AudioEntry` per
  `AudioId` and assign the real `AudioClip` when audio assets land.

## Deferred (documented now, NOT built yet)

### Order
Build **`20_Game` first**, before `GameSettings`. Settings comes after the core loop
exists.

### GameSettings — LATER
Global settings service (audio volumes, quality, controls). When added, `AudioService`
reads its volume/mixer values from here. Drop it under `Services` as an `IGameService`.

### SaveManager — LATER, and it will be AUTO-SAVE
- Save model is **auto-save** (no manual save slots for now).
- **Do not implement yet.** The save format depends on the project's runtime data, which
  isn't defined. Once the data model is locked down we'll know *what* and *how* to
  persist — then add `SaveManager` under `Services` as an `IGameService`.

## Conventions

- New scene transition? → `SceneFlow.Load(...)`, never a raw `SceneManager.LoadScene`.
- New global system? → own service component under `Services` implementing `IGameService`.
- Keep `Bootstrapper` thin. If you're tempted to add gameplay/audio/save logic there,
  it belongs in a service instead.
