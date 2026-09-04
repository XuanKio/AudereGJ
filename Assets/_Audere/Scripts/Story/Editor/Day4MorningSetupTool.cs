#if UNITY_EDITOR
using System;
using System.Linq;
using Audere.Audio;
using Audere.Core;
using Audere.Dialogue;
using Audere.Puzzle;
using Audere.Puzzle.Board;
using Audere.Puzzle.PathPieces;
using Audere.Story.Steps;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Audere.Story.Editor
{
    // Create-only: later hand-authored story content must not be replaced by a builder rerun.
    public static class Day4MorningSetupTool
    {
        public const string HomePath = "Assets/_Audere/Scenes/130_D4_Home_Morning.unity";
        public const string ClassroomPath = "Assets/_Audere/Scenes/140_D4_Classroom.unity";
        public const string DataFolder = "Assets/_Audere/Data/Puzzle/Day4";
        public const string DialoguePath = "Assets/_Audere/Data/Dialogue/Day4/Dialogue_D4_THREE_THINGS.asset";

        [MenuItem("Audere/Story/Create Missing Day 4 Morning")]
        public static void CreateMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save the current scene and leave Play before authoring Day 4.");
            EnsureFolder(DataFolder); EnsureFolder("Assets/_Audere/Data/Dialogue/Day4");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HomePath) == null) CreateHome();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ClassroomPath) == null) CreateClassroom();
            foreach (var path in new[] { HomePath, ClassroomPath })
            {
                var list = EditorBuildSettings.scenes.ToList();
                if (!list.Any(x => x.path == path)) list.Add(new EditorBuildSettingsScene(path, true));
                else list.First(x => x.path == path).enabled = true;
                EditorBuildSettings.scenes = list.ToArray();
            }
            LinkDayThree(); AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(HomePath);
        }

        private static void CreateHome()
        {
            if (!AssetDatabase.CopyAsset("Assets/_Audere/Scenes/20_D1_Home_Morning.unity", HomePath)) throw new InvalidOperationException("Home copy failed");
            var scene = EditorSceneManager.OpenScene(HomePath);
            var morning = All<StoryEvent>(scene).Single(x => x.EventId == "D1_HOME_MORNING");
            var busEvent = All<StoryEvent>(scene).Single(x => x.EventId == "D1_TO_BUS_STOP");
            var wash = All<PuzzleController>(scene).Single(x => x.PuzzleRoot.name == "PZ_D1_WASHROOM");
            var breakfast = All<PuzzleController>(scene).Single(x => x.PuzzleRoot.name == "PZ_D1_BREAKFAST");
            var bus = All<PuzzleController>(scene).Single(x => x.PuzzleRoot.name == "PZ_D1_BUS_STOP");
            Object.DestroyImmediate(busEvent.gameObject); Object.DestroyImmediate(bus.PuzzleRoot.gameObject);
            var keep = new[] { "05_PreparePuzzleSequence", "20_RevealWashroomBoard", "30_WashroomStepTileTutorial", "40_HideWashroomBoard", "60_RevealBreakfastBoard", "70_PlayBreakfastPuzzle", "80_HideBreakfastBoard" };
            foreach (Transform child in morning.transform.Cast<Transform>().ToArray()) if (!keep.Contains(child.name)) Object.DestroyImmediate(child.gameObject);
            morning.name = "D4_HOME_WITHOUT_A_VOICE";
            Set(morning, "eventId", morning.name, "autoPlayNextEvent", false, "nextEvent", null);
            Set(All<StoryDirector>(scene).Single(), "startingEvent", morning, "playOnStart", true);

            var washCells = new[] { V(0,0),V(0,1),V(1,0),V(1,1),V(2,0),V(2,1),V(2,2) };
            var breakfastCells = new[] { V(0,0),V(0,2),V(1,0),V(1,1),V(1,2),V(2,0),V(2,2),V(3,0),V(3,1),V(3,2) };
            ConfigureLevel(wash, "PZ_D4_WASHROOM", Puzzle("WASHROOM", "L_Corner_3", "L_Corner", "Line_2"), V(0,0), washCells, new[]{V(0,1),V(2,1)});
            ConfigureLevel(breakfast, "PZ_D4_BREAKFAST", Puzzle("BREAKFAST", "L_Corner", "Line_3", "L_Corner_3", "Line_2"), V(2,0), breakfastCells, new[]{V(1,2),V(3,0)});
            GoalItem(wash,"banchai",new Vector3(-.1f,.147f,0),2f);
            GoalItem(breakfast,"banhmi",new Vector3(.03f,.256f,0),1.36f);
            var washGoal = Goal(wash); var breakfastGoal = Goal(breakfast);
            breakfast.AlignPlayerStartToAnchor(washGoal.transform);
            var coordinator = All<PuzzleRootCoordinator>(scene).Single(); Set(coordinator,"puzzles",new Object[]{wash,breakfast});
            var prepare = morning.transform.Find("05_PreparePuzzleSequence").GetComponent<PuzzleSequencePrepareStep>();
            Set(prepare,"startingPuzzle",wash,"followingPuzzles",new Object[]{breakfast});
            prepare.name="004_PreparePuzzleSequence";
            Set(Step<SetActiveStep>(morning.transform,"005_KeepStandingTile"),"objectsToEnable",new Object[]{wash.PuzzleRoot.GetComponentsInChildren<BoardTile>(true).Single(x=>x.GridPosition==V(0,0)).gameObject});
            Set(morning.transform.Find("40_HideWashroomBoard").GetComponent<BoardTileTransitionStep>(),"sourcePuzzle",wash,"goalToBecomeAnchor",washGoal);
            Set(morning.transform.Find("60_RevealBreakfastBoard").GetComponent<BoardTileTransitionStep>(),"revealPuzzle",breakfast,"revealFromAnchor",washGoal.transform,
                "objectsToKeepHidden",new Object[]{breakfast.PuzzleRoot.GetComponentsInChildren<BoardTile>(true).First(x=>Vector3.Distance(x.transform.position,breakfast.Puzzle.PlayerStartTransform.position)<.001f).transform});
            Set(morning.transform.Find("70_PlayBreakfastPuzzle").GetComponent<PuzzleStep>(),"startFromAnchor",washGoal.transform);
            Set(morning.transform.Find("80_HideBreakfastBoard").GetComponent<BoardTileTransitionStep>(),"sourcePuzzle",breakfast,"goalToBecomeAnchor",breakfastGoal);
            morning.transform.Find("30_WashroomStepTileTutorial").name="30_PlayWashroom";
            foreach(var guide in All<StepTileTutorialGuide>(scene)) Object.DestroyImmediate(guide);
            foreach(var guide in All<UseAllPiecesTutorialGuide>(scene)) Object.DestroyImmediate(guide);
            foreach(var hud in All<PuzzleHud>(scene)) { hud.Clear(); hud.gameObject.SetActive(false); }
            var tutorialUI=All<Transform>(scene).FirstOrDefault(x=>x.name=="StepTile Tutorial UI"); if(tutorialUI!=null)tutorialUI.gameObject.SetActive(false);
            foreach(var manager in All<PuzzleManager>(scene))Set(manager,"hud",null);

            var cover = Cover("DAY FOUR HOME COVER"); var title = Title(scene,"DAY FOUR HOME TITLE");
            Fade(morning.transform,"000_ResetCover",cover,1,0); Fade(morning.transform,"001_ResetTitle",title,0,0);
            Card(morning.transform,"006_DayFour",title,"Ngày 4",.65f,1.1f);
            Fade(morning.transform,"007_TitleLeaves",title,0,.5f);
            Fade(morning.transform,"008_MorningLight",cover,0,1.1f);
            Wait(morning.transform,"009_NoReply",1.1f);
            var player=coordinator.SharedPlayer.transform;
            var anchors=new GameObject("DAY FOUR STAGING TARGETS").transform; anchors.gameObject.SetActive(false);
            var start=new GameObject("Audere_Waking").transform;start.SetParent(anchors,false);start.position=wash.Puzzle.PlayerStartTransform.position;
            Facing(morning.transform,"010_LookForTheVoice",player,start,CharacterFacingMode.FaceLeft,.3f);
            Wait(morning.transform,"011_StillQuiet",.7f);
            Facing(morning.transform,"012_LookBack",player,start,CharacterFacingMode.FaceRight,.3f);
            Set(Step<DialogueStep>(morning.transform,"013_ThreeThings"),"dialogueData",Opening());
            Wait(morning.transform,"014_A_Breath",.35f);
            Wait(morning.transform,"45_BrushingDone",.55f);
            Fade(morning.transform,"50_ChangeClothesCover",cover,1,.55f);
            Card(morning.transform,"51_ChangeClothes",title,"Thay đồ",.35f,.85f);
            Fade(morning.transform,"52_ClothesTitleLeaves",title,0,.3f);
            Fade(morning.transform,"53_BackToTheMorning",cover,0,.65f);
            Wait(morning.transform,"90_FinishBreakfast",.65f);
            Fade(morning.transform,"100_LeaveHome",cover,1,1f);
            Set(Step<SceneLoadStep>(morning.transform,"110_ToClassroom"),"sceneName",GameScenes.Day4Classroom,"hidePuzzleUiBeforeLoad",true);
            new GameObject("QUIET MORNING MUSIC",typeof(SceneMusicSpace));
            foreach(var w in All<Audere.World.WorldModeController>(scene))Set(w,"enableDebugHotkeys",false);
            Order(morning.transform); EditorSceneManager.SaveScene(scene);
        }

        private static void CreateClassroom()
        {
            if(!AssetDatabase.CopyAsset("Assets/_Audere/Scenes/30_Classroom.unity",ClassroomPath))throw new InvalidOperationException("Classroom copy failed");
            var scene=EditorSceneManager.OpenScene(ClassroomPath);
            var director=All<StoryDirector>(scene).Single();
            foreach(var e in All<StoryEvent>(scene))Object.DestroyImmediate(e.gameObject);
            var arrival=StepEvent(director.transform,"D4_CLASSROOM_ARRIVAL");Set(director,"startingEvent",arrival,"playOnStart",true);
            var actor=All<Transform>(scene).Single(x=>x.name=="Audere");
            var seat=All<Transform>(scene).Single(x=>x.name=="Audere_SeatPose");actor.position=seat.position;actor.gameObject.SetActive(true);
            foreach(var t in All<Transform>(scene).Where(x=>x.name=="Bianca_PLACEHOLDER"||x.name=="Teacher_PLACEHOLDER")) t.gameObject.SetActive(false);
            // Arrival only. Day 1's dialogues/encounters must never replay here.
            var cover=TransitionFade(scene);cover.alpha=1;cover.blocksRaycasts=true;cover.interactable=false;
            Fade(arrival.transform,"000_UnderCover",cover,1,0);
            Set(Step<MoveActorStep>(arrival.transform,"010_AtTheDesk"),"actor",actor,"targetTransform",seat,"duration",0f);
            Fade(arrival.transform,"020_ClassroomMorning",cover,0,1.1f);
            Wait(arrival.transform,"030_ArrivalSettles",.6f);
            new GameObject("QUIET CLASSROOM MUSIC",typeof(SceneMusicSpace));
            foreach(var w in All<Audere.World.WorldModeController>(scene))Set(w,"enableDebugHotkeys",false);
            Order(arrival.transform);EditorSceneManager.SaveScene(scene);
        }

        private static void LinkDayThree()
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Audere/Scenes/120_D3_School_Teacher.unity");
            var e=All<StoryEvent>(scene).Single(x=>x.EventId=="D3_BIANCA_REPRISE_AND_SILENCE");
            Set(Step<SceneLoadStep>(e.transform,"240_BeginDayFour"),"sceneName",GameScenes.Day4HomeMorning,"hidePuzzleUiBeforeLoad",true);
            Order(e.transform);EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureLevel(PuzzleController p,string name,PuzzleData data,Vector2Int start,Vector2Int[] cells,Vector2Int[] reds)
        {
            var root=p.PuzzleRoot;var outer=PrefabUtility.GetOutermostPrefabInstanceRoot(root.gameObject);
            if(outer!=null)PrefabUtility.UnpackPrefabInstance(outer,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            root.name=name;var board=p.Puzzle.Board;Set(p.Puzzle,"puzzleData",data,"retryWhenOutOfPieces",true,"failedAttemptResetDelay",.65f,"hud",null);
            foreach(var tile in board.BoardVisualRoot.GetComponentsInChildren<BoardTile>(true).Concat(board.LevelObjectiveRoot.GetComponentsInChildren<BoardTile>(true)).Distinct().ToArray())Object.DestroyImmediate(tile.gameObject);
            foreach(var cell in cells)
            {
                var type=cell==V(1,1)?PuzzleTileType.Goal:reds.Contains(cell)?PuzzleTileType.OneUse:PuzzleTileType.Grass;
                string path=type==PuzzleTileType.Goal?PuzzleContentConstants.AssetPaths.GoalPrefab:type==PuzzleTileType.OneUse?PuzzleContentConstants.AssetPaths.OneUsePrefab:PuzzleContentConstants.AssetPaths.GrassPrefab;
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),type==PuzzleTileType.Goal?board.LevelObjectiveRoot:board.BoardVisualRoot);
                go.name=type+" ("+cell.x+", "+cell.y+")";go.transform.position=board.GridSpace.CellToWorldCenter(cell);
                var so=new SerializedObject(go.GetComponent<BoardTile>());so.FindProperty("gridPosition").vector2IntValue=cell;so.FindProperty("tileType").intValue=(int)type;so.ApplyModifiedPropertiesWithoutUndo();
                if(type==PuzzleTileType.Goal)go.transform.Find("Visual Root/Goal Visual").gameObject.SetActive(false);
            }
            p.Puzzle.PlayerStartTransform.position=board.GridSpace.CellToWorldCenter(start);board.RegisterExistingTiles();
        }
        private static void GoalItem(PuzzleController p,string sprite,Vector3 position,float scale)
        {
            var goal=Goal(p);var t=goal.transform.Find("Visual Root/Item");t.localPosition=position;t.localScale=Vector3.one*scale;t.gameObject.SetActive(true);
            var r=t.GetComponent<SpriteRenderer>();r.sprite=Portrait("Item/"+sprite+".aseprite");r.color=Color.white;r.sortingOrder=3;
            var motion=t.GetComponent<GoalItemMotion>()??t.gameObject.AddComponent<GoalItemMotion>();motion.Motion=GoalItemMotionMode.Floating;Set(goal,"itemVisual",t.gameObject);
        }
        private static GoalTileBehaviour Goal(PuzzleController p)=>p.PuzzleRoot.GetComponentsInChildren<GoalTileBehaviour>(true).Single();
        private static PuzzleData Puzzle(string name,params string[] pieces)
        {
            var data=ScriptableObject.CreateInstance<PuzzleData>();AssetDatabase.CreateAsset(data,DataFolder+"/Puzzle_D4_"+name+".asset");
            Set(data,"puzzleId","D4_"+name,"requireAllPathPieces",true,"availablePathPieces",pieces.Select(x=>(Object)AssetDatabase.LoadAssetAtPath<PathPieceData>("Assets/_Audere/Data/Puzzle/PathPieces/PathPiece_"+x+".asset")).ToArray());return data;
        }
        private static DialogueData Opening()
        {
            var d=ScriptableObject.CreateInstance<DialogueData>();AssetDatabase.CreateAsset(d,DialoguePath);
            Set(d,"dialogueId","D4_THREE_THINGS","leftCharacter",(int)DialogueCharacterId.Audere,"rightCharacter",(int)DialogueCharacterId.None,"leftPortraitOverride",Portrait("Audere/Audere_Scared.png"));
            var so=new SerializedObject(d);var lines=so.FindProperty("lines");lines.arraySize=2;
            string[] text={"Đánh răng… thay đồ… ăn sáng.","Ba việc thôi."};
            for(int i=0;i<2;i++){var line=lines.GetArrayElementAtIndex(i);line.FindPropertyRelative("speaker").intValue=0;line.FindPropertyRelative("text").stringValue=text[i];line.FindPropertyRelative("characterOverride").intValue=0;line.FindPropertyRelative("portraitOverride").objectReferenceValue=i==1?Portrait("Audere/Audere_Tired.png"):null;}
            so.ApplyModifiedPropertiesWithoutUndo();return d;
        }
        private static void Facing(Transform p,string n,Transform a,Transform target,CharacterFacingMode facing,float duration)
        {
            var renderers=a.GetComponentsInChildren<SpriteRenderer>(true);
            Set(Step<CharacterMotionStep>(p,n),"actor",a,"targetTransform",target,"actorRenderer",renderers.First(x=>x.sortingOrder!=4),"groundedShadow",renderers.First(x=>x.sortingOrder==4).transform,
                "motionMode",(int)CharacterMotionMode.VerticalInPlace,"duration",duration,"arcHeight",0f,"travelStretch",0f,"landingDuration",0f,"facingMode",(int)facing);
        }
        private static CanvasGroup Cover(string name)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1350;
            var image=new GameObject("Neutral Cover",typeof(RectTransform),typeof(Image),typeof(CanvasGroup));image.transform.SetParent(go.transform,false);
            var rt=(RectTransform)image.transform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;image.GetComponent<Image>().color=Color.black;
            var group=image.GetComponent<CanvasGroup>();group.alpha=1;group.blocksRaycasts=true;return group;
        }
        private static CanvasGroup TransitionFade(Scene scene)=>All<CanvasGroup>(scene).Single(x=>x.name=="Fade"&&x.transform.parent!=null&&x.transform.parent.name=="Scene Transition Overlay");
        private static CanvasGroup Title(Scene s,string name)
        {
            var font=All<TMP_Text>(s).Select(x=>x.font).First(x=>x!=null);
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;go.GetComponent<Canvas>().sortingOrder=1400;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
            var text=new GameObject("Title",typeof(RectTransform),typeof(TextMeshProUGUI));text.transform.SetParent(go.transform,false);var rt=(RectTransform)text.transform;rt.anchorMin=new Vector2(.1f,.3f);rt.anchorMax=new Vector2(.9f,.7f);rt.offsetMin=rt.offsetMax=Vector2.zero;
            var label=text.GetComponent<TextMeshProUGUI>();label.font=font;label.fontSize=52;label.alignment=TextAlignmentOptions.Center;label.color=new Color(.84f,.86f,.89f,1);label.raycastTarget=false;
            var group=go.GetComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;return group;
        }
        private static void Card(Transform p,string name,CanvasGroup group,string text,float fade,float hold)=>Set(Step<StoryTitleCardStep>(p,name),"overlay",group,"titleText",group.GetComponentInChildren<TMP_Text>(),"title",text,"fadeDuration",fade,"holdDuration",hold,"leaveVisible",true);
        private static void Fade(Transform p,string n,CanvasGroup g,float alpha,float duration)=>Set(Step<CanvasFadeStep>(p,n),"canvasGroup",g,"targetAlpha",alpha,"duration",duration);
        private static void Wait(Transform p,string n,float time)=>Set(Step<WaitStep>(p,n),"duration",time);
        private static StoryEvent StepEvent(Transform p,string n){var e=StepComponent<StoryEvent>(p,n);Set(e,"eventId",n,"autoPlayNextEvent",false);return e;}
        private static T Step<T>(Transform p,string n)where T:StoryStep=>StepComponent<T>(p,n);
        private static T StepComponent<T>(Transform p,string n)where T:Component{var t=p.Find(n);if(t==null){t=new GameObject(n).transform;t.SetParent(p,false);}return t.GetComponent<T>()??t.gameObject.AddComponent<T>();}
        private static Vector2Int V(int x,int y)=>new Vector2Int(x,y);
        private static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<T>(true)).ToArray();
        private static Sprite Portrait(string p)=>AssetDatabase.LoadAllAssetsAtPath("Assets/_Audere/AssetGame/"+p).OfType<Sprite>().First();
        private static void EnsureFolder(string p){if(AssetDatabase.IsValidFolder(p))return;int i=p.LastIndexOf('/');EnsureFolder(p.Substring(0,i));AssetDatabase.CreateFolder(p.Substring(0,i),p.Substring(i+1));}
        private static void Order(Transform p){foreach(var t in p.Cast<Transform>().OrderBy(x=>int.Parse(x.name.Split('_')[0])).ToArray())t.SetAsLastSibling();}
        private static void Set(Object target,params object[] pairs)
        {
            var so=new SerializedObject(target);for(int i=0;i<pairs.Length;i+=2){var p=so.FindProperty((string)pairs[i]);var v=pairs[i+1];if(p==null)throw new InvalidOperationException(target.name+":"+pairs[i]);
                if(v is Object[] refs){p.arraySize=refs.Length;for(int j=0;j<refs.Length;j++)p.GetArrayElementAtIndex(j).objectReferenceValue=refs[j];}
                else if(v is string text)p.stringValue=text;else if(v is bool b)p.boolValue=b;else if(v is int n)p.intValue=n;else if(v is float f)p.floatValue=f;else p.objectReferenceValue=v as Object;}
            so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(target);
        }
    }
}
#endif
