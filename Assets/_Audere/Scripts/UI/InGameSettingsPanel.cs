using Audere.Audio;
using Audere.Core;
using Audere.Dialogue;
using Audere.GameplayInput;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Audere.UI
{
    [DisallowMultipleComponent]
    public sealed class InGameSettingsPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Audio")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TextMeshProUGUI musicValueText;
        [SerializeField] private TextMeshProUGUI sfxValueText;

        private bool initialized;
        private bool ownsPause;
        private bool leavingForMainMenu;
        private float timeScaleBeforeOpen = 1f;
        private static InGameSettingsPanel activePauseOwner;
        private GameplayInputGate inputGate;
        private GameplayInputToken inputToken;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            InitializeIfNeeded();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            InitializeIfNeeded();
        }

private void Update()
        {
            InitializeIfNeeded();
            MaintainSettingsPause();

            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            if (IsOpen)
                Close();
            else
                Open();
        }

private void LateUpdate()
        {
            MaintainSettingsPause();
        }


private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;

            if (initialized)
            {
                if (settingsButton != null)
                    settingsButton.onClick.RemoveListener(Open);
                if (closeButton != null)
                    closeButton.onClick.RemoveListener(Close);
                if (mainMenuButton != null)
                    mainMenuButton.onClick.RemoveListener(ExitToMainMenu);
                if (musicSlider != null)
                    musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
                if (sfxSlider != null)
                    sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);
            }

            ReleaseGameplayInput();
            RestoreGameplayTime(forceResume: true);
        }

public void Open()
        {
            InitializeIfNeeded();

            if (panelRoot == null || panelRoot.activeSelf)
                return;
            if (SceneFlow.Instance != null && SceneFlow.Instance.IsBusy)
                return;

            SyncAudioValues();
            timeScaleBeforeOpen = Time.timeScale > 0f ? Time.timeScale : 1f;
            ownsPause = true;
            activePauseOwner = this;
            panelRoot.SetActive(true);
            ClaimGameplayInput();
            MaintainSettingsPause();

            if (EventSystem.current != null && musicSlider != null)
                EventSystem.current.SetSelectedGameObject(musicSlider.gameObject);
        }

        public void Close()
        {
            if (panelRoot == null || !panelRoot.activeSelf)
                return;

            panelRoot.SetActive(false);
            ReleaseGameplayInput();
            RestoreGameplayTime();
            PlayerPrefs.Save();

            if (EventSystem.current != null && settingsButton != null)
                EventSystem.current.SetSelectedGameObject(settingsButton.gameObject);
        }

public void ExitToMainMenu()
        {
            if (SceneFlow.Instance == null)
            {
                Debug.LogError("[InGameSettings] SceneFlow missing. Cannot return to Main Menu.", this);
                return;
            }
            if (SceneFlow.Instance.IsBusy)
                return;

            leavingForMainMenu = true;
            PlayerPrefs.Save();
            panelRoot.SetActive(false);
            ReleaseGameplayInput();
            ownsPause = false;
            if (activePauseOwner == this)
                activePauseOwner = null;
            Time.timeScale = 1f;

            if (settingsButton != null)
                settingsButton.interactable = false;
            if (mainMenuButton != null)
                mainMenuButton.interactable = false;

            SceneFlow.Instance.Load(GameScenes.MainMenu);
        }

        private void InitializeIfNeeded()
        {
            if (initialized)
                return;

            initialized = true;

            if (settingsButton != null)
                settingsButton.onClick.AddListener(Open);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(ExitToMainMenu);
            if (musicSlider != null)
                musicSlider.onValueChanged.AddListener(SetMusicVolume);
            if (sfxSlider != null)
                sfxSlider.onValueChanged.AddListener(SetSfxVolume);

            SyncAudioValues();

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (leavingForMainMenu || scene.name == GameScenes.MainMenu)
                return;

            if (IsOpen)
                Close();
        }

