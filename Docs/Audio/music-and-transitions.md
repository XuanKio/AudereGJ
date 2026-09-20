# BGM, transition, quyền sở hữu và QA

[Mục lục nguồn](../03_AudioSystem.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
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
<!-- END PRESERVED SOURCE -->
