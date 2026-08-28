# Moves and mechanics

## Current attack vocabulary

Use authored move data, not enemy-ID switches.

| Move/pattern | Purpose |
| --- | --- |
| `LinearProjectilePatternMove` | Generic `AimedFan`, alternating `SideSweep`, and top `Rain`. |
| `ConvergingSideCorridorMove` | Side walls close into a readable corridor. |
| `HorizontalCorridor` | Top/bottom lanes form a moving horizontal corridor. |
| `VerticalLaserColumns` | White alpha/width telegraph, then red-pink vertical columns with safe lanes. |
| `SafeZoneRain` | Rain around a temporary safe zone. |
| `MovingGapWall` | Dense wall with an authored moving gap. |
| `SequentialFans` | Fans arrive from cardinal/corner directions. |
| `SplitBurst` | Projectile branching pressure. |
| `OrbitRing` | A ring closes around Heart while preserving gaps. |
| `SweepingLaser` | Horizontal laser sweep. |
| `RotatingBlades` | Rotating radial lanes/blades. |
| `PendulumLaser` | Alternating laser rhythm with a false pause. |
| `ClosingFinale` | Center constraint plus lethal multi-direction pressure for a data-authorized finale. |
| `ShiftingBattleBoxMove` | Keeps `Frame` fixed; changes only `Dice Field` Width and Pos X. |
| `StunZonePressureMove` | Pulses a catch-blocking zone through Hidden → Telegraph → Blocking → Fade. |
| `CompositeCombatMove` | Runs authored mechanics concurrently without merging unrelated branches. |

Add a small move subclass when an attack has a distinct spatial rule. Use composition when independent mechanics should overlap. Do not grow one enum/class until it contains unrelated lifecycle or boss logic.

## Bullets and lasers

Projectile pools are keyed by prefab. Each spawn resets transform, velocity, lifetime, collision, VFX, hit state, callback, session, and phase. Return disables collision/callback before deactivation. Old projectiles cannot hit after phase/session invalidation.

Lasers always telegraph before collision: start white at alpha `0` and narrow scale; ease alpha/width up; switch to active red-pink; enable collision only after telegraph; disable collision before fade/return. The projectile mask clips hazards inside the visible frame.

## Stun Zone contract

Stun Zone is a combat mechanic, not permanent board decoration.

- `Hidden`: alpha zero, no blocking.
- `Telegraph`: fades in at authored position/size, still no blocking.
- `Blocking`: only left-click catch is denied while Catch Cursor overlaps; cursor shows stunned styling and blocked `X` feedback.
- `Fade`: blocking ends immediately, then the visual fades out.

Dice motion/color, Heart movement, bullet collision, and right-click reroll are unchanged. Tutorial may own a fixed authored zone; production phases own dynamic zones through move data. Phase break/cancel/defeat/retry/disable hides zones and clears cursor stun.

Current Timor prototype composition is `Design Intent`: phase 4 alternates left/right vertical bands over `MovingGapWall`; phase 6 alternates left/right/center while the field shifts; phase 9 pulses center vertical and upper/lower horizontal bands over `RotatingBlades`. Change geometry/timing in Stun Zone move assets so shared runtime fixes propagate to all phases.

## Battle Box and dice

`ShiftingBattleBoxMove` changes only field width/X. Height/Y and Frame remain fixed. Preserve Heart world position while valid; clamp only when an edge crosses it; reset field/airborne overlay on every exit.

Attack damages immediately with shared scratch feedback. Shield clears nearby bullets without a colored radius. Heal restores TIME up to the active maximum. Catch resolves immediately. Reroll does not count as captured-batch progress. Dice become catchable only after authored airborne bounce lands.
