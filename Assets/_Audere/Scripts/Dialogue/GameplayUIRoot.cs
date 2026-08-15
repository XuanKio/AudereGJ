using Audere.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Audere.Dialogue
{
    [DisallowMultipleComponent]
    public sealed class GameplayUIRoot : MonoBehaviour
    {
        [SerializeField] private DialogueController dialogue;

        public static GameplayUIRoot Instance { get; private set; }
        public DialogueController Dialogue => dialogue;

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
    }
}
