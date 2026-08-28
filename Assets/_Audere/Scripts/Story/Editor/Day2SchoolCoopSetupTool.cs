#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Audere.Dialogue;
using Audere.Puzzle;
using Audere.Puzzle.Board;
using Audere.Puzzle.PathPieces;
using Audere.Story;
using Audere.Story.Steps;
using Audere.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using static Audere.EditorTools.Day2SchoolMorningSetupTool;

namespace Audere.EditorTools
{
    public static class Day2SchoolCoopSetupTool
    {
        private const string DataFolder = "Assets/_Audere/Data/Puzzle/Day2/School";
        private sealed class Layout
        {
            public Vector2Int A, B, GoalA, GoalB;
            public Vector2Int[] Cells, Red;
            public string[] Pieces;
        }
        private static Vector2Int P(int x, int y) => new Vector2Int(x, y);
        private static Layout[] Layouts() => new[]{new Layout { A=P(0,1),B=P(1,0),GoalA=P(3,1),GoalB=P(2,2),Cells=new[]{P(0,1),P(1,0),P(1,1),P(2,1),P(3,1),P(2,2)},Red=new[]{P(1,1)},Pieces=new[]{"Line_2","L_Corner_3","Line_3","Line_2"}},
new Layout { A=P(1,0),B=P(0,1),GoalA=P(2,2),GoalB=P(4,1),Cells=new[]{P(0,1),P(1,0),P(1,1),P(2,1),P(3,1),P(4,1),P(2,2)},Red=new[]{P(1,1)},Pieces=new[]{"Line_2","L_Corner_3","Line_4","Line_2"}},
new Layout { A=P(0,1),B=P(2,0),GoalA=P(4,1),GoalB=P(3,2),Cells=new[]{P(0,1),P(1,1),P(2,0),P(2,1),P(3,1),P(4,1),P(3,2)},Red=new[]{P(2,1)},Pieces=new[]{"Line_3","L_Corner_3","Line_3","Line_2"}}};

