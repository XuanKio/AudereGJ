#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Audere.Audio;
using Audere.Combat;
using Audere.Core;
using Audere.Dialogue;
using Audere.Puzzle;
using Audere.Story;
using Audere.Story.Presentation;
using Audere.Story.Steps;
using Audere.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using static Audere.EditorTools.Day2SchoolMorningSetupTool;

namespace Audere.EditorTools
{
    /// <summary>Creates missing Day 3 content. Existing scene/prefab/DialogueData edits are retained.</summary>
    public static class Day3SchoolSetupTool
    {
        public const string HomePath="Assets/_Audere/Scenes/100_D3_Home_Morning.unity";
        public const string BoardPath="Assets/_Audere/Scenes/110_D3_School_Board.unity";
        public const string TeacherPath="Assets/_Audere/Scenes/120_D3_School_Teacher.unity";
        public const string DrawingPath="Assets/_Audere/Prefabs/Story/ChalkDrawingUI.prefab";
        public const string EncounterPath="Assets/_Audere/Data/Combat/Teacher/CombatEncounter_D3_TEACHER_PRESSURE.asset";
        public const string EnemyPath="Assets/_Audere/Data/Combat/Teacher/Enemy_Teacher_PLACEHOLDER.asset";
        public const string FatiguePath="Assets/_Audere/Data/Transitions/WorldTransition_FatigueSway.asset";
        private const string CombatFolder="Assets/_Audere/Data/Combat/Teacher";
        private const string FontPath="Assets/_Audere/AssetGame/Font/Mynerve-Regular SDF.asset";
        private const string PlayerPath="Assets/_Audere/Prefabs/Puzzle/Actors/Player.prefab";
        private const string GrassPath="Assets/_Audere/Prefabs/Puzzle/Tiles/Grass.prefab";
        private const string EnemyPrefabPath="Assets/_Audere/Prefabs/Combat/Enemies/Enemy_Teacher_PLACEHOLDER.prefab";

