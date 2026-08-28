using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Audere.Core
{
    /// <summary>
    /// The single entry point of the game. Lives in the 00_Bootstrap scene. Its ONLY
    /// responsibilities are:
    ///   1. Keep the global service root alive across scene loads (DontDestroyOnLoad).
    ///   2. Initialize every service under <see cref="servicesRoot"/> in a deterministic
    ///      order (top-to-bottom sibling order in the hierarchy).
    ///   3. Hand off the first scene transition to <see cref="SceneFlow"/>.
    ///
    /// It must NEVER accumulate gameplay/audio/save logic — that belongs in dedicated
    /// services. Adding a new service = drop its component under the services root; no
    /// change to this class is needed.
    /// </summary>
    public sealed class Bootstrapper : MonoBehaviour
    {
        [Tooltip("Root GameObject holding every global service (SceneFlow, AudioService, ...). " +
                 "Sibling order under this root defines initialization order.")]
        [SerializeField] private Transform servicesRoot;

#if UNITY_EDITOR
        [Tooltip("First scene loaded once all services are initialized. Drag a scene asset here; its build-safe path is stored automatically.")]
        [SerializeField] private SceneAsset firstSceneAsset;
#endif

        [FormerlySerializedAs("firstScene")]
        [SerializeField, HideInInspector] private string firstScenePath = GameScenes.MainMenu;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            InitializeServices();
        }

        private void Start()
        {
            if (SceneFlow.Instance == null)
            {
                Debug.LogError("[Bootstrapper] SceneFlow was not initialized. Cannot load first scene.");
                return;
            }

            if (string.IsNullOrWhiteSpace(firstScenePath))
            {
                Debug.LogError("[Bootstrapper] First Scene is not assigned. Select one from the Bootstrapper dropdown.");
                return;
            }

            SceneFlow.Instance.Load(firstScenePath);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (firstSceneAsset == null)
                return;

            firstScenePath = AssetDatabase.GetAssetPath(firstSceneAsset);
        }
#endif

        private void InitializeServices()
        {
            if (servicesRoot == null)
            {
                Debug.LogError("[Bootstrapper] servicesRoot is not assigned. Aborting bootstrap.");
                return;
            }

            IGameService[] services = servicesRoot.GetComponentsInChildren<IGameService>(includeInactive: true);
            foreach (IGameService service in services)
                service.Initialize();

            Debug.Log($"[Bootstrapper] Initialized {services.Length} service(s).");
        }
    }
}
