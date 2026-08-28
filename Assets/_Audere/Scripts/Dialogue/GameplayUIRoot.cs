using Audere.Core;
using Audere.Combat;
using Audere.GameplayInput;
using Audere.Puzzle.PathPieces;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Audere.Dialogue
{
    [DisallowMultipleComponent]
    public sealed class GameplayUIRoot : MonoBehaviour
    {
        [Header("Root Canvas")]
        [SerializeField] private Canvas gameplayCanvas;

        [Header("Gameplay Sections")]
        [SerializeField] private RectTransform puzzleUi;
        [SerializeField] private PathPieceHand pathPieceHand;
        [SerializeField] private DialogueController dialogue;
        [SerializeField] private GameplayInputGate inputGate;
        [SerializeField] private CombatTutorialView combatTutorial;
        [SerializeField] private CombatRetryView combatRetry;

        public static GameplayUIRoot Instance { get; private set; }
        public Canvas GameplayCanvas => gameplayCanvas;
        public RectTransform PuzzleUi => puzzleUi;
        public PathPieceHand PathPieceHand => pathPieceHand;
        public DialogueController Dialogue => dialogue;
        public GameplayInputGate InputGate => inputGate;
        public CombatTutorialView CombatTutorial => combatTutorial;
        public CombatRetryView CombatRetry => combatRetry;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
            ResolveReferences();
            ApplyScenePresentation(SceneManager.GetActiveScene());

            if (gameObject.scene.name == GameScenes.MainMenu)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            if (dialogue != null)
                dialogue.ForceClose();
            if (combatTutorial != null)
                combatTutorial.ForceHide();
            if (combatRetry != null)
                combatRetry.ForceHide();

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == GameScenes.MainMenu)
            {
                Destroy(gameObject);
                return;
            }

            if (dialogue != null)
                dialogue.ForceClose();
            if (combatTutorial != null)
                combatTutorial.ForceHide();
            if (combatRetry != null)
                combatRetry.ForceHide();

            ApplyScenePresentation(scene);
        }

        private void ResolveReferences()
        {
            if (gameplayCanvas == null)
                gameplayCanvas = GetComponent<Canvas>();
            if (puzzleUi == null)
            {
                Transform puzzleTransform = transform.Find("PuzzleUI");
                puzzleUi = puzzleTransform as RectTransform;
            }
            if (pathPieceHand == null)
                pathPieceHand = GetComponentInChildren<PathPieceHand>(true);
            if (dialogue == null)
                dialogue = GetComponentInChildren<DialogueController>(true);
            if (inputGate == null)
                inputGate = GetComponentInChildren<GameplayInputGate>(true);
            if (combatTutorial == null)
                combatTutorial = GetComponentInChildren<CombatTutorialView>(true);
            if (combatRetry == null)
                combatRetry = GetComponentInChildren<CombatRetryView>(true);
        }

        private void ApplyScenePresentation(Scene scene)
        {
            if (puzzleUi == null)
                return;

            if (scene.name == GameScenes.Classroom || scene.name == GameScenes.Day2SchoolMorning ||
                scene.name == GameScenes.Day2HomeNight || scene.name == GameScenes.Day2HomeAwakening ||
                scene.name == GameScenes.Day3HomeMorning || scene.name == GameScenes.Day3SchoolBoard ||
                scene.name == GameScenes.Day3SchoolTeacher)
                puzzleUi.gameObject.SetActive(false);
            else if (scene.name == GameScenes.Day1HomeMorning ||
                     scene.name == GameScenes.Day2HomeMorning || scene.name == GameScenes.Day2Dream)
                puzzleUi.gameObject.SetActive(true);
        }
    }
}
