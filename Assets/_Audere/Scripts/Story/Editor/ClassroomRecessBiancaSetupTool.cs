using System;
using Audere.Combat;
using Audere.Dialogue;
using Audere.Puzzle;
using Audere.Story;
using Audere.Story.Steps;
using Audere.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Audere.EditorTools
{
    public static class ClassroomRecessBiancaSetupTool
    {
        private const string ScenePath = "Assets/_Audere/Scenes/30_Classroom.unity";
        private const string PlayerPrefabPath = "Assets/_Audere/Prefabs/Puzzle/Actors/Player.prefab";
        private const string BiancaPrefabPath = "Assets/_Audere/Prefabs/Story/Characters/Bianca.prefab";
        private const string GrassTilePrefabPath = "Assets/_Audere/Prefabs/Puzzle/Tiles/Grass.prefab";
        private const string CombatBoardPrefabPath =
            "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab";
        private const string ClassroomPrototypeEncounterPath =
            "Assets/_Audere/Data/Combat/CombatEncounter_D1_CLASSROOM_KHOANG_LANG.asset";
        private const string ClassroomEnemyDefinitionPath =
            "Assets/_Audere/Data/Combat/Enemies/Enemy_KhoangLang.asset";
        private const string Renderer2DPath = "Assets/Settings/Renderer2D.asset";
        private const string FullscreenTransitionShaderPath =
            "Assets/_Audere/Shaders/FullscreenDreamyDisorientation.shader";
        private const string FullscreenTransitionMaterialPath =
            "Assets/_Audere/Materials/PostProcess/FullscreenDreamyDisorientation.mat";
        private const string FullscreenTransitionProfilePath =
            "Assets/_Audere/Data/Transitions/WorldTransition_DreamyDisorientation.asset";
        private const string FullscreenTransitionFeatureName =
            "Audere Fullscreen World Transition";
        private const string LegacyFullscreenTransitionFeatureName =
            "Audere Fullscreen Combat Transition";
        private const string CharacterCatalogPath =
            "Assets/_Audere/Data/Dialogue/DialogueCharacterCatalog.asset";
        private const string DialogueFolder = "Assets/_Audere/Data/Dialogue/Day1/Classroom";
        private const int RecommendedDialogueCharacters = 42;

        [MenuItem("Audere/Story/Setup Classroom Recess Bianca")]
        public static void Setup()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[ClassroomRecessSetup] Stop Play Mode before authoring the scene.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                Debug.LogError($"[ClassroomRecessSetup] Open '{ScenePath}' before running setup.");
                return;
            }

            EnsureFolder("Assets/_Audere/Prefabs/Story/Characters");
            EnsureFolder(DialogueFolder);

            FullscreenTransitionAssets transitionAssets = EnsureFullscreenTransitionAssets();
            CreateOrUpdateBiancaPrefab();
            ConfigureCharacterCatalog();
            PolishClassroomAnnouncementDialogue();
            DialogueAssets dialogue = CreateDialogueAssets();
            SetupScene(scene, dialogue, transitionAssets);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(
                "[ClassroomRecessSetup] Bianca recess story and the non-canon classroom combat prototype hand-off are ready.");
        }

        [MenuItem("Audere/Story/Setup Classroom Combat Transition")]
        public static void SetupCombatTransitionOnly()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[ClassroomCombatTransitionSetup] Stop Play Mode before authoring the scene.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                Debug.LogError($"[ClassroomCombatTransitionSetup] Open '{ScenePath}' before running setup.");
                return;
            }

            FullscreenTransitionAssets transitionAssets = EnsureFullscreenTransitionAssets();
            GameObject classroom = FindRoot(scene, "CLASSROOM");
            GameObject storyRoot = FindRoot(scene, "STORY");
            if (classroom == null || storyRoot == null)
                throw new MissingReferenceException("Classroom transition setup requires CLASSROOM and STORY.");

            CombatRuntime combat = SetupCombatRuntime(scene, classroom, transitionAssets);
            StoryEvent recessEvent = RequireChild(storyRoot.transform, "D1_CLASSROOM_RECESS_BIANCA")
                .GetComponent<StoryEvent>();
            SpriteRenderer audereRenderer = RequireChild(
                    RequireChild(
                        RequireChild(classroom.transform, "CLASSROOM ART PLACEHOLDER"),
                        "Actors"),
                    "Audere")
                .GetComponent<SpriteRenderer>();
            if (recessEvent == null || audereRenderer == null)
                throw new MissingReferenceException("Recess event and Audere renderer are required.");

            Transform stepTransform = FindDirectChild(recessEvent.transform, "200_ClassroomIsConsumed")
                ?? FindDirectChild(recessEvent.transform, "200_EnterCombatPrototype");
            if (stepTransform == null)
                throw new MissingReferenceException("Missing the authored 200 combat-entry step.");

            stepTransform.name = "200_ClassroomIsConsumed";
            foreach (StoryStep existingStep in stepTransform.GetComponents<StoryStep>())
                UnityEngine.Object.DestroyImmediate(existingStep);
            FullscreenWorldModeTransitionStep transitionStep =
                stepTransform.gameObject.AddComponent<FullscreenWorldModeTransitionStep>();
            ConfigureFullscreenTransition(
                transitionStep,
                combat.TransitionController,
                combat.ModeController,
                transitionAssets.Profile,
                audereRenderer);

            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(recessEvent);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(
                "[ClassroomCombatTransitionSetup] Fullscreen Story-to-Combat transition is authored; dialogue assets were not changed.");
        }

        private static void CreateOrUpdateBiancaPrefab()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (source == null)
                throw new MissingReferenceException($"Missing Player prefab at '{PlayerPrefabPath}'.");

            GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                contents.name = "Bianca_PLACEHOLDER";
                GridPlayer gridPlayer = contents.GetComponent<GridPlayer>();
                if (gridPlayer != null)
                    UnityEngine.Object.DestroyImmediate(gridPlayer, true);

                contents.transform.localScale = Vector3.one * 1.5f;
                SpriteRenderer renderer = contents.GetComponent<SpriteRenderer>();
                if (renderer == null)
                    throw new MissingReferenceException("The Player prefab needs a root SpriteRenderer.");

                renderer.color = new Color(.88f, .70f, .78f, 1f);
                renderer.flipX = false;
                renderer.sortingOrder = 6;
                PrefabUtility.SaveAsPrefabAsset(contents, BiancaPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void ConfigureCharacterCatalog()
        {
            DialogueCharacterCatalog catalog =
                AssetDatabase.LoadAssetAtPath<DialogueCharacterCatalog>(CharacterCatalogPath);
            if (catalog == null)
                throw new MissingReferenceException($"Missing character catalog at '{CharacterCatalogPath}'.");

            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty characters = serialized.FindProperty("characters");
            SerializedProperty biancaEntry = null;
            for (int index = 0; index < characters.arraySize; index++)
            {
                SerializedProperty candidate = characters.GetArrayElementAtIndex(index);
                if (candidate.FindPropertyRelative("character").enumValueIndex ==
                    (int)DialogueCharacterId.Bianca)
                {
                    biancaEntry = candidate;
                    break;
                }
            }

            if (biancaEntry == null)
            {
                characters.arraySize++;
                biancaEntry = characters.GetArrayElementAtIndex(characters.arraySize - 1);
            }

            biancaEntry.FindPropertyRelative("character").enumValueIndex =
                (int)DialogueCharacterId.Bianca;
            biancaEntry.FindPropertyRelative("displayName").stringValue = "Bianca";
            biancaEntry.FindPropertyRelative("portrait").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static DialogueAssets CreateDialogueAssets()
        {
            return new DialogueAssets
            {
                Call = ConfigureDialogue(
                    "Dialogue_D1_CLASSROOM_BIANCA_CALL",
                    "d1-classroom-bianca-call",
                    new DialogueLine(DialogueSpeakerSide.Right, "Audere?")),
                Apology = ConfigureDialogue(
                    "Dialogue_D1_CLASSROOM_BIANCA_APOLOGY",
                    "d1-classroom-bianca-apology",
                    new DialogueLine(DialogueSpeakerSide.Right, "Xin lỗi!"),
                    new DialogueLine(DialogueSpeakerSide.Right, "Tớ làm cậu giật mình à?"),
                    new DialogueLine(DialogueSpeakerSide.Left, "Không… tớ không để ý.")),
                Invitation = ConfigureDialogue(
                    "Dialogue_D1_CLASSROOM_BIANCA_INVITATION",
                    "d1-classroom-bianca-invitation",
                    new[]
                    {
                        new DialogueLine(DialogueSpeakerSide.Right, "Tớ đang phụ phần trang trí."),
                        new DialogueLine(DialogueSpeakerSide.Right, "Bọn tớ còn thiếu người làm bảng."),
                        new DialogueLine(DialogueSpeakerSide.Right, "Cậu có muốn làm cùng không?"),
                        new DialogueLine(DialogueSpeakerSide.Right, "Chỉ một chút thôi."),
                    },
                    DialogueCharacterId.Bianca),
                Exit = ConfigureDialogue(
                    "Dialogue_D1_CLASSROOM_BIANCA_EXIT",
                    "d1-classroom-bianca-exit",
                    new DialogueLine(DialogueSpeakerSide.Right, "Không tiện cũng không sao.")),
                Timor = ConfigureDialogue(
                    "Dialogue_D1_CLASSROOM_TIMOR_INTERVENES",
                    "d1-classroom-timor-intervenes",
                    new DialogueLine(DialogueSpeakerSide.Right, "Đừng trả lời vội."),
                    new DialogueLine(DialogueSpeakerSide.Right, "Nhìn tớ này."),
                    new DialogueLine(DialogueSpeakerSide.Right, "Tớ sẽ giúp cậu."),
                    DialogueCharacterId.Timor),
            };
        }

        private static void PolishClassroomAnnouncementDialogue()
        {
            ConfigureDialogue(
                "Dialogue_D1_TEACHER_OPENING",
                "d1-teacher-opening",
                new[]
                {
                    new DialogueLine(DialogueSpeakerSide.Right, "Cả lớp ổn định chỗ ngồi nhé."),
                    new DialogueLine(DialogueSpeakerSide.Right, "Mình bắt đầu thôi."),
                    new DialogueLine(DialogueSpeakerSide.Right, "Trước hết, cô có một chuyện vui."),
                },
                DialogueCharacterId.Teacher);
            ConfigureDialogue(
                "Dialogue_D1_TEACHER_EVENT",
                "d1-teacher-event",
                new[]
                {
                    new DialogueLine(DialogueSpeakerSide.Right, "Cuối năm nay, lớp mình sẽ liên hoan."),
                    new DialogueLine(DialogueSpeakerSide.Right, "Một buổi nho nhỏ thôi."),
                },
                DialogueCharacterId.Teacher);
            ConfigureDialogue(
                "Dialogue_D1_TEACHER_DETAILS",
                "d1-teacher-details",
                new[]
                {
                    new DialogueLine(DialogueSpeakerSide.Right, "Trang trí, đồ ăn, trò chơi…"),
                    new DialogueLine(DialogueSpeakerSide.Right, "Lớp mình sẽ cùng chuẩn bị."),
                    new DialogueLine(DialogueSpeakerSide.Right, "Các em cứ chọn phần mình thích."),
                    new DialogueLine(DialogueSpeakerSide.Right, "Lát nữa ghi tên vào danh sách nhé."),
                    new DialogueLine(DialogueSpeakerSide.Right, "Mỗi người một việc vừa sức là được."),
                    new DialogueLine(DialogueSpeakerSide.Right, "Không cần vội đâu."),
                    new DialogueLine(DialogueSpeakerSide.Right, "Góp một chút để cùng vui là đủ."),
                },
                DialogueCharacterId.Teacher);
            ConfigureDialogue(
                "Dialogue_D1_CLASSROOM_REDIRECT",
                "d1-classroom-redirect",
                new[]
                {
                    new DialogueLine(DialogueSpeakerSide.Right, "Ừm… còn chuyện đó…"),
                    new DialogueLine(DialogueSpeakerSide.Right, "Chúng ta không cần tham gia đâu."),
                },
                DialogueCharacterId.Timor);
        }

        private static DialogueData ConfigureDialogue(
            string assetName,
            string dialogueId,
            DialogueLine line,
            DialogueCharacterId rightCharacter = DialogueCharacterId.Bianca)
        {
            return ConfigureDialogue(assetName, dialogueId, new[] { line }, rightCharacter);
        }

        private static DialogueData ConfigureDialogue(
            string assetName,
            string dialogueId,
            DialogueLine first,
            DialogueLine second,
            DialogueCharacterId rightCharacter = DialogueCharacterId.Bianca)
        {
            return ConfigureDialogue(assetName, dialogueId, new[] { first, second }, rightCharacter);
        }

        private static DialogueData ConfigureDialogue(
            string assetName,
            string dialogueId,
            DialogueLine first,
            DialogueLine second,
            DialogueLine third,
            DialogueCharacterId rightCharacter = DialogueCharacterId.Bianca)
        {
            return ConfigureDialogue(assetName, dialogueId, new[] { first, second, third }, rightCharacter);
        }

        private static DialogueData ConfigureDialogue(
            string assetName,
            string dialogueId,
            DialogueLine[] lines,
            DialogueCharacterId rightCharacter)
        {
            string path = $"{DialogueFolder}/{assetName}.asset";
            DialogueData asset = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DialogueData>();
                asset.name = assetName;
                AssetDatabase.CreateAsset(asset, path);
            }

            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("dialogueId").stringValue = dialogueId;
            serialized.FindProperty("leftCharacter").enumValueIndex =
                (int)DialogueCharacterId.Audere;
            serialized.FindProperty("rightCharacter").enumValueIndex = (int)rightCharacter;
            SerializedProperty serializedLines = serialized.FindProperty("lines");
            serializedLines.arraySize = lines.Length;
            for (int index = 0; index < lines.Length; index++)
            {
                SerializedProperty target = serializedLines.GetArrayElementAtIndex(index);
                target.FindPropertyRelative("speaker").enumValueIndex = (int)lines[index].Speaker;
                target.FindPropertyRelative("text").stringValue = lines[index].Text;
                if (lines[index].Text.Length > RecommendedDialogueCharacters)
                {
                    Debug.LogWarning(
                        $"[ClassroomRecessSetup] '{assetName}' line {index + 1} has " +
                        $"{lines[index].Text.Length} characters. Split or verify it at target resolution.",
                        asset);
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void SetupScene(
            Scene scene,
            DialogueAssets dialogue,
            FullscreenTransitionAssets transitionAssets)
        {
            GameObject classroom = FindRoot(scene, "CLASSROOM");
            GameObject storyRoot = FindRoot(scene, "STORY");
            GameObject transitionOverlay = FindRoot(scene, "Scene Transition Overlay");
            if (classroom == null || storyRoot == null || transitionOverlay == null)
                throw new MissingReferenceException(
                    "Classroom setup requires CLASSROOM, STORY, and Scene Transition Overlay.");

            Transform classroomArt = RequireChild(classroom.transform, "CLASSROOM ART PLACEHOLDER");
            Transform board = RequireChild(classroomArt, "Board");
            Transform actors = RequireChild(classroomArt, "Actors");
            Transform staging = RequireChild(classroom.transform, "STAGING TARGETS");
            Transform audere = RequireChild(actors, "Audere");
            GameObject teacher = RequireChild(actors, "Teacher_PLACEHOLDER").gameObject;
            Transform audereSeat = RequireChild(staging, "Audere_SeatPose");
            GameObject seatTile = RequireChild(board, "Tile_AudereSeat").gameObject;
            GameObject decorationTile = RequireChild(board, "Tile_DecorationInterest").gameObject;
            GameObject teacherTile = RequireChild(board, "Tile_TeacherFront").gameObject;
            CanvasGroup fade = RequireChild(transitionOverlay.transform, "Fade")
                .GetComponent<CanvasGroup>();
            if (fade == null)
                throw new MissingReferenceException("Scene Transition Overlay/Fade needs a CanvasGroup.");

            GameObject biancaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BiancaPrefabPath);
            GameObject tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GrassTilePrefabPath);
            if (biancaPrefab == null || tilePrefab == null)
                throw new MissingReferenceException("Bianca or Grass tile prefab is missing.");

            CombatRuntime combat = SetupCombatRuntime(scene, classroom, transitionAssets);

            GameObject bianca = EnsurePrefabChild(actors, "Bianca_PLACEHOLDER", biancaPrefab);
            bianca.transform.localPosition = new Vector3(1.4f, seatTile.transform.localPosition.y, -1f);
            bianca.transform.localRotation = Quaternion.identity;
            bianca.SetActive(false);
            SpriteRenderer biancaRenderer = bianca.GetComponent<SpriteRenderer>();
            SpriteRenderer audereRenderer = audere.GetComponent<SpriteRenderer>();
            SpriteRenderer teacherRenderer = teacher.GetComponent<SpriteRenderer>();
            if (audereRenderer == null || teacherRenderer == null || biancaRenderer == null)
                throw new MissingReferenceException("Each classroom actor needs a root SpriteRenderer.");

            GameObject startTile = EnsurePrefabChild(board, "Tile_BiancaStart", tilePrefab);
            ConfigureClassroomTile(startTile, new Vector3(1.4f, 1.15f, 0f));
            startTile.SetActive(false);
            GameObject midTile = EnsurePrefabChild(board, "Tile_BiancaMid", tilePrefab);
            ConfigureClassroomTile(midTile, new Vector3(.4f, 1.15f, 0f));
            midTile.SetActive(false);

            AlignActorFeetToTile(audere, audereRenderer, seatTile.transform);
            AlignActorFeetToTile(teacher.transform, teacherRenderer, teacherTile.transform);
            AlignActorFeetToTile(bianca.transform, biancaRenderer, startTile.transform);
            AlignAnchorToActor(audereSeat, audere);
            AlignExistingAnchorHeight(staging, "Audere_LeanPose", audere.localPosition.y);

            float actorBaselineY = bianca.transform.localPosition.y;
            Transform biancaStart = EnsureAnchor(staging, "Bianca_StartPose", 1.4f, actorBaselineY);
            Transform biancaMid = EnsureAnchor(staging, "Bianca_MidPose", .4f, actorBaselineY);
            Transform biancaEnd = EnsureAnchor(staging, "Bianca_EndPose", -.6f, actorBaselineY);
            Transform biancaNudge = EnsureAnchor(staging, "Bianca_NudgePose", -.78f, actorBaselineY);

            StoryEvent recessEvent = EnsureEvent(storyRoot.transform, "D1_CLASSROOM_RECESS_BIANCA");
            ClearChildren(recessEvent.transform);
            ConfigureStoryEvent(recessEvent, "D1_CLASSROOM_RECESS_BIANCA", false, null);

            StoryEvent announcement = RequireChild(storyRoot.transform, "D1_CLASSROOM_ANNOUNCEMENT")
                .GetComponent<StoryEvent>();
            if (announcement == null)
                throw new MissingReferenceException("D1_CLASSROOM_ANNOUNCEMENT needs StoryEvent.");
            ConfigureStoryEvent(announcement, "D1_CLASSROOM_ANNOUNCEMENT", true, recessEvent);

            ConfigureCanvasFade(CreateStep<CanvasFadeStep>(recessEvent, "00_FadeToRecess"), fade, 1f, .32f);
            ConfigureSetActive(
                CreateStep<SetActiveStep>(recessEvent, "05_NormalizeRecess"),
                new[] { audere.gameObject, bianca, startTile, seatTile },
                new[] { teacher, decorationTile, midTile, teacherTile });
            ConfigureMove(CreateStep<MoveActorStep>(recessEvent, "08_PlaceAudereAtSeat"), audere, audereSeat, 0f);
            ConfigureMove(CreateStep<MoveActorStep>(recessEvent, "10_PlaceBiancaAtStart"), bianca.transform, biancaStart, 0f);
            ConfigureCanvasFade(CreateStep<CanvasFadeStep>(recessEvent, "15_FadeInRecess"), fade, 0f, .45f);
            ConfigureWait(CreateStep<WaitStep>(recessEvent, "20_RecessBeat"), .28f);

            ConfigureBoardTransition(
                CreateStep<BoardTileTransitionStep>(recessEvent, "30_RevealBiancaMidTile"),
                Array.Empty<Transform>(), new[] { midTile.transform });
            ConfigureCharacterMotion(
                CreateStep<CharacterMotionStep>(recessEvent, "40_BiancaHopsToMid"),
                bianca.transform, biancaMid, biancaRenderer,
                CharacterMotionMode.TravelToTarget, CharacterFacingMode.FollowHorizontalTravel,
                .32f, .075f);
            ConfigureBoardTransition(
                CreateStep<BoardTileTransitionStep>(recessEvent, "50_HideBiancaStartTile"),
                new[] { startTile.transform }, Array.Empty<Transform>());
            ConfigureBoardTransition(
                CreateStep<BoardTileTransitionStep>(recessEvent, "60_RevealDecorationTile"),
                Array.Empty<Transform>(), new[] { decorationTile.transform });
            ConfigureCharacterMotion(
                CreateStep<CharacterMotionStep>(recessEvent, "70_BiancaHopsTowardAudere"),
                bianca.transform, biancaEnd, biancaRenderer,
                CharacterMotionMode.TravelToTarget, CharacterFacingMode.FollowHorizontalTravel,
                .32f, .075f);
            ConfigureBoardTransition(
                CreateStep<BoardTileTransitionStep>(recessEvent, "80_HideBiancaMidTile"),
                new[] { midTile.transform }, Array.Empty<Transform>());

            ConfigureDialogue(CreateStep<DialogueStep>(recessEvent, "90_BiancaCalls"), dialogue.Call);
            ConfigureWait(CreateStep<WaitStep>(recessEvent, "100_AudereDoesNotRespond"), .55f);
            ConfigureMove(
                CreateStep<MoveActorStep>(recessEvent, "110_BiancaNudgesCloser"),
                bianca.transform, biancaNudge, .14f);
            ConfigureCharacterMotion(
                CreateStep<CharacterMotionStep>(recessEvent, "120_AudereStartlesAndTurns"),
                audere, audereSeat, audereRenderer,
                CharacterMotionMode.VerticalInPlace, CharacterFacingMode.FaceRight, .19f, .09f);
            ConfigureDialogue(CreateStep<DialogueStep>(recessEvent, "130_BiancaApologizes"), dialogue.Apology);
            ConfigureDialogue(CreateStep<DialogueStep>(recessEvent, "140_BiancaInvites"), dialogue.Invitation);
            ConfigureWait(CreateStep<WaitStep>(recessEvent, "150_BiancaWaits"), .45f);
            ConfigureDialogue(CreateStep<DialogueStep>(recessEvent, "160_BiancaLeavesRoom"), dialogue.Exit);
            ConfigureWait(CreateStep<WaitStep>(recessEvent, "170_AudereStaysSilent"), .6f);
            ConfigureDialogue(CreateStep<DialogueStep>(recessEvent, "180_TimorIntervenes"), dialogue.Timor);
            ConfigureWait(CreateStep<WaitStep>(recessEvent, "190_HoldAfterTimor"), .25f);
            ConfigureFullscreenTransition(
                CreateStep<FullscreenWorldModeTransitionStep>(recessEvent, "200_ClassroomIsConsumed"),
                combat.TransitionController,
                combat.ModeController,
                transitionAssets.Profile,
                audereRenderer);
            ConfigureCombat(
                CreateStep<CombatStep>(recessEvent, "210_PlayCombatPrototype"),
                combat.Controller,
                combat.Encounter);
            ConfigureWorldMode(
                CreateStep<WorldModeStep>(recessEvent, "220_ReturnToStory"),
                combat.ModeController,
                WorldGameplayMode.Story);
            ConfigureWait(CreateStep<WaitStep>(recessEvent, "230_HoldAfterCombat"), .35f);

            EditorUtility.SetDirty(recessEvent);
            EditorUtility.SetDirty(announcement);
        }

        private static CombatRuntime SetupCombatRuntime(
            Scene scene,
            GameObject classroom,
            FullscreenTransitionAssets transitionAssets)
        {
            GameObject boardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CombatBoardPrefabPath);
            if (boardPrefab == null)
                throw new MissingReferenceException($"Missing Combat Board prefab at '{CombatBoardPrefabPath}'.");

            GameObject world = FindRoot(scene, "WORLD") ?? new GameObject("WORLD");
            GameObject systems = FindRoot(scene, "SYSTEMS") ?? new GameObject("SYSTEMS");
            Transform combatRoot = EnsureChild(world.transform, "Combat Root");
            Transform combatSystems = EnsureChild(systems.transform, "Combat Systems");

            GameObject board = EnsurePrefabChild(combatRoot, "Combat Board", boardPrefab);
            board.transform.localPosition = new Vector3(0f, -.42f, 0f);
            board.transform.localRotation = Quaternion.identity;
            board.transform.localScale = Vector3.one * .0025f;
            CombatBoardView boardView = board.GetComponent<CombatBoardView>();
            if (boardView == null)
                throw new MissingReferenceException("Combat Board prefab needs CombatBoardView on its root.");

            CombatEncounterData encounter = EnsurePrototypeEncounter();
            foreach (TMP_Text text in board.GetComponentsInChildren<TMP_Text>(true))
                if (text.name == "Enemy Name")
                    text.text = encounter.EnemyDisplayName;
            GameObject controllerObject = EnsureChild(combatSystems, "Combat Controller").gameObject;
            CombatController controller = GetOrAdd<CombatController>(controllerObject);
            SerializedObject combatSerialized = new SerializedObject(controller);
            combatSerialized.FindProperty("playOnStart").boolValue = false;
            SetObject(combatSerialized, "encounterData", encounter);
            SetObject(combatSerialized, "boardView", boardView);
            combatSerialized.ApplyModifiedPropertiesWithoutUndo();

            Camera camera = Camera.main;
            if (camera == null)
                throw new MissingReferenceException("Classroom combat hand-off requires Main Camera.");

            GameObject viewportMask = camera.transform.Find("PuzzleViewportMask")?.gameObject;
            CanvasGroup transitionFade = EnsureWorldTransitionOverlay(world.transform);
            WorldModeController modeController = GetOrAdd<WorldModeController>(world);
            SerializedObject modeSerialized = new SerializedObject(modeController);
            modeSerialized.FindProperty("startingMode").enumValueIndex = (int)WorldGameplayMode.Story;
            SetObject(modeSerialized, "puzzleRoot", null);
            SetObject(modeSerialized, "combatRoot", combatRoot.gameObject);
            SetObject(modeSerialized, "storyRoot", classroom);
            SetObject(modeSerialized, "puzzleViewportMask", viewportMask);
            modeSerialized.FindProperty("storyUsesPuzzleViewportMask").boolValue = true;
            SetObject(modeSerialized, "combatSystemsRoot", combatSystems.gameObject);
            SetObject(modeSerialized, "transitionFade", transitionFade);
            modeSerialized.FindProperty("revealStartingModeOnStart").boolValue = false;
            SetObject(modeSerialized, "worldCamera", camera);
            SetObject(modeSerialized, "puzzleCameraFollow", camera.GetComponent<GridCameraFollow2D>());
            modeSerialized.FindProperty("combatCameraPosition").vector3Value = new Vector3(0f, 0f, -10f);
            modeSerialized.FindProperty("combatOrthographicSize").floatValue = 1.25f;
            modeSerialized.FindProperty("storyCameraPosition").vector3Value = camera.transform.position;
            modeSerialized.FindProperty("storyOrthographicSize").floatValue = camera.orthographicSize;
            modeSerialized.ApplyModifiedPropertiesWithoutUndo();

            FullscreenTransitionController transitionController =
                GetOrAdd<FullscreenTransitionController>(world);
            SerializedObject transitionSerialized = new SerializedObject(transitionController);
            SetObject(transitionSerialized, "worldCamera", camera);
            SetObject(transitionSerialized, "rendererFeature", transitionAssets.Feature);
            transitionSerialized.ApplyModifiedPropertiesWithoutUndo();

            classroom.SetActive(true);
            combatRoot.gameObject.SetActive(false);
            combatSystems.gameObject.SetActive(false);
            if (viewportMask != null)
                viewportMask.SetActive(true);
            transitionFade.alpha = 0f;
            transitionFade.blocksRaycasts = false;
            transitionFade.interactable = false;

            EditorUtility.SetDirty(world);
            EditorUtility.SetDirty(systems);
            return new CombatRuntime(modeController, transitionController, controller, encounter);
        }

        private static FullscreenTransitionAssets EnsureFullscreenTransitionAssets()
        {
            EnsureFolder("Assets/_Audere/Materials/PostProcess");
            EnsureFolder("Assets/_Audere/Data/Transitions");

            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(FullscreenTransitionShaderPath);
            if (shader == null)
                throw new MissingReferenceException(
                    $"Missing fullscreen transition shader at '{FullscreenTransitionShaderPath}'.");

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(FullscreenTransitionMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "FullscreenDreamyDisorientation" };
                AssetDatabase.CreateAsset(material, FullscreenTransitionMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.name = "FullscreenDreamyDisorientation";
            ResetTransitionMaterial(material);
            EditorUtility.SetDirty(material);
            FullscreenTransitionProfile profile = EnsureDreamyDisorientationProfile(material);

            Renderer2DData rendererData =
                AssetDatabase.LoadAssetAtPath<Renderer2DData>(Renderer2DPath);
            if (rendererData == null)
                throw new MissingReferenceException($"Missing Renderer2D data at '{Renderer2DPath}'.");

            FullScreenPassRendererFeature feature = null;
            foreach (ScriptableRendererFeature candidate in rendererData.rendererFeatures)
            {
                if (candidate is FullScreenPassRendererFeature fullscreen &&
                    (candidate.name == FullscreenTransitionFeatureName ||
                     candidate.name == LegacyFullscreenTransitionFeatureName))
                {
                    feature = fullscreen;
                    break;
                }
            }

            if (feature == null)
            {
                foreach (UnityEngine.Object subAsset in AssetDatabase.LoadAllAssetsAtPath(Renderer2DPath))
                {
                    if (subAsset is FullScreenPassRendererFeature fullscreen &&
                        (subAsset.name == FullscreenTransitionFeatureName ||
                         subAsset.name == LegacyFullscreenTransitionFeatureName))
                    {
                        feature = fullscreen;
                        break;
                    }
                }
            }

            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                feature.name = FullscreenTransitionFeatureName;
                AssetDatabase.AddObjectToAsset(feature, rendererData);
            }

            if (!rendererData.rendererFeatures.Contains(feature))
                rendererData.rendererFeatures.Add(feature);

            feature.name = FullscreenTransitionFeatureName;

            feature.injectionPoint =
                FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
            feature.fetchColorBuffer = true;
            feature.requirements = ScriptableRenderPassInput.None;
            feature.passMaterial = material;
            feature.passIndex = 0;
            feature.bindDepthStencilAttachment = false;
            feature.SetActive(false);
            feature.Create();

            SyncRendererFeatureMap(rendererData);
            rendererData.SetDirty();
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(rendererData);
            return new FullscreenTransitionAssets(profile, material, feature);
        }

        private static FullscreenTransitionProfile EnsureDreamyDisorientationProfile(Material material)
        {
            FullscreenTransitionProfile profile =
                AssetDatabase.LoadAssetAtPath<FullscreenTransitionProfile>(
                    FullscreenTransitionProfilePath);
            if (profile != null)
                return profile;

            profile = ScriptableObject.CreateInstance<FullscreenTransitionProfile>();
            profile.name = "WorldTransition_DreamyDisorientation";
            AssetDatabase.CreateAsset(profile, FullscreenTransitionProfilePath);

            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("profileId").stringValue = "dreamy-disorientation";
            serialized.FindProperty("displayName").stringValue = "Dreamy Disorientation";
            SetObject(serialized, "material", material);
            serialized.FindProperty("duration").floatValue = 1.50f;
            serialized.FindProperty("modeSwapTime").floatValue = 1.10f;
            serialized.FindProperty("usesFocusRenderer").boolValue = true;
            serialized.FindProperty("effectTimeProperty").stringValue = "_EffectTime";

            SerializedProperty tracks = serialized.FindProperty("floatTracks");
            tracks.arraySize = 9;
            ConfigureTransitionTrack(
                tracks.GetArrayElementAtIndex(0),
                "_RotationDegrees",
                0f, 0f, .30f, -.7f, .70f, 1f, 1.10f, -1.6f, 1.25f, .5f, 1.50f, 0f);
            ConfigureTransitionTrack(
                tracks.GetArrayElementAtIndex(1),
                "_Zoom",
                0f, 1f, .30f, 1.03f, .70f, 1.035f, 1.10f, 1.06f, 1.25f, 1.025f, 1.50f, 1f);
            ConfigureTransitionTrack(
                tracks.GetArrayElementAtIndex(2),
                "_WaveStrength",
                0f, 0f, .25f, 0f, .45f, .003f, .70f, .009f, 1.05f, .018f,
                1.20f, .012f, 1.50f, 0f);
            ConfigureTransitionTrack(
                tracks.GetArrayElementAtIndex(3),
                "_DriftX",
                0f, 0f, .30f, 0f, .70f, -.004f, 1.10f, -.015f, 1.25f, .006f, 1.50f, 0f);
            ConfigureTransitionTrack(
                tracks.GetArrayElementAtIndex(4),
                "_DriftY",
                0f, 0f, .30f, 0f, .70f, .002f, 1.10f, -.006f, 1.25f, .003f, 1.50f, 0f);
            ConfigureTransitionTrack(
                tracks.GetArrayElementAtIndex(5),
                "_RadialStrength",
                0f, 0f, .35f, 0f, .55f, .008f, .90f, .025f, 1.10f, .04f,
                1.25f, .018f, 1.50f, 0f);
            ConfigureTransitionTrack(
                tracks.GetArrayElementAtIndex(6),
                "_SmearStrength",
                0f, 0f, .35f, 0f, .55f, .003f, .80f, .012f, 1.10f, .035f,
                1.25f, .015f, 1.50f, 0f);
            ConfigureTransitionTrack(
                tracks.GetArrayElementAtIndex(7),
                "_VeilStrength",
                0f, 0f, .65f, 0f, .85f, .15f, 1.10f, .90f, 1.20f, .45f, 1.50f, 0f);
            ConfigureTransitionTrack(
                tracks.GetArrayElementAtIndex(8),
                "_ChromaticOffset",
                0f, 0f, .45f, 0f, .75f, .001f, 1.10f, .003f, 1.25f, .001f, 1.50f, 0f);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void ConfigureTransitionTrack(
            SerializedProperty track,
            string shaderProperty,
            params float[] timeValuePairs)
        {
            track.FindPropertyRelative("shaderProperty").stringValue = shaderProperty;
            int keyCount = timeValuePairs.Length / 2;
            Keyframe[] keys = new Keyframe[keyCount];
            for (int index = 0; index < keyCount; index++)
            {
                keys[index] = new Keyframe(
                    timeValuePairs[index * 2],
                    timeValuePairs[index * 2 + 1],
                    0f,
                    0f);
            }
            track.FindPropertyRelative("values").animationCurveValue = new AnimationCurve(keys);
        }

        private static void SyncRendererFeatureMap(ScriptableRendererData rendererData)
        {
            SerializedObject serialized = new SerializedObject(rendererData);
            SerializedProperty featureMap = serialized.FindProperty("m_RendererFeatureMap");
            featureMap.arraySize = rendererData.rendererFeatures.Count;
            for (int index = 0; index < rendererData.rendererFeatures.Count; index++)
            {
                ScriptableRendererFeature feature = rendererData.rendererFeatures[index];
                long localId = 0;
                if (feature != null)
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out localId);
                featureMap.GetArrayElementAtIndex(index).longValue = localId;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ResetTransitionMaterial(Material material)
        {
            material.SetFloat("_EffectTime", 0f);
            material.SetVector("_FocusCenter", new Vector2(.5f, .5f));
            material.SetFloat("_AspectRatio", 16f / 9f);
            material.SetFloat("_RotationDegrees", 0f);
            material.SetFloat("_Zoom", 1f);
            material.SetFloat("_WaveStrength", 0f);
            material.SetFloat("_DriftX", 0f);
            material.SetFloat("_DriftY", 0f);
            material.SetFloat("_RadialStrength", 0f);
            material.SetFloat("_SmearStrength", 0f);
            material.SetFloat("_VeilStrength", 0f);
            material.SetFloat("_ChromaticOffset", 0f);
        }

        private static CombatEncounterData EnsurePrototypeEncounter()
        {
            CombatEncounterData encounter =
                AssetDatabase.LoadAssetAtPath<CombatEncounterData>(ClassroomPrototypeEncounterPath);
            if (encounter == null)
            {
                encounter = ScriptableObject.CreateInstance<CombatEncounterData>();
                AssetDatabase.CreateAsset(encounter, ClassroomPrototypeEncounterPath);
            }

            CombatEnemyDefinition enemy =
                AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>(ClassroomEnemyDefinitionPath);
            if (enemy == null)
                throw new MissingReferenceException($"Missing enemy definition at '{ClassroomEnemyDefinitionPath}'.");

            SerializedObject serialized = new SerializedObject(encounter);
            serialized.FindProperty("encounterId").stringValue = "d1-classroom-khoang-lang";
            serialized.FindProperty("enemyDefinition").objectReferenceValue = enemy;
            serialized.FindProperty("encounterDuration").floatValue = 30f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(encounter);
            return encounter;
        }

        private static CanvasGroup EnsureWorldTransitionOverlay(Transform world)
        {
            Transform existing = FindDirectChild(world, "World Transition Overlay");
            GameObject canvasObject = existing != null
                ? existing.gameObject
                : new GameObject("World Transition Overlay", typeof(RectTransform));
            RectTransform canvasRect = GetOrAdd<RectTransform>(canvasObject);
            canvasRect.SetParent(world, false);
            canvasObject.SetActive(true);

            Canvas canvas = GetOrAdd<Canvas>(canvasObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1000;
            CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvasObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            Transform fadeTransform = FindDirectChild(canvasRect, "Transition Fade");
            if (fadeTransform != null && !(fadeTransform is RectTransform))
            {
                UnityEngine.Object.DestroyImmediate(fadeTransform.gameObject);
                fadeTransform = null;
            }
            if (fadeTransform == null)
            {
                GameObject fadeObject = new GameObject("Transition Fade", typeof(RectTransform));
                fadeTransform = fadeObject.transform;
                fadeTransform.SetParent(canvasRect, false);
            }
            RectTransform fadeRect = (RectTransform)fadeTransform;
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.pivot = new Vector2(.5f, .5f);
            fadeRect.anchoredPosition = Vector2.zero;
            fadeRect.sizeDelta = Vector2.zero;
            fadeRect.localScale = Vector3.one;
            Image image = GetOrAdd<Image>(fadeRect.gameObject);
            image.color = Color.black;
            image.raycastTarget = true;
            return GetOrAdd<CanvasGroup>(fadeRect.gameObject);
        }

        private static void ConfigureClassroomTile(GameObject tile, Vector3 localPosition)
        {
            tile.transform.localPosition = localPosition;
            tile.transform.localRotation = Quaternion.identity;
            tile.transform.localScale = Vector3.one;
            foreach (SpriteRenderer renderer in tile.GetComponentsInChildren<SpriteRenderer>(true))
                renderer.color = new Color(.28f, .58f, .50f, renderer.color.a);
        }

        private static void AlignActorFeetToTile(
            Transform actor,
            SpriteRenderer renderer,
            Transform tile)
        {
            Vector3 tilePosition = tile.localPosition;
            actor.localPosition = new Vector3(tilePosition.x, tilePosition.y, -1f);

            Bounds bounds = renderer.bounds;
            Vector3 worldDelta = new Vector3(
                tile.position.x - bounds.center.x,
                tile.position.y - bounds.min.y,
                0f);
            actor.position += worldDelta;
        }

        private static void AlignAnchorToActor(Transform anchor, Transform actor)
        {
            Vector3 actorPosition = actor.localPosition;
            anchor.localPosition = new Vector3(actorPosition.x, actorPosition.y, -1f);
        }

        private static void AlignExistingAnchorHeight(Transform parent, string name, float localY)
        {
            Transform anchor = RequireChild(parent, name);
            Vector3 position = anchor.localPosition;
            anchor.localPosition = new Vector3(position.x, localY, position.z);
        }

        private static Transform EnsureAnchor(
            Transform parent,
            string name,
            float localX,
            float localY)
        {
            Transform anchor = EnsureChild(parent, name);
            anchor.localPosition = new Vector3(localX, localY, -1f);
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.one;
            return anchor;
        }

        private static StoryEvent EnsureEvent(Transform parent, string name)
        {
            Transform transform = FindDirectChild(parent, name) ?? EnsureChild(parent, name);
            return GetOrAdd<StoryEvent>(transform.gameObject);
        }

        private static void ConfigureStoryEvent(
            StoryEvent storyEvent,
            string eventId,
            bool autoNext,
            StoryEvent next)
        {
            SerializedObject serialized = new SerializedObject(storyEvent);
            serialized.FindProperty("eventId").stringValue = eventId;
            serialized.FindProperty("autoPlayNextEvent").boolValue = autoNext;
            serialized.FindProperty("nextEvent").objectReferenceValue = next;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T CreateStep<T>(StoryEvent storyEvent, string name) where T : StoryStep
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(storyEvent.transform, false);
            return child.AddComponent<T>();
        }

        private static void ConfigureCanvasFade(
            CanvasFadeStep step,
            CanvasGroup fade,
            float alpha,
            float duration)
        {
            SerializedObject serialized = new SerializedObject(step);
            SetObject(serialized, "canvasGroup", fade);
            serialized.FindProperty("targetAlpha").floatValue = alpha;
            serialized.FindProperty("duration").floatValue = duration;
            serialized.FindProperty("useUnscaledTime").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSetActive(
            SetActiveStep step,
            GameObject[] enable,
            GameObject[] disable)
        {
            SerializedObject serialized = new SerializedObject(step);
            SetObjectArray(serialized.FindProperty("objectsToEnable"), enable);
            SetObjectArray(serialized.FindProperty("objectsToDisable"), disable);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureMove(
            MoveActorStep step,
            Transform actor,
            Transform target,
            float duration)
        {
            SerializedObject serialized = new SerializedObject(step);
            SetObject(serialized, "actor", actor);
            SetObject(serialized, "targetTransform", target);
            serialized.FindProperty("duration").floatValue = duration;
            serialized.FindProperty("useUnscaledTime").boolValue = true;
            serialized.FindProperty("snapOnComplete").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureWait(WaitStep step, float duration)
        {
            SerializedObject serialized = new SerializedObject(step);
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

        private static void ConfigureCharacterMotion(
            CharacterMotionStep step,
            Transform actor,
            Transform target,
            SpriteRenderer renderer,
            CharacterMotionMode motionMode,
            CharacterFacingMode facing,
            float duration,
            float arcHeight)
        {
            SerializedObject serialized = new SerializedObject(step);
            SetObject(serialized, "actor", actor);
            SetObject(serialized, "targetTransform", target);
            SetObject(serialized, "actorRenderer", renderer);
            SetObject(serialized, "groundedShadow", FindGroundedShadow(actor));
            serialized.FindProperty("motionMode").enumValueIndex = (int)motionMode;
            serialized.FindProperty("duration").floatValue = duration;
            serialized.FindProperty("arcHeight").floatValue = arcHeight;
            serialized.FindProperty("travelStretch").floatValue = .065f;
            serialized.FindProperty("landingDuration").floatValue = .1f;
            serialized.FindProperty("landingSquash").floatValue = .105f;
            serialized.FindProperty("landingWiden").floatValue = .075f;
            serialized.FindProperty("useUnscaledTime").boolValue = true;
            serialized.FindProperty("facingMode").enumValueIndex = (int)facing;
            serialized.FindProperty("sourceSpriteFacesLeft").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform FindGroundedShadow(Transform actorRoot)
        {
            if (actorRoot == null)
                return null;

            Transform[] descendants = actorRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                Transform candidate = descendants[i];
                if (candidate != actorRoot &&
                    candidate.name.StartsWith("shadow", StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void ConfigureDialogue(DialogueStep step, DialogueData data)
        {
            SerializedObject serialized = new SerializedObject(step);
            SetObject(serialized, "dialogueData", data);
            SetObject(serialized, "dialogueController", null);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureWorldMode(
            WorldModeStep step,
            WorldModeController controller,
            WorldGameplayMode targetMode)
        {
            SerializedObject serialized = new SerializedObject(step);
            SetObject(serialized, "worldModeController", controller);
            serialized.FindProperty("targetMode").enumValueIndex = (int)targetMode;
            serialized.FindProperty("waitUntilTransitionFinished").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureFullscreenTransition(
            FullscreenWorldModeTransitionStep step,
            FullscreenTransitionController transitionController,
            WorldModeController modeController,
            FullscreenTransitionProfile transitionProfile,
            Renderer focusRenderer)
        {
            SerializedObject serialized = new SerializedObject(step);
            SetObject(serialized, "transitionController", transitionController);
            SetObject(serialized, "worldModeController", modeController);
            SetObject(serialized, "transitionProfile", transitionProfile);
            SetObject(serialized, "focusRenderer", focusRenderer);
            serialized.FindProperty("sourceMode").enumValueIndex = (int)WorldGameplayMode.Story;
            serialized.FindProperty("targetMode").enumValueIndex = (int)WorldGameplayMode.Combat;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureCombat(
            CombatStep step,
            CombatController controller,
            CombatEncounterData encounter)
        {
            SerializedObject serialized = new SerializedObject(step);
            SetObject(serialized, "combatController", controller);
            SetObject(serialized, "combatEncounterData", encounter);
            serialized.FindProperty("victoryBehaviour").enumValueIndex =
                (int)CombatResultBehaviour.Complete;
            serialized.FindProperty("defeatBehaviour").enumValueIndex =
                (int)CombatResultBehaviour.Complete;
            serialized.FindProperty("specialBehaviour").enumValueIndex =
                (int)CombatResultBehaviour.Complete;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject EnsurePrefabChild(
            Transform parent,
            string name,
            GameObject prefab)
        {
            Transform existing = FindDirectChild(parent, name);
            if (existing != null)
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(existing.gameObject);
                if (source == prefab)
                    return existing.gameObject;
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
                instance = UnityEngine.Object.Instantiate(prefab, parent);
            instance.name = name;
            return instance;
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform existing = FindDirectChild(parent, name);
            if (existing != null)
                return existing;

            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
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

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name)
                    return root;
            return null;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
                UnityEngine.Object.DestroyImmediate(parent.GetChild(index).gameObject);
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void SetObject(SerializedObject serialized, string name, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null)
                throw new MissingFieldException(serialized.targetObject.GetType().Name, name);
            property.objectReferenceValue = value;
        }

        private static void SetObjectArray<T>(SerializedProperty property, T[] values)
            where T : UnityEngine.Object
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
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
            public readonly DialogueSpeakerSide Speaker;
            public readonly string Text;

            public DialogueLine(DialogueSpeakerSide speaker, string text)
            {
                Speaker = speaker;
                Text = text;
            }
        }

        private struct DialogueAssets
        {
            public DialogueData Call;
            public DialogueData Apology;
            public DialogueData Invitation;
            public DialogueData Exit;
            public DialogueData Timor;
        }

        private readonly struct CombatRuntime
        {
            public readonly WorldModeController ModeController;
            public readonly FullscreenTransitionController TransitionController;
            public readonly CombatController Controller;
            public readonly CombatEncounterData Encounter;

            public CombatRuntime(
                WorldModeController modeController,
                FullscreenTransitionController transitionController,
                CombatController controller,
                CombatEncounterData encounter)
            {
                ModeController = modeController;
                TransitionController = transitionController;
                Controller = controller;
                Encounter = encounter;
            }
        }

        private readonly struct FullscreenTransitionAssets
        {
            public readonly FullscreenTransitionProfile Profile;
            public readonly Material Material;
            public readonly FullScreenPassRendererFeature Feature;

            public FullscreenTransitionAssets(
                FullscreenTransitionProfile profile,
                Material material,
                FullScreenPassRendererFeature feature)
            {
                Profile = profile;
                Material = material;
                Feature = feature;
            }
        }

    }
}
