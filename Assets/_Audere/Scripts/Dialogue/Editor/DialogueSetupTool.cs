using System;
using System.Linq;
using Audere.Puzzle;
using Audere.Puzzle.Board;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Audere.Dialogue.Editor
{
    [InitializeOnLoad]
    public static class DialogueSetupTool
    {
        private const string PreviewRequestedKey = "Audere.Dialogue.PreviewRequested";
        private const string DialogueDataFolder = "Assets/_Audere/Data/Dialogue";
        private const string DialoguePrefabFolder = "Assets/_Audere/Prefabs/UI/Dialogue";
        private const string CharacterCatalogPath = DialogueDataFolder + "/DialogueCharacterCatalog.asset";
        private const string SampleDialoguePath = DialogueDataFolder + "/Dialogue_Sample.asset";
        private const string BubblePrefabPath = DialoguePrefabFolder + "/DialogueBubble.prefab";
        private const string LeftSlotPrefabPath = DialoguePrefabFolder + "/Left.prefab";
        private const string RightSlotPrefabPath = DialoguePrefabFolder + "/Right.prefab";
        private const string GameplayRootPrefabPath = "Assets/_Audere/Prefabs/UI/GameplayUIRoot.prefab";
        private const string DialogueTilePrefabPath = "Assets/_Audere/Prefabs/Puzzle/Tiles/Dialogue.prefab";
        private const string PortraitPath = "Assets/_Audere/AssetGame/Audere/Main.png";
        private const string BubbleSpritePath = "Assets/_Audere/AssetGame/Dialogue.png";
        private const string SamplePuzzlePath = "Assets/_Audere/Data/Puzzle/Puzzle_MVP_01.asset";

        static DialogueSetupTool()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem("Audere/Dialogue/Setup From Scene Template")]
        public static void SetupFromSceneTemplate()
        {
            try
            {
                EnsureFolder(DialogueDataFolder);
                EnsureFolder(DialoguePrefabFolder);
                EnsureFolder("Assets/_Audere/Prefabs/UI");

                DialogueCharacterCatalog catalog = CreateOrUpdateCharacterCatalog();
                DialogueData sampleDialogue = CreateOrUpdateSampleDialogue();
                CreateOrUpdateDialogueTilePrefab();
                RegisterDialogueTilePrefab();
                AddDialogueToSamplePuzzle(sampleDialogue);
                BuildUiFromSceneTemplate(catalog);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[DialogueSetup] Dialogue data, prefabs, sample tile and persistent UI are ready.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        [MenuItem("Audere/Dialogue/Preview Sample")]
        public static void PreviewSample()
        {
            SessionState.SetBool(PreviewRequestedKey, true);
            if (EditorApplication.isPlaying)
                EditorApplication.delayCall += StartRequestedPreview;
            else
                EditorApplication.isPlaying = true;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode &&
                SessionState.GetBool(PreviewRequestedKey, false))
                EditorApplication.delayCall += StartRequestedPreview;
        }

        private static void StartRequestedPreview()
        {
            if (!EditorApplication.isPlaying)
                return;

            GameplayUIRoot root = GameplayUIRoot.Instance;
            DialogueData sample = AssetDatabase.LoadAssetAtPath<DialogueData>(SampleDialoguePath);
            if (root == null || sample == null)
            {
                Debug.LogError("[DialogueSetup] Cannot preview: GameplayUIRoot or sample dialogue is missing.");
                SessionState.SetBool(PreviewRequestedKey, false);
                return;
            }

            root.Dialogue.Play(sample, false);
            SessionState.SetBool(PreviewRequestedKey, false);
        }

        private static DialogueCharacterCatalog CreateOrUpdateCharacterCatalog()
        {
            DialogueCharacterCatalog catalog = AssetDatabase.LoadAssetAtPath<DialogueCharacterCatalog>(CharacterCatalogPath);
            if (catalog != null)
                return catalog;

            catalog = ScriptableObject.CreateInstance<DialogueCharacterCatalog>();
            AssetDatabase.CreateAsset(catalog, CharacterCatalogPath);

            Sprite auderePortrait = LoadFirstSprite(PortraitPath);
            SerializedObject serializedCatalog = new SerializedObject(catalog);
            SerializedProperty characters = serializedCatalog.FindProperty("characters");
            characters.arraySize = 2;
            ConfigureCharacter(characters.GetArrayElementAtIndex(0), DialogueCharacterId.Audere, "Audere", auderePortrait);
            ConfigureCharacter(characters.GetArrayElementAtIndex(1), DialogueCharacterId.Timor, "Timor", null);
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void ConfigureCharacter(
            SerializedProperty entry,
            DialogueCharacterId character,
            string displayName,
            Sprite portrait)
        {
            entry.FindPropertyRelative("character").enumValueIndex = (int)character;
            entry.FindPropertyRelative("displayName").stringValue = displayName;
            entry.FindPropertyRelative("portrait").objectReferenceValue = portrait;
        }

        private static DialogueData CreateOrUpdateSampleDialogue()
        {
            DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(SampleDialoguePath);
            if (data != null)
                return data;

            data = ScriptableObject.CreateInstance<DialogueData>();
            AssetDatabase.CreateAsset(data, SampleDialoguePath);

            SerializedObject serializedData = new SerializedObject(data);
            serializedData.FindProperty("dialogueId").stringValue = "puzzle-mvp-introduction";
            serializedData.FindProperty("leftCharacter").enumValueIndex = (int)DialogueCharacterId.Audere;
            serializedData.FindProperty("rightCharacter").enumValueIndex = (int)DialogueCharacterId.Timor;

            SerializedProperty lines = serializedData.FindProperty("lines");
            lines.arraySize = 3;
            ConfigureLine(lines.GetArrayElementAtIndex(0), DialogueSpeakerSide.Left,
                "Nhật Linh ơi, có con cừu đang ăn cỏ kìa!");
            ConfigureLine(lines.GetArrayElementAtIndex(1), DialogueSpeakerSide.Right,
                "Mình thấy rồi. Cẩn thận đừng làm nó sợ nhé.");
            ConfigureLine(lines.GetArrayElementAtIndex(2), DialogueSpeakerSide.Left,
                "Ừ, chúng mình đi tiếp thôi!");
            serializedData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void ConfigureLine(
            SerializedProperty line,
            DialogueSpeakerSide speaker,
            string text)
        {
            line.FindPropertyRelative("speaker").enumValueIndex = (int)speaker;
            line.FindPropertyRelative("text").stringValue = text;
        }

        private static void CreateOrUpdateDialogueTilePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(DialogueTilePrefabPath) != null)
                return;

            Sprite bubbleSprite = LoadFirstSprite(BubbleSpritePath);
            GameObject tileRoot = new GameObject("Dialogue", typeof(BoardTile), typeof(DialogueTileBehaviour));
            GameObject visual = new GameObject("Tile Visual", typeof(SpriteRenderer));
            visual.transform.SetParent(tileRoot.transform, false);
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sprite = bubbleSprite;
            renderer.color = new Color(0.75f, 0.95f, 1f, 1f);

            PrefabUtility.SaveAsPrefabAsset(tileRoot, DialogueTilePrefabPath);
            Object.DestroyImmediate(tileRoot);
        }

        private static void RegisterDialogueTilePrefab()
        {
            PuzzleTileCatalog catalog = AssetDatabase.LoadAssetAtPath<PuzzleTileCatalog>(
                PuzzleContentConstants.AssetPaths.TileCatalog);
            BoardTile prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueTilePrefabPath)
                .GetComponent<BoardTile>();

            SerializedObject serializedCatalog = new SerializedObject(catalog);
            SerializedProperty entries = serializedCatalog.FindProperty("entries");
            SerializedProperty targetEntry = null;

            for (int index = 0; index < entries.arraySize; index++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("tileType").enumValueIndex == (int)PuzzleTileType.Dialogue)
                {
                    targetEntry = entry;
                    break;
                }
            }

            if (targetEntry == null)
            {
                entries.InsertArrayElementAtIndex(entries.arraySize);
                targetEntry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            }

            targetEntry.FindPropertyRelative("tileType").enumValueIndex = (int)PuzzleTileType.Dialogue;
            targetEntry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void AddDialogueToSamplePuzzle(DialogueData dialogue)
        {
            PuzzleData puzzle = AssetDatabase.LoadAssetAtPath<PuzzleData>(SamplePuzzlePath);
            if (puzzle == null)
                return;

            SerializedObject serializedPuzzle = new SerializedObject(puzzle);
            SerializedProperty tiles = serializedPuzzle.FindProperty("boardTiles");
            for (int index = 0; index < tiles.arraySize; index++)
            {
                SerializedProperty tile = tiles.GetArrayElementAtIndex(index);
                if (tile.FindPropertyRelative("position").vector2IntValue != Vector2Int.zero)
                    continue;

                tile.FindPropertyRelative("tileType").enumValueIndex = (int)PuzzleTileType.Dialogue;
                tile.FindPropertyRelative("dialogue").objectReferenceValue = dialogue;
                tile.FindPropertyRelative("triggerDialogueOnce").boolValue = true;
                break;
            }

            serializedPuzzle.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(puzzle);
        }

        private static void BuildUiFromSceneTemplate(DialogueCharacterCatalog catalog)
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject sceneCanvas = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == "Canvas");
            Transform leftTemplate = sceneCanvas != null ? sceneCanvas.transform.Find("Left") : null;
            Transform rightTemplate = sceneCanvas != null ? sceneCanvas.transform.Find("Right") : null;

            if (leftTemplate != null && rightTemplate != null)
            {
                Transform bubbleTemplate = leftTemplate.Find("Dialogue Bubble");
                if (bubbleTemplate == null)
                    throw new InvalidOperationException("Canvas/Left/Dialogue Bubble was not found.");

                GameObject bubblePrefab = BuildBubblePrefab(bubbleTemplate.gameObject);
                GameObject leftSlotPrefab = BuildSlotPrefab(leftTemplate.gameObject, bubblePrefab, true);
                GameObject rightSlotPrefab = BuildSlotPrefab(rightTemplate.gameObject, bubblePrefab, false);
                BuildGameplayRootPrefab(leftSlotPrefab, rightSlotPrefab, catalog);

                Object.DestroyImmediate(leftTemplate.gameObject);
                Object.DestroyImmediate(rightTemplate.gameObject);
            }

            GameObject rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayRootPrefabPath);
            if (rootPrefab == null)
                throw new InvalidOperationException(
                    "GameplayUIRoot prefab is missing and the Left/Right scene template is unavailable.");

            GameplayUIRoot existingRoot = Object.FindFirstObjectByType<GameplayUIRoot>();
            if (existingRoot == null)
                PrefabUtility.InstantiatePrefab(rootPrefab, scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static GameObject BuildBubblePrefab(GameObject bubbleTemplate)
        {
            GameObject bubble = Object.Instantiate(bubbleTemplate);
            bubble.name = "Dialogue Bubble";

            TMP_Text dialogueText = bubble.GetComponentInChildren<TMP_Text>(true);
            if (dialogueText == null)
                throw new InvalidOperationException("Dialogue Bubble needs a TMP text child.");

            dialogueText.gameObject.name = "Dialogue Text (TMP)";
            dialogueText.text = "Dialogue text";
            RectTransform dialogueTextRect = dialogueText.rectTransform;
            dialogueTextRect.anchoredPosition = new Vector2(-2.0503f, -8f);
            dialogueTextRect.sizeDelta = new Vector2(397.9678f, 112f);

            GameObject nameObject = new GameObject(
                "Character Name (TMP)",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            RectTransform nameRect = nameObject.GetComponent<RectTransform>();
            nameRect.SetParent(bubble.transform, false);
            nameRect.anchorMin = new Vector2(0.5f, 0.5f);
            nameRect.anchorMax = new Vector2(0.5f, 0.5f);
            nameRect.anchoredPosition = new Vector2(0f, 68f);
            nameRect.sizeDelta = new Vector2(360f, 34f);

            TextMeshProUGUI nameText = nameObject.GetComponent<TextMeshProUGUI>();
            nameText.font = dialogueText.font;
            nameText.fontSize = 20f;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = Color.black;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.text = "Character";

            DialogueBubbleView bubbleView = bubble.AddComponent<DialogueBubbleView>();
            CanvasGroup bubbleGroup = bubble.GetComponent<CanvasGroup>();
            if (bubbleGroup == null)
                bubbleGroup = bubble.AddComponent<CanvasGroup>();
            SetReference(bubbleView, "characterNameText", nameText);
            SetReference(bubbleView, "dialogueText", dialogueText);
            SetReference(bubbleView, "canvasGroup", bubbleGroup);
            SetReference(bubbleView, "bubbleTransform", bubble.GetComponent<RectTransform>());

            PrefabUtility.SaveAsPrefabAsset(bubble, BubblePrefabPath);
            Object.DestroyImmediate(bubble);
            return AssetDatabase.LoadAssetAtPath<GameObject>(BubblePrefabPath);
        }

        private static GameObject BuildSlotPrefab(GameObject template, GameObject bubblePrefab, bool isLeft)
        {
            GameObject slot = Object.Instantiate(template);
            slot.name = isLeft ? "Left" : "Right";

            Transform oldBubble = slot.transform.Find("Dialogue Bubble");
            if (oldBubble != null)
                Object.DestroyImmediate(oldBubble.gameObject);

            CanvasGroup canvasGroup = slot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = slot.AddComponent<CanvasGroup>();

            GameObject bubble = (GameObject)PrefabUtility.InstantiatePrefab(bubblePrefab);
            RectTransform bubbleRect = bubble.GetComponent<RectTransform>();
            bubbleRect.SetParent(slot.transform, false);
            bubbleRect.anchoredPosition = new Vector2(isLeft ? 19f : -19f, 327f);

            DialogueCharacterSlotView view = slot.AddComponent<DialogueCharacterSlotView>();
            SetReference(view, "characterImage", slot.GetComponent<Image>());
            SetReference(view, "bubble", bubble.GetComponent<DialogueBubbleView>());
            SetReference(view, "canvasGroup", canvasGroup);

            string path = isLeft ? LeftSlotPrefabPath : RightSlotPrefabPath;
            PrefabUtility.SaveAsPrefabAsset(slot, path);
            Object.DestroyImmediate(slot);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void BuildGameplayRootPrefab(
            GameObject leftSlotPrefab,
            GameObject rightSlotPrefab,
            DialogueCharacterCatalog catalog)
        {
            GameObject root = new GameObject(
                "GameplayUIRoot",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(GameplayUIRoot));

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject dialogueUi = new GameObject(
                "DialogueUI",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(DialogueController));
            RectTransform dialogueRect = dialogueUi.GetComponent<RectTransform>();
            dialogueRect.SetParent(root.transform, false);
            dialogueRect.anchorMin = Vector2.zero;
            dialogueRect.anchorMax = Vector2.one;
            dialogueRect.offsetMin = Vector2.zero;
            dialogueRect.offsetMax = Vector2.zero;

            GameObject left = (GameObject)PrefabUtility.InstantiatePrefab(leftSlotPrefab);
            left.transform.SetParent(dialogueUi.transform, false);
            GameObject right = (GameObject)PrefabUtility.InstantiatePrefab(rightSlotPrefab);
            right.transform.SetParent(dialogueUi.transform, false);

            DialogueController controller = dialogueUi.GetComponent<DialogueController>();
            SetReference(controller, "characterCatalog", catalog);
            SetReference(controller, "dialogueGroup", dialogueUi.GetComponent<CanvasGroup>());
            SetReference(controller, "leftSlot", left.GetComponent<DialogueCharacterSlotView>());
            SetReference(controller, "rightSlot", right.GetComponent<DialogueCharacterSlotView>());
            SetReference(root.GetComponent<GameplayUIRoot>(), "dialogue", controller);

            PrefabUtility.SaveAsPrefabAsset(root, GameplayRootPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static Sprite LoadFirstSprite(string assetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().FirstOrDefault();
        }

        private static void SetReference(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }
    }
}