        // Focused migration: never rebuild SCHOOL, its events or the authored combat.
        [MenuItem("Audere/Story/Polish Existing Cooperative Puzzles Only")]
        public static void PolishExisting()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before editing cooperative puzzles.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != Day2SchoolMorningSetupTool.ScenePath)
                throw new InvalidOperationException("Open the Day 2 school scene first.");
            PolishExisting(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static void PolishExisting(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var levels = roots.Single(r => r.name == "SCHOOL").transform.Find("COOP PUZZLES");
            var story = roots.Single(r => r.name == "STORY").transform;
            var controls = roots.Single(r => r.name == "COOP PUZZLE CONTROLS");
            var returnFloor = roots.Single(r => r.name == "SCHOOL").transform.Find("SCHOOL ART PLACEHOLDER/Supplies Return Board");
            var stage = story.Find("D2_SCHOOL_COOP_01/010_StageTwoCarriers").GetComponent<SetActiveStep>();
            if (returnFloor != null)
                Set(stage, "objectsToDisable", stage.ObjectsToDisable.Concat(new[] { returnFloor.gameObject }).Distinct().ToArray());
            var label = controls.GetComponentInChildren<TMP_Text>(true);
            label.text = CooperativePuzzleControls.Objective;
            label.fontSize = 32f;
            label.rectTransform.anchoredPosition = new Vector2(0f, -35f);
            label.rectTransform.sizeDelta = new Vector2(1500f, 65f);
            label.raycastTarget = false;
            label.gameObject.SetActive(false);
            var group = controls.GetComponent<CanvasGroup>();
            group.alpha = 1f; group.blocksRaycasts = false; group.interactable = false;
            var layouts = Layouts();
            for (int i = 0; i < layouts.Length; i++)
            {
                Transform level = levels.Find("PZ_D2_COOP_0" + (i + 1));
                string retained = "Tile_" + layouts[i].Red[0].x + "_" + layouts[i].Red[0].y;
                foreach (var red in level.GetComponentsInChildren<CooperativeRedTileBehaviour>(true))
                {
                    if (red.name == retained) continue;
                    foreach (var renderer in red.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        var source = PrefabUtility.GetCorrespondingObjectFromSource(renderer);
                        if (source != null) renderer.color = source.color;
                    }
                    Object.DestroyImmediate(red);
                }
                var e = story.Find("D2_SCHOOL_COOP_0" + (i + 1)).GetComponent<StoryEvent>();
                Set(e.transform.Find("000_FadeToNextCarrySection").GetComponent<CanvasFadeStep>(), "duration", .30f);
                Set(e.transform.Find("060_FadeIntoSupplies").GetComponent<CanvasFadeStep>(), "duration", .40f);
                var objective = e.transform.Find("075_ShowCoopObjective");
                if (objective == null)
                {
                    Active(e, "075_ShowCoopObjective", new[] { label.gameObject }, new GameObject[0]);
                    objective = e.transform.Find("075_ShowCoopObjective");
                }
                objective.SetSiblingIndex(e.transform.Find("070_RevealCooperativeBoard").GetSiblingIndex() + 1);
            }
            // The second red hold no longer exists. Keep all other authored lines.
            var speech = story.Find("D2_SCHOOL_COOP_03/080_MindTheSharedRedTile").GetComponent<DialogueStep>();
            var data = new SerializedObject(speech.DialogueData);
            var line = data.FindProperty("lines").GetArrayElementAtIndex(1).FindPropertyRelative("text");
            if (line.stringValue == "Ừ. Qua rồi tớ giữ ô bên kia cho cậu.")
            {
                line.stringValue = "Ừ. Tớ qua trước nhé.";
                data.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        [MenuItem("Audere/Story/Author Day 2 Cooperative Supplies Puzzles")]
        public static void Setup() => Day2SchoolMorningSetupTool.Setup();

        public static void Author(Scene scene)
        {
            Folder(DataFolder);
            var roots = scene.GetRootGameObjects();
            Transform school = roots.Single(r=>r.name=="SCHOOL").transform;
            Transform art = school.Find("SCHOOL ART PLACEHOLDER");
            Transform actors = art.Find("Actors");
            Transform staging = school.Find("STAGING TARGETS");
            Transform story = roots.Single(r=>r.name=="STORY").transform;
            Camera camera = roots.Single(r=>r.name=="Main Camera").GetComponent<Camera>();
            CanvasGroup cover = roots.Single(r=>r.name=="Scene Transition Overlay").GetComponentInChildren<CanvasGroup>(true);
            GameplayUIRoot ui = roots.Single(r=>r.name=="GameplayUIRoot").GetComponent<GameplayUIRoot>();
            var font = ui.GetComponentsInChildren<TMP_Text>(true).First(t=>t.font!=null).font;
            GridSpace2D grid = Ensure<GridSpace2D>(school.gameObject);
            Set(grid,"cellSize",1f);
            var gridData=new SerializedObject(grid);
            gridData.FindProperty("localOrigin").vector2Value=new Vector2(.5f,0f);
            gridData.ApplyModifiedPropertiesWithoutUndo();
            GridPlayer audere = Ensure<GridPlayer>(actors.Find("Audere").gameObject);
            GridPlayer bianca = Ensure<GridPlayer>(actors.Find("Bianca_PLACEHOLDER").gameObject);
            foreach (GridPlayer actor in new[]{audere,bianca}) {
                actor.enabled=false;
                Set(actor,"stepDuration",.32f,"visualScale",1.5f,"stepArcHeight",.075f,"landingDuration",.10f);
            }
            PuzzleRuntime runtime = school.GetComponentInChildren<PuzzleRuntime>(true);
            if (runtime == null) {
                const string sourcePath="Assets/_Audere/Scenes/50_D2_Home_Morning.unity";
                Scene source=SceneManager.GetSceneByPath(sourcePath);
                bool opened=!source.isLoaded;
                if(opened) source=EditorSceneManager.OpenScene(sourcePath,OpenSceneMode.Additive);
                try {
                    var template=source.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PuzzleRuntime>(true)).Single();
                    var clone=Object.Instantiate(template.gameObject,school);
                    clone.name="Puzzle Runtime";
                    runtime=clone.GetComponent<PuzzleRuntime>();
                } finally {if(opened) EditorSceneManager.CloseScene(source,true);}
            }
            Set(runtime.Placement,"boardCamera",camera,"gridSpace",grid,"puzzleCanvas",ui.GameplayCanvas);
            runtime.gameObject.SetActive(true);
            var coordinator=Ensure<PuzzleRootCoordinator>(school.gameObject);
            Set(coordinator,"sharedPlayer",audere,"runtime",runtime,"validateOnAwake",false);
            Transform levelsRoot=Child(school,"COOP PUZZLES");
            var controls=Controls(scene,font);
            var worldObject=roots.FirstOrDefault(r=>r.name=="SCHOOL WORLD PRESENTATION") ?? new GameObject("SCHOOL WORLD PRESENTATION");
            var world=Ensure<WorldModeController>(worldObject);
            Set(world,"startingMode",(int)WorldGameplayMode.Story,"puzzleRoot",null,"combatRoot",null,"storyRoot",null,
                "puzzleViewportMask",camera.transform.Find("PuzzleViewportMask").gameObject,"storyUsesPuzzleViewportMask",true,
                "transitionFade",null,"allowChildFadeFallback",false,"revealStartingModeOnStart",false,"worldCamera",null,"puzzleCameraFollow",null,"enableDebugHotkeys",false);

            Layout[] layouts=Layouts();
            var levels=new PuzzleController[layouts.Length];
            Vector3 offset=new Vector3(-2.5f,0f,0f);
            for(int i=0;i<layouts.Length;i++) {
                if(i>0) offset+=(Vector3)(Vector2)(layouts[i-1].GoalA-layouts[i].A);
                levels[i]=Level(levelsRoot,i,layouts[i],offset,grid,audere,bianca,runtime,controls,ui,font);
            }
            Set(coordinator,"puzzles",levels);
            if(!coordinator.ValidateConfiguration(false)) throw new InvalidOperationException("Cooperative level references failed validation.");
            for(int i=1;i<levels.Length;i++) {
                var previous=levels[i-1].Puzzle.Cooperative;
                var next=levels[i].Puzzle.Cooperative;
                if(Vector3.Distance(previous.AudereGoal.transform.position,levels[i].Puzzle.PlayerStartTransform.position)>.0001f ||
                    Vector3.Distance(previous.PartnerGoal.transform.position,next.PartnerStart.position)>.0001f)
                    throw new InvalidOperationException("Both actors must keep their exact Goal-to-Start pose.");
            }

            StoryEvent classroom=story.Find("D2_CLASSROOM_SUPPLIES").GetComponent<StoryEvent>();
            StoryEvent morning=story.Find("D2_SCHOOL_BIANCA_MORNING").GetComponent<StoryEvent>();
            var events=new StoryEvent[3];
            for(int i=0;i<3;i++) {
                Transform root=Child(story,"D2_SCHOOL_COOP_0"+(i+1));
                for(int j=root.childCount-1;j>=0;j--) Object.DestroyImmediate(root.GetChild(j).gameObject);
                events[i]=Ensure<StoryEvent>(root.gameObject);
                Set(events[i],"eventId",root.name,"autoPlayNextEvent",false,"nextEvent",null);
            }
            Set(classroom,"autoPlayNextEvent",true,"nextEvent",events[0]);
            for(int i=0;i<2;i++) Set(events[i],"autoPlayNextEvent",true,"nextEvent",events[i+1]);
            var hideRoots=new[]{art.Find("Board").gameObject,art.Find("Classroom Board").gameObject,actors.Find("Teacher_PLACEHOLDER").gameObject,
                roots.Single(r=>r.name=="SCHOOL CHOICE UI")};
            for(int i=0;i<3;i++) {
                StoryEvent e=events[i];
                PuzzleController level=levels[i];
                CooperativePuzzleSession pair=level.Puzzle.Cooperative;
                Transform pose=Child(staging,"Camera_Coop_0"+(i+1));
                level.Puzzle.Board.TryGetWorldBounds(out Bounds bounds);
                pose.position=new Vector3(bounds.center.x,bounds.center.y+.11f,camera.transform.position.z);
                Fade(Step<CanvasFadeStep>(e,"000_FadeToNextCarrySection"),cover,1f,.65f);
                if(i==0) {
                    Toggle(e,"005_LockPuzzleCamera",camera.GetComponent<UnityEngine.Animations.PositionConstraint>(),false);
                    Active(e,"010_StageTwoCarriers",new[]{levelsRoot.gameObject,audere.gameObject,bianca.gameObject},hideRoots);
                    SpriteFade(e,"015_RestoreBianca",bianca.transform,1f,0f);
                    Mode(e,"020_ShowPuzzlePresentation",world,WorldGameplayMode.Puzzle);
                }
                var prepare=Step<PuzzleSequencePrepareStep>(e,"030_PrepareBothStarts");
                Set(prepare,"puzzleRootCoordinator",coordinator,"startingPuzzle",level,"followingPuzzles",levels.Where(p=>p!=level).ToArray(),
                    "showPlayerAtStart",true,"hideStartingBoardUntilReveal",true,"alignToPreviousGoal",i>0);
                var aStart=TileAt(level,layouts[i].A,layouts[i],offset:level.PuzzleRoot.localPosition);
                var bStart=TileAt(level,layouts[i].B,layouts[i],offset:level.PuzzleRoot.localPosition);
                Active(e,"040_KeepBothStandingTiles",new[]{aStart.gameObject,bStart.gameObject},new GameObject[0]);
                Move(e,"050_FrameBothActors",camera.transform,pose,i==0?0f:.5f);
                Fade(Step<CanvasFadeStep>(e,"060_FadeIntoSupplies"),cover,0f,.55f);
                var reveal=Step<BoardTileTransitionStep>(e,"070_RevealCooperativeBoard");
                Set(reveal,"puzzleRootCoordinator",coordinator,"revealPuzzle",level,"objectsToReveal",
                    level.PuzzleRoot.GetComponentsInChildren<BoardTile>(true).Where(t=>t!=aStart&&t!=bStart)
                        .OrderBy(t=>Vector3.Distance(t.transform.position,aStart.transform.position)).Select(t=>t.transform).ToArray(),
                    "transitionDuration",.22f,"revealWaveDuration",.95f,"verticalOffset",.065f,"revealOvershoot",.012f,"useUnscaledTime",true,
                    "normalizeOnCancel",prepare);
                if(i==0) {
                    Talk(e,"080_CarrySuppliesBack",Day2SchoolMorningSetupTool.Dialogue("COOP_PICKUP",DialogueCharacterId.Bianca,
                        "R|Đủ rồi. Mình bê về lớp nhé.","L|Để tớ cầm băng dính."),ui.Dialogue);
                    Talk(e,"090_TimorChoosesWhoGoesFirst",Day2SchoolMorningSetupTool.Dialogue("COOP_TIMOR_START",DialogueCharacterId.Timor,
                        "R|Để Bianca đi trước.","R|Chờ cô ấy mở đường rồi cậu theo sau."),ui.Dialogue);
                    Talk(e,"100_BiancaNeedsAudereToo",Day2SchoolMorningSetupTool.Dialogue("COOP_RED_FIRST_HOLD",DialogueCharacterId.Bianca,
                        "R|Giữ ô đỏ đó nhé. Tớ qua cùng cậu.","L|…Ừ. Cậu qua đi."),ui.Dialogue);
                } else if(i==1) {
                    Talk(e,"080_ReverseTheRoles",Day2SchoolMorningSetupTool.Dialogue("COOP_CHANGE_ROLES",DialogueCharacterId.Bianca,
                        "R|Lần này để tớ giữ. Cậu qua trước đi.","L|Đợi tớ một chút nhé.","R|Ừ, tớ đang giữ mà."),ui.Dialogue);
                } else {
                    Talk(e,"080_MindTheSharedRedTile",Day2SchoolMorningSetupTool.Dialogue("COOP_SHARED_RED_V2",DialogueCharacterId.Bianca,
                        "L|Để tớ giữ ô đỏ đầu tiên nhé.","R|Ừ. Qua rồi tớ giữ ô bên kia cho cậu."),ui.Dialogue);
                }
                var play=Step<PuzzleStep>(e,"110_PlayTogether");
                Set(play,"puzzleController",level,"puzzleRoot",level.PuzzleRoot.gameObject,"resetBeforePlay",false,"normalizeOnCancel",prepare);
                var collapse=Step<BoardTileTransitionStep>(e,"120_KeepBothGoalAnchors");
                Set(collapse,"puzzleRootCoordinator",coordinator,"sourcePuzzle",level,"goalToBecomeAnchor",pair.AudereGoal.GetComponent<GoalTileBehaviour>(),
                    "objectsToHide",level.PuzzleRoot.GetComponentsInChildren<BoardTile>(true)
                        .Where(t=>t!=pair.AudereGoal&&t!=pair.PartnerGoal).Select(t=>t.transform).Reverse().ToArray(),
                    "transitionDuration",.20f,"staggerDelay",.02f,"useUnscaledTime",true,"normalizeOnCancel",prepare);
                Wait(e,"150_HoldSharedLanding",.5f);
            }
            Day2SchoolSuppliesReturnSetupTool.Author(scene, events[2], world, cover, camera, audere, bianca, ui);
            // Morning/classroom replay must return from puzzle mode without hiding either actor.
            ModeAt(morning,"001_RestoreStoryPresentation",world,1);
            ModeAt(classroom,"005_RestoreStoryPresentation",world,1);
            foreach(var normalize in new[]{morning.transform.Find("010_NormalizeSchool").GetComponent<SetActiveStep>(),
                classroom.transform.Find("030_StageClassroomUnderFade").GetComponent<SetActiveStep>()})
                Set(normalize,"objectsToDisable",normalize.ObjectsToDisable.Concat(new[]{levelsRoot.gameObject}).Distinct().ToArray());
            // A newly authored GridPlayer initializes on Bianca's first activation; reassert her authored facing afterwards.
            var face=morning.transform.Find("101_BiancaStillFacesAway");
            if(face==null) {Facing(morning,"101_BiancaStillFacesAway",bianca.transform,false);face=morning.transform.Find("101_BiancaStillFacesAway");}
            face.SetSiblingIndex(morning.transform.Find("100_ShowBiancaFacingAway").GetSiblingIndex()+1);
            levelsRoot.gameObject.SetActive(false);
            camera.transform.position=staging.Find("Camera_OpeningPose").position;
            camera.transform.Find("PuzzleViewportMask").gameObject.SetActive(true);
            story.GetComponent<StoryDirector>().RefreshRegistry();
            PolishExisting(scene);
        }

        private static PuzzleController Level(Transform parent,int index,Layout layout,Vector3 offset,GridSpace2D grid,
            GridPlayer audere,GridPlayer bianca,PuzzleRuntime runtime,CooperativePuzzleControls controls,GameplayUIRoot ui,TMP_FontAsset font)
        {
            string id="PZ_D2_COOP_0"+(index+1);
            Transform root=Child(parent,id);
            for(int i=root.childCount-1;i>=0;i--) Object.DestroyImmediate(root.GetChild(i).gameObject);
            root.localPosition=offset;root.localRotation=Quaternion.identity;root.localScale=Vector3.one;root.gameObject.SetActive(true);
            Transform boardRoot=Child(root,"StepTile Board"),goalRoot=Child(root,"Goal Root"),systems=Child(root,"Puzzle Systems");
            var board=systems.gameObject.AddComponent<BoardManager>();
            var manager=systems.gameObject.AddComponent<PuzzleManager>();
            var controller=systems.gameObject.AddComponent<PuzzleController>();
            var pair=systems.gameObject.AddComponent<CooperativePuzzleSession>();
            var aStart=Child(root,"PlayerStart");aStart.localPosition=(Vector3)(Vector2)layout.A;
            var playerStart=aStart.gameObject.AddComponent<PuzzlePlayerStart>();
            var bStart=Child(root,"BiancaStart");bStart.localPosition=(Vector3)(Vector2)layout.B;
            var tiles=new Dictionary<Vector2Int,BoardTile>();
            foreach(Vector2Int cell in layout.Cells) {
                string path=cell==layout.GoalA?PuzzleContentConstants.AssetPaths.GoalPrefab:
                    PuzzleContentConstants.AssetPaths.GrassPrefab;
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),cell==layout.GoalA?goalRoot:boardRoot);
                go.name="Tile_"+cell.x+"_"+cell.y;
                go.transform.localPosition=(Vector3)(Vector2)cell;
                go.transform.localScale=Vector3.one;
                var tile=go.GetComponent<BoardTile>();
                tiles.Add(cell,tile);
                if(layout.Red.Contains(cell)) {
                    var visual=go.GetComponentsInChildren<SpriteRenderer>(true).First();
                    visual.color=new Color(.78f,.24f,.29f,1f);
                    var red=go.AddComponent<CooperativeRedTileBehaviour>();
                    Set(red,"session",pair,"tileRenderer",visual);
                }
                // Goal items from the source prefab do not represent these supplies.
                var goal=go.GetComponent<GoalTileBehaviour>();
                if(goal!=null) {
                    Transform visual=go.transform.Find("Visual Root");
                    if(visual!=null) foreach(Transform child in visual) if(child.name!="Tile Visual") child.gameObject.SetActive(false);
                }
            }
            Label(tiles[layout.GoalA].transform,"AudereDestination","A",new Vector3(-.33f,.26f,-.1f),font,Color.white,3.5f);
            Label(tiles[layout.GoalB].transform,"BiancaDestination","B",new Vector3(-.33f,.26f,-.1f),font,Color.white,3.5f);
            string dataPath=DataFolder+"/"+id+".asset";
            var data=AssetDatabase.LoadAssetAtPath<PuzzleData>(dataPath);
            if(data==null){data=ScriptableObject.CreateInstance<PuzzleData>();AssetDatabase.CreateAsset(data,dataPath);}
            var pieces=layout.Pieces.Select(p=>AssetDatabase.LoadAssetAtPath<PathPieceData>("Assets/_Audere/Data/Puzzle/PathPieces/PathPiece_"+p+".asset")).ToArray();
            if(pieces.Any(p=>p==null))throw new InvalidOperationException("Missing existing path piece.");
            Set(data,"puzzleId",id,"requireAllPathPieces",true,"availablePathPieces",pieces);
            Set(board,"gridSpace",grid,"boardVisualRoot",boardRoot,"levelObjectiveRoot",goalRoot,
                "tileCatalog",AssetDatabase.LoadAssetAtPath<PuzzleTileCatalog>(PuzzleContentConstants.AssetPaths.TileCatalog));
            Set(manager,"puzzleData",data,"board",board,"playerStart",playerStart,"player",audere,"hand",ui.PathPieceHand,
                "runtime",runtime,"placement",runtime.Placement,"placedPathRoot",runtime.PlacedPathRoot,"retryWhenOutOfPieces",true,"cooperative",pair);
            Set(controller,"puzzle",manager,"puzzleRoot",root,"cameraFollow",null,"playOnStart",false);
            Set(pair,"puzzle",manager,"partner",bianca,"partnerStart",bStart,"audereGoal",tiles[layout.GoalA],"partnerGoal",tiles[layout.GoalB],"controls",controls);
            Transform beats=Child(root,"Carry Presentation");
            var aFade=ActorFade(beats,"Audere arrives and fades",audere.transform,0f,.4f);
            var bFade=ActorFade(beats,"Bianca arrives and fades",bianca.transform,0f,.4f);
            var aRestore=ActorFade(beats,"Restore Audere for new attempt",audere.transform,1f,0f);
            var bRestore=ActorFade(beats,"Restore Bianca for new attempt",bianca.transform,1f,0f);
            var encouragement=Child(beats,"Encouragement after second path").gameObject.AddComponent<DialogueStep>();
            string[][] lines={
                new[]{"R|Cứ từ từ nhé. Tớ giữ được.","L|Ừ. Tớ qua đây."},
                new[]{"L|Chồng giấy có nặng không?","R|Vẫn được. Sắp tới lớp rồi."},
                new[]{"R|Còn một đoạn thôi.","L|Ừ. Tớ giữ cho cậu qua."}};
            Set(encouragement,"dialogueData",Day2SchoolMorningSetupTool.Dialogue("COOP_ENCOURAGE_"+(index+1),DialogueCharacterId.Bianca,lines[index]),"dialogueController",ui.Dialogue);
            Set(pair,"audereArrivalFade",aFade,"partnerArrivalFade",bFade,"audereRestore",aRestore,"partnerRestore",bRestore,"encouragement",encouragement);
            board.RegisterExistingTiles();
            return controller;
        }

