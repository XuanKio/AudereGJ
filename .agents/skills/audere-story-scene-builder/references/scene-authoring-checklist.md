# Audere Production Scene Checklist

Use this checklist when a request creates a scene, crosses systems, or loads another scene.

## Narrative pass

- Identify the scene's story position and relationship progression band.
- Read dialogue immediately before and after the new beat.
- Separate spoken character lines from mechanical UI instructions.
- Mark missing character profile, portrait, art, voice, or ontology as `Unresolved`.
- Split dialogue assets where a staged action must occur between lines.
- Keep each current dialogue bubble near `42` visible characters; split at complete speech
  beats and preview the longest left/right lines at target resolution.

## Hierarchy pass

```text
STORY [StoryDirector]
└── D*_PRODUCTION_EVENT [StoryEvent]
    ├── 00_Normalize
    ├── 10_VisibleAction
    ├── 20_Dialogue
    └── 30_NextAction
```

- Only direct children are executable steps.
- One active `StoryStep` component per direct child.
- Names use numeric prefixes and describe the visible action.
- Scene actors, targets, overlays, and controllers use direct Inspector references.
- No `FindFirstObjectByType` is added for authoring convenience.

## Cross-scene pass

- Source gameplay controller has completed or cancelled cleanly.
- Source UI is hidden before fade.
- Source fade reaches full opacity before loading.
- Destination starts covered and fades in using unscaled time.
- Destination `StoryDirector` has the intended production starting event.
- Scene is included in Build Settings and referenced through `GameScenes`/`SceneFlow` convention.

## Puzzle hand-off pass

- Shared player ends on the source Goal.
- Runtime preview and placed path visuals are cleared or hidden once.
- No second `PathPreview`, placement controller, or hand exists under a level prefab.
- Source Goal remains only when it is deliberately serving as the next transition anchor.
- Next puzzle places the shared player from its authored `PlayerStart` or captured transition anchor.
- Any story actor shown standing on a puzzle-style tile places its visual feet/standing anchor
  at the tile center. Do not assume the actor pivot is at the feet; current sprites use a
  center-body pivot. Derive the authoring offset from renderer bounds or a prefab feet anchor.
- Startle/reaction hops lock X/Z and return to the original Y baseline; only locomotion hops
  travel toward another tile target.

## QA pass

- Compile: zero errors.
- Normal production playthrough: no stale UI, callback, or input claim.
- Cancel/disable during each asynchronous step: one result only.
- Replay: starts from normalized scene-authored state.
- Visual check at target game resolution: dialogue fits, tile movement is readable, fade covers all presentation.
- Documentation and canon ledger are updated after the production scene is verified.
