using System;
using System.Collections;
using Audere.Audio;
using Audere.Dialogue;
using Audere.Puzzle;
using UnityEngine;

namespace Audere.World
{
    [DisallowMultipleComponent]
    public sealed class WorldModeController : MonoBehaviour
    {
        [Header("Mode Roots")]
        [SerializeField] private WorldGameplayMode startingMode = WorldGameplayMode.Combat;
        [SerializeField] private GameObject puzzleRoot;
        [SerializeField] private GameObject combatRoot;
        [SerializeField] private GameObject storyRoot;
        [Tooltip("Scene roots visible in both Story and Puzzle, hidden only during Combat.")]
        [SerializeField] private GameObject[] storyAndPuzzleRoots;
        [SerializeField] private GameObject puzzleViewportMask;
        [SerializeField] private bool storyUsesPuzzleViewportMask = true;
        [SerializeField] private GameObject combatSystemsRoot;

        [Header("Transition")]
        [SerializeField] private CanvasGroup transitionFade;
        [Tooltip("Disable when authored Story steps own the fade; do not bind unrelated child UI.")]
        [SerializeField] private bool allowChildFadeFallback = true;
        [SerializeField] private bool revealStartingModeOnStart = true;
        [SerializeField, Min(.01f)] private float fadeOutDuration = .18f;
        [SerializeField, Min(0f)] private float coveredHoldDuration = .05f;
        [SerializeField, Min(.01f)] private float fadeInDuration = .28f;

