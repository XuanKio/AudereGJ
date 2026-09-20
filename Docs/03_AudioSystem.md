---
id: audere.audio
archetype: knowledge
version: 1.0.0
schema_version: 1.0.0
cost_tier: M
summary: Id-based, data-driven audio — gameplay plays sounds by AudioId, never by file name.
---

# Audere — Audio System

> Tách theo trách nhiệm ngày 2026-09-20. Đường dẫn và các heading cũ được giữ để không mất liên kết.
> Nội dung gốc, giới hạn QA và ngày checkpoint nằm trong các trang con; không xem số liệu cũ là trạng thái mới đã xác minh.

[Bản đồ tài liệu](README.md) · [Quy tắc cập nhật](AGENTS.md) · [Cốt truyện](Story/README.md)

## Tài liệu theo nhiệm vụ

| Nhiệm vụ | Trang cần đọc / sửa |
| --- | --- |
| AudioId, catalog, API và thay âm thanh | [identity-and-api](Audio/identity-and-api.md) |
| Tuning và SFX của loạt đạn | [tuning-and-volley-sfx](Audio/tuning-and-volley-sfx.md) |
| BGM, transition, quyền sở hữu và QA | [music-and-transitions](Audio/music-and-transitions.md) |

## Heading từ tài liệu trước khi tách

## Principle

[Xem phần này](Audio/identity-and-api.md#principle)

### Do / Don't

[Xem phần này](Audio/identity-and-api.md#do--dont)

## Pieces

[Xem phần này](Audio/identity-and-api.md#pieces)

### `AudioId` (enum) — permanent identity per sound

[Xem phần này](Audio/identity-and-api.md#audioid-enum--permanent-identity-per-sound)

### `AudioEntry` — one catalog row

[Xem phần này](Audio/identity-and-api.md#audioentry--one-catalog-row)

### `AudioCatalog` — the shared mapping (ScriptableObject)

[Xem phần này](Audio/identity-and-api.md#audiocatalog--the-shared-mapping-scriptableobject)

### `AudioService` — the play API

[Xem phần này](Audio/identity-and-api.md#audioservice--the-play-api)

## How to add a new sound

[Xem phần này](Audio/identity-and-api.md#how-to-add-a-new-sound)

## Swapping a sound (designer-friendly)

[Xem phần này](Audio/identity-and-api.md#swapping-a-sound-designer-friendly)

## Current state / tuning

[Xem phần này](Audio/tuning-and-volley-sfx.md#current-state--tuning)

## Combat volley SFX — 2026-08-28

[Xem phần này](Audio/tuning-and-volley-sfx.md#combat-volley-sfx--2026-08-28)

## Shared BGM and transition contract

[Xem phần này](Audio/music-and-transitions.md#shared-bgm-and-transition-contract)

### Initial BGM verification (2026-08-28, before combat tracks were assigned)

[Xem phần này](Audio/music-and-transitions.md#initial-bgm-verification-2026-08-28-before-combat-tracks-were-assigned)

### Combat music and rhythm verification (2026-08-28)

[Xem phần này](Audio/music-and-transitions.md#combat-music-and-rhythm-verification-2026-08-28)
