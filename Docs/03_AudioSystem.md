---
id: audere.audio
archetype: knowledge
version: 1.0.0
schema_version: 1.0.0
cost_tier: M
summary: Id-based, data-driven audio — gameplay plays sounds by AudioId, never by file name.
---

# Audere — Audio System

> **Last updated:** 2026-08-28

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
| 2000 | Player / legacy Nilah | `Nilah_Step`, `Nilah_Hurt`, `Player_Fall` |
| 3000 | Timor | `Timor_Meow`, `Timor_Step` |
| 4000 | Exploration | `Tile_Rotate`, `Tile_Select`, `Tile_Connect` |
| 5000 | Combat | `Dice_Select`, `Dice_Roll`, `Dice_Hit` |
| 9000 | Music | `Music_MainMenu`, `Music_Exploration`, `Music_Combat`, `Music_TimorCombat` |

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
- `TryResolveSfx(AudioId, out clip, out volume)` dành cho component cần sở hữu và dừng
  playback riêng, hiện được `DialogueController` dùng cho typewriter loop.
- Global access via **`AudioService.Instance`**.
- Its `AudioSource` is **2D** (`spatialBlend = 0`) — Audere's UI/SFX are position-independent.
- Missing id or missing clip → logs a warning and no-ops (never throws).

## How to add a new sound

For looping BGM, use the music catalog slots and the shared rules below, not `Play(AudioId)`.

