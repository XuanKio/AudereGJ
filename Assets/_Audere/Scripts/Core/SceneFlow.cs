using System.Collections;
using Audere.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Audere.Core
{
    /// <summary>
    /// Owns all scene load/unload. The <see cref="Bootstrapper"/> delegates every scene
    /// transition here so it never becomes a God Object. Lives under the persistent
    /// Bootstrap root, so it survives Single-mode loads that unload the gameplay scene.
    /// </summary>
    public sealed class SceneFlow : MonoBehaviour, IGameService
    {
        public static SceneFlow Instance { get; private set; }

        /// <summary>True while a load is in progress. Guards against overlapping loads.</summary>
        public bool IsBusy { get; private set; }

        public void Initialize()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[SceneFlow] A second instance was initialized. Ignoring.");
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// Load a scene in Single mode (replaces the current gameplay scene). The
        /// persistent Bootstrap root is preserved via DontDestroyOnLoad.
        /// </summary>
        public Coroutine Load(string sceneName)
        {
            return StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            if (IsBusy)
            {
                Debug.LogWarning($"[SceneFlow] Load('{sceneName}') requested while busy. Ignored.");
                yield break;
            }

            IsBusy = true;
            AudioService audio = AudioService.Instance;
            audio?.SetMusicDuck(this, 0f);
            Debug.Log($"[SceneFlow] Loading '{sceneName}'...");

            try
            {
                AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                while (op != null && !op.isDone)
                    yield return null;
                // Target Awake/Start registers its cover before releasing the load's silence.
                yield return null;
                Debug.Log($"[SceneFlow] Loaded '{sceneName}'.");
            }
            finally
            {
                audio?.ReleaseMusicOwner(this);
                IsBusy = false;
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            AudioService.Instance?.ReleaseMusicOwner(this);
            IsBusy = false;
        }

        private void OnDestroy()
        {
            AudioService.Instance?.ReleaseMusicOwner(this);
            if (Instance == this) Instance = null;
        }
    }
}
