# Audere Combat Gameplay

> Tách theo trách nhiệm ngày 2026-09-20. Đường dẫn và các heading cũ được giữ để không mất liên kết.
> Nội dung gốc, giới hạn QA và ngày checkpoint nằm trong các trang con; không xem số liệu cũ là trạng thái mới đã xác minh.

[Bản đồ tài liệu](README.md) · [Quy tắc cập nhật](AGENTS.md) · [Cốt truyện](Story/README.md)

## Tài liệu theo nhiệm vụ

| Nhiệm vụ | Trang cần đọc / sửa |
| --- | --- |
| World lifecycle, music ownership và Battle Box | [world-and-board](Combat/world-and-board.md) |
| Vòng combat, dice, Heart và Stun Zone | [loop-dice-heart](Combat/loop-dice-heart.md) |
| Tutorial D1 và nhịp dẫn vào trận thật | [tutorial-and-pacing](Combat/tutorial-and-pacing.md) |
| Data, enemy presentation và motion reference | [data-and-presentation](Combat/data-and-presentation.md) |
| Setup, Story integration và Retry | [setup-lifecycle-retry](Combat/setup-lifecycle-retry.md) |
| Lịch sử prototype lớp học và hướng mở rộng | [classroom-prototype-history](Combat/classroom-prototype-history.md) |
| Capture sequence, Timor ban đêm và nhạc | [timor-capture-and-music](Combat/timor-capture-and-music.md) |
| Timor finale Scene150: hồi TIME và checkpoint phase | [timor-final-checkpoints](Combat/timor-final-checkpoints.md) |
| Timor finale: chiêu mới, Battle Box đồng bộ và cao trào | [timor-finale-moves](Combat/timor-finale-moves.md) |
| Bianca supplies encounter và bằng chứng QA | [bianca-supplies](Combat/bianca-supplies.md) |
| Teacher, phấn, Victory và fading pressure | [teacher-and-fading-pressure](Combat/teacher-and-fading-pressure.md) |
| Crowd phản đòn, split board và VFX | [crowd-reactions-and-vfx](Combat/crowd-reactions-and-vfx.md) |

## Heading từ tài liệu trước khi tách

## 1. Scene hierarchy và lifecycle

[Xem phần này](Combat/world-and-board.md#1-scene-hierarchy-và-lifecycle)

### Required auto-dialogue tại shared HP threshold

[Xem phần này](Combat/world-and-board.md#required-auto-dialogue-tại-shared-hp-threshold)

## 2. Shared Battle Box

[Xem phần này](Combat/world-and-board.md#2-shared-battle-box)

### Music ownership

[Xem phần này](Combat/world-and-board.md#music-ownership)

### Board presentation

[Xem phần này](Combat/world-and-board.md#board-presentation)

## 3. Chu kỳ combat real-time

[Xem phần này](Combat/loop-dice-heart.md#3-chu-kỳ-combat-real-time)

## 4. Dice và hiệu ứng tức thì

[Xem phần này](Combat/loop-dice-heart.md#4-dice-và-hiệu-ứng-tức-thì)

## 5. Enemy attack và Audere Heart

[Xem phần này](Combat/loop-dice-heart.md#5-enemy-attack-và-audere-heart)

### Tutorial combat D1 Classroom

[Xem phần này](Combat/tutorial-and-pacing.md#tutorial-combat-d1-classroom)

### Production narrative pacing sau tutorial

[Xem phần này](Combat/tutorial-and-pacing.md#production-narrative-pacing-sau-tutorial)

## 6. Data và runtime code

[Xem phần này](Combat/data-and-presentation.md#6-data-và-runtime-code)

### Scene-authored enemy presentation

[Xem phần này](Combat/data-and-presentation.md#scene-authored-enemy-presentation)

## 7. Motion reference từ GIF `ry0CXX (1).gif`

[Xem phần này](Combat/data-and-presentation.md#7-motion-reference-từ-gif-ry0cxx-1gif)

## 8. Setup và debug QA

[Xem phần này](Combat/setup-lifecycle-retry.md#8-setup-và-debug-qa)

## 9. Lifecycle và Story integration (2026-08-22)

[Xem phần này](Combat/setup-lifecycle-retry.md#9-lifecycle-và-story-integration-2026-08-22)

### Shared Heartbreak Retry và Heart trắng — 2026-09-19

[Xem phần này](Combat/setup-lifecycle-retry.md#shared-heartbreak-retry-và-heart-trắng--2026-09-19)

## 10. Classroom prototype và hướng mở rộng (2026-08-23)

[Xem phần này](Combat/classroom-prototype-history.md#10-classroom-prototype-và-hướng-mở-rộng-2026-08-23)

## 11. Captured-dice sequence và D1 Timor night pressure (2026-08-25)

[Xem phần này](Combat/timor-capture-and-music.md#11-captured-dice-sequence-và-d1-timor-night-pressure-2026-08-25)

### Shared attack SFX — 2026-08-28

[Xem phần này](Combat/timor-capture-and-music.md#shared-attack-sfx--2026-08-28)

### Timor music grid — 2026-08-28

[Xem phần này](Combat/timor-capture-and-music.md#timor-music-grid--2026-08-28)

## Bianca supplies encounter — 2026-09-20

[Xem phần này](Combat/bianca-supplies.md#bianca-supplies-encounter--2026-09-20)

### Crossing ribbon correction — 2026-09-20

[Xem phần này](Combat/bianca-supplies.md#crossing-ribbon-correction--2026-09-20)

### Three-phase QA — 2026-09-20

[Xem phần này](Combat/bianca-supplies.md#three-phase-qa--2026-09-20)

### Projectile polish QA — 2026-08-28

[Xem phần này](Combat/bianca-supplies.md#projectile-polish-qa--2026-08-28)

### Day3 teacher pressure — 2026-08-28

[Xem phần này](Combat/teacher-and-fading-pressure.md#day3-teacher-pressure--2026-08-28)

### Radial chalk and catch-blocking trails — current contract

[Xem phần này](Combat/teacher-and-fading-pressure.md#radial-chalk-and-catch-blocking-trails--current-contract)

### Optional Victory dialogue presentation

[Xem phần này](Combat/teacher-and-fading-pressure.md#optional-victory-dialogue-presentation)

### Optional fading-pressure encounter (Day3 Bianca reprise)

[Xem phần này](Combat/teacher-and-fading-pressure.md#optional-fading-pressure-encounter-day3-bianca-reprise)

#### Catch avoidance correction — 2026-08-28

[Xem phần này](Combat/teacher-and-fading-pressure.md#catch-avoidance-correction--2026-08-28)

### Damage reactions and separated board halves — 2026-09-20

[Xem phần này](Combat/crowd-reactions-and-vfx.md#damage-reactions-and-separated-board-halves--2026-09-20)

### Following hit VFX and split outer bounds — 2026-09-20

[Xem phần này](Combat/crowd-reactions-and-vfx.md#following-hit-vfx-and-split-outer-bounds--2026-09-20)