private void RestoreGameplayTime(bool forceResume = false)
        {
            if (!ownsPause)
                return;

            if (Time.timeScale > 0f)
                timeScaleBeforeOpen = Time.timeScale;

            bool externalDialoguePause = !forceResume && HasActiveDialoguePause();
            Time.timeScale = externalDialoguePause
                ? 0f
                : Mathf.Max(0.0001f, timeScaleBeforeOpen);
            ownsPause = false;
            if (activePauseOwner == this)
                activePauseOwner = null;
        }

private static bool HasActiveDialoguePause()
        {
            Audere.Dialogue.GameplayUIRoot root = Audere.Dialogue.GameplayUIRoot.Instance;
            return root != null && root.Dialogue != null && root.Dialogue.IsPlaying;
        }

public static bool TryGetResumeTimeScale(out float scale)
        {
            scale = 1f;
            InGameSettingsPanel panel = activePauseOwner;
            if (panel == null || !panel.IsOpen || !panel.ownsPause)
                return false;

            scale = Mathf.Max(0.0001f, panel.timeScaleBeforeOpen);
            return true;
        }

        private void ClaimGameplayInput()
        {
            ReleaseGameplayInput();

            GameplayUIRoot root = GetComponentInParent<GameplayUIRoot>(true);
            if (root == null)
                root = GameplayUIRoot.Instance;

            GameplayInputGate gate = root != null ? root.InputGate : null;
            if (gate == null || !gate.isActiveAndEnabled)
            {
                Debug.LogError("[InGameSettings] GameplayInputGate is not available.", this);
                return;
            }

            GameplayInputToken token = gate.PushMode(this, GameplayInputMode.Modal);
            if (!token.IsValid)
                return;

            inputGate = gate;
            inputToken = token;
        }

        private void ReleaseGameplayInput()
        {
            GameplayInputGate gate = inputGate;
            GameplayInputToken token = inputToken;
            inputGate = null;
            inputToken = default;

            if (gate != null && token.IsValid)
                gate.Release(token);
        }



private void MaintainSettingsPause()
        {
            if (!ownsPause || !IsOpen)
                return;

            if (Time.timeScale > 0f)
                timeScaleBeforeOpen = Time.timeScale;

            Time.timeScale = 0f;
        }


        private void SyncAudioValues()
        {
            float musicVolume = AudioService.Instance != null
                ? AudioService.Instance.MusicVolume
                : PlayerPrefs.GetFloat(AudioService.MusicVolumePrefKey, 0.8f);
            float sfxVolume = AudioService.Instance != null
                ? AudioService.Instance.SfxVolume
                : PlayerPrefs.GetFloat(AudioService.SfxVolumePrefKey, 0.8f);

            if (musicSlider != null)
                musicSlider.SetValueWithoutNotify(musicVolume);
            if (sfxSlider != null)
                sfxSlider.SetValueWithoutNotify(sfxVolume);

            RefreshValueLabels(musicVolume, sfxVolume);
        }

        private void SetMusicVolume(float value)
        {
            value = Mathf.Clamp01(value);

            if (AudioService.Instance != null)
                AudioService.Instance.SetMusicVolume(value);
            else
                PlayerPrefs.SetFloat(AudioService.MusicVolumePrefKey, value);

            if (musicValueText != null)
                musicValueText.text = Mathf.RoundToInt(value * 100f).ToString();
        }

        private void SetSfxVolume(float value)
        {
            value = Mathf.Clamp01(value);

            if (AudioService.Instance != null)
                AudioService.Instance.SetSfxVolume(value);
            else
                PlayerPrefs.SetFloat(AudioService.SfxVolumePrefKey, value);

            if (sfxValueText != null)
                sfxValueText.text = Mathf.RoundToInt(value * 100f).ToString();
        }

        private void RefreshValueLabels(float musicVolume, float sfxVolume)
        {
            if (musicValueText != null)
                musicValueText.text = Mathf.RoundToInt(musicVolume * 100f).ToString();
            if (sfxValueText != null)
                sfxValueText.text = Mathf.RoundToInt(sfxVolume * 100f).ToString();
        }
    }
}
