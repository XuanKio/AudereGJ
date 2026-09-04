using Audere.Audio;
using Audere.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Audere.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuSettingsPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button closeButton;

        [Header("Audio")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TextMeshProUGUI musicValueText;
        [SerializeField] private TextMeshProUGUI sfxValueText;

        [Header("Difficulty")]
        [SerializeField] private Button easyDifficultyButton;
        [SerializeField] private Button hardDifficultyButton;
        [SerializeField] private TextMeshProUGUI easyDifficultyText;
        [SerializeField] private TextMeshProUGUI hardDifficultyText;
        [SerializeField] private TextMeshProUGUI difficultyDescriptionText;
        [SerializeField] private Color difficultySelectedBackground = new Color(0.94f, 0.97f, 1f, 1f);
        [SerializeField] private Color difficultyIdleBackground = new Color(0.94f, 0.97f, 1f, 0.16f);
        [SerializeField] private Color difficultySelectedText = new Color(0.008f, 0.06f, 0.18f, 1f);
        [SerializeField] private Color difficultyIdleText = new Color(0.96f, 0.98f, 1f, 1f);

        private bool initialized;

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void Start()
        {
            InitializeIfNeeded();
        }

        private void Update()
        {
            InitializeIfNeeded();

            if (panelRoot != null
                && panelRoot.activeSelf
                && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        private void OnDestroy()
        {
            if (!initialized)
                return;

            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(Open);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);

            if (musicSlider != null)
                musicSlider.onValueChanged.RemoveListener(SetMusicVolume);

            if (sfxSlider != null)
                sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);

            if (easyDifficultyButton != null)
                easyDifficultyButton.onClick.RemoveListener(SetEasyDifficulty);

            if (hardDifficultyButton != null)
                hardDifficultyButton.onClick.RemoveListener(SetHardDifficulty);
        }

        public void Open()
        {
            InitializeIfNeeded();

            if (panelRoot == null)
                return;

            panelRoot.SetActive(true);

            if (EventSystem.current != null && musicSlider != null)
                EventSystem.current.SetSelectedGameObject(musicSlider.gameObject);
        }

        public void Close()
        {
            if (panelRoot == null)
                return;

            panelRoot.SetActive(false);
            PlayerPrefs.Save();

            if (EventSystem.current != null && settingsButton != null)
                EventSystem.current.SetSelectedGameObject(settingsButton.gameObject);
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

            if (musicSlider != null)
                musicSlider.onValueChanged.AddListener(SetMusicVolume);

            if (sfxSlider != null)
                sfxSlider.onValueChanged.AddListener(SetSfxVolume);

            if (easyDifficultyButton != null)
                easyDifficultyButton.onClick.AddListener(SetEasyDifficulty);

            if (hardDifficultyButton != null)
                hardDifficultyButton.onClick.AddListener(SetHardDifficulty);

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
            RefreshDifficultyVisuals(GameplayDifficultySettings.Current);

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void SetMusicVolume(float value)
        {
            if (AudioService.Instance != null)
                AudioService.Instance.SetMusicVolume(value);
            else
                PlayerPrefs.SetFloat(AudioService.MusicVolumePrefKey, Mathf.Clamp01(value));

            if (musicValueText != null)
                musicValueText.text = Mathf.RoundToInt(value * 100f).ToString();
        }

        private void SetSfxVolume(float value)
        {
            if (AudioService.Instance != null)
                AudioService.Instance.SetSfxVolume(value);
            else
                PlayerPrefs.SetFloat(AudioService.SfxVolumePrefKey, Mathf.Clamp01(value));

            if (sfxValueText != null)
                sfxValueText.text = Mathf.RoundToInt(value * 100f).ToString();
        }

        private void SetEasyDifficulty()
        {
            SetDifficulty(GameDifficulty.Easy);
        }

        private void SetHardDifficulty()
        {
            SetDifficulty(GameDifficulty.Hard);
        }

        private void SetDifficulty(GameDifficulty difficulty)
        {
            GameplayDifficultySettings.Current = difficulty;
            RefreshDifficultyVisuals(difficulty);
        }

        private void RefreshDifficultyVisuals(GameDifficulty difficulty)
        {
            bool hard = difficulty == GameDifficulty.Hard;
            ApplyDifficultyVisual(easyDifficultyButton, easyDifficultyText, !hard);
            ApplyDifficultyVisual(hardDifficultyButton, hardDifficultyText, hard);

            if (difficultyDescriptionText != null)
            {
                difficultyDescriptionText.text = hard
                    ? "Máu địch +36% · TIME -18%"
                    : "Nhịp chiến đấu tiêu chuẩn";
            }
        }

        private void ApplyDifficultyVisual(Button button, TextMeshProUGUI label, bool selected)
        {
            if (button != null && button.targetGraphic != null)
                button.targetGraphic.color = selected ? difficultySelectedBackground : difficultyIdleBackground;

            if (label != null)
                label.color = selected ? difficultySelectedText : difficultyIdleText;
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

