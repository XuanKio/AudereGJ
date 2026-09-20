using System;
using System.Linq;
using System.IO;
using Audere.Audio;
using Audere.Dialogue;
using Audere.GameplayInput;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Audere.Combat.Editor
{
    public static class CombatRetryPresentationAuthoring
    {
        public const string UiPath = "Assets/_Audere/Prefabs/UI/GameplayUIRoot.prefab";
        public const string ProfilePath = "Assets/_Audere/Data/Combat/CombatRetryPresentation.asset";
        public const string RetryFontPath = "Assets/_Audere/AssetGame/Font/Mynerve-Regular SDF.asset";
        private const string HeartPath = "Assets/_Audere/Prefabs/Combat/Player/HeartVisual.prefab";
        private const string BoardPath = "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab";

        [MenuItem("Audere/Combat/Setup Heartbreak Retry")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring Retry presentation.");
            CombatRetryPresentationProfile profile = GetOrCreateProfile();
            EditPrefab(HeartPath, root => root.GetComponent<Image>().color = Color.white);
            EditPrefab(BoardPath, root => {
                foreach (CombatPlayerView player in root.GetComponentsInChildren<CombatPlayerView>(true))
                    WhitenPlayer(player);
            });
            EditPrefab(UiPath, root => Configure(root, profile));
            RegisterCrackAudio();
            MigrateSceneOverrides(profile);
            AssetDatabase.SaveAssets();
            Debug.Log("[HeartbreakRetry] Shared Retry UI, white Heart prefabs and crack audio are ready.");
        }

        public static CombatRetryPresentationProfile GetOrCreateProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<CombatRetryPresentationProfile>(ProfilePath);
            if (profile != null) return profile;
            profile = ScriptableObject.CreateInstance<CombatRetryPresentationProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
            return profile;
        }

        private static void MigrateSceneOverrides(CombatRetryPresentationProfile profile)
        {
            string boardGuid = AssetDatabase.AssetPathToGUID(BoardPath);
            string uiGuid = AssetDatabase.AssetPathToGUID(UiPath);
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Audere/Scenes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string yaml = File.ReadAllText(path);
                if (!yaml.Contains("32ae51753931fc94cb63331956372d09") &&
                    !yaml.Contains("8b5f3137bdd14dd42832e4523e6499cd") &&
                    !yaml.Contains(boardGuid) && !yaml.Contains(uiGuid)) continue;
                Scene scene = SceneManager.GetSceneByPath(path);
                bool wasLoaded = scene.IsValid() && scene.isLoaded;
                if (wasLoaded && scene.isDirty)
                    throw new InvalidOperationException("Scene has unsaved edits: " + path);
                if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    bool changed = false;
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        foreach (CombatPlayerView player in root.GetComponentsInChildren<CombatPlayerView>(true))
                        {
                            if (new SerializedObject(player).FindProperty("normalColor").colorValue == Color.white &&
                                player.GetComponentsInChildren<Image>(true).All(image => image.color == Color.white)) continue;
                            WhitenPlayer(player);
                            changed = true;
                        }
                        foreach (CombatRetryView retry in root.GetComponentsInChildren<CombatRetryView>(true))
                        {
                            changed |= ApplyRetryFont(retry.transform.Find("Retry Panel/Retry Content"));
                            if (HasCompleteReferences(retry)) continue;
                            GameplayUIRoot ui = retry.GetComponentInParent<GameplayUIRoot>(true);
                            if (ui == null) throw new MissingReferenceException("Retry requires GameplayUIRoot: " + path);
                            Configure(ui.gameObject, profile);
                            changed = true;
                        }
                    }
                    if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                }
                finally { if (!wasLoaded) EditorSceneManager.CloseScene(scene, true); }
            }
        }

        public static bool HasCompleteReferences(CombatRetryView retry)
        {
            var data = new SerializedObject(retry);
            return new[] { "retryRoot", "messageText", "retryButton", "presentation", "intactHeart",
                "leftHeart", "rightHeart", "retryButtonGroup", "inputGate", "crackSource", "backgroundCover" }
                .All(name => data.FindProperty(name).objectReferenceValue != null);
        }

        public static void WhitenPlayer(CombatPlayerView player)
        {
            var data = new SerializedObject(player);
            data.FindProperty("normalColor").colorValue = Color.white;
            data.ApplyModifiedPropertiesWithoutUndo();
            foreach (Image image in player.GetComponentsInChildren<Image>(true))
            {
                var graphic = new SerializedObject(image);
                graphic.FindProperty("m_Color").colorValue = Color.white;
                graphic.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public static void Configure(GameObject root, CombatRetryPresentationProfile profile)
        {
            Transform retry = root.transform.Find("CombatRetryUI");
            if (retry == null) throw new MissingReferenceException("GameplayUIRoot/CombatRetryUI is missing.");
            CombatRetryView view = retry.GetComponent<CombatRetryView>();
            Transform panel = retry.Find("Retry Panel");
            Transform content = panel.Find("Retry Content");
            ApplyRetryFont(content);
            Image blocker = panel.Find("Fullscreen Blocker").GetComponent<Image>();
            blocker.color = Color.clear;
            blocker.raycastTarget = true;
            Stretch(blocker.rectTransform, Vector2.zero, Vector2.one);
            Image oldCard = content.GetComponent<Image>();
            if (oldCard != null) { oldCard.color = Color.clear; oldCard.raycastTarget = false; }
            Stretch((RectTransform)content, Vector2.zero, Vector2.one);

            TMP_Text message = content.Find("Retry Message").GetComponent<TMP_Text>();
            Stretch(message.rectTransform, new Vector2(.08f, .73f), new Vector2(.92f, .93f));
            message.text = profile.Message;
            message.fontSize = 46f;
            message.enableAutoSizing = true;
            message.fontSizeMin = 32f;
            message.fontSizeMax = 46f;
            message.alignment = TextAlignmentOptions.Center;
            message.color = Color.white;
            message.raycastTarget = false;

            RectTransform heartRoot = Child(content, "Broken Heart");
            Fixed(heartRoot, new Vector2(.5f, .42f), Vector2.zero, new Vector2(80f, 80f));
            Sprite sprite = AssetDatabase.LoadAssetAtPath<GameObject>(HeartPath).GetComponent<Image>().sprite;
            Image whole = Ensure<Image>(Child(heartRoot, "Heart Visual").gameObject);
            Stretch(whole.rectTransform, Vector2.zero, Vector2.one);
            whole.sprite = sprite;
            whole.color = Color.white;
            whole.preserveAspect = true;
            whole.raycastTarget = false;
            CombatHeartHalfGraphic left = Half(heartRoot, "Left Half", sprite, false);
            CombatHeartHalfGraphic right = Half(heartRoot, "Right Half", sprite, true);

            Button button = content.Find("Retry Button").GetComponent<Button>();
            Fixed((RectTransform)button.transform, new Vector2(.5f, .64f), Vector2.zero, new Vector2(300f, 88f));
            Image buttonBackground = button.GetComponent<Image>();
            buttonBackground.color = Color.clear;
            buttonBackground.raycastTarget = true;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            label.text = "Thử lại";
            label.fontSize = 42f;
            label.color = Color.white;
            button.targetGraphic = label;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, .88f, .45f);
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(.7f, .7f, .7f);
            button.colors = colors;
            CanvasGroup buttonGroup = Ensure<CanvasGroup>(button.gameObject);
            AudioSource crackSource = Ensure<AudioSource>(retry.gameObject);
            crackSource.playOnAwake = false;
            crackSource.loop = false;
            crackSource.spatialBlend = 0f;
            GameplayInputGate gate = root.GetComponentInChildren<GameplayInputGate>(true);
            if (gate == null) throw new MissingReferenceException("GameplayUIRoot InputGate is missing.");

            var serialized = new SerializedObject(view);
            Set(serialized, "retryRoot", panel.gameObject);
            Set(serialized, "messageText", message);
            Set(serialized, "retryButton", button);
            Set(serialized, "presentation", profile);
            Set(serialized, "intactHeart", whole);
            Set(serialized, "leftHeart", left);
            Set(serialized, "rightHeart", right);
            Set(serialized, "retryButtonGroup", buttonGroup);
            Set(serialized, "inputGate", gate);
            Set(serialized, "crackSource", crackSource);
            Set(serialized, "backgroundCover", blocker);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            view.ApplyPresentationLayout();
            panel.gameObject.SetActive(false);
        }

        private static bool ApplyRetryFont(Transform content)
        {
            if (content == null) throw new MissingReferenceException("Retry Content is missing.");
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RetryFontPath);
            if (font == null) throw new MissingReferenceException("Mynerve-Regular SDF font is missing.");
            bool changed = false;
            foreach (TMP_Text text in content.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font == font && text.fontSharedMaterial == font.material) continue;
                text.font = font;
                text.fontSharedMaterial = font.material;
                EditorUtility.SetDirty(text);
                changed = true;
            }
            return changed;
        }

        private static CombatHeartHalfGraphic Half(Transform parent, string name, Sprite sprite, bool right)
        {
            CombatHeartHalfGraphic graphic = Ensure<CombatHeartHalfGraphic>(Child(parent, name).gameObject);
            Stretch(graphic.rectTransform, Vector2.zero, Vector2.one);
            graphic.Configure(sprite, right);
            return graphic;
        }

        private static void RegisterCrackAudio()
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Audere/Audio/HeartBreak.wav");
            if (clip == null) throw new MissingReferenceException("HeartBreak.wav is missing.");
            AudioCatalog catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>("Assets/_Audere/Data/Audio/AudioCatalog.asset");
            var data = new SerializedObject(catalog);
            SerializedProperty entries = data.FindProperty("entries");
            SerializedProperty entry = null;
            for (int i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("id").intValue == (int)AudioId.Player_HeartBreak)
                    entry = entries.GetArrayElementAtIndex(i);
            if (entry == null) { entries.arraySize++; entry = entries.GetArrayElementAtIndex(entries.arraySize - 1); }
            entry.FindPropertyRelative("id").intValue = (int)AudioId.Player_HeartBreak;
            entry.FindPropertyRelative("clip").objectReferenceValue = clip;
            entry.FindPropertyRelative("volume").floatValue = .8f;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EditPrefab(string path, Action<GameObject> edit)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try { edit(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static T Ensure<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }
        private static void Set(SerializedObject data, string name, UnityEngine.Object value) => data.FindProperty(name).objectReferenceValue = value;
        private static RectTransform Child(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return (RectTransform)child;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
        private static void Fixed(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = position; rect.sizeDelta = size; rect.localScale = Vector3.one;
        }
    }
}
