using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Audere.Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatBoardView : MonoBehaviour
    {
        [Header("Shared Battle Box")]
        [SerializeField] private RectTransform playArea;
        [SerializeField] private RectTransform stunZoneRoot;
        [SerializeField] private RectTransform bulletRoot;
        [SerializeField] private RectTransform diceRoot;
        [SerializeField] private RectTransform airborneDiceRoot;
        [SerializeField] private RectTransform playerRoot;
        [SerializeField] private RectTransform catchCursorRoot;
        [SerializeField] private RectTransform feedbackRoot;
        [SerializeField] private RectTransform catchCursor;
        [SerializeField] private CombatCatchCursorView catchCursorView;
        [SerializeField] private CombatPlayerView playerView;

        [Header("Authored Presentation")]
        [FormerlySerializedAs("enemyStatus")]
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private Slider enemyHealthSlider;
        [SerializeField] private Image enemyHealthFill;
        [SerializeField] private Image enemyHealthDamageFill;
        [SerializeField] private Image enemyHealthOutline;
        [SerializeField] private Image timerFill;
        [SerializeField] private Image timerDamageFill;
        [SerializeField] private Transform enemyMount;
        [Tooltip("Edit-mode staging preview only. Runtime always spawns from CombatEnemyDefinition.")]
        [SerializeField] private CombatEnemyActor authoredEnemyPreview;
        [SerializeField] private Transform enemyVisual;
        [SerializeField] private Transform vfxRoot;
        [SerializeField] private GameObject enemyScratchVfxPrefab;

        [Header("Enemy Damage Number")]
        [SerializeField] private RectTransform damageNumberRoot;
        [SerializeField] private TMP_FontAsset damageNumberFont;
        [SerializeField] private Color damageNumberColor = new Color(1f, .90f, .68f, 1f);
        [SerializeField, Min(8f)] private float damageNumberFontSize = 84f;
        [SerializeField, Min(.1f)] private float damageNumberDuration = .72f;
        [SerializeField, Min(0f)] private float damageNumberRiseDistance = 74f;
        [SerializeField, Min(0f)] private float damageNumberSpawnSpread = 18f;

        [Header("Presentation")]
        [SerializeField] private CombatDieView attackDicePrefab;
        [FormerlySerializedAs("armorDicePrefab")]
        [SerializeField] private CombatDieView shieldDicePrefab;
        [SerializeField] private CombatDieView healDicePrefab;
        [SerializeField] private CombatBulletView enemyBulletPrefab;
        [SerializeField] private Vector2 diceSize = new Vector2(
            CombatDiceConstants.DefaultVisualSize,
            CombatDiceConstants.DefaultVisualSize);
        [SerializeField] private Color timerSafeColor = new Color(.50f, .33f, .46f, 1f);
        [SerializeField] private Color timerDangerColor = new Color(.92f, .34f, .31f, 1f);
        [SerializeField] private Color timerDamageColor = new Color(1f, 1f, 1f, .96f);
        [SerializeField, Min(0f)] private float timerDamageHoldDuration = .12f;
        [SerializeField, Min(.05f)] private float timerDamageDrainDuration = .34f;
        [SerializeField] private Color enemyHealthColor = new Color(96f / 255f, 88f / 255f, 104f / 255f, 1f);
        [SerializeField, Min(0f)] private float enemyHealthDamageHoldDuration = .12f;
        [SerializeField, Min(.05f)] private float enemyHealthDamageDrainDuration = .34f;
        [SerializeField, Min(.05f)] private float damageShakeDuration = .20f;
        [SerializeField, Range(0f, .15f)] private float damageShakeStrength = .045f;
        [SerializeField, Min(1f)] private float damageShakeFrequency = 38f;

        private readonly List<CombatBulletView> activeBullets = new List<CombatBulletView>();
        private readonly List<GameObject> activeHitVfx = new List<GameObject>();
        private readonly List<CombatDamageNumberView> damageNumberPool = new List<CombatDamageNumberView>();
        private Camera eventCamera;
        private SpriteRenderer[] enemySpriteRenderers;
        private Material[] enemyOriginalMaterials;
        private Graphic[] enemyGraphics;
        private Color[] enemyOriginalGraphicColors;
        private Material enemyWhiteFlashMaterial;
        private Coroutine enemyHitRoutine;
        private Coroutine timerDamageRoutine;
        private Coroutine cameraShakeRoutine;
        private Coroutine enemyHealthDamageRoutine;
        private Vector3 enemyAuthoredLocalPosition;
        private Transform shakeCameraTransform;
        private Vector3 shakeCameraBasePosition;
        private float currentTimerNormalized = 1f;
        private float timerDamageTargetNormalized = 1f;
        private float enemyHealthDamageTargetNormalized = 1f;
        private CombatEnemyActor activeEnemyActor;
        private int activeEnemySessionVersion;

        public RectTransform PlayArea => playArea;
        public RectTransform CatchCursor => catchCursor;
        public RectTransform StunZoneFocusTarget
        {
            get
            {
                if (stunZoneRoot == null)
                    return null;
                for (int i = 0; i < stunZoneRoot.childCount; i++)
                {
                    if (stunZoneRoot.GetChild(i) is RectTransform zone && zone.gameObject.activeInHierarchy)
                        return zone;
                }
                return stunZoneRoot;
            }
        }
        public RectTransform TimerFocusTarget => timerFill != null
            ? timerFill.rectTransform.parent as RectTransform ?? timerFill.rectTransform
            : null;
        public CombatEnemyActor ActiveEnemyActor => activeEnemyActor;
        public bool IsCursorStunned => catchCursorView != null && catchCursorView.IsStunned;
        public Vector2 PlayerPosition
        {
            get
            {
                if (playArea == null || playerView == null || playerView.RectTransform == null)
                    return catchCursor != null ? catchCursor.anchoredPosition : Vector2.zero;

                Vector3 worldCenter = playerView.RectTransform.TransformPoint(playerView.RectTransform.rect.center);
                Vector3 localCenter = playArea.InverseTransformPoint(worldCenter);
                return new Vector2(localCenter.x, localCenter.y);
            }
        }

        private void Awake()
        {
            ResolveReferences();
            eventCamera = Camera.main;
            if (Application.isPlaying && authoredEnemyPreview != null)
                authoredEnemyPreview.gameObject.SetActive(false);
            if (enemyVisual != null)
                enemyAuthoredLocalPosition = enemyVisual.localPosition;
        }

        public void PrepareEncounter(string enemyName)
        {
            ResolveReferences();
            ResetPlayerDamageFeedback();
            ResetEnemyHealth();
            SetEnemyName(enemyName);
            if (catchCursorView != null) catchCursorView.SetStunned(false, true);
            SetCursorVisible(false);
            UpdateTimer(1f);
        }

        public CombatEnemyActor SpawnEnemyActor(CombatEnemyActor prefab, int sessionVersion)
        {
            ResolveReferences();
            ClearEnemyActor();
            if (prefab == null || enemyMount == null)
                return null;

            activeEnemyActor = Instantiate(prefab, enemyMount);
            activeEnemyActor.name = prefab.name;
            activeEnemySessionVersion = sessionVersion;
            enemyVisual = activeEnemyActor.VisualRoot != null
                ? activeEnemyActor.VisualRoot
                : activeEnemyActor.transform;
            vfxRoot = activeEnemyActor.VfxAnchor;
            enemyAuthoredLocalPosition = enemyVisual.localPosition;
            enemySpriteRenderers = null;
            enemyOriginalMaterials = null;
            enemyGraphics = activeEnemyActor.Graphics;
            CaptureEnemyGraphicColors();
            return activeEnemyActor;
        }

        public Vector2 WorldToPlayArea(Vector3 worldPosition)
        {
            if (playArea == null)
                return Vector2.zero;
            Vector3 local = playArea.InverseTransformPoint(worldPosition);
            return new Vector2(local.x, local.y);
        }

        public CombatDieView SpawnDie(CombatSymbol symbol, float speed)
        {
            ResolveReferences();
            if (playArea == null || diceRoot == null)
                return null;

            CombatDieView die = FindPooledDie(symbol);
            if (die == null)
            {
                CombatDieView prefab = GetDicePrefab(symbol);
                die = prefab != null ? Instantiate(prefab, diceRoot) : CreateFallbackDie(symbol);
            }

            die.ConfigurePresentationRoots(diceRoot, airborneDiceRoot);
            die.gameObject.SetActive(true);
            Vector2 direction = Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude < .1f) direction = Vector2.right;
            die.Setup(symbol, RandomPositionInside(playArea.rect, diceSize), direction * speed);
            return die;
        }

        public CombatDieView RerollDie(CombatDieView currentDie, CombatSymbol nextSymbol)
        {
            if (currentDie == null || !currentDie.CanInteract || playArea == null || catchCursor == null)
                return currentDie;

            if (catchCursorView != null)
                catchCursorView.PlayRerollFeedback();

            Vector2 diePosition = GetCenterInSpace(currentDie.RectTransform, playArea);
            Vector2 dieSizeInPlayArea = GetSizeInSpace(currentDie.RectTransform, playArea);
            Vector2 catcherPosition = GetCenterInSpace(catchCursor, playArea);
            Vector2 catcherSizeInPlayArea = GetSizeInSpace(catchCursor, playArea);
            float catcherRadius = Mathf.Min(catcherSizeInPlayArea.x, catcherSizeInPlayArea.y) * .5f;
            CombatRerollLaunchPlan launchPlan = CombatRerollPhysics.Calculate(
                playArea.rect,
                dieSizeInPlayArea,
                diePosition,
                catcherPosition,
                catcherRadius);

            if (currentDie.PrefabSymbol == nextSymbol)
            {
                currentDie.Reroll(nextSymbol, launchPlan, playArea);
                return currentDie;
            }

            Vector2 velocity = currentDie.MoveVelocity;
            currentDie.ReturnToPool();

            CombatDieView replacement = FindPooledDie(nextSymbol);
            if (replacement == null)
            {
                CombatDieView prefab = GetDicePrefab(nextSymbol);
                replacement = prefab != null ? Instantiate(prefab, diceRoot) : CreateFallbackDie(nextSymbol);
            }

            replacement.ConfigurePresentationRoots(diceRoot, airborneDiceRoot);
            replacement.gameObject.SetActive(true);
            replacement.SetupReroll(nextSymbol, launchPlan, playArea, velocity);
            return replacement;
        }

        public void SpawnEnemyBullet(Vector2 startPosition, Vector2 velocity)
        {
            SpawnEnemyBullet(enemyBulletPrefab, startPosition, velocity, 0, 0);
        }

        public CombatBulletView SpawnEnemyBullet(
            CombatBulletView sourcePrefab,
            Vector2 startPosition,
            Vector2 velocity,
            int sessionVersion,
            int phaseVersion)
        {
            ResolveReferences();
            if (bulletRoot == null)
                return null;

            CombatBulletView resolvedPrefab = sourcePrefab != null ? sourcePrefab : enemyBulletPrefab;
            CombatBulletView bullet = FindPooledBullet(resolvedPrefab);
            if (bullet == null)
                bullet = resolvedPrefab != null ? Instantiate(resolvedPrefab, bulletRoot) : CreateFallbackBullet();
            bullet.Setup(resolvedPrefab, startPosition, velocity, sessionVersion, phaseVersion);
            activeBullets.Add(bullet);
            return bullet;
        }

        public int TickBullets(float deltaTime, float playerInvulnerability)
        {
            if (playArea == null || playerView == null) return 0;

            int registeredHits = 0;
            Rect playRect = playArea.rect;
            for (int i = activeBullets.Count - 1; i >= 0; i--)
            {
                CombatBulletView bullet = activeBullets[i];
                if (bullet == null || !bullet.gameObject.activeInHierarchy)
                {
                    activeBullets.RemoveAt(i);
                    continue;
                }

                if (!bullet.TickMovement(playRect, deltaTime))
                {
                    bullet.ReturnToPool();
                    activeBullets.RemoveAt(i);
                    continue;
                }

                if (!bullet.CollisionActive || !RectTransformsOverlap(bullet.RectTransform, playerView.RectTransform)) continue;
                if (playerView.TryRegisterHit(playerInvulnerability)) registeredHits++;
                bullet.ReturnToPool();
                activeBullets.RemoveAt(i);
            }
            return registeredHits;
        }

        public int DestroyBulletsNearPlayer(float radius)
        {
            if (playArea == null || radius <= 0f)
                return 0;

            Vector2 center = PlayerPosition;
            int destroyedCount = 0;
            for (int i = activeBullets.Count - 1; i >= 0; i--)
            {
                CombatBulletView bullet = activeBullets[i];
                if (bullet == null || !bullet.gameObject.activeInHierarchy)
                {
                    activeBullets.RemoveAt(i);
                    continue;
                }

                RectTransform bulletRect = bullet.RectTransform;
                Vector3 bulletWorldCenter = bulletRect.TransformPoint(bulletRect.rect.center);
                Vector3 bulletLocalCenter = playArea.InverseTransformPoint(bulletWorldCenter);
                float bulletRadius = Mathf.Min(bulletRect.rect.width, bulletRect.rect.height) * .5f;
                float effectiveRadius = radius + bulletRadius;
                if (((Vector2)bulletLocalCenter - center).sqrMagnitude > effectiveRadius * effectiveRadius)
                    continue;

                bullet.ReturnToPool();
                activeBullets.RemoveAt(i);
                destroyedCount++;
            }

            return destroyedCount;
        }

        public void ResetPlayer()
        {
            ResolveReferences();
            if (catchCursor != null) catchCursor.anchoredPosition = Vector2.zero;
            if (playerView != null) playerView.ResetPlayer();
            SetCursorVisible(true);
        }

        public void TickHeartFeedback(float deltaTime)
        {
            if (playerView != null)
                playerView.TickVisual(deltaTime);
        }

        public void UpdateCursor(Vector2 screenPosition)
        {
            if (playArea == null || catchCursor == null) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    playArea, screenPosition, eventCamera, out Vector2 localPoint)) return;

            Vector2 half = catchCursor.rect.size * .5f;
            Rect bounds = playArea.rect;
            localPoint.x = Mathf.Clamp(localPoint.x, bounds.xMin + half.x, bounds.xMax - half.x);
            localPoint.y = Mathf.Clamp(localPoint.y, bounds.yMin + half.y, bounds.yMax - half.y);
            catchCursor.anchoredPosition = localPoint;
            SetCursorVisible(true);
            if (catchCursorView != null)
                catchCursorView.SetStunned(OverlapsActiveStunZone(catchCursor));
        }

        public bool CursorOverlaps(CombatDieView die)
        {
            return catchCursor != null && catchCursor.gameObject.activeInHierarchy && die != null &&
                CircleOverlapsRectTransform(catchCursor, die.RectTransform);
        }

        public void PlayBlockedCursorFeedback()
        {
            if (catchCursorView != null)
                catchCursorView.PlayBlockedFeedback();
        }

        private bool OverlapsActiveStunZone(RectTransform target)
        {
            if (stunZoneRoot == null || target == null || !stunZoneRoot.gameObject.activeInHierarchy)
                return false;
            for (int i = 0; i < stunZoneRoot.childCount; i++)
            {
                if (stunZoneRoot.GetChild(i) is RectTransform zone && zone.gameObject.activeInHierarchy &&
                    CircleOverlapsRectTransform(target, zone)) return true;
            }
            return false;
        }

        public void SetCursorVisible(bool visible)
        {
            if (catchCursor != null && catchCursor.gameObject.activeSelf != visible)
                catchCursor.gameObject.SetActive(visible);
            if (!visible && catchCursorView != null)
                catchCursorView.SetStunned(false, true);
        }

        public void UpdateTimer(float normalized)
        {
            if (timerFill == null) return;
            normalized = Mathf.Clamp01(normalized);

            bool increased = normalized > currentTimerNormalized + .0001f;
            currentTimerNormalized = normalized;
            SetTimerFillNormalized(timerFill, normalized);

            if (increased && timerDamageRoutine != null)
            {
                StopCoroutine(timerDamageRoutine);
                timerDamageRoutine = null;
            }

            if (timerDamageRoutine == null)
                SetTimerFillNormalized(timerDamageFill, normalized);
            else
                timerDamageTargetNormalized = normalized;

            float warning = 1f - Mathf.InverseLerp(.15f, .45f, normalized);
            timerFill.color = Color.Lerp(timerSafeColor, timerDangerColor, warning);
        }

        public void PlayPlayerDamageFeedback(float previousNormalized, float currentNormalized)
        {
            ResolveReferences();
            previousNormalized = Mathf.Clamp01(previousNormalized);
            currentNormalized = Mathf.Clamp01(currentNormalized);

            currentTimerNormalized = currentNormalized;
            SetTimerFillNormalized(timerFill, currentNormalized);

            if (timerDamageRoutine != null)
                StopCoroutine(timerDamageRoutine);

            float currentTrail = GetTimerFillNormalized(timerDamageFill);
            float trailStart = Mathf.Max(previousNormalized, currentTrail);
            timerDamageTargetNormalized = currentNormalized;
            if (timerDamageFill != null)
            {
                timerDamageFill.color = timerDamageColor;
                timerDamageFill.gameObject.SetActive(true);
                SetTimerFillNormalized(timerDamageFill, trailStart);
                timerDamageRoutine = isActiveAndEnabled
                    ? StartCoroutine(AnimateTimerDamageTrail())
                    : null;
                if (timerDamageRoutine == null)
                    SetTimerFillNormalized(timerDamageFill, currentNormalized);
            }

            TriggerDamageCameraShake();
        }

        private static void SetTimerFillNormalized(Image target, float normalized)
        {
            if (target == null) return;
            normalized = Mathf.Clamp01(normalized);

            // Timer Fill currently uses Unity's sprite-less Image. A sprite-less Image
            // ignores Image.fillAmount and always renders as a full quad, so resize its
            // RectTransform from the left edge instead. This also keeps working after
            // the placeholder art is replaced.
            RectTransform fillRect = target.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(normalized, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.pivot = new Vector2(0f, .5f);
            target.fillAmount = normalized;
        }

        private static float GetTimerFillNormalized(Image target)
        {
            return target != null ? Mathf.Clamp01(target.rectTransform.anchorMax.x) : 0f;
        }

        private IEnumerator AnimateTimerDamageTrail()
        {
            float elapsed = 0f;
            while (elapsed < timerDamageHoldDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            float start = GetTimerFillNormalized(timerDamageFill);
            elapsed = 0f;
            while (elapsed < timerDamageDrainDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / timerDamageDrainDuration);
                float eased = t * t * (3f - 2f * t);
                SetTimerFillNormalized(
                    timerDamageFill,
                    Mathf.LerpUnclamped(start, timerDamageTargetNormalized, eased));
                yield return null;
            }

            SetTimerFillNormalized(timerDamageFill, timerDamageTargetNormalized);
            timerDamageRoutine = null;
        }

        private void TriggerDamageCameraShake()
        {
            Camera targetCamera = eventCamera != null ? eventCamera : Camera.main;
            if (targetCamera == null || !isActiveAndEnabled)
                return;

            if (cameraShakeRoutine != null)
            {
                StopCoroutine(cameraShakeRoutine);
                RestoreCameraAfterShake();
            }

            shakeCameraTransform = targetCamera.transform;
            shakeCameraBasePosition = shakeCameraTransform.position;
            cameraShakeRoutine = StartCoroutine(AnimateDamageCameraShake());
        }

        private IEnumerator AnimateDamageCameraShake()
        {
            float elapsed = 0f;
            float seed = Time.unscaledTime * 17.13f;
            while (elapsed < damageShakeDuration && shakeCameraTransform != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / damageShakeDuration);
                float envelope = (1f - t) * (1f - t);
                float sample = elapsed * damageShakeFrequency;
                float x = Mathf.PerlinNoise(seed + sample, .17f) * 2f - 1f;
                float y = Mathf.PerlinNoise(.73f, seed + sample) * 2f - 1f;
                shakeCameraTransform.position = shakeCameraBasePosition +
                    new Vector3(x, y, 0f) * (damageShakeStrength * envelope);
                yield return null;
            }

            RestoreCameraAfterShake();
            cameraShakeRoutine = null;
        }

        private void ResetPlayerDamageFeedback()
        {
            if (timerDamageRoutine != null)
                StopCoroutine(timerDamageRoutine);
            timerDamageRoutine = null;
            timerDamageTargetNormalized = 1f;
            currentTimerNormalized = 1f;
            SetTimerFillNormalized(timerDamageFill, 1f);

            if (cameraShakeRoutine != null)
                StopCoroutine(cameraShakeRoutine);
            cameraShakeRoutine = null;
            RestoreCameraAfterShake();
        }

        private void RestoreCameraAfterShake()
        {
            if (shakeCameraTransform != null)
                shakeCameraTransform.position = shakeCameraBasePosition;
            shakeCameraTransform = null;
        }

        public void SetEnemyName(string enemyName)
        {
            if (enemyNameText != null)
                enemyNameText.text = enemyName;
        }

        public void PlayEnemyDamageFeedback(float previousNormalized, float currentNormalized)
        {
            ResolveReferences();
            previousNormalized = Mathf.Clamp01(previousNormalized);
            currentNormalized = Mathf.Clamp01(currentNormalized);

            SetEnemyHealthNormalized(currentNormalized);

            if (enemyHealthDamageRoutine != null)
                StopCoroutine(enemyHealthDamageRoutine);

            float currentTrail = GetEnemyHealthFillNormalized(enemyHealthDamageFill);
            float trailStart = Mathf.Max(previousNormalized, currentTrail);
            enemyHealthDamageTargetNormalized = currentNormalized;
            if (enemyHealthDamageFill != null)
            {
                enemyHealthDamageFill.color = Color.white;
                enemyHealthDamageFill.gameObject.SetActive(true);
                SetEnemyHealthFillNormalized(enemyHealthDamageFill, trailStart);
                enemyHealthDamageRoutine = isActiveAndEnabled
                    ? StartCoroutine(AnimateEnemyHealthDamageTrail())
                    : null;
                if (enemyHealthDamageRoutine == null)
                    SetEnemyHealthFillNormalized(enemyHealthDamageFill, currentNormalized);
            }
        }

        private void SetEnemyHealthNormalized(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            if (enemyHealthSlider != null)
                enemyHealthSlider.SetValueWithoutNotify(normalized);
            SetEnemyHealthFillNormalized(enemyHealthFill, normalized);
        }

        private static void SetEnemyHealthFillNormalized(Image target, float normalized)
        {
            if (target != null)
                target.fillAmount = Mathf.Clamp01(normalized);
        }

        private static float GetEnemyHealthFillNormalized(Image target)
        {
            return target != null ? Mathf.Clamp01(target.fillAmount) : 0f;
        }

        private IEnumerator AnimateEnemyHealthDamageTrail()
        {
            float elapsed = 0f;
            while (elapsed < enemyHealthDamageHoldDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            float start = GetEnemyHealthFillNormalized(enemyHealthDamageFill);
            elapsed = 0f;
            while (elapsed < enemyHealthDamageDrainDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / enemyHealthDamageDrainDuration);
                float eased = t * t * (3f - 2f * t);
                SetEnemyHealthFillNormalized(
                    enemyHealthDamageFill,
                    Mathf.LerpUnclamped(start, enemyHealthDamageTargetNormalized, eased));
                yield return null;
            }

            SetEnemyHealthFillNormalized(enemyHealthDamageFill, enemyHealthDamageTargetNormalized);
            enemyHealthDamageRoutine = null;
        }

        private void ResetEnemyHealth()
        {
            if (enemyHealthDamageRoutine != null)
                StopCoroutine(enemyHealthDamageRoutine);
            enemyHealthDamageRoutine = null;
            enemyHealthDamageTargetNormalized = 1f;

            if (enemyHealthFill != null)
                enemyHealthFill.color = enemyHealthColor;
            if (enemyHealthDamageFill != null)
                enemyHealthDamageFill.color = Color.white;

            SetEnemyHealthNormalized(1f);
            SetEnemyHealthFillNormalized(enemyHealthDamageFill, 1f);
        }

        public void PlayAttackHitVfx()
        {
            ResolveReferences();
            if (vfxRoot == null || enemyScratchVfxPrefab == null || !isActiveAndEnabled) return;
            StartCoroutine(PlayAttackHitVfxRoutine());
        }

        public void PlayEnemyDamageNumber(int damage)
        {
            ResolveReferences();
            if (damage <= 0 || damageNumberRoot == null || damageNumberFont == null || !isActiveAndEnabled)
                return;

            CombatDamageNumberView numberView = FindPooledDamageNumber();
            if (numberView == null) numberView = CreateDamageNumber();
            if (numberView == null) return;

            Vector3 worldAnchor = activeEnemyActor != null
                ? activeEnemyActor.DamageAnchor.position
                : vfxRoot != null ? vfxRoot.position
                : enemyVisual != null ? enemyVisual.position : damageNumberRoot.position;
            Vector3 localAnchor = damageNumberRoot.InverseTransformPoint(worldAnchor);
            Vector2 spread = new Vector2(
                Random.Range(-damageNumberSpawnSpread, damageNumberSpawnSpread),
                Random.Range(0f, damageNumberSpawnSpread * .55f));
            float horizontalDrift = Random.Range(-damageNumberSpawnSpread, damageNumberSpawnSpread);

            numberView.Play(
                damage,
                new Vector2(localAnchor.x, localAnchor.y) + spread,
                damageNumberColor,
                damageNumberFontSize,
                damageNumberDuration,
                damageNumberRiseDistance,
                horizontalDrift);
        }

        public IEnumerator PlayEnemyIntro()
        {
            if (enemyVisual == null) yield break;

            const float duration = .34f;
            Vector3 targetScale = enemyVisual.localScale;
            Vector3 startScale = targetScale * .78f;
            float elapsed = 0f;
            enemyVisual.localScale = startScale;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float overshoot = Mathf.Sin(t * Mathf.PI) * .08f;
                enemyVisual.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased) * (1f + overshoot);
                yield return null;
            }
            enemyVisual.localScale = targetScale;
        }

        public void TriggerEnemyHitFeedback()
        {
            if (!isActiveAndEnabled) return;
            if (enemyHitRoutine != null) StopCoroutine(enemyHitRoutine);
            SetEnemyWhiteFlash(false);
            if (enemyVisual != null) enemyVisual.localPosition = enemyAuthoredLocalPosition;
            enemyHitRoutine = StartCoroutine(PlayEnemyHit());
        }

        public IEnumerator PlayEnemyHit()
        {
            if (enemyVisual == null) yield break;

            Transform hitTarget = enemyVisual;
            int hitSessionVersion = activeEnemySessionVersion;
            PrepareEnemyFlash();
            float duration = GetAttackHitFeedbackDuration();
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (hitTarget == null || hitTarget != enemyVisual ||
                    hitSessionVersion != activeEnemySessionVersion)
                    break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float shakeT = Mathf.Clamp01(t / .62f);
                float shake = Mathf.Sin(shakeT * Mathf.PI * 6f) * (1f - shakeT) * 8f;
                hitTarget.localPosition = enemyAuthoredLocalPosition + Vector3.right * shake;
                SetEnemyWhiteFlash(IsEnemyHitFlashActive(t));
                yield return null;
            }
            SetEnemyWhiteFlash(false);
            if (hitTarget != null && hitTarget == enemyVisual &&
                hitSessionVersion == activeEnemySessionVersion)
                hitTarget.localPosition = enemyAuthoredLocalPosition;
            enemyHitRoutine = null;
        }

        public void PreviewEnemyWhiteFlash(float duration = .75f)
        {
            if (isActiveAndEnabled) StartCoroutine(PreviewEnemyWhiteFlashRoutine(duration));
        }

        public void ClearCombatRuntime()
        {
            ClearRuntimeDice();
            ClearRuntimeBullets();
            ClearEnemyActor();
            ClearActiveHitVfx();
            ClearDamageNumbers();
        }

        public void ClearRuntimeDice()
        {
            if (diceRoot == null) return;
            if (airborneDiceRoot != null)
            {
                for (int i = airborneDiceRoot.childCount - 1; i >= 0; i--)
                    ReturnDieToPool(airborneDiceRoot.GetChild(i));
            }
            for (int i = diceRoot.childCount - 1; i >= 0; i--)
                ReturnDieToPool(diceRoot.GetChild(i));
        }

        private static void ReturnDieToPool(Transform child)
        {
            if (child == null) return;
            CombatDieView die = child.GetComponent<CombatDieView>();
            if (die != null)
            {
                die.ReturnToPool();
                return;
            }

            if (child.gameObject.activeSelf)
                child.gameObject.SetActive(false);
        }

        public void ClearRuntimeBullets()
        {
            activeBullets.Clear();
            if (bulletRoot == null) return;
            for (int i = bulletRoot.childCount - 1; i >= 0; i--)
            {
                CombatBulletView bullet = bulletRoot.GetChild(i).GetComponent<CombatBulletView>();
                if (bullet != null) bullet.ReturnToPool();
                else bulletRoot.GetChild(i).gameObject.SetActive(false);
            }
        }

        public void ClearRuntimeBullets(int sessionVersion, int phaseVersion = -1)
        {
            for (int i = activeBullets.Count - 1; i >= 0; i--)
            {
                CombatBulletView bullet = activeBullets[i];
                if (bullet == null)
                {
                    activeBullets.RemoveAt(i);
                    continue;
                }
                if (bullet.OwnerSessionVersion != sessionVersion ||
                    (phaseVersion >= 0 && bullet.OwnerPhaseVersion != phaseVersion))
                    continue;
                bullet.ReturnToPool();
                activeBullets.RemoveAt(i);
            }
        }

        public void SetEnemyHealthVisible(bool visible)
        {
            if (enemyHealthSlider != null) enemyHealthSlider.gameObject.SetActive(visible);
            if (enemyHealthOutline != null) enemyHealthOutline.gameObject.SetActive(visible);
        }

        public void ClearEnemyActor()
        {
            if (enemyHitRoutine != null)
            {
                StopCoroutine(enemyHitRoutine);
                enemyHitRoutine = null;
            }
            SetEnemyWhiteFlash(false);
            if (enemyVisual != null)
                enemyVisual.localPosition = enemyAuthoredLocalPosition;
            if (activeEnemyActor != null)
            {
                activeEnemyActor.Shutdown();
                Destroy(activeEnemyActor.gameObject);
            }
            activeEnemyActor = null;
            activeEnemySessionVersion = 0;
            enemyVisual = null;
            vfxRoot = null;
            enemySpriteRenderers = null;
            enemyOriginalMaterials = null;
            enemyGraphics = null;
            enemyOriginalGraphicColors = null;
        }

        private CombatDieView FindPooledDie(CombatSymbol symbol)
        {
            if (diceRoot == null) return null;
            for (int i = 0; i < diceRoot.childCount; i++)
            {
                CombatDieView die = diceRoot.GetChild(i).GetComponent<CombatDieView>();
                if (die != null && die.PrefabSymbol == symbol && !die.gameObject.activeSelf) return die;
            }
            return null;
        }

        private CombatBulletView FindPooledBullet(CombatBulletView sourcePrefab)
        {
            if (bulletRoot == null) return null;
            for (int i = 0; i < bulletRoot.childCount; i++)
            {
                CombatBulletView bullet = bulletRoot.GetChild(i).GetComponent<CombatBulletView>();
                if (bullet != null && !bullet.gameObject.activeSelf && bullet.SourcePrefab == sourcePrefab)
                    return bullet;
            }
            return null;
        }

        private CombatDieView GetDicePrefab(CombatSymbol symbol)
        {
            return symbol switch
            {
                CombatSymbol.Attack => attackDicePrefab,
                CombatSymbol.Shield => shieldDicePrefab,
                _ => healDicePrefab,
            };
        }

        private CombatDieView CreateFallbackDie(CombatSymbol symbol)
        {
            GameObject dieObject = new GameObject("Runtime Die", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(CombatDieView));
            RectTransform dieRect = dieObject.GetComponent<RectTransform>();
            dieRect.SetParent(diceRoot, false);
            dieRect.sizeDelta = diceSize;

            GameObject labelObject = new GameObject("Symbol", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(dieRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 20f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(.08f, .07f, .11f, 1f);
            label.raycastTarget = false;

            Image image = dieObject.GetComponent<Image>();
            CombatDieView die = dieObject.GetComponent<CombatDieView>();
            die.ConfigureVisuals(symbol, image, label);
            return die;
        }

        private CombatBulletView CreateFallbackBullet()
        {
            GameObject bulletObject = new GameObject("Enemy Bullet", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CombatBulletView));
            RectTransform rect = bulletObject.GetComponent<RectTransform>();
            rect.SetParent(bulletRoot, false);
            rect.sizeDelta = new Vector2(16f, 16f);
            rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Image image = bulletObject.GetComponent<Image>();
            image.color = new Color(.86f, .36f, .48f, 1f);
            image.raycastTarget = false;
            return bulletObject.GetComponent<CombatBulletView>();
        }

        private CombatDamageNumberView FindPooledDamageNumber()
        {
            for (int i = 0; i < damageNumberPool.Count; i++)
            {
                CombatDamageNumberView numberView = damageNumberPool[i];
                if (numberView != null && !numberView.gameObject.activeSelf)
                    return numberView;
            }
            return null;
        }

        private CombatDamageNumberView CreateDamageNumber()
        {
            if (damageNumberRoot == null || damageNumberFont == null) return null;

            GameObject numberObject = new GameObject(
                "Enemy Damage Number",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI),
                typeof(CanvasGroup),
                typeof(CombatDamageNumberView));
            RectTransform rect = numberObject.GetComponent<RectTransform>();
            rect.SetParent(damageNumberRoot, false);
            rect.anchorMin = new Vector2(.5f, .5f);
            rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(220f, 96f);

            TextMeshProUGUI label = numberObject.GetComponent<TextMeshProUGUI>();
            label.font = damageNumberFont;
            label.fontSize = damageNumberFontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = damageNumberColor;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.outlineColor = new Color32(39, 25, 45, 255);
            label.outlineWidth = .18f;

            CanvasGroup group = numberObject.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            CombatDamageNumberView numberView = numberObject.GetComponent<CombatDamageNumberView>();
            numberView.Configure(label, group);
            damageNumberPool.Add(numberView);
            numberObject.SetActive(false);
            return numberView;
        }

        private void ClearDamageNumbers()
        {
            for (int i = 0; i < damageNumberPool.Count; i++)
            {
                if (damageNumberPool[i] != null)
                    damageNumberPool[i].StopImmediately();
            }
        }

        private IEnumerator PlayAttackHitVfxRoutine()
        {
            GameObject instance = Instantiate(enemyScratchVfxPrefab, vfxRoot);
            instance.name = "Scratch Hit VFX";
            Transform instanceTransform = instance.transform;
            instanceTransform.localPosition = Vector3.zero;
            instanceTransform.localRotation = Quaternion.identity;
            instanceTransform.localScale = Vector3.one;
            activeHitVfx.Add(instance);

            CenterScratchOnVfxAnchor(instance);

            Animator animator = instance.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                animator.Rebind();
                animator.Update(0f);
            }

            float duration = GetAttackHitFeedbackDuration(animator);
            yield return new WaitForSecondsRealtime(duration);
            activeHitVfx.Remove(instance);
            if (instance != null) Destroy(instance);
        }

        private float GetAttackHitFeedbackDuration(Animator runtimeAnimator = null)
        {
            const float fallbackDuration = .6f;
            Animator animator = runtimeAnimator;
            if (animator == null && enemyScratchVfxPrefab != null)
                animator = enemyScratchVfxPrefab.GetComponentInChildren<Animator>(true);

            RuntimeAnimatorController controller = animator != null ? animator.runtimeAnimatorController : null;
            if (controller == null) return fallbackDuration;

            AnimationClip[] clips = controller.animationClips;
            float longestClip = 0f;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                    longestClip = Mathf.Max(longestClip, clips[i].length);
            }
            return longestClip > 0f ? Mathf.Max(.05f, longestClip) : fallbackDuration;
        }

        private static bool IsEnemyHitFlashActive(float normalizedTime)
        {
            return normalizedTime <= .13f ||
                (normalizedTime >= .28f && normalizedTime <= .40f) ||
                (normalizedTime >= .55f && normalizedTime <= .68f) ||
                normalizedTime >= .86f;
        }

        private void CenterScratchOnVfxAnchor(GameObject instance)
        {
            SpriteRenderer[] scratchRenderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
            if (scratchRenderers.Length == 0) return;

            Bounds scratchBounds = scratchRenderers[0].bounds;
            for (int i = 1; i < scratchRenderers.Length; i++)
                scratchBounds.Encapsulate(scratchRenderers[i].bounds);

            Vector3 anchorPosition = vfxRoot.position;
            Vector3 offset = anchorPosition - scratchBounds.center;
            offset.z = 0f;
            instance.transform.position += offset;

            int sortingLayerId = scratchRenderers[0].sortingLayerID;
            int sortingOrder = scratchRenderers[0].sortingOrder;
            if (enemyVisual != null)
            {
                SpriteRenderer[] renderers = enemyVisual.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null || renderers[i].sortingOrder < sortingOrder) continue;
                    sortingLayerId = renderers[i].sortingLayerID;
                    sortingOrder = renderers[i].sortingOrder + 1;
                }
            }

            for (int i = 0; i < scratchRenderers.Length; i++)
            {
                scratchRenderers[i].sortingLayerID = sortingLayerId;
                scratchRenderers[i].sortingOrder = sortingOrder + i;
            }
        }

        private void ClearActiveHitVfx()
        {
            for (int i = activeHitVfx.Count - 1; i >= 0; i--)
            {
                if (activeHitVfx[i] != null) Destroy(activeHitVfx[i]);
            }
            activeHitVfx.Clear();
        }

        private void PrepareEnemyFlash()
        {
            if (enemyVisual == null) return;
            SpriteRenderer[] allRenderers = enemyVisual.GetComponentsInChildren<SpriteRenderer>(true);
            List<SpriteRenderer> authoredRenderers = new List<SpriteRenderer>(allRenderers.Length);
            for (int i = 0; i < allRenderers.Length; i++)
            {
                SpriteRenderer renderer = allRenderers[i];
                if (renderer == null || (vfxRoot != null && renderer.transform.IsChildOf(vfxRoot))) continue;
                authoredRenderers.Add(renderer);
            }
            SpriteRenderer[] renderers = authoredRenderers.ToArray();
            if (enemySpriteRenderers == null || enemySpriteRenderers.Length != renderers.Length)
            {
                enemySpriteRenderers = renderers;
                enemyOriginalMaterials = new Material[renderers.Length];
                for (int i = 0; i < renderers.Length; i++) enemyOriginalMaterials[i] = renderers[i].sharedMaterial;
            }

            if (enemyWhiteFlashMaterial == null)
            {
                Shader shader = Shader.Find("Audere/Sprite White Flash");
                if (shader != null)
                    enemyWhiteFlashMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
        }

        private void SetEnemyWhiteFlash(bool enabled)
        {
            if (enemySpriteRenderers != null && enemyOriginalMaterials != null)
            {
                for (int i = 0; i < enemySpriteRenderers.Length; i++)
                {
                    if (enemySpriteRenderers[i] != null)
                        enemySpriteRenderers[i].sharedMaterial = enabled && enemyWhiteFlashMaterial != null
                            ? enemyWhiteFlashMaterial : enemyOriginalMaterials[i];
                }
            }

            if (enemyGraphics == null || enemyOriginalGraphicColors == null) return;
            for (int i = 0; i < enemyGraphics.Length && i < enemyOriginalGraphicColors.Length; i++)
            {
                if (enemyGraphics[i] != null)
                    enemyGraphics[i].color = enabled
                        ? Color.Lerp(enemyOriginalGraphicColors[i], new Color(1f, .55f, .62f, 1f), .62f)
                        : enemyOriginalGraphicColors[i];
            }
        }

        private void CaptureEnemyGraphicColors()
        {
            if (enemyGraphics == null)
                return;
            enemyOriginalGraphicColors = new Color[enemyGraphics.Length];
            for (int i = 0; i < enemyGraphics.Length; i++)
                enemyOriginalGraphicColors[i] = enemyGraphics[i] != null ? enemyGraphics[i].color : Color.white;
        }

        private IEnumerator PreviewEnemyWhiteFlashRoutine(float duration)
        {
            PrepareEnemyFlash();
            SetEnemyWhiteFlash(true);
            yield return new WaitForSecondsRealtime(Mathf.Max(.05f, duration));
            SetEnemyWhiteFlash(false);
        }

        private void OnDisable()
        {
            if (enemyHitRoutine != null) StopCoroutine(enemyHitRoutine);
            enemyHitRoutine = null;
            if (timerDamageRoutine != null) StopCoroutine(timerDamageRoutine);
            timerDamageRoutine = null;
            if (enemyHealthDamageRoutine != null) StopCoroutine(enemyHealthDamageRoutine);
            enemyHealthDamageRoutine = null;
            if (cameraShakeRoutine != null) StopCoroutine(cameraShakeRoutine);
            cameraShakeRoutine = null;
            RestoreCameraAfterShake();
            ClearActiveHitVfx();
            ClearDamageNumbers();
            SetEnemyWhiteFlash(false);
            if (enemyVisual != null) enemyVisual.localPosition = enemyAuthoredLocalPosition;
        }

        private void OnDestroy()
        {
            if (enemyWhiteFlashMaterial != null) Destroy(enemyWhiteFlashMaterial);
        }

        private void ResolveReferences()
        {
            playArea = ResolveRect(playArea, "Dice Field", "Play Area");
            stunZoneRoot = ResolveRect(stunZoneRoot, "Stun Zone Root", "Hazard Zone Root");
            bulletRoot = ResolveRect(bulletRoot, "Bullet Root");
            diceRoot = ResolveRect(diceRoot, "Dice Root");
            airborneDiceRoot = ResolveRect(airborneDiceRoot, "Airborne Dice Overlay");
            playerRoot = ResolveRect(playerRoot, "Audere Heart Root", "Player Root");
            catchCursorRoot = ResolveRect(catchCursorRoot, "Catch Cursor Root", "Catch Zone Root");
            feedbackRoot = ResolveRect(feedbackRoot, "Feedback FX Root", "Feedback Root");
            catchCursor = ResolveRect(catchCursor, "Catch Cursor");
            if (catchCursorView == null && catchCursor != null)
                catchCursorView = catchCursor.GetComponent<CombatCatchCursorView>();
            if (playerView == null && playerRoot != null) playerView = playerRoot.GetComponent<CombatPlayerView>();
            if (timerFill == null)
            {
                Transform child = FindDescendant(transform, "Timer Fill");
                if (child != null) timerFill = child.GetComponent<Image>();
            }
            if (timerDamageFill == null)
            {
                Transform child = FindDescendant(transform, "Timer Damage Fill");
                if (child != null) timerDamageFill = child.GetComponent<Image>();
            }
            if (enemyHealthSlider == null)
            {
                Transform child = FindDescendant(transform, "Enemy Health Slider");
                if (child != null) enemyHealthSlider = child.GetComponent<Slider>();
            }
            if (enemyHealthFill == null)
            {
                Transform child = FindDescendant(transform, "Enemy Health Fill");
                if (child != null) enemyHealthFill = child.GetComponent<Image>();
            }
            if (enemyHealthDamageFill == null)
            {
                Transform child = FindDescendant(transform, "Enemy Health Damage Fill");
                if (child != null) enemyHealthDamageFill = child.GetComponent<Image>();
            }
            if (enemyHealthOutline == null)
            {
                Transform child = FindDescendant(transform, "HealthBarOutline");
                if (child != null) enemyHealthOutline = child.GetComponent<Image>();
            }
            if (enemyNameText == null)
                enemyNameText = ResolveText("Enemy Name");
            if (enemyMount == null) enemyMount = FindDescendant(transform, "Enemy Mount");
            if (damageNumberRoot == null)
                damageNumberRoot = FindDescendant(transform, "Damage Number Root") as RectTransform;
        }

        private RectTransform ResolveRect(RectTransform current, params string[] names)
        {
            if (current != null) return current;
            for (int i = 0; i < names.Length; i++)
            {
                Transform found = FindDescendant(transform, names[i]);
                if (found is RectTransform rect) return rect;
            }
            return null;
        }

        private TMP_Text ResolveText(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Transform child = FindDescendant(transform, names[i]);
                if (child != null) return child.GetComponent<TMP_Text>();
            }
            return null;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDescendant(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static Vector2 GetCenterInSpace(RectTransform target, RectTransform space)
        {
            Vector3 worldCenter = target.TransformPoint(target.rect.center);
            Vector3 localCenter = space.InverseTransformPoint(worldCenter);
            return new Vector2(localCenter.x, localCenter.y);
        }

        private static Vector2 GetSizeInSpace(RectTransform target, RectTransform space)
        {
            Vector3[] worldCorners = new Vector3[4];
            target.GetWorldCorners(worldCorners);
            Vector3 minimum = space.InverseTransformPoint(worldCorners[0]);
            Vector3 maximum = minimum;
            for (int i = 1; i < worldCorners.Length; i++)
            {
                Vector3 local = space.InverseTransformPoint(worldCorners[i]);
                minimum = Vector3.Min(minimum, local);
                maximum = Vector3.Max(maximum, local);
            }

            return new Vector2(maximum.x - minimum.x, maximum.y - minimum.y);
        }

        private static Vector2 RandomPositionInside(Rect rect, Vector2 size)
        {
            Vector2 half = size * .5f;
            return new Vector2(Random.Range(rect.xMin + half.x, rect.xMax - half.x), Random.Range(rect.yMin + half.y, rect.yMax - half.y));
        }

        private static bool RectTransformsOverlap(RectTransform a, RectTransform b)
        {
            if (a == null || b == null) return false;
            Vector3[] aCorners = new Vector3[4];
            Vector3[] bCorners = new Vector3[4];
            a.GetWorldCorners(aCorners);
            b.GetWorldCorners(bCorners);
            Rect aRect = Rect.MinMaxRect(aCorners[0].x, aCorners[0].y, aCorners[2].x, aCorners[2].y);
            Rect bRect = Rect.MinMaxRect(bCorners[0].x, bCorners[0].y, bCorners[2].x, bCorners[2].y);
            return aRect.Overlaps(bRect, true);
        }

        private static bool CircleOverlapsRectTransform(RectTransform circle, RectTransform target)
        {
            if (circle == null || target == null) return false;

            Vector3[] worldCorners = new Vector3[4];
            target.GetWorldCorners(worldCorners);
            Vector2[] localCorners = new Vector2[4];
            for (int i = 0; i < localCorners.Length; i++)
                localCorners[i] = circle.InverseTransformPoint(worldCorners[i]);

            Vector2 center = circle.rect.center;
            float radius = Mathf.Min(circle.rect.width, circle.rect.height) * .5f;
            float radiusSquared = radius * radius;

            if (PointInsideQuad(center, localCorners))
                return true;

            for (int i = 0; i < localCorners.Length; i++)
            {
                Vector2 corner = localCorners[i];
                if ((corner - center).sqrMagnitude <= radiusSquared)
                    return true;

                Vector2 nextCorner = localCorners[(i + 1) % localCorners.Length];
                if (DistanceToSegmentSquared(center, corner, nextCorner) <= radiusSquared)
                    return true;
            }

            return false;
        }

        private static bool PointInsideQuad(Vector2 point, Vector2[] corners)
        {
            bool hasNegative = false;
            bool hasPositive = false;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 edge = corners[(i + 1) % corners.Length] - corners[i];
                Vector2 toPoint = point - corners[i];
                float cross = edge.x * toPoint.y - edge.y * toPoint.x;
                hasNegative |= cross < 0f;
                hasPositive |= cross > 0f;
                if (hasNegative && hasPositive)
                    return false;
            }
            return true;
        }

        private static float DistanceToSegmentSquared(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
                return (point - start).sqrMagnitude;

            float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            Vector2 closest = start + segment * t;
            return (point - closest).sqrMagnitude;
        }
    }
}
