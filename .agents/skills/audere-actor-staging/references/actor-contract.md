# Actor contract

Use this reference before changing actor hierarchy, renderer, scale, pivot, facing, or shadow setup.

## Current production contract

An actor used by story staging needs these direct references:

| Reference | Meaning | Rules |
|---|---|---|
| `Actor` | Character transform animated by the step | Must be a real scene/prefab transform and start at a grounded authored pose. |
| `Target Transform` | Destination or reference ground pose | Use a scene anchor. `VerticalInPlace` still requires it for lifecycle/validation, normally the actor's current seat/pose anchor. |
| `Actor Renderer` | SpriteRenderer used for facing | Confirm whether the source sprite faces left before setting `Source Sprite Faces Left`. |
| `Grounded Shadow` | The actor's contact shadow | Assign directly. It may be a descendant of Actor; the motion step counteracts inherited height and scale. Actor staging preserves its authored color, alpha, and material. |

Current Audere, Bianca, and Teacher prefabs place the SpriteRenderer on the actor root and use a child named `shadow (1)`. Do not rely on that name for new production authoring; the serialized reference is the contract.

## Ground versus visible motion

Treat every actor pose as two related trajectories:

```text
groundPosition(t) = authored path from Actor to Target
bodyPosition(t)   = groundPosition(t) + vertical hop/reaction offset
shadowPosition(t) = groundPosition(t) + authored shadow ground offset
```

During in-place reaction:

```text
groundPosition(t) = startPosition
shadowPosition(t) = constant
```

During travel hop, the shadow may follow the actor horizontally across the floor, but it must not follow the vertical arc. Landing squash applies to the body only. The current `CharacterMotionStep` also preserves shadow world rotation and world scale while its actor parent stretches or squashes.

## Pivot and baseline

- The actor's authored position is the ground pose used by Story anchors. Do not compensate a bad sprite pivot by giving every target a different arbitrary Y offset.
- Keep all anchors for one staging plane on a shared baseline unless perspective or a deliberate step up/down requires otherwise.
- If artwork changes sprite bounds or pivot, validate every location prefab and story anchor that uses that actor.
- Do not use the shadow transform as the actor destination. The target describes the actor's ground pose, not the shadow's sprite center.

## Scale ownership

- Author the stable actor scale on the prefab/root. A StoryStep may apply temporary stretch/squash but must restore the exact starting scale.
- Avoid nested animated scales from two simultaneous systems. Do not run Animator scale clips and `CharacterMotionStep` squash on the same transform without a dedicated presentation split.
- Shadow scale is independent during story motion and remains at its authored value. Do not use shadow tint, alpha, material, or scale changes to sell an actor reaction; never lift it.

## Presentation ownership

- Actor movement owns the actor transform, intentional facing, and temporary body deformation only.
- It does not own the visual styling of the grounded shadow or the board/floor tile beneath the actor.
- Preserve the authored `SpriteRenderer.color`, alpha, material, and related tint properties on both shadow and tile before, during, after, cancel, and replay.
- Do not turn an ordinary actor hop, startle, hesitation, or arrival into a tile highlight or shadow-color effect.
- If a scene explicitly needs an environmental color effect, author it as a separate presentation component/StoryStep so its lifecycle is visible and independent from actor motion.

## Facing

- `FollowHorizontalTravel`: use for an actor visibly walking/hopping left or right.
- `FaceLeft` or `FaceRight`: use when a story beat explicitly turns toward another character or object.
- `Preserve`: use when direction carries continuity or horizontal displacement is too small to justify a flip.
- A flip is a pose decision, not a movement effect. Do not flip repeatedly around near-zero travel.
- Verify the source sprite orientation in the prefab. Audere's current source is authored facing left before `flipX` is applied.

## Ownership conflicts

Before staging, determine who currently owns the actor:

- `GridPlayer` owns Audere during StepTile traversal and falling.
- `CharacterMotionStep` owns the actor only for its StoryStep session.
- `MoveActorStep` owns plain position interpolation.
- An Animator may own sprite/pose properties if explicitly configured.

Never run two owners concurrently on the same transform. End or disable gameplay ownership before story staging begins.
