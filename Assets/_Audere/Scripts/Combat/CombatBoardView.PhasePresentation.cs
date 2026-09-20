using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    public sealed partial class CombatBoardView
    {
        [Header("Phase presentation")]
        [SerializeField] private RectTransform enemyPresentationRoot;
        [SerializeField] private RectTransform enemyLabelRoot;
        private bool phaseLayoutCaptured;
        private Vector2 originalLabelPosition;
        private Vector2 originalFieldSize, originalFieldPosition, originalAirSize, originalAirPosition;
        private Vector2 originalFrameSize, originalFramePosition, originalEnemyPosition, originalNumberPosition;
        private float recoveryElapsed = .4f, recoveryWidth = 1f, recoveryX;
        private Vector2 recoveryCursorPosition;
        private const float MoveRecoveryDuration = .4f;
        private readonly List<ExitImage> exitImages = new List<ExitImage>();
        private sealed class ExitImage { public Image Image; public Color Color; public bool Active; }
        public bool IsRecoveringMove => recoveryElapsed < MoveRecoveryDuration;

        // Snapshot only the presentation. Originals are cancelled/pool-returned immediately,
        // so no retired projectile can collide or be reused underneath its fading image.
        public void CaptureMoveExit()
        {
            FinishMoveRecovery();
            recoveryWidth = battleBoxWidthFraction;
            recoveryX = battleBoxNormalizedX;
            recoveryCursorPosition = catchCursor != null ? catchCursor.anchoredPosition : Vector2.zero;
            foreach (var bullet in activeBullets)
                if (bullet != null && bullet.gameObject.activeInHierarchy) CaptureExitImages(bullet.transform);
            foreach (var laser in activeLasers)
                if (laser != null && laser.gameObject.activeInHierarchy) CaptureExitImages(laser.transform);
            foreach (var echo in ribbonEchoes)
                if (echo.Active && echo.Image != null) CaptureExitImages(echo.Image.transform);
            ClearRibbonEchoes();
            recoveryElapsed = 0f;
        }

        public void CaptureDiceExit()
        {
            if (diceRoot != null) CaptureExitImages(diceRoot);
            if (airborneDiceRoot != null) CaptureExitImages(airborneDiceRoot);
        }

        private void CaptureExitImages(Transform source)
        {
            foreach (var sourceImage in source.GetComponentsInChildren<Image>())
            {
                if (!sourceImage.enabled || sourceImage.color.a <= .001f) continue;
                ExitImage entry = null;
                foreach (var candidate in exitImages)
                    if (!candidate.Active && candidate.Image != null) { entry = candidate; break; }
                if (entry == null)
                {
                    if (exitImages.Count >= 256) return;
                    var go = new GameObject("Move exit (pooled)", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    go.transform.SetParent(transform, false);
                    entry = new ExitImage { Image = go.GetComponent<Image>() };
                    entry.Image.raycastTarget = false;
                    exitImages.Add(entry);
                }
                var target = entry.Image;
                target.sprite = sourceImage.sprite;
                target.material = sourceImage.material;
                target.type = sourceImage.type;
                target.preserveAspect = sourceImage.preserveAspect;
                target.fillAmount = sourceImage.fillAmount;
                target.rectTransform.pivot = sourceImage.rectTransform.pivot;
                target.rectTransform.sizeDelta = sourceImage.rectTransform.rect.size;
                target.rectTransform.SetPositionAndRotation(sourceImage.transform.position, sourceImage.transform.rotation);
                Vector3 scale = sourceImage.transform.lossyScale;
                Vector3 parentScale = transform.lossyScale;
                target.rectTransform.localScale = new Vector3(scale.x / parentScale.x, scale.y / parentScale.y, 1f);
                entry.Color = sourceImage.color;
                foreach (var group in sourceImage.GetComponentsInParent<CanvasGroup>())
                    entry.Color.a *= group.alpha;
                target.color = entry.Color;
                entry.Active = true;
                target.gameObject.SetActive(true);
            }
        }

        public void RestoreMoveExitPose()
        {
            SetBattleBoxHorizontalLayout(recoveryWidth, recoveryX);
            if (catchCursor != null) catchCursor.anchoredPosition = recoveryCursorPosition;
        }

        public void TickMoveRecovery(float deltaTime)
        {
            if (!IsRecoveringMove) return;
            recoveryElapsed = Mathf.Min(MoveRecoveryDuration, recoveryElapsed + Mathf.Max(0f, deltaTime));
            float t = Mathf.SmoothStep(0f, 1f, recoveryElapsed / MoveRecoveryDuration);
            foreach (var entry in exitImages)
                if (entry.Active && entry.Image != null)
                    entry.Image.color = new Color(entry.Color.r, entry.Color.g, entry.Color.b, entry.Color.a * (1f - t));
            SetBattleBoxHorizontalLayout(Mathf.Lerp(recoveryWidth, 1f, t), Mathf.Lerp(recoveryX, 0f, t));
            if (!IsRecoveringMove) FinishMoveRecovery();
        }

        private void FinishMoveRecovery()
        {
            recoveryElapsed = MoveRecoveryDuration;
            foreach (var entry in exitImages)
            {
                entry.Active = false;
                if (entry.Image != null) entry.Image.gameObject.SetActive(false);
            }
        }

        private void CapturePhaseLayout()
        {
            if (phaseLayoutCaptured || playArea == null) return;
            CaptureBattleBoxLayout();
            originalFieldSize = battleBoxAuthoredSize; originalFieldPosition = battleBoxAuthoredPosition;
            originalAirSize = airborneDiceAuthoredSize; originalAirPosition = airborneDiceAuthoredPosition;
            if (battleBoxFrame != null) { originalFrameSize = battleBoxFrame.sizeDelta; originalFramePosition = battleBoxFrame.anchoredPosition; }
            if (enemyPresentationRoot != null) originalEnemyPosition = enemyPresentationRoot.anchoredPosition;
            if (damageNumberRoot != null) originalNumberPosition = damageNumberRoot.anchoredPosition;
            if (enemyLabelRoot != null) originalLabelPosition = enemyLabelRoot.anchoredPosition;
            phaseLayoutCaptured = true;
        }

        public IEnumerator TransitionPhasePresentation(CombatPhasePresentationProfile profile)
        {
            CapturePhaseLayout();
            if (!phaseLayoutCaptured) yield break;
            Vector2 fieldFrom = battleBoxAuthoredPosition, sizeFrom = battleBoxAuthoredSize;
            Vector2 frameFrom = battleBoxFrame != null ? battleBoxFrame.anchoredPosition : Vector2.zero;
            Vector2 frameSizeFrom = battleBoxFrame != null ? battleBoxFrame.sizeDelta : Vector2.zero;
            Vector2 enemyFrom = enemyPresentationRoot != null ? enemyPresentationRoot.anchoredPosition : Vector2.zero;
            Vector2 labelFrom = enemyLabelRoot != null ? enemyLabelRoot.anchoredPosition : Vector2.zero;
            Vector2 labelOffset = profile != null ? profile.EnemyLabelOffset : Vector2.zero;
            Vector2 numberFrom = damageNumberRoot != null ? damageNumberRoot.anchoredPosition : Vector2.zero;
            Vector2 offset = profile != null ? profile.PlayerOffset : Vector2.zero;
            Vector2 enemyOffset = profile != null ? profile.EnemyOffset : Vector2.zero;
            float width = profile != null ? profile.BoardWidth : 1f;
            Vector2 sizeTo = new Vector2(originalFieldSize.x * width, originalFieldSize.y);
            Vector2 frameSizeTo = new Vector2(originalFrameSize.x - originalFieldSize.x + sizeTo.x, originalFrameSize.y);
            Vector2 heart = catchCursor != null ? catchCursor.anchoredPosition : Vector2.zero;
            if (Vector2.Distance(fieldFrom, originalFieldPosition + offset) < .01f &&
                Vector2.Distance(sizeFrom, sizeTo) < .01f &&
                (enemyPresentationRoot == null || Vector2.Distance(enemyFrom, originalEnemyPosition + enemyOffset) < .01f)) yield break;
            float duration = profile != null ? profile.Duration : 1.1f;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                Apply(Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }
            Apply(1f);

            void Apply(float t)
            {
                battleBoxAuthoredSize = Vector2.Lerp(sizeFrom, sizeTo, t);
                battleBoxAuthoredPosition = Vector2.Lerp(fieldFrom, originalFieldPosition + offset, t);
                airborneDiceAuthoredSize = originalAirSize + battleBoxAuthoredSize - originalFieldSize;
                airborneDiceAuthoredPosition = originalAirPosition + battleBoxAuthoredPosition - originalFieldPosition;
                if (battleBoxFrame != null) { battleBoxFrame.sizeDelta = Vector2.Lerp(frameSizeFrom, frameSizeTo, t); battleBoxFrame.anchoredPosition = Vector2.Lerp(frameFrom, originalFramePosition + offset, t); }
                if (enemyPresentationRoot != null) enemyPresentationRoot.anchoredPosition = Vector2.Lerp(enemyFrom, originalEnemyPosition + enemyOffset, t);
                if (damageNumberRoot != null) damageNumberRoot.anchoredPosition = Vector2.Lerp(numberFrom, originalNumberPosition + enemyOffset, t);
                if (enemyLabelRoot != null) enemyLabelRoot.anchoredPosition = Vector2.Lerp(labelFrom, originalLabelPosition + labelOffset, t);
                ResetBattleBoxLayout();
                if (catchCursor != null) catchCursor.anchoredPosition = ClampCursorToBattleBox(new Vector2(heart.x * battleBoxAuthoredSize.x / Mathf.Max(1f, sizeFrom.x), heart.y));
                SyncExteriorProjectileRoot();
            }
        }

        private void ResetPhasePresentation()
        {
            FinishMoveRecovery();
            ClearRibbonEchoes();
            if (!phaseLayoutCaptured) return;
            battleBoxAuthoredSize = originalFieldSize; battleBoxAuthoredPosition = originalFieldPosition;
            airborneDiceAuthoredSize = originalAirSize; airborneDiceAuthoredPosition = originalAirPosition;
            if (battleBoxFrame != null) { battleBoxFrame.sizeDelta = originalFrameSize; battleBoxFrame.anchoredPosition = originalFramePosition; }
            if (enemyPresentationRoot != null) enemyPresentationRoot.anchoredPosition = originalEnemyPosition;
            if (damageNumberRoot != null) damageNumberRoot.anchoredPosition = originalNumberPosition;
            if (enemyLabelRoot != null) enemyLabelRoot.anchoredPosition = originalLabelPosition;
            ResetBattleBoxLayout();
        }
    }
}
