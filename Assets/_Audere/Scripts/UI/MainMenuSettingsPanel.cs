using Audere.Audio;
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

        private void RefreshValueLabels(float musicVolume, float sfxVolume)
        {
            if (musicValueText != null)
                musicValueText.text = Mathf.RoundToInt(musicVolume * 100f).ToString();

            if (sfxValueText != null)
                sfxValueText.text = Mathf.RoundToInt(sfxVolume * 100f).ToString();
        }
    }
}