        [Header("Camera")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private GridCameraFollow2D puzzleCameraFollow;
        [SerializeField] private Vector3 combatCameraPosition = new Vector3(0f, 0f, -10f);
        [SerializeField, Min(.01f)] private float combatOrthographicSize = 1.25f;
        [SerializeField] private Vector3 storyCameraPosition = new Vector3(0f, 0f, -10f);
        [SerializeField, Min(.01f)] private float storyOrthographicSize = 1.25f;

        [Header("Development")]
        [Tooltip("F1 = Puzzle, F2 = Combat, F3 = Story. Disable before a release build if those keys are needed elsewhere.")]
        [SerializeField] private bool enableDebugHotkeys = true;

        private Coroutine transitionRoutine;
        private RectTransform puzzleUi;

        public WorldGameplayMode CurrentMode { get; private set; }
        public bool IsTransitioning { get; private set; }
        public event Action<WorldGameplayMode> ModeChanged;

        private void Awake()
        {
            ResolveReferences();
            ApplyModeImmediate(startingMode);

            if (transitionFade != null)
            {
                transitionFade.gameObject.SetActive(true);
                transitionFade.alpha = revealStartingModeOnStart ? 1f : 0f;
                transitionFade.blocksRaycasts = revealStartingModeOnStart;
                transitionFade.interactable = false;
            }
        }

        private void Start()
        {
            SyncMusicPresentation();
            if (transitionFade != null && revealStartingModeOnStart)
                transitionRoutine = StartCoroutine(RevealStartingMode());
        }

        private void LateUpdate() => SyncMusicPresentation();

        private void SyncMusicPresentation()
        {
            AudioService audio = AudioService.Instance;
            if (audio == null) return;
            audio.TrackScreenFade(transitionFade);
            var combatController = combatSystemsRoot != null ? combatSystemsRoot.GetComponent<Audere.Combat.CombatController>() : null;
            AudioId? track = combatController != null && combatController.CurrentEncounter != null
                ? combatController.CurrentEncounter.Music : (AudioId?)null;
            audio.SetCombatMusicOwner(this, CurrentMode == WorldGameplayMode.Combat, track);
        }

        private void OnDisable()
        {
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = null;
            IsTransitioning = false;
            AudioService.Instance?.ReleaseMusicOwner(this);
        }

        private void Update()
        {
            if (!enableDebugHotkeys || IsTransitioning)
                return;

            if (Input.GetKeyDown(KeyCode.F1) && puzzleRoot != null)
                ShowPuzzle();
            else if (Input.GetKeyDown(KeyCode.F2) && combatRoot != null)
                ShowCombat();
            else if (Input.GetKeyDown(KeyCode.F3) && storyRoot != null)
                ShowStory();
        }

        public void ShowPuzzle() => SwitchTo(WorldGameplayMode.Puzzle);
        public void ShowCombat() => SwitchTo(WorldGameplayMode.Combat);
        public void ShowStory() => SwitchTo(WorldGameplayMode.Story);

        public void SwitchTo(WorldGameplayMode nextMode)
        {
            if (nextMode == CurrentMode && !IsTransitioning)
                return;

            if (transitionRoutine != null)
                StopCoroutine(transitionRoutine);

            transitionRoutine = StartCoroutine(TransitionTo(nextMode));
        }

        public void ApplyModeImmediate(WorldGameplayMode mode)
        {
            ResolveGameplayUi();
            bool puzzleActive = mode == WorldGameplayMode.Puzzle;
            bool combatActive = mode == WorldGameplayMode.Combat;
            bool storyActive = mode == WorldGameplayMode.Story;

            SetActiveIfNeeded(puzzleRoot, puzzleActive);
            SetActiveIfNeeded(combatRoot, combatActive);
            SetActiveIfNeeded(storyRoot, storyActive);
            if (storyAndPuzzleRoots != null)
                foreach (GameObject root in storyAndPuzzleRoots) SetActiveIfNeeded(root, !combatActive);
            SetActiveIfNeeded(
                puzzleViewportMask,
                puzzleActive || (storyActive && storyUsesPuzzleViewportMask));
            SetActiveIfNeeded(puzzleUi != null ? puzzleUi.gameObject : null, puzzleActive);
            SetActiveIfNeeded(combatSystemsRoot, combatActive);

            if (puzzleCameraFollow != null)
                puzzleCameraFollow.enabled = puzzleActive;

            if (combatActive && worldCamera != null)
            {
                worldCamera.transform.position = combatCameraPosition;
                if (worldCamera.orthographic)
                    worldCamera.orthographicSize = combatOrthographicSize;
            }
            else if (storyActive && worldCamera != null)
            {
                worldCamera.transform.position = storyCameraPosition;
                if (worldCamera.orthographic)
                    worldCamera.orthographicSize = storyOrthographicSize;
            }

            CurrentMode = mode;
            SyncMusicPresentation();
            ModeChanged?.Invoke(mode);
        }

        private IEnumerator RevealStartingMode()
        {
            IsTransitioning = true;
            yield return Fade(1f, 0f, fadeInDuration);
            CompleteTransition();
        }

        private IEnumerator TransitionTo(WorldGameplayMode nextMode)
        {
            IsTransitioning = true;

            if (transitionFade == null)
            {
                ApplyModeImmediate(nextMode);
                CompleteTransition();
                yield break;
            }

            transitionFade.gameObject.SetActive(true);
            transitionFade.blocksRaycasts = true;
            yield return Fade(transitionFade.alpha, 1f, fadeOutDuration);

            ApplyModeImmediate(nextMode);
            if (coveredHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(coveredHoldDuration);

            yield return Fade(1f, 0f, fadeInDuration);
            CompleteTransition();
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            duration = Mathf.Max(.01f, duration);
            transitionFade.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                transitionFade.alpha = Mathf.LerpUnclamped(from, to, t);
                yield return null;
            }

            transitionFade.alpha = to;
        }

        private void CompleteTransition()
        {
            if (transitionFade != null)
            {
                transitionFade.alpha = 0f;
                transitionFade.blocksRaycasts = false;
            }

            IsTransitioning = false;
            transitionRoutine = null;
        }

        private void ResolveReferences()
        {
            if (puzzleRoot == null)
            {
                Transform child = transform.Find("Puzzle Root");
                if (child != null) puzzleRoot = child.gameObject;
            }

            if (combatRoot == null)
            {
                Transform child = transform.Find("Combat Root");
                if (child != null) combatRoot = child.gameObject;
            }

            if (storyRoot == null)
            {
                Transform child = transform.Find("Story Root");
                if (child != null) storyRoot = child.gameObject;
            }

            if (worldCamera == null)
                worldCamera = Camera.main;
            if (puzzleViewportMask == null && worldCamera != null)
            {
                Transform child = worldCamera.transform.Find("PuzzleViewportMask");
                if (child != null) puzzleViewportMask = child.gameObject;
            }
            if (puzzleCameraFollow == null && worldCamera != null)
                puzzleCameraFollow = worldCamera.GetComponent<GridCameraFollow2D>();
            if (transitionFade == null && allowChildFadeFallback)
                transitionFade = GetComponentInChildren<CanvasGroup>(true);

            ResolveGameplayUi();
        }

        private void ResolveGameplayUi()
        {
            GameplayUIRoot uiRoot = GameplayUIRoot.Instance;
            if (uiRoot == null)
                uiRoot = FindFirstObjectByType<GameplayUIRoot>(FindObjectsInactive.Include);
            puzzleUi = uiRoot != null ? uiRoot.PuzzleUi : null;
        }

        private static void SetActiveIfNeeded(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