1. Add an id to `AudioId` with an explicit number in the right range (don't reuse a number).
2. Drop the clip into `Assets/_Audere/Audio/…`.
3. Open `AudioCatalog.asset`, add an `AudioEntry`: set `id`, assign the `clip`, set `volume`.
4. Call `AudioService.Instance.Play(AudioId.YourId)` from gameplay.

## Swapping a sound (designer-friendly)

Open `AudioCatalog.asset`, change the `clip` on that entry to the new file. Gameplay code
(`Play(AudioId.Tile_Rotate)`) stays identical — no code touched.

## Current state / tuning

| AudioId | Clip | Catalog volume | Runtime rule |
| --- | --- | ---: | --- |
| `Dialogue_Text` | `Text.mp3` | `0.55` | Loop trên AudioSource riêng; dừng ngay khi text hiện đủ/skip/cancel/close. |
| `Tile_Pop` | `TilePop.mp3` | `0.16` | Reveal/hide tile, throttle tối thiểu `0.11 s` để tránh chồng peak. |
| `Player_Fall` | `Fall.mp3` | `0.60` | Phát đúng lúc `PuzzleManager` nhận fall started. |
| `Enemy_BulletVolley = 5005` | `dan.wav` | `1.00` | One beat for a group of activated bullets; minimum spacing 0.25 s. |
| `Enemy_LaserVolley = 5006` | `laze.mp3` | `0.20` | One beat for simultaneous laser activations; minimum spacing 0.12 s. |
| `Bus_Approach` | generated placeholder | `0.72` | Beat trạm xe bus. |
| `Classroom_Murmur` | generated placeholder | `0.34` | Nhịp lớp học xôn xao. |

Phân tích source ngày 2026-08-23: `Text` peak `-18.1 dBFS`, `TilePop` peak `-2.0 dBFS`,
`Fall` peak `-11.7 dBFS`. Vì `TilePop` nóng hơn rõ rệt và có thể phát liên tục trong wave,
volume của nó được đặt thấp nhất và code giới hạn mật độ playback.

- Catalog không còn empty; clip chưa có entry vẫn warning/no-op như trước.

## Combat volley SFX — 2026-08-28

`CombatBoardView` requests SFX when a projectile becomes active, after its telegraph;
zero-delay projectiles request immediately on spawn. `CombatVolleyAudio` coalesces those
requests using combat-active time. Three bullets every 0.35 seconds produce three sounds
across nine bullets. A faster stream is thinned without changing its projectile cadence.

Two lazily created, board-owned AudioSources play the catalog clips through
`AudioService.TryResolveSfx`, respecting saved SFX volume. Each kind has one voice: a new
beat replaces the previous tail instead of stacking copies. BGM, typewriter and other SFX
sources are untouched. Board Inspector exposes both minimum intervals.

Pause freezes the sources and cooldowns; no delayed sound queue exists. Hazard fade, phase
clear, cancel/Retry, disable and destroy stop owned voices. Versioned cleanup ignores an old
session/phase, and reused sources do not multiply. No enemy-ID conditions or scene edits.

PCM inspection: `dan.wav` is 0.368 s, peak 0.0983/RMS 0.0208; `laze.mp3` is 0.888 s,
peak 0.8892/RMS 0.1308. The laser catalog gain is lower because its source is much louder.

Verification: final **89/89 tests passed**, including four new volley tests in
`MusicPresentationTests`, combat runtime/Evening and EnemyActor bob regressions. Controlled
Play on the Scene120 board measured **9 bullets → 3 sounds**, **3 lasers → 1 sound**, two
voices maximum, nonzero output from both clips, silence before activation/during pause,
and silence after cleanup/cancelled telegraphs. The initial probe mixed Editor elapsed time
with a prior frame delta; it was replaced by one consistent sample clock and rerun successfully.
This verifies playback/lifecycle, not a full scene playthrough or subjective listening/mix review.
The attempted screenshot was covered black and is not visual QA evidence. Results live at
`Temp/CombatVolleyQA/play.json` and `tests_89_pass.xml`. Console 0 errors; Scene120 restored
clean with startup=true, Play OFF and no QA callback.

## Shared BGM and transition contract

Implemented 2026-08-28. `AudioService` remains the single audio owner under persistent
Bootstrap. Its direct `MusicSource` reference points to a separate 2D, looping child source;
`playOnAwake` is off. `bgm.mp3` uses Streaming import for long playback. No scene-local
music player, new singleton, low-pass, or AudioMixer is introduced.

| Catalog slot | Current clip | Clip volume | Use |
| --- | --- | ---: | --- |
| `Music_Exploration = 9002` | `Assets/_Audere/Audio/bgm.mp3` | `0.4` | Default menu / Story / Puzzle BGM. |
| `Music_Combat = 9003` | `Assets/_Audere/Audio/combat1_final (mp3cut.net).mp3` | `0.4` | Default for all non-Timor encounters, including Bianca and Teacher. |
| `Music_TimorCombat = 9004` | `Assets/_Audere/Audio/bossfightfull.mp3` | `0.4` | Timor night pressure encounter. |

Replace tracks through `Assets/_Audere/Data/Audio/AudioCatalog.asset`. Both combat clips use
Streaming import. `CombatEncounterData.Music` defaults to `Music_Combat`; the Timor encounter
explicitly selects `Music_TimorCombat`. There is no enemy-ID branch in runtime. An empty
selected slot still means silence, never a different track. `Music_MainMenu` remains reserved.

- Final source volume = saved Music setting × catalog clip volume × presentation gain.
  Ducking never changes PlayerPrefs or SFX/typewriter/SchoolBell/Message volumes.
- `WorldModeController` registers its direct transition cover; `CanvasFadeStep` registers
  its direct full-screen CanvasGroup in Awake and Execute. The service observes the **cover**,
  not step completion: alpha 1 stays silent throughout a black hold between steps. Fade-in
  restores music with the visible scene. Ordinary actor/UI/status fades are not registered.
- `FullscreenTransitionController` fades gain from 1 to 0 with SmoothStep from start to
  the shared profile's mode-swap time. It holds 0 through the remaining effect and releases
  its owner only on completion/cancel. No per-scene audio timeline is copied.
- `SceneFlow` holds an independent mute during async loading, releasing after target
  Awake/Start can register its cover. Error/disable/destroy also release the load owner.
- All active covers/duck owners combine by the lowest gain; one owner cannot unmute another.
  Destroyed scene objects are pruned. A disabled/inactive cover does not mute a visible scene.
- Recovery is unscaled and smoothed (`musicReturnDuration = 0.35 s`). An uncovered track
  change fades the old track out over `musicSwitchFadeDuration = 0.18 s`; switching under
  black is immediate but remains silent until reveal. Same-track fades do not restart playback.
- Both World Combat presentation and a running CombatController session claim combat music.
  Session completion/cancel/retry releases only its own claim, so a visible Combat/Retry
  presentation cannot accidentally resume normal BGM before returning to Story/Puzzle.
  The world reads the encounter through its existing direct `combatSystemsRoot` controller.
  Session claims have priority 1, world claims priority 0; equal-priority changes use latest
  claim order. Repeating the same claim does not reorder it. Timor's world claim therefore
  retains Timor's track after session cleanup, rather than briefly selecting regular combat.
- AudioService initialization is idempotent; disable/destroy clears its music owners.

New scene setup: reuse `WorldModeController`/`CanvasFadeStep`/`FullscreenWorldModeTransitionStep`
and run through `00_Bootstrap`. Playing an isolated scene without Bootstrap still does not
initialize global audio. A custom full-screen cover must call `TrackScreenFade(cover)`;
custom non-canvas transitions use `SetMusicDuck(owner, gain)` / `ReleaseMusicOwner(owner)`.

### Initial BGM verification (2026-08-28, before combat tracks were assigned)

- C# compiled; `Audere.Audio.Editor.Tests.MusicPresentationTests`: **21/21 passed**.
- Tests cover alpha/black holds, overlapping owners, destroyed/inactive covers, combat/session
  ownership, fullscreen envelope, saved-volume isolation, empty and assigned combat slots,
  cancel/replay, idempotent initialization, catalog and Bootstrap direct source binding.
- Play from Bootstrap → Scene30: real `bgm` source playing, gain 1, volume 0.4 at saved Music
  1, nonzero output samples while dialogue paused gameplay (`Time.timeScale = 0`). Entering
  Combat selected the empty slot: clip null, source stopped, gain/volume 0.
- Scene60 was restored without saving QA mutations; scene validation found 0 missing scripts
  and 0 broken prefabs. Scene60's separate authoring task also reported its CanvasFadeStep
  playback completed without errors.
- Detailed live frame sampling / cancel-before-and-after-swap was interrupted by an Editor
  domain reload; not counted as a PlayMode pass. The temporary MCP sampling callback then
  lost its reference and logged an exception; this was QA code, not a production stack.
  Exact envelope/cancel ownership remains covered by EditMode tests. Manual listening,
  final mix balance, and a full playthrough of all scenes remain unverified.

### Combat music and rhythm verification (2026-08-28)

- **79/79 passed**: `MusicPresentationTests` (25), `CombatEnemyRuntimeTests`, and
  `EveningNightPressureTests`. Added coverage for selected-track priority/cleanup, empty
  Timor slot behavior, beat scheduling across pause/loop, and all five encounter bindings.
- Controlled Play QA in Scene120 and Scene40 initialized the existing AudioService with the
  production catalog. Both selected tracks played and produced nonzero output samples.
  Cancel retained the corresponding combat track while Combat remained visible; Story restored
  `bgm`. Continuous sampling replaced a first single-read output probe that returned zero.
- Timor's live clock was sampled across nine emissions and an actual clip wrap after seeking
  near the end. Largest observed launch-plus-telegraph grid error: **31.83 ms**. Laser collision
  stayed off during telegraph, became active afterward, and double cancel plus board cleanup
  removed all lasers. The active laser screenshot was inspected; the telegraph screenshot
  caught its initial transparent frame, so it is not evidence of the full visible fade.
- Console: 0 errors. Scene40/120 direct music bindings and startup=true verified, missing
  scripts=0. Restored Scene120 clean, Play OFF, no QA callback left running; no scene asset saved.
- Evidence: `Temp/CombatMusicQA/regular-play.json`, `timor-play.json`, and laser screenshots.
  These are focused playback/mechanic checks, not a full manual playthrough or listening/mix review.
