using System;
using UnityEngine;

namespace Audere.Combat
{
    public enum NarrativePressurePatternKind
    {
        HorizontalCorridor = 0,
        VerticalLaserColumns = 1,
        SafeZoneRain = 2,
        MovingGapWall = 3,
        SequentialFans = 4,
        SplitBurst = 5,
        OrbitRing = 6,
        SweepingLaser = 7,
        RotatingBlades = 8,
        PendulumLaser = 9,
        ClosingFinale = 10,
    }

    [CreateAssetMenu(
        menuName = "Audere/Combat/Moves/Narrative Pressure Pattern",
        fileName = "Move_NarrativePressure")]
    public sealed class NarrativePressurePatternMove : CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView projectilePrefab;
        [SerializeField] private NarrativePressurePatternKind pattern;
        [SerializeField, Min(.08f)] private float waveInterval = .75f;
        [SerializeField, Min(20f)] private float speed = 135f;
        [SerializeField, Min(12f)] private float spacing = 38f;
        [SerializeField, Range(0f, 1f)] private float telegraphDuration = .2f;
        [SerializeField, Range(.05f, .8f)] private float safeGapFraction = .28f;
        [SerializeField, Min(1)] private int intensity = 5;

        [Header("Optional Music Grid")]
        [SerializeField] private Audere.Audio.AudioId rhythmMusic = Audere.Audio.AudioId.None;
        [SerializeField, Min(0f)] private float rhythmBpm;
        [SerializeField] private float rhythmBeatOffset;
        [SerializeField, Min(.25f)] private float waveBeats = 2f;
        public bool UsesMusicGrid => rhythmMusic != Audere.Audio.AudioId.None && rhythmBpm > 0f;
        public float RhythmBpm => rhythmBpm;
        public float WaveBeats => waveBeats;

        public CombatBulletView ProjectilePrefab => projectilePrefab;
        public NarrativePressurePatternKind Pattern => pattern;
        public float WaveInterval => waveInterval;
        public float Speed => speed;
        public float Spacing => spacing;
        public float TelegraphDuration => telegraphDuration;
        public float SafeGapFraction => safeGapFraction;
        public int Intensity => Mathf.Max(1, intensity);