        private static BoardTile TileAt(PuzzleController level,Vector2Int cell,Layout layout,Vector3 offset) =>
            level.PuzzleRoot.GetComponentsInChildren<BoardTile>(true).Single(t=>t.name=="Tile_"+cell.x+"_"+cell.y);
        private static void Label(Transform parent,string name,string value,Vector3 local,TMP_FontAsset font,Color color,float size)
        {
            Transform t=Child(parent,name);t.localPosition=local;
            var label=t.gameObject.AddComponent<TextMeshPro>();label.font=font;label.text=value;label.fontSize=size;
            label.color=color;label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(1f,.6f);
            label.GetComponent<MeshRenderer>().sortingOrder=3;
        }
        private static CooperativePuzzleControls Controls(Scene scene,TMP_FontAsset font)
        {
            var root=scene.GetRootGameObjects().FirstOrDefault(r=>r.name=="COOP PUZZLE CONTROLS");
            if(root==null) root=new GameObject("COOP PUZZLE CONTROLS",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=70;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
            var group=Ensure<CanvasGroup>(root);group.alpha=0f;group.interactable=false;group.blocksRaycasts=false;
            var controls=Ensure<CooperativePuzzleControls>(root);
            for(int i=root.transform.childCount-1;i>=0;i--)Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            var statusObject=new GameObject("Path rules",typeof(RectTransform),typeof(TextMeshProUGUI));statusObject.transform.SetParent(root.transform,false);
            var status=statusObject.GetComponent<TextMeshProUGUI>();status.font=font;status.fontSize=27f;status.alignment=TextAlignmentOptions.Center;
            status.color=new Color(.9f,.85f,.95f);status.raycastTarget=false;status.text="Nối path vào ô của người cần đi · R xoay";
            var rect=status.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1f);rect.pivot=new Vector2(.5f,1f);
            rect.anchoredPosition=new Vector2(0,-30);rect.sizeDelta=new Vector2(1100,130);
            Set(controls,"group",group,"status",status);
            return controls;
        }
        private static SpriteGroupFadeStep ActorFade(Transform parent,string name,Transform actor,float visibility,float duration)
        {
            var step=Child(parent,name).gameObject.AddComponent<SpriteGroupFadeStep>();
            var renderers=actor.GetComponentsInChildren<SpriteRenderer>(true);
            Set(step,"renderers",renderers,"authoredAlphas",renderers.Select(r=>{
                var source=PrefabUtility.GetCorrespondingObjectFromSource(r);
                return source!=null?source.color.a:r.color.a;
            }).ToArray(),"targetVisibility",visibility,"duration",duration,"useUnscaledTime",true);
            return step;
        }
        private static T Ensure<T>(GameObject go) where T:Component {var c=go.GetComponent<T>();return c!=null?c:go.AddComponent<T>();}
        private static void Mode(StoryEvent e,string name,WorldModeController world,WorldGameplayMode mode) =>
            Set(Step<WorldModeStep>(e,name),"worldModeController",world,"targetMode",(int)mode,"waitUntilTransitionFinished",true);
        private static void ModeAt(StoryEvent e,string name,WorldModeController world,int index)
        {
            var t=e.transform.Find(name);
            if(t==null){Mode(e,name,world,WorldGameplayMode.Story);t=e.transform.Find(name);}
            t.SetSiblingIndex(index);
        }
    }
}
#endif
