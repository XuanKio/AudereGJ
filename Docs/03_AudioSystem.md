---
id: audere.audio
archetype: knowledge
version: 1.0.0
schema_version: 1.0.0
cost_tier: M
summary: Id-based, data-driven audio — gameplay plays sounds by AudioId, never by file name.
---

# Audere — Audio System

> **Last updated:** 2026-08-11

## Principle

Gameplay refers to sounds by a **stable id**, never by file name or path. The mapping from
id → clip lives in a ScriptableObject catalog, so swapping a sound is a data edit — zero
code change.

```
Gameplay
   ↓  AudioService.Instance.Play(AudioId.UI_Click)
AudioService
   ↓  catalog.TryGet(id)
AudioCatalog (.asset)  →  AudioEntry { id, clip, volume }
   ↓
AudioSource.PlayOneShot(clip, volume)   // Unity 6: many overlapping one-shots on one source
```

### Do / Don't

```csharp
// ✅ DO
AudioService.Instance.Play(AudioId.Tile_Rotate);

// ❌ DON'T
Play("Assets/_Audere/Audio/SFX/tile_rotate_03.wav");
Play("tile_rotate_03");
Resources.Load<AudioClip>("Audio/tile_rotate");
```

## Pieces

### `AudioId` (enum) — permanent identity per sound
Explicit numeric ids grouped by category so blocks stay separated and ids never shift:

| Range | Category | Examples |
|-------|----------|----------|
| 1000 | UI | `UI_Click = 1001`, `UI_Hover`, `UI_Back` |
| 2000 | Nilah | `Nilah_Step`, `Nilah_Hurt` |
| 3000 | Timor | `Timor_Meow`, `Timor_Step` |
| 4000 | Exploration | `Tile_Rotate`, `Tile_Select`, `Tile_Connect` |
| 5000 | Combat | `Dice_Select`, `Dice_Roll`, `Dice_Hit` |
| 9000 | Music | `Music_MainMenu`, `Music_Exploration`, `Music_Combat` |

**Rule:** an id, once assigned, is a permanent identity (`1001 = UI_Click`). Never reuse a
number for a different sound, even after removing an entry. Adding/removing/reordering
entries never changes existing ids — that's the whole point of explicit numbers over
auto-incremented enum values.

### `AudioEntry` — one catalog row
`[Serializable] { AudioId id; AudioClip clip; [Range(0,1)] float volume = 1f; }`.

### `AudioCatalog` — the shared mapping (ScriptableObject)
- Holds `List<AudioEntry>`; builds a `Dictionary<AudioId, AudioEntry>` at load
  (`OnEnable`), exposes `TryGet(id, out entry)`.
- Lives as a standalone asset: **`Assets/_Audere/Data/Audio/AudioCatalog.asset`**.
- Create more via **Create ▸ Audere ▸ Audio ▸ Audio Catalog**.
- Unity stores clip links by **GUID/fileID** (in the `.meta`), so renaming or moving a clip
  file keeps the link intact.

### `AudioService` — the play API
- `IGameService`; initialized by the Bootstrapper. Lives under `Bootstrap › Services`.
- `Play(AudioId)` → resolves via catalog → `AudioSource.PlayOneShot(clip, volume)`.
- Global access via **`AudioService.Instance`**.
- Its `AudioSource` is **2D** (`spatialBlend = 0`) — Audere's UI/SFX are position-independent.
- Missing id or missing clip → logs a warning and no-ops (never throws).

## How to add a new sound

1. Add an id to `AudioId` with an explicit number in the right range (don't reuse a number).
2. Drop the clip into `Assets/_Audere/Audio/…`.
3. Open `AudioCatalog.asset`, add an `AudioEntry`: set `id`, assign the `clip`, set `volume`.
4. Call `AudioService.Instance.Play(AudioId.YourId)` from gameplay.

## Swapping a sound (designer-friendly)

Open `AudioCatalog.asset`, change the `clip` on that entry to the new file. Gameplay code
(`Play(AudioId.Tile_Rotate)`) stays identical — no code touched.

## Current state / TODO

- **The catalog ships EMPTY.** Add an `AudioEntry` per `AudioId` and assign real clips when
  audio assets land. Until then `Play(...)` logs `Audio not found: <id>` and no-ops.
- **Music not wired yet.** The `Music_*` ids exist, but `AudioService` only fires one-shot
  SFX. When music is needed, add a dedicated **looping** music `AudioSource` + a
  `PlayMusic(AudioId)` / `StopMusic()` API (and later, volume from `GameSettings`).
