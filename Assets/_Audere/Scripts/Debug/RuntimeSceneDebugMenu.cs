using Audere.Dialogue;
using Audere.GameplayInput;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Audere.Core
{
    /// <summary>
    /// Hidden runtime scene navigator for quickly checking production builds.
    /// Toggle with Ctrl + S + K. It is created before the first scene and survives loads.
    /// </summary>
    public sealed class RuntimeSceneDebugMenu : MonoBehaviour
    {
        private readonly struct SceneEntry
        {
            public SceneEntry(string label, string sceneName)
            {
                Label = label;
                SceneName = sceneName;
            }

            public string Label { get; }
            public string SceneName { get; }
        }

        private static readonly SceneEntry[] Scenes =
        {
            new SceneEntry("10 · Main Menu", GameScenes.MainMenu),
            new SceneEntry("20 · Ngày 1 — Nhà, buổi sáng", GameScenes.Day1HomeMorning),
            new SceneEntry("30 · Ngày 1 — Lớp học", GameScenes.Classroom),
            new SceneEntry("40 · Ngày 1 — Buổi tối", GameScenes.Evening),
            new SceneEntry("50 · Ngày 2 — Nhà, buổi sáng", GameScenes.Day2HomeMorning),
            new SceneEntry("60 · Ngày 2 — Trường học", GameScenes.Day2SchoolMorning),
            new SceneEntry("70 · Ngày 2 — Nhà, buổi tối", GameScenes.Day2HomeNight),
            new SceneEntry("80 · Ngày 2 — Giấc mơ", GameScenes.Day2Dream),
            new SceneEntry("90 · Ngày 2 — Tỉnh giấc", GameScenes.Day2HomeAwakening),
            new SceneEntry("100 · Ngày 3 — Nhà, buổi sáng", GameScenes.Day3HomeMorning),
            new SceneEntry("110 · Ngày 3 — Bảng lớp", GameScenes.Day3SchoolBoard),
            new SceneEntry("120 · Ngày 3 — Cô giáo", GameScenes.Day3SchoolTeacher),
            new SceneEntry("130 · Ngày 4 — Nhà, buổi sáng", GameScenes.Day4HomeMorning),
            new SceneEntry("140 · Ngày 4 — Đám đông", GameScenes.Day4Classroom),
            new SceneEntry("150 · Ngày 4 — Timor", GameScenes.Day4HomeEvening),
        };

        private const int WindowId = 0x415544;
        private const float WindowWidth = 680f;

        private Rect windowRect;
        private Vector2 scrollPosition;
        private bool isVisible;
        private bool hotkeyLatched;
        private bool previousCursorVisible;
        private CursorLockMode previousCursorLockMode;
        private GameplayInputGate claimedGate;
        private GameplayInputToken inputToken;
        private GUIStyle titleStyle;
        private GUIStyle sceneStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            if (FindAnyObjectByType<RuntimeSceneDebugMenu>() != null)
                return;

            GameObject host = new GameObject("RUNTIME DEBUG SCENE MENU");
            DontDestroyOnLoad(host);
            host.AddComponent<RuntimeSceneDebugMenu>();
        }

        private void Awake()
        {
            windowRect = new Rect(
                Mathf.Max(12f, (Screen.width - WindowWidth) * .5f),
                Mathf.Max(12f, Screen.height * .08f),
                WindowWidth,
                Mathf.Min(720f, Screen.height * .84f));
        }

        private void Update()
        {
            bool controlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool chordHeld = controlHeld && Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.K);
            if (chordHeld && !hotkeyLatched)
                SetVisible(!isVisible);
            hotkeyLatched = chordHeld;

            if (isVisible && Input.GetKeyDown(KeyCode.Escape))
                SetVisible(false);
        }

        private void OnGUI()
        {
            if (!isVisible)
                return;

            EnsureStyles();
            GUI.depth = -10000;
            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, .72f);
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none);
            GUI.color = previousColor;

            windowRect = GUILayout.Window(WindowId, windowRect, DrawWindow, "AUDERE · DEBUG SCENE");
        }

        private void DrawWindow(int id)
        {
            string activeScene = SceneManager.GetActiveScene().name;
            GUILayout.Space(4f);
            GUILayout.Label("Đang ở: " + activeScene, titleStyle);
            GUILayout.Label("Ctrl + S + K: đóng/mở  •  Esc: đóng", sceneStyle);
            GUILayout.Space(8f);

            int activeIndex = FindSceneIndex(activeScene);
            bool canLoad = SceneFlow.Instance == null || !SceneFlow.Instance.IsBusy;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = canLoad;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("← Scene trước", GUILayout.Height(34f)) && activeIndex > 0)
                LoadScene(Scenes[activeIndex - 1].SceneName);
            if (GUILayout.Button("Tải lại scene", GUILayout.Height(34f)))
                LoadScene(activeScene);
            if (GUILayout.Button("Scene sau →", GUILayout.Height(34f)) &&
                activeIndex >= 0 && activeIndex < Scenes.Length - 1)
                LoadScene(Scenes[activeIndex + 1].SceneName);
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
            for (int index = 0; index < Scenes.Length; index += 2)
            {
                GUILayout.BeginHorizontal();
                DrawSceneButton(Scenes[index], activeScene);
                if (index + 1 < Scenes.Length)
                    DrawSceneButton(Scenes[index + 1], activeScene);
                else
                    GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUI.enabled = previousEnabled;
            GUILayout.Space(6f);
            if (GUILayout.Button("Đóng", GUILayout.Height(32f)))
                SetVisible(false);

            GUI.DragWindow(new Rect(0f, 0f, WindowWidth, 30f));
        }

        private void DrawSceneButton(SceneEntry entry, string activeScene)
        {
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && entry.SceneName != activeScene;
            if (GUILayout.Button(entry.Label, GUILayout.MinWidth(310f), GUILayout.Height(38f)))
                LoadScene(entry.SceneName);
            GUI.enabled = wasEnabled;
        }

        private void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[RuntimeSceneDebugMenu] Scene '{sceneName}' is not available in Build Settings.", this);
                return;
            }

            SceneFlow flow = SceneFlow.Instance;
            if (flow != null && flow.IsBusy)
                return;

            SetVisible(false);
            if (flow != null)
                flow.Load(sceneName);
            else
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        private void SetVisible(bool visible)
        {
            if (isVisible == visible)
                return;

            isVisible = visible;
            if (visible)
            {
                previousCursorVisible = Cursor.visible;
                previousCursorLockMode = Cursor.lockState;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                ClaimGameplayInput();
            }
            else
            {
                ReleaseGameplayInput();
                Cursor.visible = previousCursorVisible;
                Cursor.lockState = previousCursorLockMode;
            }
        }

        private void ClaimGameplayInput()
        {
            GameplayUIRoot root = GameplayUIRoot.Instance;
            GameplayInputGate gate = root != null ? root.InputGate : null;
            if (gate == null || !gate.isActiveAndEnabled)
                return;

            claimedGate = gate;
            inputToken = gate.PushMode(this, GameplayInputMode.Modal);
        }

        private void ReleaseGameplayInput()
        {
            if (claimedGate != null && inputToken.IsValid)
                claimedGate.Release(inputToken);
            claimedGate = null;
            inputToken = default;
        }

        private void OnDisable()
        {
            if (isVisible)
                SetVisible(false);
        }

        private static int FindSceneIndex(string sceneName)
        {
            for (int index = 0; index < Scenes.Length; index++)
                if (Scenes[index].SceneName == sceneName)
                    return index;
            return -1;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
                return;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
            };
            sceneStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
            };
        }
    }
}
