using System.Collections;
using System.Collections.Generic;
using Audere.Audio;
using UnityEngine;

namespace Audere.Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatController : MonoBehaviour
    {
        public enum State
        {
            Idle,
            EncounterIntro,
            Playing,
            Victory,
            Defeat,
        }

        [SerializeField] private CombatEncounterData encounterData;
        [SerializeField] private CombatBoardView boardView;
        [SerializeField, Min(.01f)] private float spawnStagger = .065f;

        [Header("Dice Collision")]
        [SerializeField, Range(0f, 1f)] private float diceCollisionBounciness = .92f;
        [SerializeField, Min(0f)] private float diceCollisionSeparationPadding = .5f;
        [SerializeField, Range(1, 4)] private int diceCollisionIterations = 2;

        private readonly List<CombatDieView> activeDice = new List<CombatDieView>();
        private int playerArmor;
        private int enemyHealth;
        private int batchIndex;
        private int patternIndex;
        private int patternShotIndex;
        private float encounterTimeRemaining;
        private float patternTimeRemaining;
        private float shotCooldown;
        private bool hasStarted;
        private bool isBatchSpawning;
        private Coroutine batchRoutine;

        public State CurrentState { get; private set; } = State.Idle;
        public int PlayerArmor => playerArmor;
        public float PlayerTime => encounterTimeRemaining;
        public int EnemyHealth => enemyHealth;
        public int BatchIndex => batchIndex;
        public float EncounterTimeRemaining => encounterTimeRemaining;

        private void Awake()
        {
            if (boardView == null)
                boardView = FindFirstObjectByType<CombatBoardView>(FindObjectsInactive.Include);
        }

        private void Start()
        {
            hasStarted = true;
            BeginEncounter();
        }

        private void OnEnable()
        {
            if (hasStarted)
                BeginEncounter();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            batchRoutine = null;
            activeDice.Clear();
            isBatchSpawning = false;
            if (boardView != null)
                boardView.ClearCombatRuntime();
            CurrentState = State.Idle;
        }

        private void Update()
        {
            if (CurrentState != State.Playing || encounterData == null || boardView == null)
                return;

            float deltaTime = Time.deltaTime;
            encounterTimeRemaining = Mathf.Max(0f, encounterTimeRemaining - deltaTime);
            boardView.UpdateTimer(encounterTimeRemaining / encounterData.EncounterDuration);

            // The Heart is centered inside the catcher, so mouse movement drives
            // both dice interaction and dodging in the same shared space.
            boardView.UpdateCursor(Input.mousePosition);
            boardView.TickHeartFeedback(deltaTime);

            TickDice(deltaTime);
            TickEnemyAttack(deltaTime);

            int bulletHits = boardView.TickBullets(
                deltaTime,
                encounterData.PlayerHitInvulnerability);
            for (int i = 0; i < bulletHits && CurrentState == State.Playing; i++)
                ApplyPlayerHit();

            if (Input.GetMouseButtonDown(0))
                TryCatchUnderCursor();
            else if (Input.GetMouseButtonDown(1))
                TryRerollUnderCursor();

            if (encounterTimeRemaining <= 0f)
                EndCombat(State.Defeat);
        }

        private void LateUpdate()
        {
            if (CurrentState == State.Defeat && Input.GetKeyDown(KeyCode.R))
                BeginEncounter();
        }

        public void BeginEncounter()
        {
            StopAllCoroutines();
            batchRoutine = null;
            activeDice.Clear();
            isBatchSpawning = false;

            if (encounterData == null || boardView == null)
            {
                Debug.LogError("[CombatController] Assign Encounter Data and Combat Board View.", this);
                CurrentState = State.Idle;
                return;
            }

            playerArmor = 0;
            enemyHealth = encounterData.EnemyMaxHealth;
            encounterTimeRemaining = encounterData.EncounterDuration;
            batchIndex = 0;
            patternIndex = 0;
            patternShotIndex = 0;

            boardView.ClearCombatRuntime();
            boardView.PrepareEncounter(encounterData.EnemyDisplayName);
            StartCoroutine(EncounterIntro());
        }

        public void DebugApplyDiceEffect(CombatSymbol symbol)
        {
            if (CurrentState == State.Playing)
                ApplyImmediateDiceEffect(symbol);
        }

        public void DebugExpireTimer()
        {
            encounterTimeRemaining = 0f;
            if (encounterData != null && boardView != null)
                boardView.UpdateTimer(0f);
            if (CurrentState == State.Playing)
                EndCombat(State.Defeat);
        }

        public void DebugSetTimerHalf()
        {
            if (encounterData == null || boardView == null)
                return;
            encounterTimeRemaining = encounterData.EncounterDuration * .5f;
            boardView.UpdateTimer(.5f);
        }

        public void DebugTakePlayerHit()
        {
            if (CurrentState == State.Playing)
                ApplyPlayerHit();
        }

        private IEnumerator EncounterIntro()
        {
            CurrentState = State.EncounterIntro;
            yield return boardView.PlayEnemyIntro();
            yield return new WaitForSecondsRealtime(.12f);

            CurrentState = State.Playing;
            boardView.ResetPlayer();
            BeginPattern(0);
            ScheduleNextBatch(0f);
        }

        private void TickDice(float deltaTime)
        {
            Rect playRect = boardView.PlayArea != null ? boardView.PlayArea.rect : default;
            for (int i = activeDice.Count - 1; i >= 0; i--)
            {
                CombatDieView die = activeDice[i];
                if (die == null || die.IsCaptured || !die.gameObject.activeInHierarchy)
                {
                    activeDice.RemoveAt(i);
                    continue;
                }

                die.TickMovement(playRect, deltaTime);
            }

            ResolveDiceCollisions(playRect);
        }

        private void ResolveDiceCollisions(Rect playRect)
        {
            int iterations = Mathf.Clamp(diceCollisionIterations, 1, 4);
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                for (int i = 0; i < activeDice.Count - 1; i++)
                {
                    CombatDieView first = activeDice[i];
                    if (first == null || first.IsCaptured || !first.gameObject.activeInHierarchy) continue;

                    for (int j = i + 1; j < activeDice.Count; j++)
                    {
                        CombatDieView second = activeDice[j];
                        if (second == null || second.IsCaptured || !second.gameObject.activeInHierarchy) continue;
                        first.ResolveCollisionWith(
                            second,
                            diceCollisionBounciness,
                            diceCollisionSeparationPadding);
                    }
                }
            }

            for (int i = 0; i < activeDice.Count; i++)
            {
                CombatDieView die = activeDice[i];
                if (die != null && !die.IsCaptured && die.gameObject.activeInHierarchy)
                    die.ConstrainToBounds(playRect);
            }
        }

        private void TryCatchUnderCursor()
        {
            for (int i = activeDice.Count - 1; i >= 0; i--)
            {
                CombatDieView die = activeDice[i];
                if (die != null && die.CanInteract && boardView.CursorOverlaps(die))
                {
                    if (boardView.IsCursorStunned)
                    {
                        boardView.PlayBlockedCursorFeedback();
                        return;
                    }
                    CatchDie(die);
                    return;
                }
            }
        }

        private void TryRerollUnderCursor()
        {
            for (int i = activeDice.Count - 1; i >= 0; i--)
            {
                CombatDieView die = activeDice[i];
                if (die != null && die.CanInteract && boardView.CursorOverlaps(die))
                {
                    activeDice[i] = boardView.RerollDie(die, encounterData.RollSymbol());
                    AudioService.Instance?.Play(AudioId.Dice_Roll);
                    return;
                }
            }
        }

        private void CatchDie(CombatDieView die)
        {
            if (die == null || die.IsCaptured || !activeDice.Remove(die))
                return;

            CombatSymbol symbol = die.Symbol;
            die.PlayCaptured();
            AudioService.Instance?.Play(AudioId.Dice_Select);
            ApplyImmediateDiceEffect(symbol);

            if (activeDice.Count == 0 && !isBatchSpawning && CurrentState == State.Playing)
                ScheduleNextBatch(encounterData.BatchRespawnDelay);
        }

        private void ApplyImmediateDiceEffect(CombatSymbol symbol)
        {
            switch (symbol)
            {
                case CombatSymbol.Attack:
                    int previousEnemyHealth = enemyHealth;
                    enemyHealth = Mathf.Max(0, enemyHealth - encounterData.AttackDamage);
                    boardView.PlayEnemyDamageFeedback(
                        previousEnemyHealth / (float)encounterData.EnemyMaxHealth,
                        enemyHealth / (float)encounterData.EnemyMaxHealth);
                    boardView.PlayEnemyDamageNumber(previousEnemyHealth - enemyHealth);
                    boardView.PlayAttackHitVfx();
                    boardView.TriggerEnemyHitFeedback();
                    AudioService.Instance?.Play(AudioId.Dice_Hit);
                    if (enemyHealth <= 0)
                    {
                        EndCombat(State.Victory);
                        return;
                    }
                    break;

                case CombatSymbol.Armor:
                    playerArmor += encounterData.ArmorPerDie;
                    break;

                case CombatSymbol.Heal:
                    encounterTimeRemaining = Mathf.Min(
                        encounterData.EncounterDuration,
                        encounterTimeRemaining + encounterData.HealTimeSeconds);
                    boardView.UpdateTimer(encounterTimeRemaining / encounterData.EncounterDuration);
                    break;
            }
        }

        private void ApplyPlayerHit()
        {
            if (playerArmor > 0)
                playerArmor--;
            else
            {
                float previousTime = encounterTimeRemaining;
                encounterTimeRemaining = Mathf.Max(
                    0f,
                    encounterTimeRemaining - encounterData.BulletTimePenaltySeconds);
                boardView.PlayPlayerDamageFeedback(
                    previousTime / encounterData.EncounterDuration,
                    encounterTimeRemaining / encounterData.EncounterDuration);
                AudioService.Instance?.Play(AudioId.Nilah_Hurt);
            }

            if (encounterTimeRemaining <= 0f)
                EndCombat(State.Defeat);
        }

        private void ScheduleNextBatch(float delay)
        {
            if (batchRoutine != null || CurrentState != State.Playing)
                return;
            batchRoutine = StartCoroutine(SpawnBatchAfterDelay(delay));
        }

        private IEnumerator SpawnBatchAfterDelay(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            if (CurrentState != State.Playing)
            {
                batchRoutine = null;
                yield break;
            }

            isBatchSpawning = true;
            batchIndex++;

            for (int i = 0; i < encounterData.DicePerBatch; i++)
            {
                if (CurrentState != State.Playing)
                    break;

                float speed = UnityEngine.Random.Range(
                    encounterData.MinimumDiceSpeed,
                    encounterData.MaximumDiceSpeed);
                CombatDieView die = boardView.SpawnDie(encounterData.RollSymbol(), speed);
                if (die != null)
                    activeDice.Add(die);
                yield return new WaitForSeconds(spawnStagger);
            }

            isBatchSpawning = false;
            batchRoutine = null;
            if (activeDice.Count == 0 && CurrentState == State.Playing)
                ScheduleNextBatch(encounterData.BatchRespawnDelay);
        }

        private void BeginPattern(int index)
        {
            patternIndex = index;
            patternShotIndex = 0;
            EnemyAttackPatternDefinition pattern = encounterData.GetAttackPattern(patternIndex);
            patternTimeRemaining = Mathf.Max(.5f, pattern.Duration);
            shotCooldown = .15f;
        }

        private void TickEnemyAttack(float deltaTime)
        {
            EnemyAttackPatternDefinition pattern = encounterData.GetAttackPattern(patternIndex);
            patternTimeRemaining -= deltaTime;
            shotCooldown -= deltaTime;

            if (shotCooldown <= 0f)
            {
                SpawnPatternShot(pattern);
                shotCooldown += Mathf.Max(.08f, pattern.ShotInterval);
                patternShotIndex++;
            }

            if (patternTimeRemaining <= 0f)
                BeginPattern(patternIndex + 1);
        }

        private void SpawnPatternShot(EnemyAttackPatternDefinition pattern)
        {
            Rect rect = boardView.PlayArea.rect;
            int count = Mathf.Max(1, pattern.BulletsPerShot);
            float speed = Mathf.Max(20f, pattern.BulletSpeed);

            switch (pattern.Kind)
            {
                case EnemyAttackPatternKind.SideSweep:
                {
                    bool fromLeft = patternShotIndex % 2 == 0;
                    float x = fromLeft ? rect.xMin + 10f : rect.xMax - 10f;
                    float baseY = UnityEngine.Random.Range(rect.yMin + 50f, rect.yMax - 50f);
                    for (int i = 0; i < count; i++)
                    {
                        float yOffset = (i - (count - 1) * .5f) * 42f;
                        Vector2 direction = fromLeft ? Vector2.right : Vector2.left;
                        boardView.SpawnEnemyBullet(new Vector2(x, baseY + yOffset), direction * speed);
                    }
                    break;
                }

                case EnemyAttackPatternKind.Rain:
                    for (int i = 0; i < count; i++)
                    {
                        float x = UnityEngine.Random.Range(rect.xMin + 30f, rect.xMax - 30f);
                        float angle = UnityEngine.Random.Range(-pattern.SpreadDegrees, pattern.SpreadDegrees);
                        Vector2 direction = Rotate(Vector2.down, angle);
                        boardView.SpawnEnemyBullet(new Vector2(x, rect.yMax - 10f), direction * speed);
                    }
                    break;

                default:
                {
                    Vector2 origin = new Vector2(0f, rect.yMax - 10f);
                    Vector2 aimed = (boardView.PlayerPosition - origin).normalized;
                    for (int i = 0; i < count; i++)
                    {
                        float t = count <= 1 ? .5f : (float)i / (count - 1);
                        float angle = Mathf.Lerp(-pattern.SpreadDegrees, pattern.SpreadDegrees, t);
                        boardView.SpawnEnemyBullet(origin, Rotate(aimed, angle) * speed);
                    }
                    break;
                }
            }
        }

        private void EndCombat(State result)
        {
            if (CurrentState == State.Victory || CurrentState == State.Defeat)
                return;

            CurrentState = result;
            if (batchRoutine != null)
                StopCoroutine(batchRoutine);
            batchRoutine = null;
            isBatchSpawning = false;
            activeDice.Clear();
            boardView.ClearCombatRuntime();
            boardView.SetCursorVisible(false);
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos);
        }
    }
}
