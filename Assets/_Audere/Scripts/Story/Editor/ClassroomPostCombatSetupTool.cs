using System;
using System.Collections.Generic;
using Audere.Audio;
using Audere.Core;
using Audere.Dialogue;
using Audere.Story;
using Audere.Story.Presentation;
using Audere.Story.Steps;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Audere.EditorTools
{
    public static class ClassroomPostCombatSetupTool
    {
        private const string ClassroomScenePath = "Assets/_Audere/Scenes/30_Classroom.unity";
        private const string EveningScenePath = "Assets/_Audere/Scenes/40_Evening.unity";
        private const string DialogueFolder = "Assets/_Audere/Data/Dialogue/Day1/Classroom/PostCombat";
        private const string OverlayFolder = "Assets/_Audere/Prefabs/Story/Overlays";
        private const string SheetPrefabPath = OverlayFolder + "/RegistrationSheet_PLACEHOLDER.prefab";
        private const string OverlayPrefabPath = OverlayFolder + "/StoryRegistrationOverlay.prefab";
        private const string GrassTilePrefabPath = "Assets/_Audere/Prefabs/Puzzle/Tiles/Grass.prefab";
        private const string AudioCatalogPath = "Assets/_Audere/Data/Audio/AudioCatalog.asset";
        private const string SchoolBellPath = "Assets/_Audere/Audio/SchoolBell.mp3";
        private const string CaptionFontPath =
            "Assets/_Audere/AssetGame/Font/Mynerve-Regular SDF.asset";

        [MenuItem("Audere/Story/Setup Classroom Post Combat")]
        public static void Setup()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[ClassroomPostCombatSetup] Stop Play Mode before authoring.");
                return;
            }

            Scene classroomScene = SceneManager.GetActiveScene();
            if (classroomScene.path != ClassroomScenePath)
            {
                Debug.LogError($"[ClassroomPostCombatSetup] Open '{ClassroomScenePath}' first.");
                return;
            }

            EnsureFolder(DialogueFolder);
            EnsureFolder(OverlayFolder);
            ConfigureSchoolBellAudio();

            GameObject overlayPrefab = EnsureOverlayPrefab();
            DialogueAssets dialogue = CreateDialogueAssets();
            SetupClassroomScene(classroomScene, overlayPrefab, dialogue);
            SetupEveningScene(classroomScene);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(classroomScene);
            EditorSceneManager.SaveScene(classroomScene);
            Debug.Log(
                "[ClassroomPostCombatSetup] Victory follow-up, registration overlay, Bianca departure and Evening hand-off are ready.");
        }

        private static DialogueAssets CreateDialogueAssets()
        {
            return new DialogueAssets
            {
                Response = ConfigureDialogue(
                    "Dialogue_D1_CLASSROOM_POST_COMBAT_RESPONSE",
                    "d1-classroom-post-combat-response",
                    DialogueCharacterId.Bianca,
                    new DialogueLine(DialogueSpeakerSide.Right, "Audere?"),
                    new DialogueLine(DialogueSpeakerSide.Left, "Tớ…"),
                    new DialogueLine(DialogueSpeakerSide.Left, "Tớ muốn thử."),
                    new DialogueLine(DialogueSpeakerSide.Right, "Phần làm bảng ấy à?"),
                    new DialogueLine(DialogueSpeakerSide.Left, "…Ừ.")),
                Acceptance = ConfigureDialogue(
                    "Dialogue_D1_CLASSROOM_POST_COMBAT_ACCEPTANCE",
                    "d1-classroom-post-combat-acceptance",
                    DialogueCharacterId.Bianca,
                    new DialogueLine(DialogueSpeakerSide.Right, "Được."),
                    new DialogueLine(DialogueSpeakerSide.Right, "Vậy làm cùng tớ nhé."),
                    new DialogueLine(DialogueSpeakerSide.Left, "Ừ.")),
                Signup = ConfigureDialogue(
                    "Dialogue_D1_CLASSROOM_POST_COMBAT_SIGNUP",
                    "d1-classroom-post-combat-signup",
                    DialogueCharacterId.Bianca,
                    new DialogueLine(DialogueSpeakerSide.Right, "Tớ ghi tên cậu vào đây nhé?"),
                    new DialogueLine(DialogueSpeakerSide.Left, "Để tớ ghi."),
                    new DialogueLine(DialogueSpeakerSide.Right, "Ừ.")),
                AfterDeparture = ConfigureDialogue(
                    "Dialogue_D1_CLASSROOM_AFTER_BIANCA_DEPARTURE",
                    "d1-classroom-after-bianca-departure",
                    DialogueCharacterId.Timor,
                    new DialogueLine(DialogueSpeakerSide.Left, "Tay tớ vẫn run."),
                    new DialogueLine(DialogueSpeakerSide.Right, "Tớ biết."),
                    new DialogueLine(DialogueSpeakerSide.Left, "Nhưng tớ đã nói được."),
                    new DialogueLine(DialogueSpeakerSide.Right, "Ừ."),
                    new DialogueLine(DialogueSpeakerSide.Left, "Timor."),
                    new DialogueLine(DialogueSpeakerSide.Right, "Ừ?"),
                    new DialogueLine(DialogueSpeakerSide.Left, "Cảm ơn vì đã ở đó."),
                    new DialogueLine(DialogueSpeakerSide.Right, "Tớ vẫn luôn ở đây mà.")),
            };
        }

        private static DialogueData ConfigureDialogue(
            string assetName,
            string dialogueId,
            DialogueCharacterId counterpart,
            params DialogueLine[] lines)
        {
            string path = $"{DialogueFolder}/{assetName}.asset";
            DialogueData asset = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DialogueData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("dialogueId").stringValue = dialogueId;
            serialized.FindProperty("leftCharacter").enumValueIndex =
                (int)DialogueCharacterId.Audere;
            serialized.FindProperty("rightCharacter").enumValueIndex =
                (int)counterpart;

            SerializedProperty serializedLines = serialized.FindProperty("lines");
            serializedLines.arraySize = lines.Length;
            for (int index = 0; index < lines.Length; index++)
            {
                SerializedProperty line = serializedLines.GetArrayElementAtIndex(index);
                line.FindPropertyRelative("speaker").enumValueIndex = (int)lines[index].Side;
                line.FindPropertyRelative("text").stringValue = lines[index].Text;
                if (lines[index].Text.Length > 42)
                {
                    Debug.LogWarning(
                        $"[ClassroomPostCombatSetup] '{assetName}' line {index + 1} exceeds 42 characters.",
                        asset);
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void SetupClassroomScene(
            Scene scene,
            GameObject overlayPrefab,
            DialogueAssets dialogue)
        {
            GameObject classroom = RequireRoot(scene, "CLASSROOM");
            GameObject storyRoot = RequireRoot(scene, "STORY");
            GameObject transitionOverlay = RequireRoot(scene, "Scene Transition Overlay");
            Transform classroomArt = RequireChild(classroom.transform, "CLASSROOM ART PLACEHOLDER");
            Transform actors = RequireChild(classroomArt, "Actors");
            Transform board = RequireChild(classroomArt, "Board");
            Transform staging = RequireChild(classroom.transform, "STAGING TARGETS");
            Transform bianca = RequireChild(actors, "Bianca_PLACEHOLDER");
            SpriteRenderer biancaRenderer = bianca.GetComponent<SpriteRenderer>();
            if (biancaRenderer == null)
                throw new MissingReferenceException("Bianca_PLACEHOLDER needs a root SpriteRenderer.");

            StoryEvent recessEvent = RequireChild(storyRoot.transform, "D1_CLASSROOM_RECESS_BIANCA")
                .GetComponent<StoryEvent>();
            if (recessEvent == null)
                throw new MissingReferenceException("D1_CLASSROOM_RECESS_BIANCA needs StoryEvent.");

            Transform decorationTile = RequireChild(board, "Tile_DecorationInterest");
            Transform departureTile1 = RequireChild(board, "Tile_BiancaMid");
            Transform departureTile2 = RequireChild(board, "Tile_BiancaStart");
            GameObject tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GrassTilePrefabPath);
            if (tilePrefab == null)
                throw new MissingReferenceException($"Missing tile prefab '{GrassTilePrefabPath}'.");

            GameObject departureTile3 = EnsurePrefabChild(
                board,
                "Tile_BiancaDeparture3",
                tilePrefab);
            departureTile3.transform.localPosition = new Vector3(2.4f, departureTile2.localPosition.y, 0f);
            departureTile3.transform.localRotation = Quaternion.identity;
            departureTile3.transform.localScale = Vector3.one;
            departureTile3.SetActive(false);
            MatchTileColor(departureTile3, departureTile2.gameObject);

            float actorBaseline = RequireChild(staging, "Bianca_EndPose").localPosition.y;
            Transform settlePose = EnsureAnchor(
                staging,
                "Bianca_PostCombatSettlePose",
                decorationTile.localPosition.x,
                actorBaseline);
            Transform departurePose1 = EnsureAnchor(
                staging,
                "Bianca_Departure1Pose",
                departureTile1.localPosition.x,
                actorBaseline);
            Transform departurePose2 = EnsureAnchor(
                staging,
                "Bianca_Departure2Pose",
                departureTile2.localPosition.x,
                actorBaseline);
            Transform departurePose3 = EnsureAnchor(
                staging,
                "Bianca_Departure3Pose",
                departureTile3.transform.localPosition.x,
                actorBaseline);

            StoryIllustrationOverlayView overlayView = EnsureSceneOverlay(scene, overlayPrefab);
            CanvasGroup fade = RequireChild(transitionOverlay.transform, "Fade")
                .GetComponent<CanvasGroup>();
            if (fade == null)
                throw new MissingReferenceException("Scene Transition Overlay/Fade needs CanvasGroup.");

            ConfigureCombatVictoryGate(recessEvent);
            RemovePostCombatSteps(recessEvent.transform);
            EnsureVisibilityResetStep(recessEvent, bianca);

            ConfigureWait(CreateStep<WaitStep>(recessEvent, "230_HoldAfterCombat"), .28f);
            ConfigureDialogue(CreateStep<DialogueStep>(recessEvent, "240_AudereAnswersBianca"), dialogue.Response);
            ConfigureMove(
                CreateStep<MoveActorStep>(recessEvent, "250_BiancaSettlesRight"),
                bianca,
                settlePose,
                .18f);
            ConfigureDialogue(CreateStep<DialogueStep>(recessEvent, "260_BiancaAccepts"), dialogue.Acceptance);
            ConfigureDialogue(CreateStep<DialogueStep>(recessEvent, "270_SignupExchange"), dialogue.Signup);
            ConfigureIllustration(
                CreateStep<StoryIllustrationStep>(recessEvent, "280_ShowRegistrationSheet"),
                overlayView);
            ConfigureFacing(
                CreateStep<SetActorFacingStep>(recessEvent, "290_BiancaTurnsAway"),
                biancaRenderer,
                true);

            ConfigureBoardTransition(
                CreateStep<BoardTileTransitionStep>(recessEvent, "300_RevealDepartureTile1"),
                Array.Empty<Transform>(),
                new[] { departureTile1 });
            ConfigureCharacterMotion(
                CreateStep<CharacterMotionStep>(recessEvent, "310_BiancaHopsDeparture1"),
                bianca,
                departurePose1,
                biancaRenderer);
            ConfigureBoardTransition(
                CreateStep<BoardTileTransitionStep>(recessEvent, "320_HideDecorationTile"),
                new[] { decorationTile },
                Array.Empty<Transform>());

            ConfigureBoardTransition(
                CreateStep<BoardTileTransitionStep>(recessEvent, "330_RevealDepartureTile2"),
                Array.Empty<Transform>(),
                new[] { departureTile2 });
            ConfigureCharacterMotion(
                CreateStep<CharacterMotionStep>(recessEvent, "340_BiancaHopsDeparture2"),
                bianca,
                departurePose2,
                biancaRenderer);
            ConfigureBoardTransition(
                CreateStep<BoardTileTransitionStep>(recessEvent, "350_HideDepartureTile1"),
                new[] { departureTile1 },
                Array.Empty<Transform>());

            ConfigureBoardTransition(
                CreateStep<BoardTileTransitionStep>(recessEvent, "360_RevealDepartureTile3"),
                Array.Empty<Transform>(),
                new[] { departureTile3.transform });
            ConfigureCharacterMotion(
                CreateStep<CharacterMotionStep>(recessEvent, "370_BiancaHopsDeparture3"),
                bianca,
                departurePose3,
                biancaRenderer);
            ConfigureBoardTransition(
                CreateStep<BoardTileTransitionStep>(recessEvent, "380_HideDepartureTile2"),
                new[] { departureTile2 },
                Array.Empty<Transform>());
            ConfigureSpriteFade(
                CreateStep<SpriteGroupFadeStep>(recessEvent, "390_BiancaFadesOut"),
                bianca,
                0f,
                .34f);
            ConfigureBoardTransition(
                CreateStep<BoardTileTransitionStep>(recessEvent, "400_HideDepartureTile3"),
                new[] { departureTile3.transform },
                Array.Empty<Transform>());

            ConfigureDialogue(
                CreateStep<DialogueStep>(recessEvent, "410_AudereThanksTimor"),
                dialogue.AfterDeparture);
            ConfigureAudio(
                CreateStep<PlayAudioStep>(recessEvent, "420_PlaySchoolBell"),
                AudioId.School_Bell);
            ConfigureCanvasFade(
                CreateStep<CanvasFadeStep>(recessEvent, "430_FadeToEvening"),
                fade,
                1f,
                .85f);
            ConfigureSceneLoad(
                CreateStep<SceneLoadStep>(recessEvent, "440_LoadEvening"),
                GameScenes.Evening);

            EditorUtility.SetDirty(recessEvent);
        }

        private static void ConfigureCombatVictoryGate(StoryEvent storyEvent)
        {
            Transform combatStepTransform = FindDirectChild(storyEvent.transform, "210_PlayKhoangLangPrototype")
                ?? FindDirectChild(storyEvent.transform, "210_PlayCombatPrototype");
            CombatStep combatStep = combatStepTransform != null
                ? combatStepTransform.GetComponent<CombatStep>()
                : null;
            if (combatStep == null)
                throw new MissingReferenceException("The production 210 CombatStep is required.");

            combatStepTransform.name = "210_PlayKhoangLangPrototype";
            SerializedObject serialized = new SerializedObject(combatStep);
            serialized.FindProperty("victoryBehaviour").enumValueIndex =
                (int)CombatResultBehaviour.Complete;
            serialized.FindProperty("defeatBehaviour").enumValueIndex =
                (int)CombatResultBehaviour.Retry;
            serialized.FindProperty("specialBehaviour").enumValueIndex =
                (int)CombatResultBehaviour.Complete;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureVisibilityResetStep(StoryEvent storyEvent, Transform bianca)
        {
            Transform existing = FindDirectChild(storyEvent.transform, "06_ResetBiancaVisibility");
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing.gameObject);

            SpriteGroupFadeStep reset = CreateStep<SpriteGroupFadeStep>(
                storyEvent,
                "06_ResetBiancaVisibility");
            ConfigureSpriteFade(reset, bianca, 1f, 0f);

            Transform normalize = FindDirectChild(storyEvent.transform, "05_NormalizeRecess");
            int targetIndex = normalize != null ? normalize.GetSiblingIndex() + 1 : 0;
            reset.transform.SetSiblingIndex(targetIndex);
        }

        private static void RemovePostCombatSteps(Transform eventTransform)
        {
            for (int index = eventTransform.childCount - 1; index >= 0; index--)
            {
                Transform child = eventTransform.GetChild(index);
                string prefix = child.name.Split('_')[0];
                if (int.TryParse(prefix, out int number) && number >= 230)
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static StoryIllustrationOverlayView EnsureSceneOverlay(
            Scene scene,
            GameObject prefab)
        {
            GameObject existing = FindRoot(scene, "Story Registration Overlay");
            if (existing != null && PrefabUtility.GetCorrespondingObjectFromSource(existing) != prefab)
            {
                UnityEngine.Object.DestroyImmediate(existing);
                existing = null;
            }

            if (existing == null)
            {
                existing = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (existing == null)
                    existing = UnityEngine.Object.Instantiate(prefab);
                existing.name = "Story Registration Overlay";
                SceneManager.MoveGameObjectToScene(existing, scene);
            }

            existing.SetActive(true);
            StoryIllustrationOverlayView view = existing.GetComponent<StoryIllustrationOverlayView>();
            if (view == null)
                throw new MissingReferenceException("Story Registration Overlay needs its view component.");
            return view;
        }

        private static GameObject EnsureOverlayPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
            if (existing != null)
                return existing;

            GameObject sheetPrefab = EnsureRegistrationSheetPrefab();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CaptionFontPath);

            GameObject root = new GameObject(
                "StoryRegistrationOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup),
                typeof(StoryIllustrationOverlayView));
            try
            {
                RectTransform rootRect = root.GetComponent<RectTransform>();
                Stretch(rootRect);
                Canvas canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 1100;
                CanvasScaler scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = .5f;

                GameObject blocker = new GameObject(
                    "Fullscreen Dismiss Blocker",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));
                RectTransform blockerRect = blocker.GetComponent<RectTransform>();
                blockerRect.SetParent(rootRect, false);
                Stretch(blockerRect);
                Image blockerImage = blocker.GetComponent<Image>();
                blockerImage.color = new Color(0f, 0f, 0f, .72f);
                blockerImage.raycastTarget = true;
                Button button = blocker.GetComponent<Button>();
                button.transition = Selectable.Transition.None;

                GameObject sheet = PrefabUtility.InstantiatePrefab(sheetPrefab, blockerRect) as GameObject;
                if (sheet == null)
                    sheet = UnityEngine.Object.Instantiate(sheetPrefab, blockerRect);
                sheet.name = "RegistrationSheet_PLACEHOLDER";
                RectTransform sheetRect = sheet.GetComponent<RectTransform>();
                sheetRect.anchorMin = sheetRect.anchorMax = new Vector2(.5f, .5f);
                sheetRect.pivot = new Vector2(.5f, .5f);
                sheetRect.anchoredPosition = new Vector2(0f, 35f);
                sheetRect.sizeDelta = new Vector2(820f, 500f);
                Image sheetImage = sheet.GetComponent<Image>();
                if (sheetImage != null)
                    sheetImage.raycastTarget = false;

                GameObject captionObject = new GameObject(
                    "Caption",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI));
                RectTransform captionRect = captionObject.GetComponent<RectTransform>();
                captionRect.SetParent(blockerRect, false);
                captionRect.anchorMin = captionRect.anchorMax = new Vector2(.5f, .5f);
                captionRect.pivot = new Vector2(.5f, .5f);
                captionRect.anchoredPosition = new Vector2(0f, -285f);
                captionRect.sizeDelta = new Vector2(1200f, 80f);
                TextMeshProUGUI caption = captionObject.GetComponent<TextMeshProUGUI>();
                caption.text = "Phiếu đăng ký hoàn thành";
                caption.font = font != null ? font : TMP_Settings.defaultFontAsset;
                caption.fontSize = 48f;
                caption.alignment = TextAlignmentOptions.Center;
                caption.color = Color.white;
                caption.raycastTarget = false;

                SerializedObject view = new SerializedObject(
                    root.GetComponent<StoryIllustrationOverlayView>());
                view.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
                view.FindProperty("dismissButton").objectReferenceValue = button;
                view.FindProperty("caption").objectReferenceValue = caption;
                view.ApplyModifiedPropertiesWithoutUndo();

                CanvasGroup group = root.GetComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
                return PrefabUtility.SaveAsPrefabAsset(root, OverlayPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject EnsureRegistrationSheetPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(SheetPrefabPath);
            if (existing != null)
                return existing;

            GameObject sheet = new GameObject(
                "RegistrationSheet_PLACEHOLDER",
                typeof(RectTransform),
                typeof(Image),
                typeof(Outline));
            try
            {
                RectTransform rect = sheet.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(820f, 500f);
                Image image = sheet.GetComponent<Image>();
                image.color = new Color(.97f, .965f, .98f, 1f);
                image.raycastTarget = false;
                Outline outline = sheet.GetComponent<Outline>();
                outline.effectColor = new Color(.58f, .54f, .62f, .8f);
                outline.effectDistance = new Vector2(3f, -3f);
                return PrefabUtility.SaveAsPrefabAsset(sheet, SheetPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static void ConfigureSchoolBellAudio()
        {
            AudioCatalog catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(AudioCatalogPath);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SchoolBellPath);
            if (catalog == null || clip == null)
                throw new MissingReferenceException("SchoolBell clip and AudioCatalog are required.");

            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty entries = serialized.FindProperty("entries");
            for (int index = entries.arraySize - 1; index >= 0; index--)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("clip").objectReferenceValue == clip &&
                    entry.FindPropertyRelative("id").intValue != (int)AudioId.School_Bell)
                {
                    entries.DeleteArrayElementAtIndex(index);
                }
            }

            SerializedProperty target = null;
            for (int index = 0; index < entries.arraySize; index++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("id").intValue == (int)AudioId.School_Bell)
                {
                    target = entry;
                    break;
                }
            }

            if (target == null)
            {
                entries.arraySize++;
                target = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            }

            target.FindPropertyRelative("id").intValue = (int)AudioId.School_Bell;
            target.FindPropertyRelative("clip").objectReferenceValue = clip;
            target.FindPropertyRelative("volume").floatValue = .72f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void SetupEveningScene(Scene classroomScene)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(EveningScenePath) != null)
            {
                EnsureSceneInBuildSettings(EveningScenePath);
                return;
            }

            Scene evening = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(evening);

            GameObject cameraObject = new GameObject(
                "Main Camera",
                typeof(Camera),
                typeof(UniversalAdditionalCameraData));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.035f, .023f, .055f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            GameObject lightObject = new GameObject("Directional Light", typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = .5f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            new GameObject("EVENING_PLACEHOLDER_NO_ART");
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            GameObject overlay = new GameObject(
                "Scene Transition Overlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas overlayCanvas = overlay.GetComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 1000;
            CanvasScaler overlayScaler = overlay.GetComponent<CanvasScaler>();
            overlayScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            overlayScaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject fadeObject = new GameObject(
                "Fade",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup));
            RectTransform fadeRect = fadeObject.GetComponent<RectTransform>();
            fadeRect.SetParent(overlay.transform, false);
            Stretch(fadeRect);
            fadeObject.GetComponent<Image>().color = Color.black;
            CanvasGroup fade = fadeObject.GetComponent<CanvasGroup>();
            fade.alpha = 1f;
            fade.blocksRaycasts = true;
            fade.interactable = false;

            GameObject storyRoot = new GameObject("STORY", typeof(StoryDirector));
            GameObject eventObject = new GameObject("D1_EVENING_PLACEHOLDER", typeof(StoryEvent));
            eventObject.transform.SetParent(storyRoot.transform, false);
            StoryEvent storyEvent = eventObject.GetComponent<StoryEvent>();
            SerializedObject eventSerialized = new SerializedObject(storyEvent);
            eventSerialized.FindProperty("eventId").stringValue = "D1_EVENING_PLACEHOLDER";
            eventSerialized.FindProperty("autoPlayNextEvent").boolValue = false;
            eventSerialized.FindProperty("nextEvent").objectReferenceValue = null;
            eventSerialized.ApplyModifiedPropertiesWithoutUndo();

            ConfigureCanvasFade(
                CreateStep<CanvasFadeStep>(storyEvent, "00_FadeIn"),
                fade,
                0f,
                .65f);
            ConfigureWait(CreateStep<WaitStep>(storyEvent, "10_Hold"), .15f);

            SerializedObject director = new SerializedObject(storyRoot.GetComponent<StoryDirector>());
            director.FindProperty("storyEventsRoot").objectReferenceValue = storyRoot.transform;
            director.FindProperty("playOnStart").boolValue = true;
            director.FindProperty("startingEvent").objectReferenceValue = storyEvent;
            director.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(evening, EveningScenePath);
            EnsureSceneInBuildSettings(EveningScenePath);
            SceneManager.SetActiveScene(classroomScene);
            EditorSceneManager.CloseScene(evening, true);
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            for (int index = 0; index < current.Length; index++)
            {
                if (current[index].path == scenePath)
                    return;
            }

            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(current)
            {
                new EditorBuildSettingsScene(scenePath, true),
            };
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ConfigureDialogue(DialogueStep step, DialogueData data)
        {
            SerializedObject serialized = new SerializedObject(step);
            serialized.FindProperty("dialogueData").objectReferenceValue = data;
            serialized.FindProperty("dialogueController").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureIllustration(
            StoryIllustrationStep step,
            StoryIllustrationOverlayView view)
        {
            SerializedObject serialized = new SerializedObject(step);
            serialized.FindProperty("overlayView").objectReferenceValue = view;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureMove(
            MoveActorStep step,
            Transform actor,
            Transform target,
            float duration)
        {
            SerializedObject serialized = new SerializedObject(step);
            serialized.FindProperty("actor").objectReferenceValue = actor;
            serialized.FindProperty("targetTransform").objectReferenceValue = target;
            serialized.FindProperty("duration").floatValue = duration;
            serialized.FindProperty("useUnscaledTime").boolValue = true;
            serialized.FindProperty("snapOnComplete").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureFacing(
            SetActorFacingStep step,
            SpriteRenderer renderer,
            bool faceRight)
        {
            SerializedObject serialized = new SerializedObject(step);
            serialized.FindProperty("actorRenderer").objectReferenceValue = renderer;
            serialized.FindProperty("faceRight").boolValue = faceRight;
            serialized.FindProperty("sourceSpriteFacesLeft").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureCharacterMotion(
            CharacterMotionStep step,
            Transform actor,
            Transform target,
            SpriteRenderer renderer)
        {
            SerializedObject serialized = new SerializedObject(step);
            serialized.FindProperty("actor").objectReferenceValue = actor;
            serialized.FindProperty("targetTransform").objectReferenceValue = target;
            serialized.FindProperty("actorRenderer").objectReferenceValue = renderer;
            serialized.FindProperty("groundedShadow").objectReferenceValue = FindGroundedShadow(actor);
            serialized.FindProperty("motionMode").enumValueIndex = (int)CharacterMotionMode.TravelToTarget;
            serialized.FindProperty("duration").floatValue = .32f;
            serialized.FindProperty("arcHeight").floatValue = .075f;
            serialized.FindProperty("travelStretch").floatValue = .065f;
            serialized.FindProperty("landingDuration").floatValue = .1f;
            serialized.FindProperty("landingSquash").floatValue = .105f;
            serialized.FindProperty("landingWiden").floatValue = .075f;
            serialized.FindProperty("useUnscaledTime").boolValue = true;
            serialized.FindProperty("facingMode").enumValueIndex =
                (int)CharacterFacingMode.FollowHorizontalTravel;
            serialized.FindProperty("sourceSpriteFacesLeft").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSpriteFade(
            SpriteGroupFadeStep step,
            Transform actor,
            float targetVisibility,
            float duration)
        {
            SpriteRenderer[] renderers = actor.GetComponentsInChildren<SpriteRenderer>(true);
            SerializedObject serialized = new SerializedObject(step);
            SerializedProperty rendererArray = serialized.FindProperty("renderers");
            SerializedProperty authoredAlphas = serialized.FindProperty("authoredAlphas");
            rendererArray.arraySize = renderers.Length;
            authoredAlphas.arraySize = renderers.Length;
            for (int index = 0; index < renderers.Length; index++)
            {
                rendererArray.GetArrayElementAtIndex(index).objectReferenceValue = renderers[index];
                authoredAlphas.GetArrayElementAtIndex(index).floatValue = renderers[index].color.a;
            }
            serialized.FindProperty("targetVisibility").floatValue = targetVisibility;
            serialized.FindProperty("duration").floatValue = duration;
            serialized.FindProperty("useUnscaledTime").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureBoardTransition(
            BoardTileTransitionStep step,
            Transform[] hide,
            Transform[] reveal)
        {
            SerializedObject serialized = new SerializedObject(step);
            SetObjectArray(serialized.FindProperty("objectsToHide"), hide);
            SetObjectArray(serialized.FindProperty("objectsToReveal"), reveal);
            serialized.FindProperty("autoCollectHideTiles").boolValue = false;
            serialized.FindProperty("autoCollectRevealTiles").boolValue = false;
            serialized.FindProperty("transitionDuration").floatValue = .22f;
            serialized.FindProperty("staggerDelay").floatValue = 0f;
            serialized.FindProperty("revealWaveDuration").floatValue = .22f;
            serialized.FindProperty("verticalOffset").floatValue = .065f;
            serialized.FindProperty("revealOvershoot").floatValue = .012f;
            serialized.FindProperty("useUnscaledTime").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureAudio(PlayAudioStep step, AudioId id)
        {
            SerializedObject serialized = new SerializedObject(step);
            serialized.FindProperty("audioId").intValue = (int)id;
            serialized.FindProperty("failIfAudioServiceMissing").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureCanvasFade(
            CanvasFadeStep step,
            CanvasGroup group,
            float alpha,
            float duration)
        {
            SerializedObject serialized = new SerializedObject(step);
            serialized.FindProperty("canvasGroup").objectReferenceValue = group;
            serialized.FindProperty("targetAlpha").floatValue = alpha;
            serialized.FindProperty("duration").floatValue = duration;
            serialized.FindProperty("useUnscaledTime").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSceneLoad(SceneLoadStep step, string sceneName)
        {
            SerializedObject serialized = new SerializedObject(step);
            serialized.FindProperty("sceneName").stringValue = sceneName;
            serialized.FindProperty("hidePuzzleUiBeforeLoad").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureWait(WaitStep step, float duration)
        {
            SerializedObject serialized = new SerializedObject(step);
            serialized.FindProperty("duration").floatValue = duration;
            serialized.FindProperty("useUnscaledTime").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T CreateStep<T>(StoryEvent storyEvent, string name) where T : StoryStep
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(storyEvent.transform, false);
            return child.AddComponent<T>();
        }

        private static Transform FindGroundedShadow(Transform actor)
        {
            Transform[] descendants = actor.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < descendants.Length; index++)
            {
                Transform candidate = descendants[index];
                if (candidate != actor &&
                    candidate.name.StartsWith("shadow", StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }
            throw new MissingReferenceException("Bianca needs a directly authored grounded shadow.");
        }

        private static GameObject EnsurePrefabChild(Transform parent, string name, GameObject prefab)
        {
            Transform existing = FindDirectChild(parent, name);
            if (existing != null)
            {
                if (PrefabUtility.GetCorrespondingObjectFromSource(existing.gameObject) == prefab)
                    return existing.gameObject;
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
                instance = UnityEngine.Object.Instantiate(prefab, parent);
            instance.name = name;
            return instance;
        }

        private static Transform EnsureAnchor(
            Transform parent,
            string name,
            float localX,
            float localY)
        {
            Transform anchor = FindDirectChild(parent, name);
            if (anchor == null)
            {
                anchor = new GameObject(name).transform;
                anchor.SetParent(parent, false);
            }
            anchor.localPosition = new Vector3(localX, localY, -1f);
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.one;
            return anchor;
        }

        private static void MatchTileColor(GameObject target, GameObject source)
        {
            SpriteRenderer[] targetRenderers = target.GetComponentsInChildren<SpriteRenderer>(true);
            SpriteRenderer[] sourceRenderers = source.GetComponentsInChildren<SpriteRenderer>(true);
            int count = Mathf.Min(targetRenderers.Length, sourceRenderers.Length);
            for (int index = 0; index < count; index++)
                targetRenderers[index].color = sourceRenderers[index].color;
        }

        private static void SetObjectArray<T>(SerializedProperty property, T[] values)
            where T : UnityEngine.Object
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            GameObject result = FindRoot(scene, name);
            if (result == null)
                throw new MissingReferenceException($"Scene '{scene.name}' is missing root '{name}'.");
            return result;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name)
                    return root;
            return null;
        }

        private static Transform RequireChild(Transform parent, string name)
        {
            Transform child = FindDirectChild(parent, name);
            if (child == null)
                throw new MissingReferenceException($"Missing '{name}' under '{parent.name}'.");
            return child;
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (child.name == name)
                    return child;
            }
            return null;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private readonly struct DialogueLine
        {
            public readonly DialogueSpeakerSide Side;
            public readonly string Text;

            public DialogueLine(DialogueSpeakerSide side, string text)
            {
                Side = side;
                Text = text;
            }
        }

        private struct DialogueAssets
        {
            public DialogueData Response;
            public DialogueData Acceptance;
            public DialogueData Signup;
            public DialogueData AfterDeparture;
        }
    }
}
