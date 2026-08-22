using Audere.Core;
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

        public static GameplayUIRoot Instance { get; private set; }
        public Canvas GameplayCanvas => gameplayCanvas;
        public RectTransform PuzzleUi => puzzleUi;
        public PathPieceHand PathPieceHand => pathPieceHand;
        public DialogueController Dialogue => dialogue;
        public GameplayInputGate InputGate => inputGate;

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

            if (gameObject.scene.name == GameScenes.MainMenu)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            if (dialogue != null)
                dialogue.ForceClose();

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
        }
    }
}