        [MenuItem("Audere/Story/Author Day 3 Board and Teacher")]
        public static void Author()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before authoring Day 3.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save dirty scenes first. Day 3 author never discards scene edits.");
            Scene original=SceneManager.GetActiveScene();
            Scene source=SceneManager.GetSceneByPath(Day2NightDreamSetupTool.HomePath);
            bool opened=!source.isLoaded;
            if(opened)source=EditorSceneManager.OpenScene(Day2NightDreamSetupTool.HomePath,OpenSceneMode.Additive);
            try
            {
                DrawingPrefab(); FatigueProfile(); EnemyAndEncounter();
                CreateMissing(HomePath,source,BuildHome);
                CreateMissing(BoardPath,source,BuildBoard);
                CreateMissing(TeacherPath,source,BuildTeacher);
                LinkDay2();
                var scenes=EditorBuildSettings.scenes.ToList();
                foreach(string p in new[]{HomePath,BoardPath,TeacherPath})
                {var old=scenes.FirstOrDefault(x=>x.path==p);if(old==null)scenes.Add(new EditorBuildSettingsScene(p,true));else old.enabled=true;}
                EditorBuildSettings.scenes=scenes.ToArray();
                AssetDatabase.SaveAssets();
            }
            finally
            {
                if(opened)EditorSceneManager.CloseScene(source,true);
                if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);
            }
            Debug.Log("[Day3] Missing scenes/assets created; existing authored content preserved.");
        }

        private static void CreateMissing(string path,Scene source,Action<Stage> build)
        {
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)!=null)return;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var stage=Common(scene,source);build(stage);
            EditorSceneManager.SaveScene(scene,path);EditorSceneManager.CloseScene(scene,true);
        }

        private static Stage Common(Scene scene,Scene source)
        {
            foreach(string name in new[]{"Main Camera","Directional Light","EventSystem","Scene Transition Overlay"})
            {var go=Object.Instantiate(source.GetRootGameObjects().Single(x=>x.name==name));go.name=name;SceneManager.MoveGameObjectToScene(go,scene);}
            var camera=scene.GetRootGameObjects().Single(x=>x.name=="Main Camera").GetComponent<Camera>();
            camera.transform.position=new Vector3(0,-.04f,-10);camera.orthographicSize=1.25f;
            var fade=scene.GetRootGameObjects().Single(x=>x.name=="Scene Transition Overlay").GetComponentInChildren<CanvasGroup>(true);
            fade.gameObject.SetActive(true);fade.alpha=1;fade.blocksRaycasts=true;
            var ui=Prefab("Assets/_Audere/Prefabs/UI/GameplayUIRoot.prefab",null,"GameplayUIRoot").GetComponent<GameplayUIRoot>();
            ui.PuzzleUi.gameObject.SetActive(false);
            var world=ChildRoot("WORLD");var story=Child(world,"Story Root");
            story.localScale=Vector3.one*.25f;story.position=new Vector3(0,-.04f,0);
            var anchors=Child(world,"STAGING TARGETS");anchors.gameObject.SetActive(false);
            var actor=Actor(story,"Audere",PlayerPath,Vector2.zero);
            var tile=Tile(story,0,0,"Start Tile");
            var pose=Anchor(anchors,"Audere_Opening",actor.position);
            var cameraPose=Anchor(anchors,"Camera_Opening",camera.transform.position);
            var director=ChildRoot("STORY").gameObject.AddComponent<StoryDirector>();
            return new Stage{Scene=scene,World=world,Story=story,Anchors=anchors,Audere=actor,StartTile=tile,
                ActorStart=pose,CameraStart=cameraPose,Camera=camera,Fade=fade,Ui=ui,Director=director};
        }

        private static void BuildHome(Stage s)
        {
            s.Story.name="Home Stage PLACEHOLDER_NO_ART";
            var e=Event(s,"D3_HOME_MORNING");Opening(e,s);
            var title=Title(s,"Ngày 3",10,new Vector2(.5f,.82f));
            Set(Step<StoryTitleCardStep>(e,"020_DayThree"),"overlay",title.group,"titleText",title.text,
                "title","Ngày 3","fadeDuration",.45f,"holdDuration",.7f,"leaveVisible",true);
            Say(e,"030_AwakeBeforeTheInstruction",D("HOME_AWAKE",DialogueCharacterId.Timor,"Audere_Tired","TimorLolang",
                "R|Audere.","L|Tớ dậy rồi.","R|…Ừ.","R|Vậy đầu tiên—"));
            Wait(e,"040_CutTheRoutineShort",.2f);
            Set(Step<PlayAudioStep>(e,"050_SchoolBell"),"audioId",(int)AudioId.School_Bell);
            Cover(e,"060_FadeToSchool",s.Fade,1,.9f);
            Load(e,"070_ArriveAtSchool",GameScenes.Day3SchoolBoard);
        }

        private static void BuildBoard(Stage s)
        {
            s.Story.name="School Board Stage PLACEHOLDER_NO_ART";
            var e=Event(s,"D3_SCHOOL_DECORATE_BOARD");
            var tiles=new List<Transform>{s.StartTile};
            for(int i=1;i<=6;i++){var tile=Tile(s.Story,i,0,"Walk Tile "+i);tile.gameObject.SetActive(false);tiles.Add(tile);}
            var bianca=Actor(s.Story,"Bianca","Assets/_Audere/Prefabs/Story/Characters/Bianca.prefab",new Vector2(6,0));
            bianca.GetComponent<SpriteRenderer>().flipX=true;bianca.gameObject.SetActive(false);
            var bPose=Anchor(s.Anchors,"Bianca_BesideBoard",bianca.position);
            var aPoses=Enumerable.Range(1,5).Select(i=>Anchor(s.Anchors,"Audere_Walk_"+i,s.ActorStart.position+Vector3.right*(i*.25f))).ToArray();
            var follow=s.Camera.gameObject.AddComponent<PositionConstraint>();
            follow.SetSources(new List<ConstraintSource>{new ConstraintSource{sourceTransform=s.Audere,weight=1}});
            follow.translationAxis=Axis.X;follow.translationAtRest=s.CameraStart.position;
            follow.translationOffset=s.CameraStart.position-s.ActorStart.position;follow.weight=1;follow.locked=true;follow.constraintActive=true;follow.enabled=false;
            var centered=Anchor(s.Anchors,"Camera_TwoClassmates",new Vector3(1.375f,-.04f,-10));
            var drawing=Prefab(DrawingPath,null,"Chalk Drawing UI").GetComponent<ChalkDrawingView>();
            var fx=AddFullscreen(s);

            Cover(e,"000_CoverArrival",s.Fade,1,0);
            Toggle(e,"001_DisableCameraFollow",follow,false);
            Move(e,"002_ResetCamera",s.Camera.transform,s.CameraStart,0);
            Move(e,"003_ResetAudere",s.Audere,s.ActorStart,0);
            Move(e,"004_ResetBianca",bianca,bPose,0);
            Active(e,"005_NormalizeWalk",new[]{s.Audere.gameObject,s.StartTile.gameObject},tiles.Skip(1).Select(t=>t.gameObject).Concat(new[]{bianca.gameObject}).ToArray());
            Facing(e,"006_BiancaInitiallyFacesBoard",bianca,true);
            Cover(e,"010_RevealSchool",s.Fade,0,.6f);
            Toggle(e,"015_FollowHorizontalTravelOnly",follow,true);
            var speech=new[]{
                D("WALK_BOARD_ONLY",DialogueCharacterId.Timor,"Audere_Tired","TimorLolang","R|Hôm nay cậu cứ làm phần bảng thôi."),
                D("WALK_NO_MORE",DialogueCharacterId.Timor,"Audere_Tired","TimorLolang","R|Không cần nhận thêm gì cả."),
                D("WALK_AUDERE",DialogueCharacterId.Timor,"Audere_Tired","TimorLolang","R|Audere?"),
                D("WALK_LISTENING",DialogueCharacterId.Timor,"Audere_Tired","TimorLolang","L|Tớ nghe."),
                D("WALK_THATS_ENOUGH",DialogueCharacterId.Timor,"Audere_Tired","TimorLolang","R|Ừ. Vậy là được.")};
            for(int i=0;i<5;i++)
            {
                int id=20+i*10;
                if(i==3)Active(e,"049_BiancaComesIntoView",new[]{bianca.gameObject,tiles[6].gameObject},new GameObject[0]);
                var parallel=Step<ParallelStoryStep>(e,id+"_WalkAndListen");
                var walk=Branch(parallel,"Movement","D3_WALK_"+i);var talk=Branch(parallel,"Speech","D3_WALK_SPEECH_"+i);
                Set(parallel,"branches",new Object[]{walk,talk});
                Wait(walk,"000_HearTheStart",.28f);
                TileTransition(walk,"010_NextTile",null,tiles[i+1]);
                Hop(walk,"020_OneStepRight",s.Audere,aPoses[i],false);
                TileTransition(walk,"030_PreviousTileFades",tiles[i],null);
                Say(talk,"000_TimorSpeaks",speech[i]);
            }
            Toggle(e,"070_StopFollowAfterFifthTile",follow,false);
            Facing(e,"080_BiancaTurnsToAudere",bianca,false);
            Move(e,"085_CenterThePair",s.Camera.transform,centered,.35f);
            Say(e,"090_Greeting",D("BIANCA_GREETING",DialogueCharacterId.Bianca,"Audere_Tired",null,
                "R|Chào Audere. Hôm nay cậu ổn không?","L|Chào cậu."));
            Say(e,"100_AgreeOnTheBoard",D("BOARD_AGREEMENT",DialogueCharacterId.Bianca,"Audere_Tired",null,
                "R|Hôm nay cậu trang trí bảng nhé.","R|Tớ phụ cậu chuẩn bị đồ.","R|Cần gì cứ gọi tớ.","L|Ừ.","L|Cảm ơn cậu nhiều nha."));
            Say(e,"110_FinishThenRest",D("FINISH_THEN_REST",DialogueCharacterId.Timor,"Audere_Tired","TimorLolang",
                "R|Mình làm nhanh rồi nghỉ nhé, Audere."));
            Set(Step<ChalkDrawingStep>(e,"120_DrawWithChalk"),"view",drawing);
            Hop(e,"130_BiancaBrightensAtTheDrawing",bianca,bPose,true,.055f);
            Say(e,"140_BiancaLikesTheDetails",D("BIANCA_PRAISE",DialogueCharacterId.Bianca,"Audere_Tired",null,
                "R|Đẹp quá!","R|Cậu vẽ nét này khéo thật đấy.","R|Bảng lớp mình sáng hẳn lên rồi.","L|…Ừ."));
            Wait(e,"150_AudereNeedsASecond",.65f);
            Say(e,"160_LastNightHasNotPassed",D("SLEEPLESS",DialogueCharacterId.Bianca,"Audere_Tired","Bianca_Worried",
                "L|Xin lỗi… cậu vừa nói gì?","R|Audere, cậu mệt à?","L|Đêm qua tớ cứ tỉnh giấc.",
                "L|Nhắm mắt lại là gặp giấc mơ đó.","R|Vậy cậu nghỉ chút nhé.","L|Chỉ hơi chóng mặt thôi."));
            var dizzy=Step<ParallelStoryStep>(e,"170_TheRoomDriftsWhileBiancaCalls");
            var effect=Branch(dizzy,"WorldSway","D3_FATIGUE_SWAY");var calls=Branch(dizzy,"BiancaCalls","D3_BIANCA_CALLS");
            Set(dizzy,"branches",new Object[]{effect,calls});
            Set(Step<FullscreenPresentationStep>(effect,"000_SharedFatigueProfile"),"controller",fx,"profile",Required<FullscreenTransitionProfile>(FatiguePath),"focusRenderer",s.Audere.GetComponent<SpriteRenderer>());
            Set(Step<AutoDialogueStep>(calls,"000_NoClickNeededForHerCalls"),"dialogueData",D("BIANCA_CALLS",DialogueCharacterId.Bianca,"Audere_Tired","Bianca_Worried",
                "R|Audere?","R|Audere, cậu có nghe tớ không?"),"minimumLineDuration",1.8f,"charactersPerSecond",16f);
            Cover(e,"180_FadeToTheTeacher",s.Fade,1,.7f);
            Load(e,"190_TeacherChecksOnHer",GameScenes.Day3SchoolTeacher);
        }

        private static void BuildTeacher(Stage s)
        {
            s.Story.name="Teacher Check In Stage PLACEHOLDER_NO_ART";
            s.StartTile.localPosition=Vector3.left;
            s.Audere.position+=Vector3.left*.25f;s.ActorStart.position=s.Audere.position;
            Tile(s.Story,1,0,"Teacher Tile");
            var teacher=Actor(s.Story,"Teacher PLACEHOLDER","Assets/_Audere/Prefabs/Story/Characters/Teacher.prefab",Vector2.right);
            teacher.GetComponent<SpriteRenderer>().flipX=false;
            var tPose=Anchor(s.Anchors,"Teacher_FacesAudere",teacher.position);
            var combatRoot=Child(s.World,"Combat Root");
            var board=Prefab("Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab",combatRoot,"CombatBoard").GetComponent<CombatBoardView>();
            board.gameObject.SetActive(true);
            board.GetComponent<Canvas>().worldCamera=s.Camera;
            var bso=new SerializedObject(board);
            var mount=(Transform)bso.FindProperty("enemyMount").objectReferenceValue;
            if(mount==null)mount=board.transform.Find("Enemy/Enemy Mount");
            if(mount==null)throw new MissingReferenceException("Shared board needs Enemy Mount.");
            // Only inside a newly-created board instance. Preserve shared prefab and all existing scenes.
            foreach(var old in mount.GetComponentsInChildren<CombatEnemyActor>(true))Object.DestroyImmediate(old.gameObject);
            var enemy=Prefab(EnemyPrefabPath,mount,"Enemy_Teacher_PLACEHOLDER").GetComponent<CombatEnemyActor>();
            Set(board,"authoredEnemyActor",enemy);
            var enemyText=(TMP_Text)new SerializedObject(board).FindProperty("enemyNameText").objectReferenceValue;
            enemyText.text="Cô giáo";enemyText.fontSize=57;enemyText.enableAutoSizing=false;
            var systems=new GameObject("SYSTEMS").transform;
            var controller=Child(systems,"Combat Systems").gameObject.AddComponent<CombatController>();
            Set(controller,"boardView",board,"encounterData",Required<CombatEncounterData>(EncounterPath),"playOnStart",false);
            var mode=s.World.gameObject.AddComponent<WorldModeController>();
            Set(mode,"startingMode",(int)WorldGameplayMode.Story,"storyRoot",s.Story.gameObject,"combatRoot",combatRoot.gameObject,
                "combatSystemsRoot",controller.gameObject,"worldCamera",s.Camera,"puzzleViewportMask",s.Camera.transform.Find("PuzzleViewportMask").gameObject,
                "allowChildFadeFallback",false,"revealStartingModeOnStart",false,"enableDebugHotkeys",false);
            var mso=new SerializedObject(mode);
            mso.FindProperty("storyCameraPosition").vector3Value=s.Camera.transform.position;
            mso.FindProperty("combatCameraPosition").vector3Value=new Vector3(0,0,-10);
            mso.FindProperty("storyOrthographicSize").floatValue=1.25f;
            mso.FindProperty("combatOrthographicSize").floatValue=1.25f;mso.ApplyModifiedPropertiesWithoutUndo();
            combatRoot.gameObject.SetActive(false);
            var fx=AddFullscreen(s);var e=Event(s,"D3_TEACHER_CHECK_IN_PRESSURE");Opening(e,s);
            Move(e,"015_ResetTeacher",teacher,tPose,0);
            Facing(e,"020_TeacherFacesAudere",teacher,false);
            Say(e,"030_AQuietPlaceToRest",D("TEACHER_CHECKS",DialogueCharacterId.Teacher,"Audere_Tired",null,
                "R|Audere, em ngồi nghỉ một chút nhé.","R|Cô ở đây. Không cần vội.","L|Dạ… em xin lỗi.",
                "R|Có gì phải xin lỗi đâu.","R|Để cô lấy cho em chút nước nhé."));
            Wait(e,"040_HerConcernBecomesAQuestion",.6f);
            Say(e,"050_TimorInterpretsThePause",D("TEACHER_PROJECTION",DialogueCharacterId.Timor,"Audere_Tired","TimorLoLangKhongVui",
                "R|Giờ cô cũng phải dừng việc lại vì cậu.","L|Cô chỉ đang hỏi tớ thôi.","R|Ừ. Nhưng cô đã thấy cậu thế này.",
                "R|Lần sau cô còn dám giao việc cho cậu à?","L|Tớ chỉ cần nghỉ một lát."));
            Wait(e,"060_LeaveTheSentenceUnfinished",.45f);
            Say(e,"070_CouldSheBeThinking",D("TEACHER_UNFINISHED",DialogueCharacterId.Timor,"Audere_Scared","TimorLoLangKhongVui",
                "R|Cậu có nghĩ cô ấy đang…"));
            Set(Step<FullscreenWorldModeTransitionStep>(e,"080_EnterTeacherPressure"),"transitionController",fx,"worldModeController",mode,
                "transitionProfile",Required<FullscreenTransitionProfile>("Assets/_Audere/Data/Transitions/WorldTransition_DreamyDisorientation.asset"),
                "focusRenderer",s.Audere.GetComponent<SpriteRenderer>(),"sourceMode",2,"targetMode",1);
            Set(Step<CombatStep>(e,"090_PlayTeacherPressure"),"combatController",controller,"combatEncounterData",Required<CombatEncounterData>(EncounterPath),
                "victoryBehaviour",0,"defeatBehaviour",2,"specialBehaviour",1);
            Cover(e,"100_CoverAfterCombat",s.Fade,1,.45f);
            Set(Step<WorldModeStep>(e,"110_ReturnToTeacher"),"worldModeController",mode,"targetMode",2);
            Cover(e,"120_RevealTeacherAgain",s.Fade,0,.55f);
            Wait(e,"130_AftermathUnresolved",.6f);
        }

        private static void DrawingPrefab()
        {
            if(AssetDatabase.LoadAssetAtPath<GameObject>(DrawingPath)!=null)return;
            Folder("Assets/_Audere/Prefabs/Story");Folder("Assets/_Audere/Materials/UI");
            string matPath="Assets/_Audere/Materials/UI/ChalkDrawing.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(mat==null){mat=new Material(Shader.Find("Audere/UI/Chalk"));AssetDatabase.CreateAsset(mat,matPath);}
            var go=new GameObject("Chalk Drawing UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CanvasGroup),typeof(ChalkDrawingView));
            try
            {
                var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1100;
                var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=1f;
                var group=go.GetComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;group.interactable=false;
                var blocker=RectChild(go.transform,"Fullscreen Dimmer");Stretch(blocker);
                var bg=blocker.gameObject.AddComponent<Image>();bg.color=new Color(0,0,0,.78f);bg.raycastTarget=true;
                var board=RectChild(go.transform,"Board Image - bang.aseprite");board.sizeDelta=new Vector2(1360,700);board.anchoredPosition=new Vector2(0,50);
                var image=board.gameObject.AddComponent<Image>();image.sprite=SpriteAt("Assets/_Audere/AssetGame/bang.aseprite");image.preserveAspect=true;image.raycastTarget=false;
                var mask=RectChild(board,"Drawable Interior Mask");Stretch(mask);mask.offsetMin=new Vector2(44,64);mask.offsetMax=new Vector2(-44,-44);mask.gameObject.AddComponent<RectMask2D>();
                var surfaceRect=RectChild(mask,"Chalk Strokes - Player Input");Stretch(surfaceRect);
                var surface=surfaceRect.gameObject.AddComponent<ChalkDrawingSurface>();surface.color=new Color(.96f,.96f,.90f,1);surface.material=mat;surface.raycastTarget=true;
                Label(go.transform,"Heading","Trang trí bảng lớp",new Vector2(0,455),new Vector2(1200,65),38);
                Label(go.transform,"Instruction","Giữ chuột trái và kéo trong bảng để vẽ.",new Vector2(0,-330),new Vector2(1400,60),29);
                var buttonRect=RectChild(go.transform,"Hoàn thành");buttonRect.sizeDelta=new Vector2(320,78);buttonRect.anchoredPosition=new Vector2(0,-420);
                var buttonImage=buttonRect.gameObject.AddComponent<Image>();buttonImage.color=new Color(.15f,.30f,.28f,1);
                var button=buttonRect.gameObject.AddComponent<Button>();button.targetGraphic=buttonImage;
                var colors=button.colors;colors.disabledColor=new Color(.35f,.35f,.35f,.55f);colors.highlightedColor=new Color(.8f,1,1,1);button.colors=colors;
                Label(buttonRect,"Label","Hoàn thành",Vector2.zero,new Vector2(300,70),34);
                Set(go.GetComponent<ChalkDrawingView>(),"group",group,"surface",surface,"completeButton",button);
                PrefabUtility.SaveAsPrefabAsset(go,DrawingPath);
            }
            finally{Object.DestroyImmediate(go);}
        }

        private static void FatigueProfile()
        {
            Asset<FullscreenTransitionProfile>(FatiguePath,p=>
            {
                Set(p,"profileId","fatigue-sway","displayName","Fatigue Sway — no mode swap",
                    "material",Required<Material>("Assets/_Audere/Materials/PostProcess/FullscreenDreamyDisorientation.mat"),
                    "duration",4.4f,"modeSwapTime",2.2f,"usesFocusRenderer",true);
                var so=new SerializedObject(p);var tracks=so.FindProperty("floatTracks");
                string[] props={"_RotationDegrees","_Zoom","_WaveStrength","_DriftX","_DriftY","_RadialStrength","_SmearStrength","_ChromaticOffset","_VeilStrength"};
                float[] peaks={1.1f,1.025f,.009f,.004f,.003f,.007f,.003f,.0006f,0};
                tracks.arraySize=props.Length;
                for(int i=0;i<props.Length;i++)
                {var t=tracks.GetArrayElementAtIndex(i);t.FindPropertyRelative("shaderProperty").stringValue=props[i];float neutral=i==1?1:0;
                    t.FindPropertyRelative("values").animationCurveValue=new AnimationCurve(new Keyframe(0,neutral),new Keyframe(1.1f,i==0?-.6f:Mathf.Lerp(neutral,peaks[i],.65f)),new Keyframe(2.7f,peaks[i]),new Keyframe(4.4f,neutral));}
                so.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        private static void EnemyAndEncounter()
        {
            Folder(CombatFolder+"/Moves");Folder("Assets/_Audere/Prefabs/Combat/Enemies");Folder("Assets/_Audere/Prefabs/Combat/Bullets");
            EnemyPrefab();
            var chalk=Bullet("Bullet_ChalkRod",new Vector2(120,19));
            var stream=Required<CombatBulletView>("Assets/_Audere/Prefabs/Combat/Bullets/EnemyBullet.prefab");
            var fence=Asset<ChalkFenceMove>(CombatFolder+"/Moves/Move_ChalkFence.asset",p=>
            {Set(p,"duration",6f,"projectilePrefab",chalk);Audere.Combat.Editor.TeacherRadialTrailSetupTool.EnableTrail(p);});
            var sweep=Asset<ChalkSweepMove>(CombatFolder+"/Moves/Move_ChalkSweep.asset",p=>
            {Set(p,"duration",6f,"projectilePrefab",chalk);Audere.Combat.Editor.TeacherRadialTrailSetupTool.EnableTrail(p);});
            var rain=Asset<SineProjectileStreamMove>(CombatFolder+"/Moves/Move_ChalkSineStream.asset",p=>Set(p,"duration",7f,"projectilePrefab",stream));
            var impulse=Asset<VerticalPlayerImpulseMove>(CombatFolder+"/Moves/Move_VerticalImpulse.asset",p=>Set(p,"duration",6f));
            var impulseSweep=Asset<CompositeCombatMove>(CombatFolder+"/Moves/Move_ChalkSweepAndImpulse.asset",p=>Set(p,"duration",6f,"children",new Object[]{sweep,impulse}));
            var laser=Asset<NarrativePressurePatternMove>(CombatFolder+"/Moves/Move_TeacherLaserColumns.asset",p=>Set(p,"duration",7f,"projectilePrefab",stream,"pattern",1,
                "waveInterval",2.4f,"telegraphDuration",.85f,"intensity",3,"safeGapFraction",.42f));
            var squeeze=Asset<ShiftingBattleBoxMove>(CombatFolder+"/Moves/Move_TeacherFieldShift.asset",p=>
            {Set(p,"duration",7f,"telegraphDuration",.65f,"squeezeDuration",.65f,"holdDuration",.7f,"returnDuration",.6f);
                var so=new SerializedObject(p);var poses=so.FindProperty("poses");poses.arraySize=2;
                for(int i=0;i<2;i++){poses.GetArrayElementAtIndex(i).FindPropertyRelative("widthFraction").floatValue=.82f;poses.GetArrayElementAtIndex(i).FindPropertyRelative("normalizedX").floatValue=i==0?-.6f:.6f;}so.ApplyModifiedPropertiesWithoutUndo();});
            var final=Asset<CompositeCombatMove>(CombatFolder+"/Moves/Move_TeacherFinalPressure.asset",p=>Set(p,"duration",7f,"children",new Object[]{rain,laser}));
            var squeezeSweep=Asset<CompositeCombatMove>(CombatFolder+"/Moves/Move_TeacherShiftAndSweep.asset",p=>Set(p,"duration",7f,"children",new Object[]{squeeze,sweep}));
            var radial=Asset<RadialInwardTrailMove>(CombatFolder+"/Moves/Move_TeacherRadialInwardTrails.asset",p=>
            {Set(p,"duration",8.2f,"projectilePrefab",chalk);Audere.Combat.Editor.TeacherRadialTrailSetupTool.EnableTrail(p);});
            var sets=new[]{SetAsset("ChalkCorridor",fence,sweep),SetAsset("ForcedRhythm",radial,sweep,fence),SetAsset("OverlappingPressure",final,squeezeSweep)};
            var barks=new[]{
                D("COMBAT_PROJECTION_01",DialogueCharacterId.Timor,"Audere_Scared","TimorLoLangKhongVui","R|…nghĩ lẽ ra không nên giao cậu làm?"),
                D("COMBAT_PROJECTION_02",DialogueCharacterId.Timor,"Audere_Scared","TimorLoLangKhongVui","R|Chỉ một việc thôi mà cậu cũng mệt."),
                D("COMBAT_PROJECTION_03",DialogueCharacterId.Timor,"Audere_Scared","TimorLoLangKhongVui","R|Lần sau cô sẽ không gọi cậu nữa đâu.")};
            var enemy=Asset<CombatEnemyDefinition>(EnemyPath,p=>
            {
                var so=new SerializedObject(p);so.FindProperty("enemyId").stringValue="d3-teacher-perceived-pressure";so.FindProperty("displayName").stringValue="Cô giáo";
                so.FindProperty("actorPrefab").objectReferenceValue=Required<CombatEnemyActor>(EnemyPrefabPath);
                so.FindProperty("phasePolicy").intValue=1;so.FindProperty("sharedMaxHealth").intValue=15;
                var phases=so.FindProperty("phases");phases.arraySize=3;
                for(int i=0;i<3;i++)
                {
                    var phase=phases.GetArrayElementAtIndex(i);phase.FindPropertyRelative("phaseId").stringValue=new[]{"chalk-corridor","forced-rhythm","overlapping-pressure"}[i];
                    phase.FindPropertyRelative("maxHealth").intValue=15;phase.FindPropertyRelative("sharedExitThreshold").intValue=new[]{7,4,0}[i];
                    phase.FindPropertyRelative("moveSet").objectReferenceValue=sets[i];phase.FindPropertyRelative("spawnDice").boolValue=true;
                    phase.FindPropertyRelative("allowsPlayerDefeat").boolValue=true;phase.FindPropertyRelative("advanceOnMoveComplete").boolValue=false;
                    var cues=phase.FindPropertyRelative("dialogueCues");cues.arraySize=1;var cue=cues.GetArrayElementAtIndex(0);
                    cue.FindPropertyRelative("cueId").stringValue="d3-teacher-phase-"+i;cue.FindPropertyRelative("trigger").intValue=0;
                    cue.FindPropertyRelative("presentation").intValue=1;cue.FindPropertyRelative("minimumLineDuration").floatValue=1.7f;cue.FindPropertyRelative("charactersPerSecond").floatValue=18;
                    var seq=cue.FindPropertyRelative("sequence");seq.arraySize=1;seq.GetArrayElementAtIndex(0).objectReferenceValue=barks[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();if(!p.Validate(out string error))throw new InvalidOperationException(error);
            });
            Asset<CombatEncounterData>(EncounterPath,p=>Set(p,"encounterId","d3-teacher-perceived-pressure","enemyDefinition",enemy,
                "encounterDuration",90f,"dicePerBatch",3,"maximumAttacksPerBatch",2,"bulletTimePenaltySeconds",3f,"victoryFadeDuration",.75f));
        }

        private static void EnemyPrefab()
        {
            if(AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath)!=null)return;
            var go=Object.Instantiate(Required<GameObject>("Assets/_Audere/Prefabs/Combat/Enemies/Enemy_KhoangLang_PLACEHOLDER.prefab"));
            try
            {
                go.name="Enemy_Teacher_PLACEHOLDER";
                var image=go.GetComponentsInChildren<Image>(true).First(x=>x.sprite!=null);
                image.name="Teacher Visual PLACEHOLDER - editable";
                image.sprite=Required<GameObject>("Assets/_Audere/Prefabs/Story/Characters/Teacher.prefab").GetComponent<SpriteRenderer>().sprite;
                image.color=Color.white;image.preserveAspect=true;
                image.rectTransform.sizeDelta=new Vector2(160,240);
                PrefabUtility.SaveAsPrefabAsset(go,EnemyPrefabPath);
            }finally{Object.DestroyImmediate(go);}
        }
        private static CombatBulletView Bullet(string name,Vector2 size)
        {
            string path="Assets/_Audere/Prefabs/Combat/Bullets/"+name+".prefab";
            var existing=AssetDatabase.LoadAssetAtPath<CombatBulletView>(path);if(existing!=null)return existing;
            var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(CombatBulletView));
            go.GetComponent<RectTransform>().sizeDelta=size;
            var image=go.GetComponent<Image>();image.sprite=SpriteAt("Assets/_Audere/AssetGame/Item/phan.aseprite");image.color=Color.white;image.raycastTarget=false;
            PrefabUtility.SaveAsPrefabAsset(go,path);Object.DestroyImmediate(go);return Required<CombatBulletView>(path);
        }
        private static CombatMoveSet SetAsset(string name,params CombatMoveDefinition[] moves)=>Asset<CombatMoveSet>(CombatFolder+"/Moves/MoveSet_"+name+".asset",p=>
        {var so=new SerializedObject(p);var entries=so.FindProperty("entries");entries.arraySize=moves.Length;
            for(int i=0;i<moves.Length;i++){entries.GetArrayElementAtIndex(i).FindPropertyRelative("move").objectReferenceValue=moves[i];entries.GetArrayElementAtIndex(i).FindPropertyRelative("weight").floatValue=1;}
            so.ApplyModifiedPropertiesWithoutUndo();});

        private static void LinkDay2()
        {
            var s=SceneManager.GetSceneByPath(Day2NightDreamSetupTool.WakePath);bool opened=!s.isLoaded;
            if(opened)s=EditorSceneManager.OpenScene(Day2NightDreamSetupTool.WakePath,OpenSceneMode.Additive);
            try
            {
                var e=s.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<StoryEvent>(true)).Single(x=>x.EventId=="D2_HOME_WAKE_FROM_DREAM");
                if(e.transform.Find("070_BeginDayThree")!=null)return;
                var title=e.GetComponentsInChildren<StoryTitleCardStep>(true).Single();
                var so=new SerializedObject(title);var text=(TMP_Text)so.FindProperty("titleText").objectReferenceValue;
                var group=(CanvasGroup)so.FindProperty("overlay").objectReferenceValue;
                group.GetComponent<Canvas>().sortingOrder=1500;
                text.rectTransform.anchorMin=text.rectTransform.anchorMax=new Vector2(.5f,.5f);text.rectTransform.anchoredPosition=Vector2.zero;
                Set(title,"title","Ngày 2 - Kết thúc","holdDuration",2f,"waitForConfirm",false,"allowConfirmSkip",true);
                title.name="050_DayTwoEnds";
                var cover=s.GetRootGameObjects().Single(x=>x.name=="Scene Transition Overlay").GetComponentInChildren<CanvasGroup>(true);
                var fade=Step<CanvasFadeStep>(e,"040_FadeOutDayTwo");Fade(fade,cover,1,.85f);fade.transform.SetSiblingIndex(title.transform.GetSiblingIndex());
                Load(e,"070_BeginDayThree",GameScenes.Day3HomeMorning);
                EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);
            }
            finally{if(opened)EditorSceneManager.CloseScene(s,true);}
        }

        private static FullscreenTransitionController AddFullscreen(Stage s)
        {
            var fx=s.World.gameObject.AddComponent<FullscreenTransitionController>();
            var feature=AssetDatabase.LoadAllAssetsAtPath("Assets/Settings/Renderer2D.asset").OfType<FullScreenPassRendererFeature>().FirstOrDefault();
            if(feature==null)
            {
                string path=AssetDatabase.GUIDToAssetPath("424799608f7334c24bf367e4bbfa7f9a");
                feature=AssetDatabase.LoadAllAssetsAtPath(path).OfType<FullScreenPassRendererFeature>().Single();
            }
            Set(fx,"worldCamera",s.Camera,"rendererFeature",feature);return fx;
        }
        private static T Asset<T>(string path,Action<T> configure) where T:ScriptableObject
        {var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset!=null)return asset;Folder(System.IO.Path.GetDirectoryName(path).Replace('\\','/'));
            asset=ScriptableObject.CreateInstance<T>();asset.name=System.IO.Path.GetFileNameWithoutExtension(path);configure(asset);AssetDatabase.CreateAsset(asset,path);return asset;}
        private static DialogueData D(string suffix,DialogueCharacterId counterpart,string audere,string counterpartPortrait,params string[] lines)
        {
            return Asset<DialogueData>("Assets/_Audere/Data/Dialogue/Day3/Dialogue_D3_"+suffix+".asset",p=>
            {
                foreach(var line in lines)if(line.Substring(2).Length>42)throw new InvalidOperationException("Split bubble: "+line);
                var so=new SerializedObject(p);so.FindProperty("dialogueId").stringValue="d3-"+suffix.ToLowerInvariant().Replace('_','-');
                so.FindProperty("leftCharacter").intValue=1;so.FindProperty("rightCharacter").intValue=(int)counterpart;
                so.FindProperty("leftPortraitOverride").objectReferenceValue=Portrait("Audere",audere);
                so.FindProperty("rightPortraitOverride").objectReferenceValue=counterpartPortrait==null?null:Portrait(counterpart==DialogueCharacterId.Bianca?"Bianca":"Timor",counterpartPortrait);
                var array=so.FindProperty("lines");array.arraySize=lines.Length;
                for(int i=0;i<lines.Length;i++){var l=array.GetArrayElementAtIndex(i);l.FindPropertyRelative("speaker").intValue=lines[i][0]=='L'?0:1;l.FindPropertyRelative("text").stringValue=lines[i].Substring(2);}
                so.ApplyModifiedPropertiesWithoutUndo();
            });
        }
        private static Sprite Portrait(string folder,string name)=>AssetDatabase.LoadAllAssetsAtPath("Assets/_Audere/AssetGame/"+folder+"/"+name+".png").OfType<Sprite>().Single(x=>x.name==name+"_0");
        private static Sprite SpriteAt(string path)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();
        private static T Required<T>(string path)where T:Object=>AssetDatabase.LoadAssetAtPath<T>(path)??throw new MissingReferenceException(path);
        private static GameObject Prefab(string path,Transform parent,string name){var go=(GameObject)PrefabUtility.InstantiatePrefab(Required<GameObject>(path));go.name=name;if(parent!=null)go.transform.SetParent(parent,false);return go;}
        private static Transform ChildRoot(string name)=>new GameObject(name).transform;
        private static Transform Anchor(Transform parent,string name,Vector3 pose){var t=Child(parent,name);t.position=pose;return t;}
        private static Transform Tile(Transform parent,float x,float y,string name){var t=Prefab(GrassPath,parent,name).transform;t.localPosition=new Vector3(x,y,0);return t;}
        private static Transform Actor(Transform parent,string name,string path,Vector2 cell)
        {var t=Prefab(path,parent,name).transform;t.localScale=Vector3.one*1.5f;var r=t.GetComponent<SpriteRenderer>();
            t.localPosition=new Vector3(cell.x,cell.y-r.sprite.bounds.min.y*1.5f,-1);r.flipX=true;return t;}
        private static Transform Shadow(Transform actor)=>actor.GetComponentsInChildren<SpriteRenderer>(true).Single(x=>x!=actor.GetComponent<SpriteRenderer>()&&x.sortingOrder==4).transform;
        private static void Hop(StoryEvent e,string name,Transform actor,Transform target,bool inPlace,float arc=.075f)=>Set(Step<CharacterMotionStep>(e,name),
            "actor",actor,"targetTransform",target,"actorRenderer",actor.GetComponent<SpriteRenderer>(),"groundedShadow",Shadow(actor),
            "motionMode",inPlace?1:0,"duration",inPlace?.19f:.34f,"arcHeight",arc,"facingMode",inPlace?0:1,"useUnscaledTime",true);
        private static void TileTransition(StoryEvent e,string name,Transform hide,Transform reveal)=>Set(Step<BoardTileTransitionStep>(e,name),
            "objectsToHide",hide==null?new Object[0]:new Object[]{hide},"objectsToReveal",reveal==null?new Object[0]:new Object[]{reveal},"transitionDuration",.14f,"staggerDelay",0f);
        private static StoryEvent Event(Stage s,string id)
        {var e=Child(s.Director.transform,id).gameObject.AddComponent<StoryEvent>();Set(e,"eventId",id);Set(s.Director,"storyEventsRoot",s.Director.transform,"startingEvent",e,"playOnStart",true);return e;}
        private static StoryEvent Branch(ParallelStoryStep owner,string name,string id){var e=Child(owner.transform,name).gameObject.AddComponent<StoryEvent>();Set(e,"eventId",id);return e;}
        private static void Say(StoryEvent e,string name,DialogueData data)=>Set(Step<DialogueStep>(e,name),"dialogueData",data);
        private static void Cover(StoryEvent e,string name,CanvasGroup group,float alpha,float duration)=>Fade(Step<CanvasFadeStep>(e,name),group,alpha,duration);
        private static void Load(StoryEvent e,string name,string scene)=>Set(Step<SceneLoadStep>(e,name),"sceneName",scene,"hidePuzzleUiBeforeLoad",true);
        private static void Opening(StoryEvent e,Stage s){Cover(e,"000_CoverArrival",s.Fade,1,0);Move(e,"005_ResetAudere",s.Audere,s.ActorStart,0);Cover(e,"010_Reveal",s.Fade,0,.6f);}
        private static RectTransform RectChild(Transform parent,string name){var t=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();t.SetParent(parent,false);t.anchorMin=t.anchorMax=new Vector2(.5f,.5f);return t;}
        private static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        private static TMP_Text Label(Transform parent,string name,string text,Vector2 pos,Vector2 size,float font)
        {var r=RectChild(parent,name);r.sizeDelta=size;r.anchoredPosition=pos;var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=Required<TMP_FontAsset>(FontPath);t.text=text;t.fontSize=font;t.alignment=TextAlignmentOptions.Center;t.color=new Color(.88f,.84f,.91f,1);t.raycastTarget=false;return t;}
        private static (CanvasGroup group,TMP_Text text) Title(Stage s,string text,int order,Vector2 anchor)
        {var go=new GameObject("Day Label",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));
            go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;go.GetComponent<Canvas>().sortingOrder=order;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
            var group=go.GetComponent<CanvasGroup>();group.alpha=0;var label=Label(go.transform,"Title",text,Vector2.zero,new Vector2(700,85),42);
            label.rectTransform.anchorMin=label.rectTransform.anchorMax=anchor;return(group,label);}
        private sealed class Stage
        {public Scene Scene;public Transform World,Story,Anchors,Audere,StartTile,ActorStart,CameraStart;public Camera Camera;public CanvasGroup Fade;public GameplayUIRoot Ui;public StoryDirector Director;}
    }
}
#endif
