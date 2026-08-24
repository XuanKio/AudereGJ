# Story authoring with actors

Read this when actor motion participates in a production StoryEvent or an editor setup tool.

## Scene-first hierarchy

Keep actor performance visible as ordered direct-child StorySteps:

```text
D1_EXAMPLE [StoryEvent]
├── 00_NormalizeActors        [SetActiveStep]
├── 10_PlaceActorUnderFade    [MoveActorStep, duration 0]
├── 20_FadeIn                 [CanvasFadeStep]
├── 30_DialogueBefore         [DialogueStep]
├── 40_ActorStartles          [CharacterMotionStep]
├── 50_ReactionBeat           [WaitStep]
└── 60_DialogueAfter          [DialogueStep]
```

Sibling order is performance order. Do not bury a reaction in dialogue playback or a general-purpose manager.

## Staging anchors

- Put authored targets under a clearly named inactive `STAGING` or `Story Anchors` root.
- Name anchors by actor and pose, such as `Audere_Seat`, `Bianca_ApproachMid`, or `Teacher_Front`.
- Use direct Transform references. Do not find anchors by name at runtime.
- Anchor position is the actor's grounded world pose. Keep anchors free of renderers, colliders, and gameplay behavior.
- Reuse an anchor only when it truly represents the same pose. Avoid one generic target whose position is rewritten between beats.

## Choosing the step

Use `MoveActorStep` when:

- placement occurs under a fade;
- the motion should be plain interpolation with no hop;
- an actor makes a subtle short nudge;
- exact current-to-target travel is sufficient.

Use `CharacterMotionStep` when:

- the motion must carry the shared light hop/landing feel;
- an actor travels visibly between staging anchors;
- the actor reacts vertically in place;
- facing should follow or be explicitly set during the motion.

Do not use either step to move the puzzle player during active traversal; `GridPlayer` owns that lifecycle.

Actor steps do not own tile presentation. Do not recolor or fade the tile under an actor to support a movement or reaction, and do not change the grounded shadow's color/alpha/material. If an explicitly requested scene effect needs to alter the environment, place it in a separate, visible StoryStep with its own references and lifecycle.

## Inspector contract for CharacterMotionStep

- `Actor`: direct actor root.
- `Target Transform`: direct staging anchor.
- `Actor Renderer`: direct renderer used for `flipX`.
- `Grounded Shadow`: direct shadow transform. Required in production even though legacy fallback exists.
- `Motion Mode`: `TravelToTarget` or `VerticalInPlace`.
- `Duration`, `Arc Height`, stretch, landing values: choose from `motion-language.md` and tune visually.
- `Use Unscaled Time`: normally enabled for narrative staging.
- `Facing Mode`: deliberate choice; do not leave `FollowHorizontalTravel` on a zero-distance reaction when the actor must turn.
- `Source Sprite Faces Left`: verify against prefab artwork.

## Editor setup tools

When an editor setup tool creates or refreshes a CharacterMotionStep, it must assign all four direct references, including `groundedShadow`. It may locate the shadow while authoring, then serialize the result; production runtime must not depend on a global search.

Setup tools should be idempotent:

- reuse production roots and event IDs;
- avoid creating duplicate anchors or actors;
- preserve unrelated scene work;
- save the intended scene explicitly;
- compile before assigning newly introduced fields/components.

## Dialogue and action interleaving

Split dialogue data where a visible performance action occurs:

```text
Dialogue: Bianca calls
Wait: Audere does not answer
CharacterMotion: Audere startles and turns
Dialogue: Bianca apologizes
```

This preserves callback ownership and makes timing editable in Hierarchy. If new words are written or revised, use the dialogue voice skill; actor staging does not decide character voice.

## Cancellation and replay

- Cancel during a travel hop: stop at the current ground projection, remove lift/squash, keep facing already applied, and do not snap to start or target.
- Cancel during an in-place reaction: return to the same ground pose without leaving vertical offset.
- Replay starts from the actor's current pose unless a preceding normalize step intentionally restores a canonical starting pose.
- A story normalize step may place actors instantly before fade-in, but must not accidentally trigger visible movement or dialogue.
