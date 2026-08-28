using UnityEngine;
using UnityEngine.UI;
using Audere.Core;

namespace Audere.UI
{
    /// <summary>
    /// Drives the 10_MainMenu scene. Auto-wires its buttons in code (via the serialized
    /// references) so no persistent OnClick wiring is needed in the Inspector. Scene
    /// transitions go through <see cref="SceneFlow"/>, never a direct SceneManager call,
    /// keeping the flow: MainMenu -> New Game -> 20_D1_Home_Morning.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            if (newGameButton != null) newGameButton.onClick.AddListener(NewGame);
            if (quitButton != null)    quitButton.onClick.AddListener(QuitGame);
        }

        private void OnDestroy()
        {
            if (newGameButton != null) newGameButton.onClick.RemoveListener(NewGame);
            if (quitButton != null)    quitButton.onClick.RemoveListener(QuitGame);
        }

        public void NewGame()
        {
            if (SceneFlow.Instance == null)
            {
                Debug.LogError("[MainMenu] SceneFlow missing. Did you start from 00_Bootstrap?");
                return;
            }

            SceneFlow.Instance.Load(GameScenes.Day1HomeMorning);
        }

        public void QuitGame()
        {
            Debug.Log("[MainMenu] Quit requested.");
            Application.Quit();
        }
    }
}
