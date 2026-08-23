---
id: audere.bootstrap
archetype: knowledge
version: 1.0.0
schema_version: 1.0.0
cost_tier: M
summary: The single entry point, global services, and scene flow — and how to extend them.
---

# Audere — Bootstrap & Scene Flow

> **Last updated:** 2026-08-23

## Goal

`00_Bootstrap` is the **single entry point** of the game. It initializes the global
systems, then hands scene transitions to a dedicated `SceneFlow` service — so the
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
├── AudioService      → global audio: Play(AudioId) → AudioCatalog → clip  (see 03_AudioSystem.md)
│
├── SaveManager       ← LATER (see "Deferred")
│
└── GameSettings      ← LATER (see "Deferred")
```

## Hierarchy in `00_Bootstrap.unity`

```
Bootstrap            (Bootstrapper)          ← DontDestroyOnLoad root
└── Services         (servicesRoot)
    ├── SceneFlow    (SceneFlow    : IGameService)
    └── AudioService (AudioService : IGameService, + AudioSource, catalog ref)
```

- The whole `Bootstrap` root is `DontDestroyOnLoad`, so every service survives the
  Single-mode scene loads that swap out gameplay scenes.
- **Init order = sibling order** under `Services`. Add a global service by dropping its
  component under `Services` and implementing `IGameService` — **no change to
  `Bootstrapper`** is needed.

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
| 2     | `20_Game`      | Day 1 morning StepTile sequence.              |
| 3     | `30_Classroom` | Day 1 classroom announcement sequence.        |

Scene names are centralized in `Audere.Core.GameScenes` — reference those constants,
never magic strings. Keep them in sync with Build Settings.

### Scene contents (quick reference)

- **00_Bootstrap** — `Bootstrap` (Bootstrapper) › `Services` › `SceneFlow`, `AudioService`
  (+AudioSource, catalog assigned). No camera (transitions out immediately; a one-frame
  "no audio listener" warning here is harmless).
- **10_MainMenu** — `Main Camera` (+AudioListener), `EventSystem`
  (`InputSystemUIInputModule`), `Canvas` (overlay, scale-with-screen 1920×1080) › `Title`
  (TMP "AUDERE"), `NewGameButton` (Image+Button) › `Label` (TMP), `MainMenu`
  (MainMenuController → `newGameButton`).
- **20_Game** — scene-first Day 1 morning and bus-stop puzzle flow under `STORY`.
- **30_Classroom** — classroom staging, local `StoryDirector`, transition overlay and
  `D1_CLASSROOM_ANNOUNCEMENT`. It also contains a `GameplayUIRoot` prefab instance as a
  safe direct-entry fallback; the persistent instance destroys the duplicate in normal flow.

## Scripts

- `IGameService` — `Initialize()` contract; discovered + called by the Bootstrapper.
- `GameScenes` — scene-name constants (SSOT).
- `Bootstrapper` — entry point / init orchestrator / first-scene handoff. `firstScene`
  and `servicesRoot` are serialized in the Inspector.
- `SceneFlow` — `Load(sceneName)` async Single-mode load; `IsBusy` guard. All scene
  transitions go through here. Global access via `SceneFlow.Instance`.
- `MainMenuController` (`Audere.UI`) — auto-wires its buttons in code (serialized `Button`
  refs, `AddListener` in `Awake`); New Game → `SceneFlow.Load(GameScenes.Game)`.

## Extending

**Add a global service:**
1. Write a `MonoBehaviour` implementing `IGameService` (do setup in `Initialize()`, not
   `Awake`, so the Bootstrapper controls order). Expose a static `Instance` if gameplay
   needs to reach it.
2. Add its GameObject under `Bootstrap › Services` (position it for the right init order).
3. Done — the Bootstrapper discovers and initializes it automatically.

**Add a scene:** create it, add a `GameScenes` constant, add it to Build Settings, load it
via `SceneFlow.Load(GameScenes.X)`.

Production cross-scene story uses `SceneLoadStep`, which delegates to the persistent
`SceneFlow`. Fade the source scene before the load and author a local fade-in step at the
start of the destination event. Do not connect `StoryEvent` references across scenes.

## Deferred (documented now, NOT built yet)

**Order:** build **`20_Game` first**, before `GameSettings`.

### GameSettings — LATER
Global settings service (audio volumes, quality, controls). When added, `AudioService`
reads its volume/mixer values from here. Drop it under `Services` as an `IGameService`.

### SaveManager — LATER, and it will be AUTO-SAVE
- Save model is **auto-save** (no manual save slots for now).
- **Do not implement yet.** The save format depends on the runtime data model, which isn't
  defined. Once it's locked down we'll know *what* and *how* to persist — then add
  `SaveManager` under `Services` as an `IGameService`.

## Conventions

- New scene transition? → `SceneFlow.Load(...)`, never a raw `SceneManager.LoadScene`.
- New global system? → own service under `Services` implementing `IGameService`.
- Keep `Bootstrapper` thin. Gameplay/audio/save logic belongs in a service, not here.