        public override bool Validate(out string error)
        {
            if (!base.Validate(out error))
                return false;
            if (projectilePrefab == null)
            {
                error = $"Move '{name}' requires a projectile prefab.";
                return false;
            }
            if (rhythmMusic != Audere.Audio.AudioId.None &&
                (rhythmBpm <= 0f || float.IsNaN(rhythmBpm) || float.IsInfinity(rhythmBpm) ||
                 waveBeats < .25f || float.IsNaN(waveBeats) || float.IsInfinity(waveBeats)))
            {
                error = $"Move '{name}' requires a finite positive music grid.";
                return false;
            }
            error = null;
            return true;
        }

        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context)
        {
            if (!Validate(out string error))
                throw new InvalidOperationException(error);
            return new Execution(this, context);
        }

        private sealed class Execution : ICombatMoveExecution
        {
            private readonly NarrativePressurePatternMove data;
            private readonly CombatMoveExecutionContext context;
            private float elapsed;
            private float cooldown;
            private readonly CombatMusicBeatClock musicClock = new CombatMusicBeatClock();
            private int waveIndex;
            private bool cancelled;

            public Execution(NarrativePressurePatternMove data, CombatMoveExecutionContext context)
            {
                this.data = data;
                this.context = context;
            }

            public bool IsComplete => cancelled ||
                (data.Pattern != NarrativePressurePatternKind.ClosingFinale && elapsed >= data.Duration);

            public void Tick(float activeDeltaTime)
            {
                if (cancelled || context.Board == null || context.Board.PlayArea == null)
                    return;

                elapsed += Mathf.Max(0f, activeDeltaTime);
                if (data.Pattern == NarrativePressurePatternKind.ClosingFinale)
                {
                    Rect rect = context.Board.PlayArea.rect;
                    float startRadius = Mathf.Min(rect.width, rect.height) * .42f;
                    context.Board.SetPlayerConstraint(Vector2.zero, Mathf.Lerp(startRadius, 8f, Mathf.Clamp01(elapsed / 1.25f)));
                }

                if (activeDeltaTime <= 0f) return;
                var audio = Audere.Audio.AudioService.Instance;
                if (data.UsesMusicGrid && audio != null && audio.CurrentMusicId == data.rhythmMusic &&
                    audio.MusicSource != null && audio.MusicSource.isPlaying && audio.MusicSource.clip != null)
                {
                    double songTime = audio.MusicSource.timeSamples / (double)audio.MusicSource.clip.frequency;
                    double period = 60d / data.rhythmBpm * data.waveBeats;
                    if (!IsComplete && musicClock.Tick(songTime, period, data.rhythmBeatOffset, data.TelegraphDuration, activeDeltaTime))
                    {
                        SpawnWave();
                        waveIndex++;
                    }
                    return;
                }
                // A live audio service may still be fading/loading the selected track.
                // Wait for its clock; an isolated scene without audio keeps local pacing.
                if (data.UsesMusicGrid && audio != null &&
                    (audio.CurrentMusicId != data.rhythmMusic ||
                     audio.MusicSource != null && audio.MusicSource.clip != null)) return;
                cooldown -= activeDeltaTime;
                while (cooldown <= 0f && !IsComplete)
                {
                    SpawnWave();
                    cooldown += Mathf.Max(.08f, data.WaveInterval);
                    waveIndex++;
                }
            }

            public void Cancel()
            {
                if (cancelled)
                    return;
                cancelled = true;
                if (data.Pattern == NarrativePressurePatternKind.ClosingFinale)
                    context.Board?.ClearPlayerConstraint();
            }

            private void SpawnWave()
            {
                Rect rect = context.Board.PlayArea.rect;
                switch (data.Pattern)
                {
                    case NarrativePressurePatternKind.HorizontalCorridor:
                        SpawnHorizontalCorridor(rect);
                        break;
                    case NarrativePressurePatternKind.VerticalLaserColumns:
                        SpawnVerticalLaserColumns(rect);
                        break;
                    case NarrativePressurePatternKind.SafeZoneRain:
                        SpawnSafeZoneRain(rect);
                        break;
                    case NarrativePressurePatternKind.MovingGapWall:
                        SpawnMovingGapWall(rect);
                        break;
                    case NarrativePressurePatternKind.SequentialFans:
                        SpawnSequentialFan(rect);
                        break;
                    case NarrativePressurePatternKind.SplitBurst:
                        SpawnSplitBurst(rect);
                        break;
                    case NarrativePressurePatternKind.OrbitRing:
                        SpawnOrbitRing(rect);
                        break;
                    case NarrativePressurePatternKind.SweepingLaser:
                        SpawnSweepingLaser(rect);
                        break;
                    case NarrativePressurePatternKind.RotatingBlades:
                        SpawnRotatingBlades(rect);
                        break;
                    case NarrativePressurePatternKind.PendulumLaser:
                        SpawnPendulumLaser(rect);
                        break;
                    case NarrativePressurePatternKind.ClosingFinale:
                        SpawnClosingFinale(rect);
                        break;
                }
            }

            private void SpawnHorizontalCorridor(Rect rect)
            {
                float gap = rect.height * data.SafeGapFraction;
                bool fromLeft = waveIndex % 2 == 0;
                for (float y = rect.yMin + data.Spacing * .5f; y < rect.yMax; y += data.Spacing)
                {
                    if (Mathf.Abs(y - rect.center.y) < gap * .5f)
                        continue;
                    Vector2 start = new Vector2(fromLeft ? rect.xMin + 8f : rect.xMax - 8f, y);
                    Spawn(start, (fromLeft ? Vector2.right : Vector2.left) * data.Speed);
                }
            }

            private void SpawnVerticalLaserColumns(Rect rect)
            {
                int laneCount = Mathf.Clamp(data.Intensity, 4, 6);
                int safeLane = waveIndex % laneCount;
                float laneWidth = rect.width / laneCount;
                for (int lane = 0; lane < laneCount; lane++)
                {
                    if (lane == safeLane || lane == (safeLane + 1) % laneCount)
                        continue;
                    float x = rect.xMin + laneWidth * (lane + .5f);
                    SpawnLaser(
                        new Vector2(x, rect.center.y),
                        new Vector2(x, rect.center.y),
                        new Vector2(Mathf.Max(16f, laneWidth * .24f), rect.height + 8f),
                        0f,
                        Mathf.Max(.38f, data.TelegraphDuration),
                        .32f);
                }
            }

            private void SpawnSafeZoneRain(Rect rect)
            {
                float safeHalf = Mathf.Max(42f, rect.width * data.SafeGapFraction * .5f);
                float playerX = context.Board.PlayerPosition.x;
                int count = Mathf.Max(3, data.Intensity);
                for (int i = 0; i < count; i++)
                {
                    float x = context.Random.Range(rect.xMin + 14f, rect.xMax - 14f);
                    if (Mathf.Abs(x - playerX) < safeHalf)
                        x = x < playerX ? playerX - safeHalf : playerX + safeHalf;
                    x = Mathf.Clamp(x, rect.xMin + 14f, rect.xMax - 14f);
                    Spawn(new Vector2(x, rect.yMax - 8f), Vector2.down * data.Speed);
                }
            }

            private void SpawnMovingGapWall(Rect rect)
            {
                float gapX = rect.center.x + Mathf.Sin(elapsed * 1.7f) * rect.width * .3f;
                float halfGap = Mathf.Max(32f, rect.width * data.SafeGapFraction * .5f);
                for (float x = rect.xMin + data.Spacing * .5f; x < rect.xMax; x += data.Spacing)
                {
                    if (Mathf.Abs(x - gapX) < halfGap)
                        continue;
                    Spawn(new Vector2(x, rect.yMax - 8f), Vector2.down * data.Speed);
                }
            }

            private void SpawnSequentialFan(Rect rect)
            {
                int side = waveIndex % 6;
                Vector2 origin = side switch
                {
                    0 => new Vector2(rect.center.x, rect.yMax - 8f),
                    1 => new Vector2(rect.xMin + 8f, rect.center.y),
                    2 => new Vector2(rect.center.x, rect.yMin + 8f),
                    3 => new Vector2(rect.xMax - 8f, rect.center.y),
                    4 => new Vector2(rect.xMax - 8f, rect.yMax - 8f),
                    _ => new Vector2(rect.xMin + 8f, rect.yMin + 8f),
                };
                Vector2 aimed = (context.Board.PlayerPosition - origin).normalized;
                SpawnFan(origin, aimed, Mathf.Max(3, data.Intensity), 46f);
            }

            private void SpawnSplitBurst(Rect rect)
            {
                int level = waveIndex % 4;
                int count = 1 << level;
                Vector2 origin = new Vector2(rect.center.x, rect.yMax - 8f);
                SpawnFan(origin, Vector2.down, count, Mathf.Lerp(8f, 76f, level / 3f));
            }

            private void SpawnOrbitRing(Rect rect)
            {
                int count = Mathf.Max(8, data.Intensity * 2);
                float radius = Mathf.Min(rect.width, rect.height) * Mathf.Lerp(.42f, .25f, Mathf.PingPong(elapsed * .12f, 1f));
                float gapAngle = elapsed * 80f;
                Vector2 center = context.Board.PlayerPosition;
                for (int i = 0; i < count; i++)
                {
                    float angle = gapAngle + i * (360f / count);
                    if (Mathf.Abs(Mathf.DeltaAngle(angle, gapAngle + 24f)) < 18f)
                        continue;
                    Vector2 radial = Rotate(Vector2.right, angle);
                    Vector2 tangent = new Vector2(-radial.y, radial.x);
                    Spawn(center + radial * radius, (tangent - radial * .18f).normalized * data.Speed);
                }
            }

            private void SpawnSweepingLaser(Rect rect)
            {
                bool fromLeft = waveIndex % 2 == 0;
                float startX = fromLeft ? rect.xMin + 12f : rect.xMax - 12f;
                float endX = fromLeft ? rect.xMax - 12f : rect.xMin + 12f;
                SpawnLaser(
                    new Vector2(startX, rect.center.y),
                    new Vector2(endX, rect.center.y),
                    new Vector2(22f, rect.height + 8f),
                    0f,
                    Mathf.Max(.45f, data.TelegraphDuration),
                    .92f);
            }

            private void SpawnRotatingBlades(Rect rect)
            {
                int arms = Mathf.Clamp(data.Intensity, 4, 6);
                float rotation = elapsed * 92f;
                Vector2 center = rect.center;
                for (int arm = 0; arm < arms; arm++)
                {
                    Vector2 direction = Rotate(Vector2.up, rotation + arm * (360f / arms));
                    for (int segment = 1; segment <= 3; segment++)
                        Spawn(center + direction * segment * data.Spacing, direction * data.Speed * .55f);
                }
            }

            private void SpawnPendulumLaser(Rect rect)
            {
                float irregularPause = waveIndex % 4 == 3 ? .35f : 0f;
                cooldown += irregularPause;
                float angle = Mathf.Sin((waveIndex + 1) * 1.17f) * 48f;
                float diagonalLength = Mathf.Sqrt(rect.width * rect.width + rect.height * rect.height) + 24f;
                SpawnLaser(
                    rect.center,
                    rect.center,
                    new Vector2(18f, diagonalLength),
                    angle,
                    Mathf.Max(.48f, data.TelegraphDuration),
                    .34f);
            }

            private void SpawnClosingFinale(Rect rect)
            {
                if (waveIndex % 3 == 0)
                {
                    float laneX = rect.center.x + Mathf.Sin(waveIndex * 1.37f) * rect.width * .24f;
                    SpawnLaser(
                        new Vector2(laneX, rect.center.y),
                        new Vector2(laneX, rect.center.y),
                        new Vector2(24f, rect.height + 8f),
                        0f,
                        Mathf.Max(.18f, data.TelegraphDuration),
                        .28f);
                    return;
                }
                int count = Mathf.Max(5, data.Intensity);
                for (int i = 0; i < count; i++)
                {
                    float t = (i + .5f) / count;
                    float x = Mathf.Lerp(rect.xMin + 8f, rect.xMax - 8f, t);
                    float y = Mathf.Lerp(rect.yMin + 8f, rect.yMax - 8f, t);
                    Spawn(new Vector2(x, rect.yMax - 8f), Vector2.down * data.Speed);
                    Spawn(new Vector2(x, rect.yMin + 8f), Vector2.up * data.Speed);
                    Spawn(new Vector2(rect.xMin + 8f, y), Vector2.right * data.Speed);
                    Spawn(new Vector2(rect.xMax - 8f, y), Vector2.left * data.Speed);
                }
            }

            private void SpawnFan(Vector2 origin, Vector2 baseDirection, int count, float spread)
            {
                for (int i = 0; i < count; i++)
                {
                    float t = count <= 1 ? .5f : i / (float)(count - 1);
                    Spawn(origin, Rotate(baseDirection, Mathf.Lerp(-spread, spread, t)) * data.Speed);
                }
            }

            private void Spawn(Vector2 origin, Vector2 velocity)
            {
                context.Board.SpawnEnemyBullet(
                    data.ProjectilePrefab,
                    origin,
                    velocity,
                    context.SessionVersion,
                    context.PhaseVersion,
                    data.TelegraphDuration);
            }

            private void SpawnLaser(
                Vector2 start,
                Vector2 end,
                Vector2 size,
                float rotation,
                float telegraph,
                float active)
            {
                context.Board.SpawnEnemyLaser(
                    start,
                    end,
                    size,
                    rotation,
                    telegraph,
                    active,
                    context.SessionVersion,
                    context.PhaseVersion);
            }

            private static Vector2 Rotate(Vector2 vector, float degrees)
            {
                float radians = degrees * Mathf.Deg2Rad;
                float sin = Mathf.Sin(radians);
                float cos = Mathf.Cos(radians);
                return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
            }
        }

        private void OnValidate()
        {
            if (!Validate(out string error))
                Debug.LogError($"[NarrativePressurePatternMove] {error}", this);
        }
    }
}
