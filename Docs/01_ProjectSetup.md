---
id: audere.setup
archetype: knowledge
version: 1.1.0
schema_version: 1.0.0
cost_tier: S
summary: How to open, run, and build Audere — engine version, packages, and project layout.
---

# Audere — Project Setup

> **Last updated:** 2026-08-23

## Engine

- **Unity `6000.0.79f1`** (Unity 6.0). Open the folder `D:\PJ\AudereGJ` with this exact
  version via Unity Hub.
- Render pipeline: **Universal Render Pipeline (URP) 17.0.4**.
- Project is **2D** (2D feature set).

## Key packages

| Package | Version | Why it matters |
|---------|---------|----------------|
| `com.unity.render-pipelines.universal` | 17.0.4 | URP renderer + global settings. |
| `com.unity.inputsystem` | 1.19.0 | **New Input System.** UI uses `InputSystemUIInputModule` on the EventSystem (NOT the legacy `StandaloneInputModule`). |
| `com.unity.ugui` | 2.0.0 | uGUI + **TextMeshPro is bundled here** in Unity 6 (no separate TMP package). |
| `com.unity.feature.2d` | 2.0.1 | 2D tooling. |
| `com.coplaydev.unity-mcp` | git `#main` | MCP for Unity — lets tooling drive the Editor. |

Full list: `Packages/manifest.json`.

## How to run

1. Open the project in Unity 6000.0.79f1.
2. Open **`Assets/_Audere/Scenes/00_Bootstrap.unity`**.
3. Press **Play**. Expected console:
   ```
   [Bootstrapper] Initialized 2 service(s).
   [SceneFlow] Loading '10_MainMenu'... → Loaded
   ```
   The MainMenu shows the **AUDERE** title + a **New Game** button → loads `20_Game`.

> Always start from `00_Bootstrap`. Entering Play from `10_MainMenu`/`20_Game` directly
> means the services (SceneFlow, AudioService) were never initialized — `*.Instance` will
> be null and those scenes will log errors.

## Build Settings

Scenes, in order (must match `Audere.Core.GameScenes`):

| Index | Scene |
|-------|-------|
| 0 | `Assets/_Audere/Scenes/00_Bootstrap.unity` |
| 1 | `Assets/_Audere/Scenes/10_MainMenu.unity` |
| 2 | `Assets/_Audere/Scenes/20_Game.unity` |
| 3 | `Assets/_Audere/Scenes/30_Classroom.unity` |

`SampleScene.unity` is a Unity template leftover and is intentionally **not** in Build Settings.

## Project layout

- First-party content: **`Assets/_Audere/`** (underscore sorts it to the top).
- Code namespaces map to folders: `Audere.Core` → `Scripts/Core/`, `Audere.UI` →
  `Scripts/UI/`, `Audere.Audio` → `Scripts/Audio/`.
- Docs: repo-root **`Docs/`** (this folder) — outside `Assets/`, so Unity does not import them.

## Conventions (quick)

- New scene transition → `SceneFlow.Load(GameScenes.X)`, never raw `SceneManager.LoadScene`.
- New global system → implement `IGameService`, drop under `Bootstrap › Services`.
- Play a sound → `AudioService.Instance.Play(AudioId.X)`, never a file path.

See [`02_Bootstrap.md`](02_Bootstrap.md), [`03_AudioSystem.md`](03_AudioSystem.md) and
[`08_VisualPalette.md`](08_VisualPalette.md).
