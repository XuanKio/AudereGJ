#if UNITY_EDITOR
using System;
using System.Linq;
using Audere.Core;
using Audere.Dialogue;
using Audere.Story;
using Audere.Story.Steps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Audere.EditorTools
{
    public static class Day2SchoolMorningSetupTool
    {
        public const string ScenePath = "Assets/_Audere/Scenes/60_D2_School_Morning.unity";
        private const string SourcePath = "Assets/_Audere/Scenes/30_Classroom.unity";
        private const string HomePath = "Assets/_Audere/Scenes/50_D2_Home_Morning.unity";
        private const string DialogueFolder = "Assets/_Audere/Data/Dialogue/Day2/School";

        [MenuItem("Audere/Story/Author Day 2 School Morning")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring.");
            Scene original = SceneManager.GetActiveScene();
            if (original.isDirty)
                throw new InvalidOperationException("Save the active scene before authoring Day 2 school.");
            Scene source = SceneManager.GetSceneByPath(SourcePath);
            bool openedSource = !source.isLoaded;
            if (openedSource) source = EditorSceneManager.OpenScene(SourcePath, OpenSceneMode.Additive);
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded)
                scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                    ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                    : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                Author(scene, source);
                EditorSceneManager.SaveScene(scene, ScenePath);
                EnsureBuildScene();
                LinkHome();
                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (openedSource) EditorSceneManager.CloseScene(source, true);
                SceneManager.SetActiveScene(scene);
            }
            // Do not leave the source scene's camera and director loaded beside the new scene.
            if (original.IsValid() && original.isLoaded && original != scene && !original.isDirty)
                EditorSceneManager.CloseScene(original, true);
            Debug.Log("[Day2SchoolMorning] Authored school encounter and Scene 50 hand-off. All actors, tiles, anchors and branches are serialized.");
        }

        private static void Author(Scene scene, Scene source)
        {
            foreach (string name in new[] { "Main Camera", "Directional Light", "EventSystem", "GameplayUIRoot", "Scene Transition Overlay" })
            {
                if (Root(scene, name) != null) continue;
                GameObject clone = Object.Instantiate(Root(source, name));
                clone.name = name;
                SceneManager.MoveGameObjectToScene(clone, scene);
            }
            Camera camera = Root(scene, "Main Camera").GetComponent<Camera>();
            CanvasGroup cover = Root(scene, "Scene Transition Overlay").GetComponentInChildren<CanvasGroup>(true);
            cover.alpha = 1f;
            GameplayUIRoot ui = Root(scene, "GameplayUIRoot").GetComponent<GameplayUIRoot>();
            ui.PuzzleUi.gameObject.SetActive(false);

            GameObject school = Root(scene, "SCHOOL");
            if (school == null)
            {
                school = new GameObject("SCHOOL");
                Transform reference = Root(source, "CLASSROOM").transform;
                school.transform.position = reference.position;
                school.transform.rotation = reference.rotation;
                school.transform.localScale = reference.localScale;
            }
            Transform art = Child(school.transform, "SCHOOL ART PLACEHOLDER");
            Transform board = Child(art, "Board");
            Transform actors = Child(art, "Actors");
            Transform staging = Child(school.transform, "STAGING TARGETS");
            staging.gameObject.SetActive(false);

            Transform referenceArt = Root(source, "CLASSROOM").transform.Find("CLASSROOM ART PLACEHOLDER");
            Transform referenceAudere = referenceArt.Find("Actors/Audere");
            Transform referenceBianca = referenceArt.Find("Actors/Bianca_PLACEHOLDER");
            Transform referenceTile = referenceArt.Find("Board/Tile_AudereSeat");
            float floor = referenceTile.localPosition.y;
            float baseline = referenceAudere.localPosition.y;
            float z = referenceAudere.localPosition.z;

            Transform audere = Actor(actors, "Audere", "Assets/_Audere/Prefabs/Puzzle/Actors/Player.prefab", referenceAudere);
            Transform bianca = Actor(actors, "Bianca_PLACEHOLDER", "Assets/_Audere/Prefabs/Story/Characters/Bianca.prefab", referenceBianca);
            Transform aStart = Anchor(staging, "Audere_Start", 1.5f, baseline, z);
            Transform aLean = Anchor(staging, "Audere_HalfStepTowardBianca", 1.32f, baseline, z);
            Transform bStart = Anchor(staging, "Bianca_StartFacingAway", -1.5f, baseline, z);
            Transform bMid = Anchor(staging, "Bianca_ApproachMid", -.5f, baseline, z);
            Transform bHello = Anchor(staging, "Bianca_Greeting", .5f, baseline, z);
            // Audere turns away from Bianca and continues right on the same walkway.
            RemoveLegacyDeparture(staging, board);
            Transform aAway1 = Anchor(staging, "Audere_FirstTileAway", 2.5f, baseline, z);
            Transform aAway3 = Anchor(staging, "Audere_ThreeTilesAway", 3.5f, baseline, z);
            Transform aQuestion = Anchor(staging, "Audere_QuestionWhileWalking", 4.5f, baseline, z);
            Transform aFinal = Anchor(staging, "Audere_FinalWalkPose", 5.5f, baseline, z);

            Transform tStart = Tile(board, "Tile_AudereStart", 1.5f, floor, referenceTile);
            Transform tBStart = Tile(board, "Tile_BiancaStart", -1.5f, floor, referenceTile);
            Transform tBMid = Tile(board, "Tile_BiancaMid", -.5f, floor, referenceTile);
            Transform tHello = Tile(board, "Tile_BiancaGreeting", .5f, floor, referenceTile);
            Transform tAway1 = Tile(board, "Tile_AudereFirstAway", 2.5f, floor, referenceTile);
            Transform tAway3 = Tile(board, "Tile_AudereThreeAway", 3.5f, floor, referenceTile);
            Transform tQuestion = Tile(board, "Tile_AudereQuestion", 4.5f, floor, referenceTile);
            Transform tFinal = Tile(board, "Tile_AudereFinal", 5.5f, floor, referenceTile);
            Transform[] tiles = { tStart, tBStart, tBMid, tHello, tAway1, tAway3, tQuestion, tFinal };
            foreach (Transform tile in tiles) tile.gameObject.SetActive(tile == tStart);
            audere.position = aStart.position;
            bianca.position = bStart.position;
            audere.gameObject.SetActive(true);
            bianca.gameObject.SetActive(false);
            audere.GetComponent<SpriteRenderer>().flipX = false;
            bianca.GetComponent<SpriteRenderer>().flipX = false;

            Folder(DialogueFolder);
            DialogueData notice = Dialogue("NOTICE", DialogueCharacterId.Timor,
                "R|Đừng nhìn nữa.", "L|Tớ có nhìn đâu.", "R|Ừ. Nhìn đường thôi.");
            DialogueData greeting = Dialogue("GREETING", DialogueCharacterId.Bianca,
                "R|Chào buổi sáng.", "L|…Chào.");
            DialogueData after = Dialogue("AFTER_PASSING", DialogueCharacterId.Timor,
                "L|Cậu ấy không nhắc chuyện tối qua.", "R|Vậy thì cậu đỡ phải lo rồi.", "L|Ừm…");
            DialogueData question = Dialogue("WORRIED_QUESTION", DialogueCharacterId.Timor,
                "L|Cậu ấy có giận tớ không?");
            DialogueData redirect = Dialogue("UNCERTAIN_REPLY", DialogueCharacterId.Timor,
                "R|Cậu đâu biết được.");

            GameObject story = Root(scene, "STORY") ?? new GameObject("STORY");
            StoryDirector director = story.GetComponent<StoryDirector>() ?? story.AddComponent<StoryDirector>();
            Transform eventRoot = Child(story.transform, "D2_SCHOOL_BIANCA_MORNING");
            StoryEvent main = eventRoot.GetComponent<StoryEvent>() ?? eventRoot.gameObject.AddComponent<StoryEvent>();
            for (int i = eventRoot.childCount - 1; i >= 0; i--) Object.DestroyImmediate(eventRoot.GetChild(i).gameObject);
            Set(main, "eventId", "D2_SCHOOL_BIANCA_MORNING", "autoPlayNextEvent", false, "nextEvent", null);
            Set(director, "storyEventsRoot", story.transform, "playOnStart", true, "startingEvent", main);

            Transform cameraStart = staging.Find("Camera_OpeningPose");
            if (cameraStart == null)
            {
                cameraStart = Child(staging, "Camera_OpeningPose");
                cameraStart.position = camera.transform.position;
            }
            camera.transform.position = cameraStart.position;
            var follow = camera.GetComponent<UnityEngine.Animations.PositionConstraint>();
            if (follow == null) follow = camera.gameObject.AddComponent<UnityEngine.Animations.PositionConstraint>();
            follow.enabled = false;
            follow.constraintActive = false;
            follow.locked = false;
            follow.SetSources(new System.Collections.Generic.List<UnityEngine.Animations.ConstraintSource>
            {
                new UnityEngine.Animations.ConstraintSource { sourceTransform = audere, weight = 1f }
            });
            follow.translationAxis = UnityEngine.Animations.Axis.X;
            follow.translationAtRest = cameraStart.position;
            follow.translationOffset = cameraStart.position - aStart.position;
            follow.weight = 1f;
            follow.locked = true;
            follow.constraintActive = true;
            EditorUtility.SetDirty(follow);

            Fade(Step<CanvasFadeStep>(main, "000_CoverArrival"), cover, 1f, 0f);
            Toggle(main, "002_StopCameraFollowForOpening", follow, false);
            Move(main, "003_ResetCameraOpeningPose", camera.transform, cameraStart, 0f);
            Active(main, "010_NormalizeSchool", new[] { audere.gameObject, tStart.gameObject },
                tiles.Where(t => t != tStart).Select(t => t.gameObject).Concat(new[] { bianca.gameObject }).ToArray());
            SpriteFade(main, "020_ResetBiancaVisibility", bianca, 1f, 0f);
            Move(main, "030_ResetAuderePose", audere, aStart, 0f);
            Move(main, "040_ResetBiancaPose", bianca, bStart, 0f);
            Facing(main, "050_AudereFacesLeft", audere, false);
            Facing(main, "060_BiancaFacesSameWay", bianca, false);
            Fade(Step<CanvasFadeStep>(main, "070_FadeIntoSchool"), cover, 0f, .65f);
            Wait(main, "080_ThreeSecondsAlone", 3f);
            TileTransition(main, "090_RevealBiancaTile", null, tBStart);
            Active(main, "100_ShowBiancaFacingAway", new[] { bianca.gameObject }, new GameObject[0]);
            Wait(main, "110_NoticeBianca", .18f);
            Hop(main, "120_AudereStartlesInPlace", audere, aStart, true);
            Move(main, "130_AudereHalfStepForward", audere, aLean, .14f);
            Move(main, "140_AudereWithdrawsImmediately", audere, aStart, .16f);
            Wait(main, "150_TimorNotices", .25f);
            Talk(main, "160_TimorStopsTheLook", notice, ui.Dialogue);
            Facing(main, "170_BiancaTurnsTowardAudere", bianca, true);
            TileTransition(main, "180_RevealBiancaMid", null, tBMid);
            Hop(main, "190_BiancaHopsToMid", bianca, bMid);
            TileTransition(main, "200_HideBiancaStart", tBStart, null);
            TileTransition(main, "210_RevealGreetingTile", null, tHello);
            Hop(main, "220_BiancaHopsToGreeting", bianca, bHello);
            TileTransition(main, "230_HideBiancaMid", tBMid, null);
            Wait(main, "240_SettleBeforeGreeting", .2f);
            Talk(main, "250_BriefMorningGreeting", greeting, ui.Dialogue);
            Facing(main, "255_AudereTurnsAwayFromBianca", audere, true);
            Toggle(main, "256_CameraFollowsAudere", follow, true);
            Wait(main, "260_AudereDoesNotStay", .18f);

            ParallelStoryStep leave = Step<ParallelStoryStep>(main, "270_AudereLeavesAsBiancaFades");
            StoryEvent walking = Branch(leave, "Audere_AuthoredDeparture", "D2_SCHOOL_DEPARTURE");
            StoryEvent fading = Branch(leave, "Bianca_AuthoredFade", "D2_SCHOOL_BIANCA_FADE");
            Set(leave, "branches", new Object[] { walking, fading });
            Walk(walking, "010", audere, aAway1, tStart, tAway1);
            Walk(walking, "040", audere, aAway3, tAway1, tAway3);
            Wait(fading, "010_HoldBiancaAsAudereLeaves", 1f);
            SpriteFade(fading, "020_BiancaFadesIntoDistance", bianca, 0f, 1.1f);
            Active(fading, "030_HideBiancaAfterFade", new GameObject[0], new[] { bianca.gameObject });
            TileTransition(fading, "040_HideBiancaGreetingTile", tHello, null);

            Talk(main, "280_AudereNoticesNoQuestion", after, ui.Dialogue);
            Wait(main, "290_DoubtReturns", 1.3f);
            WalkAndTalk(main, "300_QuestionWhileWalking", "QUESTION", audere, aQuestion, tAway3, tQuestion, question, ui.Dialogue);
            WalkAndTalk(main, "310_TimorAnswersWhileWalking", "REPLY", audere, aFinal, tQuestion, tFinal, redirect, ui.Dialogue);
            AuthorClassroom(scene, source, main, art, staging, audere, bianca, camera, follow, cover, ui);
            Day2SchoolCoopSetupTool.Author(scene);
            director.RefreshRegistry();
            EditorUtility.SetDirty(director);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void AuthorClassroom(Scene scene, Scene source, StoryEvent morning, Transform art,
            Transform staging, Transform audere, Transform bianca, Camera camera, Behaviour follow,
            CanvasGroup cover, GameplayUIRoot ui)
        {
            Transform referenceArt = Root(source, "CLASSROOM").transform.Find("CLASSROOM ART PLACEHOLDER");
            Transform referenceTile = referenceArt.Find("Board/Tile_AudereSeat");
            Transform referenceTeacher = referenceArt.Find("Actors/Teacher_PLACEHOLDER");
            Transform hallBoard = art.Find("Board");
            Transform classroomBoard = Child(art, "Classroom Board");
            Transform teacher = Actor(art.Find("Actors"), "Teacher_PLACEHOLDER",
                "Assets/_Audere/Prefabs/Story/Characters/Teacher.prefab", referenceTeacher);
            float floor = referenceTile.localPosition.y;
            float baseline = staging.Find("Audere_Start").localPosition.y;
            float z = staging.Find("Audere_Start").localPosition.z;
            // The last hallway landing is also Audere's classroom pose: preserve the match cut.
            float finalX = staging.Find("Audere_FinalWalkPose").localPosition.x;
            Transform aClass = Anchor(staging, "Audere_ClassroomLeft", finalX, baseline, z);
            Transform bClass = Anchor(staging, "Bianca_ClassroomBehindAudere", finalX - 1f, baseline, z);
            Transform tClass = Anchor(staging, "Teacher_ClassroomRight", finalX + 3.2f, referenceTeacher.localPosition.y, z);
            Transform aTile = Tile(classroomBoard, "Tile_ClassroomAudere", finalX, floor, referenceTile);
            Transform bTile = Tile(classroomBoard, "Tile_ClassroomBiancaBehind", finalX - 1f, floor, referenceTile);
            Transform tTile = Tile(classroomBoard, "Tile_ClassroomTeacher", finalX + 3.2f, floor, referenceTile);
            Transform mask = camera.transform.Find("PuzzleViewportMask");
            if (mask == null) throw new MissingReferenceException("Camera needs the authored PuzzleViewportMask.");
            teacher.position = tClass.position;
            teacher.gameObject.SetActive(false);
            classroomBoard.gameObject.SetActive(false);
            aTile.gameObject.SetActive(true);
            tTile.gameObject.SetActive(true);
            bTile.gameObject.SetActive(false);
            mask.gameObject.SetActive(true);

            // Finish the visible camera move before fading; class normalization is then a no-op
            // for Audere and the camera, while still supporting direct classroom playback.
            Transform cameraClass = Child(staging, "Camera_ClassroomPose");
            Vector3 openingCamera = staging.Find("Camera_OpeningPose").position;
            cameraClass.position = new Vector3((aClass.position.x + tClass.position.x) * .5f,
                openingCamera.y, openingCamera.z);
            Toggle(morning, "315_StopFollowAtLastTile", follow, false);
            Move(morning, "316_FrameClassroomFromLastTile", camera.transform, cameraClass, .8f);
            Wait(morning, "320_HoldUnansweredFeeling", .35f);

            var choiceView = ClassroomChoices(scene, ui);
            GameObject choiceUi = choiceView.transform.parent.gameObject;
            choiceUi.SetActive(false);
            // Replaying the morning must hide the classroom and restore the hallway board.
            SetActiveStep normalize = morning.transform.Find("010_NormalizeSchool").GetComponent<SetActiveStep>();
            Set(normalize, "objectsToEnable", normalize.ObjectsToEnable.Concat(new[] { hallBoard.gameObject, mask.gameObject }).Distinct().ToArray(),
                "objectsToDisable", normalize.ObjectsToDisable.Concat(new[] {
                    classroomBoard.gameObject, teacher.gameObject, choiceUi }).Distinct().ToArray());

            DialogueData request = Dialogue("CLASS_SUPPLIES_REQUEST", DialogueCharacterId.Teacher,
                "R|Giấy màu, băng dính còn ở kho mỹ thuật.",
                "R|Bạn nào tiện thì lấy giúp cô nhé.");
            DialogueData volunteer = Dialogue("CLASS_BIANCA_VOLUNTEERS", DialogueCharacterId.Bianca,
                "R|Em đi lấy ạ.");
            DialogueData partner = Dialogue("CLASS_TEACHER_NEEDS_PARTNER", DialogueCharacterId.Teacher,
                "R|Đồ hơi nhiều, cô cần thêm một bạn nữa.",
                "R|Ai đi cùng Bianca được nhỉ?");
            DialogueData silence = Dialogue("CLASS_TIMOR_SUGGESTS_SILENCE", DialogueCharacterId.Timor,
                "R|Chưa cần lên tiếng đâu.", "R|Sẽ có bạn khác xung phong mà.");
            string[] answers = { "…Em đi cùng Bianca ạ.", "Em cầm giúp một ít ạ.", "Để em đi cùng bạn ạ." };
            DialogueData[] replies = answers.Select((answer, index) =>
                Dialogue("CLASS_AUDERE_VOLUNTEER_" + (index + 1), DialogueCharacterId.Teacher, "L|" + answer)).ToArray();
            DialogueData relieved = Dialogue("CLASS_BIANCA_RELIEVED", DialogueCharacterId.Bianca,
                "R|À…", "R|Tớ cứ tưởng cậu giận tớ chuyện gì.", "R|Có cậu đi cùng thì tốt quá.");
            DialogueData boundary = Dialogue("CLASS_AUDERE_SMALL_BOUNDARY", DialogueCharacterId.Timor,
                "R|Cậu không cần phải làm thế.", "L|Tớ biết.", "R|Vậy—", "L|Tớ chỉ đi lấy đồ thôi.");

            Transform eventRoot = Child(morning.transform.parent, "D2_CLASSROOM_SUPPLIES");
            StoryEvent classroom = eventRoot.GetComponent<StoryEvent>();
            if (classroom == null) classroom = eventRoot.gameObject.AddComponent<StoryEvent>();
            for (int i = eventRoot.childCount - 1; i >= 0; i--) Object.DestroyImmediate(eventRoot.GetChild(i).gameObject);
            Set(classroom, "eventId", "D2_CLASSROOM_SUPPLIES", "autoPlayNextEvent", false, "nextEvent", null);
            Set(morning, "autoPlayNextEvent", true, "nextEvent", classroom);

            // Neutral location/time cut, using the same focused fades as Scene 30 recess.
            Fade(Step<CanvasFadeStep>(classroom, "000_FadeToClassroom"), cover, 1f, .32f);
            Toggle(classroom, "010_StopCameraFollowForClassroom", follow, false);
            Move(classroom, "020_ResetCameraForClassroom", camera.transform, cameraClass, 0f);
            Active(classroom, "030_StageClassroomUnderFade",
                new[] { classroomBoard.gameObject, aTile.gameObject, tTile.gameObject, audere.gameObject,
                    teacher.gameObject, mask.gameObject, choiceUi },
                new[] { hallBoard.gameObject, bianca.gameObject, bTile.gameObject });
            Move(classroom, "040_PlaceAudereLeft", audere, aClass, 0f);
            Move(classroom, "050_PlaceTeacherRight", teacher, tClass, 0f);
            Move(classroom, "060_PlaceBiancaBehindAudere", bianca, bClass, 0f);
            SpriteFade(classroom, "070_ResetBiancaForReveal", bianca, 0f, 0f);
            Facing(classroom, "080_AudereFacesTeacher", audere, true);
            Facing(classroom, "090_TeacherFacesClass", teacher, false);
            Facing(classroom, "100_BiancaFacesTeacher", bianca, true);
            Fade(Step<CanvasFadeStep>(classroom, "110_FadeIntoClassroom"), cover, 0f, .45f);
            Talk(classroom, "120_TeacherRequestsSupplies", request, ui.Dialogue);
            TileTransition(classroom, "130_RevealTileBehindAudere", null, bTile);
            Active(classroom, "140_ShowBiancaBehindAudere", new[] { bianca.gameObject }, new GameObject[0]);
            SpriteFade(classroom, "150_BiancaAppears", bianca, 1f, .24f);
            Talk(classroom, "160_BiancaVolunteers", volunteer, ui.Dialogue);
            Facing(classroom, "170_BiancaTurnsTowardExit", bianca, false);
            Talk(classroom, "180_TeacherAsksForPartner", partner, ui.Dialogue);
            Talk(classroom, "190_TimorSuggestsSilence", silence, ui.Dialogue);
            Wait(classroom, "200_AudereHesitates", .6f);
            StoryChoiceBranchStep choice = Step<StoryChoiceBranchStep>(classroom, "210_ChooseHowToVolunteer");
            StoryEvent[] branches = new StoryEvent[3];
            for (int i = 0; i < branches.Length; i++)
            {
                Transform branchRoot = Child(choice.transform, "0" + i + "_AudereAnswer");
                branches[i] = branchRoot.gameObject.AddComponent<StoryEvent>();
                Set(branches[i], "eventId", "D2_CLASSROOM_VOLUNTEER_" + (i + 1), "autoPlayNextEvent", false);
                Talk(branches[i], "010_AudereVolunteers", replies[i], ui.Dialogue);
            }
            Set(choice, "choiceView", choiceView, "options", answers, "branches", branches);
            Facing(classroom, "220_BiancaTurnsBackToAudere", bianca, true);
            Facing(classroom, "225_AudereLooksAtBianca", audere, false);
            Talk(classroom, "230_BiancaIsRelieved", relieved, ui.Dialogue);
            Wait(classroom, "240_TimorSaysNothing", 3f);
            Talk(classroom, "250_AudereKeepsHerSmallChoice", boundary, ui.Dialogue);
            Wait(classroom, "260_HoldAfterDecision", .75f);
        }

        private static Audere.Story.Presentation.StoryChoiceView ClassroomChoices(Scene scene, GameplayUIRoot ui)
        {
            GameObject root = Root(scene, "SCHOOL CHOICE UI");
            if (root == null)
            {
                const string eveningPath = "Assets/_Audere/Scenes/40_Evening.unity";
                Scene evening = SceneManager.GetSceneByPath(eveningPath);
                bool opened = !evening.isLoaded;
                if (opened) evening = EditorSceneManager.OpenScene(eveningPath, OpenSceneMode.Additive);
                try
                {
                    GameObject reference = Root(evening, "NIGHT MESSAGE UI");
                    root = Object.Instantiate(reference);
                    root.name = "SCHOOL CHOICE UI";
                    SceneManager.MoveGameObjectToScene(root, scene);
                    for (int i = root.transform.childCount - 1; i >= 0; i--)
                        if (root.transform.GetChild(i).name != "Reply Choices")
                            Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                }
                finally { if (opened) EditorSceneManager.CloseScene(evening, true); }
            }
            var view = root.GetComponentInChildren<Audere.Story.Presentation.StoryChoiceView>(true);
            Set(view, "inputGate", ui.InputGate);
            CanvasGroup group = view.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return view;
        }

        public static void LinkHome()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) return;
            Scene home = SceneManager.GetSceneByPath(HomePath);
            bool opened = !home.isLoaded;
            if (opened) home = EditorSceneManager.OpenScene(HomePath, OpenSceneMode.Additive);
            StoryEvent bus = home.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<StoryEvent>(true))
                .Single(e => e.EventId == "D2_TO_BUS_STOP");
            Transform end = bus.transform.Find("120_LoadDay2School");
            SceneLoadStep load = end != null ? end.GetComponent<SceneLoadStep>() : Step<SceneLoadStep>(bus, "120_LoadDay2School");
            Set(load, "sceneName", GameScenes.Day2SchoolMorning, "hidePuzzleUiBeforeLoad", true);
            load.transform.SetAsLastSibling();
            EditorSceneManager.MarkSceneDirty(home);
            EditorSceneManager.SaveScene(home);
            if (opened) EditorSceneManager.CloseScene(home, true);
        }

        private static void EnsureBuildScene()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            int index = scenes.FindIndex(s => s.path == ScenePath);
            if (index < 0) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            else scenes[index].enabled = true;
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        private static GameObject Root(Scene scene, string name) => scene.GetRootGameObjects().FirstOrDefault(r => r.name == name);
        internal static Transform Child(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;
            child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }
        private static Transform Anchor(Transform parent, string name, float x, float y, float z)
        {
            Transform anchor = Child(parent, name);
            anchor.localPosition = new Vector3(x, y, z);
            return anchor;
        }
        private static Transform Actor(Transform parent, string name, string path, Transform reference)
        {
            Transform actor = parent.Find(name);
            if (actor == null)
            {
                actor = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent)).transform;
                actor.name = name;
            }
            actor.localScale = reference.localScale;
            foreach (MonoBehaviour behaviour in actor.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (SpriteRenderer sr in actor.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.sortingLayerName = "Player";
                sr.sortingOrder = sr.transform == actor ? 5 : 4;
            }
            return actor;
        }
        private static Transform Tile(Transform parent, string name, float x, float y, Transform reference)
        {
            Transform tile = parent.Find(name);
            if (tile == null)
            {
                string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(reference.gameObject);
                tile = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent)).transform;
                tile.name = name;
            }
            tile.localPosition = new Vector3(x, y, 0f);
            tile.localScale = reference.localScale;
            SpriteRenderer[] source = reference.GetComponentsInChildren<SpriteRenderer>(true);
            SpriteRenderer[] target = tile.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < Mathf.Min(source.Length, target.Length); i++) target[i].color = source[i].color;
            return tile;
        }
        internal static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
        internal static DialogueData Dialogue(string suffix, DialogueCharacterId other, params string[] lines)
        {
            string name = "Dialogue_D2_SCHOOL_" + suffix;
            string path = DialogueFolder + "/" + name + ".asset";
            DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            // Existing dialogue and portrait edits belong to the authored asset.
            if (data != null) return data;
            data = ScriptableObject.CreateInstance<DialogueData>();
            AssetDatabase.CreateAsset(data, path);
            Set(data, "dialogueId", "d2-school-" + suffix.ToLowerInvariant().Replace('_', '-'),
                "leftCharacter", (int)DialogueCharacterId.Audere, "rightCharacter", (int)other);
            var so = new SerializedObject(data);
            SerializedProperty array = so.FindProperty("lines");
            array.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                string text = lines[i].Substring(2);
                if (text.Length > 42) throw new InvalidOperationException("Dialogue bubble exceeds 42 characters: " + text);
                SerializedProperty line = array.GetArrayElementAtIndex(i);
                line.FindPropertyRelative("speaker").intValue = lines[i][0] == 'L' ? 0 : 1;
                line.FindPropertyRelative("text").stringValue = text;
                line.FindPropertyRelative("portraitOverride").objectReferenceValue = null;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }
        internal static T Step<T>(StoryEvent owner, string name) where T : StoryStep => Child(owner.transform, name).gameObject.AddComponent<T>();
        private static StoryEvent Branch(ParallelStoryStep owner, string name, string id)
        {
            StoryEvent branch = Child(owner.transform, name).gameObject.AddComponent<StoryEvent>();
            Set(branch, "eventId", id, "autoPlayNextEvent", false);
            return branch;
        }
        private static void RemoveLegacyDeparture(Transform staging, Transform board)
        {
            foreach (string name in new[] { "Audere_PassInFront", "Audere_ClearBianca", "Audere_RejoinWalkway" })
            {
                Transform old = staging.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            foreach (string name in new[] { "Tile_AuderePassInFront", "Tile_AudereClearBianca", "Tile_AudereRejoin" })
            {
                Transform old = board.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
        }
        internal static void Toggle(StoryEvent owner, string name, Behaviour target, bool enable) =>
            Set(Step<SetBehaviourEnabledStep>(owner, name), "target", target, "enable", enable);
        internal static void Wait(StoryEvent owner, string name, float seconds) => Set(Step<WaitStep>(owner, name), "duration", seconds, "useUnscaledTime", true);
        internal static void Talk(StoryEvent owner, string name, DialogueData data, DialogueController controller) =>
            Set(Step<DialogueStep>(owner, name), "dialogueData", data, "dialogueController", controller);
        internal static void Move(StoryEvent owner, string name, Transform actor, Transform target, float seconds) =>
            Set(Step<MoveActorStep>(owner, name), "actor", actor, "targetTransform", target, "duration", seconds, "useUnscaledTime", true);
        internal static void Facing(StoryEvent owner, string name, Transform actor, bool right) =>
            Set(Step<SetActorFacingStep>(owner, name), "actorRenderer", actor.GetComponent<SpriteRenderer>(), "faceRight", right, "sourceSpriteFacesLeft", true);
        internal static void Fade(CanvasFadeStep step, CanvasGroup group, float alpha, float duration) =>
            Set(step, "canvasGroup", group, "targetAlpha", alpha, "duration", duration, "useUnscaledTime", true);
        internal static void Active(StoryEvent owner, string name, GameObject[] enable, GameObject[] disable) =>
            Set(Step<SetActiveStep>(owner, name), "objectsToEnable", enable, "objectsToDisable", disable);
        private static void Hop(StoryEvent owner, string name, Transform actor, Transform target, bool startle = false)
        {
            Transform shadow = actor.GetComponentsInChildren<SpriteRenderer>(true).Single(r => r.sortingOrder == 4).transform;
            Set(Step<CharacterMotionStep>(owner, name), "actor", actor, "targetTransform", target,
                "actorRenderer", actor.GetComponent<SpriteRenderer>(), "groundedShadow", shadow,
                "motionMode", startle ? 1 : 0, "duration", startle ? .19f : .32f,
                "arcHeight", startle ? .09f : .075f, "useUnscaledTime", true,
                "facingMode", startle ? 0 : 1, "sourceSpriteFacesLeft", true);
        }
        private static void TileTransition(StoryEvent owner, string name, Transform hide, Transform reveal) =>
            Set(Step<BoardTileTransitionStep>(owner, name),
                "objectsToHide", hide == null ? new Object[0] : new Object[] { hide },
                "objectsToReveal", reveal == null ? new Object[0] : new Object[] { reveal },
                "transitionDuration", .22f, "staggerDelay", 0f, "revealWaveDuration", .22f,
                "verticalOffset", .065f, "revealOvershoot", .012f, "useUnscaledTime", true);
        internal static void SpriteFade(StoryEvent owner, string name, Transform actor, float visibility, float duration)
        {
            SpriteRenderer[] renderers = actor.GetComponentsInChildren<SpriteRenderer>(true);
            // Fade is an explicitly requested presentation step, separate from grounded motion.
            Set(Step<SpriteGroupFadeStep>(owner, name), "renderers", renderers,
                "authoredAlphas", renderers.Select(r => {
                    SpriteRenderer source = PrefabUtility.GetCorrespondingObjectFromSource(r);
                    return source != null ? source.color.a : r.color.a;
                }).ToArray(),
                "targetVisibility", visibility, "duration", duration, "useUnscaledTime", true);
        }
        private static void Walk(StoryEvent owner, string prefix, Transform actor, Transform target, Transform oldTile, Transform nextTile)
        {
            TileTransition(owner, prefix + "_RevealNextTile", null, nextTile);
            Hop(owner, prefix + "_Hop", actor, target);
            TileTransition(owner, prefix + "_HidePreviousTile", oldTile, null);
        }
        private static void WalkAndTalk(StoryEvent owner, string name, string id, Transform actor, Transform target,
            Transform oldTile, Transform nextTile, DialogueData data, DialogueController controller)
        {
            ParallelStoryStep step = Step<ParallelStoryStep>(owner, name);
            StoryEvent motion = Branch(step, "AuthoredWalking", "D2_SCHOOL_" + id + "_WALK");
            StoryEvent speech = Branch(step, "AuthoredDialogue", "D2_SCHOOL_" + id + "_TALK");
            Set(step, "branches", new Object[] { motion, speech });
            Wait(motion, "000_LetDialogueEnter", .3f);
            Walk(motion, "010", actor, target, oldTile, nextTile);
            Talk(speech, "010_SpeakWhileWalking", data, controller);
        }
        internal static void Set(Object target, params object[] values)
        {
            var so = new SerializedObject(target);
            for (int i = 0; i < values.Length; i += 2)
            {
                string name = (string)values[i];
                SerializedProperty property = so.FindProperty(name);
                if (property == null) throw new MissingFieldException(target.GetType().Name, name);
                object value = values[i + 1];
                if (value == null) property.objectReferenceValue = null;
                else if (value is Object obj) property.objectReferenceValue = obj;
                else if (value is string str) property.stringValue = str;
                else if (value is bool flag) property.boolValue = flag;
                else if (value is float number) property.floatValue = number;
                else if (value is int integer) property.intValue = integer;
                else if (value is Object[] refs)
                {
                    property.arraySize = refs.Length;
                    for (int j = 0; j < refs.Length; j++) property.GetArrayElementAtIndex(j).objectReferenceValue = refs[j];
                }
                else if (value is string[] strings)
                {
                    property.arraySize = strings.Length;
                    for (int j = 0; j < strings.Length; j++) property.GetArrayElementAtIndex(j).stringValue = strings[j];
                }
                else if (value is float[] numbers)
                {
                    property.arraySize = numbers.Length;
                    for (int j = 0; j < numbers.Length; j++) property.GetArrayElementAtIndex(j).floatValue = numbers[j];
                }
                else throw new NotSupportedException(name);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
