# Tuning và SFX của loạt đạn

[Mục lục nguồn](../03_AudioSystem.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
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

<!-- END PRESERVED SOURCE -->
