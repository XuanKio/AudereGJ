#if UNITY_EDITOR
using System;
using System.Linq;
using Audere.Combat;
using Audere.Core;
using Audere.Dialogue;
using Audere.Story.Steps;
using Audere.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Audere.Story.Editor
{
    // Scoped authoring: Scene140, its own content, and a create-only evening arrival.
    public static partial class Day4CrowdSetupTool
    {
        public const string ScenePath = "Assets/_Audere/Scenes/140_D4_Classroom.unity";
        public const string EveningPath = "Assets/_Audere/Scenes/150_D4_Home_Evening.unity";
        public const string Folder = "Assets/_Audere/Data/Combat/Crowd";
        public const string DialogueFolder = "Assets/_Audere/Data/Dialogue/Day4/Crowd";
        public const string EncounterPath = Folder + "/CombatEncounter_D4_CROWD.asset";
        private const string Prefabs = "Assets/_Audere/Prefabs/Combat";

        [MenuItem("Audere/Story/Author Active Day4 Crowd Classroom")]
        public static void AuthorActive()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || scene.isDirty || EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Open saved Scene140 in Edit Mode first.");
            EnsureFolder(Folder); EnsureFolder(DialogueFolder);
            var encounter = AuthorCombat();
            AddCrowdCatalog();
            var room = scene.GetRootGameObjects().Single(x => x.name == "CLASSROOM").transform;
            var musicSpace=All<Audere.Audio.SceneMusicSpace>(scene).FirstOrDefault();if(musicSpace!=null)musicSpace.transform.SetParent(room,true);
            var a = room.GetComponentsInChildren<Transform>(true).Single(x => x.name == "Audere");
            var b = room.GetComponentsInChildren<Transform>(true).Single(x => x.name == "Bianca_PLACEHOLDER" || x.name == "Bianca");
            b.name = "Bianca";
            var oldScenery = room.GetComponentsInChildren<Transform>(true).Where(x => x.name.StartsWith("Tile_") || x.name == "Classroom_Backdrop_PLACEHOLDER").ToArray();
            foreach (var t in oldScenery) t.gameObject.SetActive(false);
            var stage = Child(room, "DAY FOUR TILE CLASSROOM");
            foreach (Transform t in stage.Cast<Transform>().ToArray()) Object.DestroyImmediate(t.gameObject);
            var tileSprite = Sprite("Step Tile/grass.aseprite");
            Vector3 center = new Vector3(-.125f, -.04f, 0f);
            MakeTile(stage, "Audere Tile", center, tileSprite);
            MakeTile(stage, "Bianca Tile", center + Vector3.right * .25f, tileSprite);
            for (int y = -2; y <= 2; y += 2) for (int x = -2; x <= 2; x += 2)
            {
                if (x == 0 && y == 0) continue;
                Vector3 position = center + new Vector3(x * .25f, y * .25f, 0f);
                var tile = MakeTile(stage, "Desk Tile " + x + " " + y, position, tileSprite);
                var desk = Child(tile, "Desk centered on tile");
                var renderer = desk.gameObject.AddComponent<SpriteRenderer>(); renderer.sprite = Sprite("Item/ban.aseprite");
                renderer.sortingLayerName = "Player"; renderer.sortingOrder = y < 0 ? 8 : y == 0 ? 5 : 2;
                renderer.color = y < 0 ? new Color(.76f,.73f,.77f) : new Color(.63f,.66f,.73f);
                float width = .215f / renderer.sprite.bounds.size.x;
                desk.localScale = Vector3.one * width / tile.lossyScale.x;
                Vector3 foot = new Vector3(renderer.sprite.bounds.center.x, renderer.sprite.bounds.min.y, 0);
                desk.position += position - desk.TransformPoint(foot);
            }
            a.rotation = Quaternion.identity; Ground(a, center); Ground(b, center + Vector3.right * .25f);
            a.gameObject.SetActive(true); b.gameObject.SetActive(false);
            a.GetComponent<SpriteRenderer>().flipX = true; b.GetComponent<SpriteRenderer>().flipX = false;
            var anchors = Child(room, "DAY FOUR POSE ANCHORS"); anchors.gameObject.SetActive(false);
            var stand = Anchor(anchors, "Audere Standing", a.position, Quaternion.identity);
            var lie = Anchor(anchors, "Audere Lying On Tile", new Vector3(center.x, center.y + .025f, a.position.z), Quaternion.Euler(0,0,90));
            var aShadow = Shadow(a); var shadowPose = Anchor(anchors, "Audere Grounded Shadow", aShadow.position, aShadow.rotation);
            var bStand = Anchor(anchors, "Bianca Beside Audere", b.position, Quaternion.identity);
            var bLean = Anchor(anchors,"Bianca Offers Support",b.position + Vector3.left*.018f,Quaternion.Euler(0,0,8));
            var bShadow=Shadow(b);var bGround=Anchor(anchors,"Bianca Grounded Shadow",bShadow.position,bShadow.rotation);
            foreach(var scaler in All<CanvasScaler>(scene).Where(x=>x.name=="GameplayUIRoot"))Set(scaler,"m_ScreenMatchMode",(int)CanvasScaler.ScreenMatchMode.Expand);
            var director = All<StoryDirector>(scene).Single();
            foreach (var e in All<StoryEvent>(scene).Where(x=>x.GetComponentsInParent<StoryEvent>(true).Length==1).ToArray()) Object.DestroyImmediate(e.gameObject);
            var story = Component<StoryEvent>(Child(director.transform, "D4_CLASSROOM_CROWD"));
            Set(story,"eventId","D4_CLASSROOM_CROWD","autoPlayNextEvent",false);
            Set(director,"startingEvent",story,"playOnStart",true);
            var mode = All<WorldModeController>(scene).Single();
            Set(mode,"storyUsesPuzzleViewportMask",true,"allowChildFadeFallback",false,"enableDebugHotkeys",false,
                "storyOrthographicSize",.9f,"revealStartingModeOnStart",false);
            var board = All<CombatBoardView>(scene).Single(); var controller = All<CombatController>(scene).Single();
            var systems = scene.GetRootGameObjects().Single(x=>x.name=="SYSTEMS").transform;
            var combatSystems = new SerializedObject(mode).FindProperty("combatSystemsRoot").objectReferenceValue as GameObject;
            if(combatSystems!=null) combatSystems.transform.SetParent(systems,true);
            Set(controller,"encounterData",encounter,"playOnStart",false);
            var mount = (Transform)new SerializedObject(board).FindProperty("enemyMount").objectReferenceValue;
            var oldEnemy = mount.GetComponentsInChildren<CombatEnemyActor>(true);
            foreach (var enemy in oldEnemy) Object.DestroyImmediate(enemy.gameObject);
            var actor = ((GameObject)PrefabUtility.InstantiatePrefab(encounter.EnemyDefinition.ActorPrefab.gameObject,mount)).GetComponent<CombatEnemyActor>();
            actor.name="Enemy_Crowd";actor.gameObject.SetActive(false);Set(board,"authoredEnemyActor",actor);
            var cover = TransitionFade(scene);cover.alpha=1f;cover.blocksRaycasts=true;cover.interactable=false;
            Transform p=story.transform;
            Fade(p,"000_ResetCover",cover,1f,0f);
            Set(Step<SetActiveStep>(p,"002_AloneAtFirst"),"objectsToDisable",new Object[]{b.gameObject},"objectsToEnable",new Object[]{a.gameObject});
            Pose(p,"004_StandingAgainOnReplay",a,stand,aShadow,shadowPose,0f);
            Fade(p,"010_TheClassroom",cover,0f,1.1f);Wait(p,"020_OneSmallTask",.7f);
            Talk(p,"030_I_BroughtIt",D("OPENING",DialogueCharacterId.None,"Audere_Tired.png",null,
                "L|Hôm nay không có Timor nhắc…","L|Tớ vẫn mang đồ cô nhờ lên lớp được rồi.","L|Đặt ở đây là xong."));
            Wait(p,"040_SomethingGivesWay",.45f);
            Pose(p,"050_AudereFalls",a,lie,aShadow,shadowPose,.42f);
            Wait(p,"055_OnTheFloor",.2f);
            Talk(p,"060_TheyMustBeLaughing",D("THOUGHT",DialogueCharacterId.None,"Audere_Scared.png",null,
                "L|…Họ đang cười mình à?"));
            Set(Step<FullscreenWorldModeTransitionStep>(p,"070_TheRoomBecomesPressure"),"transitionController",All<FullscreenTransitionController>(scene).Single(),
                "worldModeController",mode,"transitionProfile",AssetDatabase.LoadAssetAtPath<FullscreenTransitionProfile>("Assets/_Audere/Data/Transitions/WorldTransition_DreamyDisorientation.asset"),
                "focusRenderer",a.GetComponent<SpriteRenderer>(),"sourceMode",2,"targetMode",1);
            Set(Step<CombatStep>(p,"080_TheCrowd"),"combatController",controller,"combatEncounterData",encounter,"enemyActorOverride",actor,"defeatBehaviour",2,"victoryBehaviour",0);
            Fade(p,"090_TheNoiseFallsAway",cover,1f,.9f);
            Set(Step<WorldModeStep>(p,"100_BackInTheClassroom"),"worldModeController",mode,"targetMode",2);
            Set(Step<SetActiveStep>(p,"105_BiancaAtHerRight"),"objectsToEnable",new Object[]{b.gameObject});
            Set(Step<MoveActorStep>(p,"106_BiancaCentered"),"actor",b,"targetTransform",bStand,"duration",0f);
            Fade(p,"110_TheyAreStillHere",cover,0f,.9f);
            Talk(p,"120_DoNotRush",D("HELP",DialogueCharacterId.Bianca,"Audere_Tired.png","Bianca/Bianca_Worried.png",
                "R|Từ từ thôi. Cậu có đau ở đâu không?","L|Đầu gối… hơi đau.","R|Cậu vịn vào tớ nhé?","L|…Ừ."));
            var help=Step<ParallelStoryStep>(p,"130_BiancaHelpsHerUp");
            var helpA=Component<StoryEvent>(Child(help.transform,"Audere Rises Branch"));Set(helpA,"eventId","D4_AUDERE_RISES","autoPlayNextEvent",false);
            var helpB=Component<StoryEvent>(Child(help.transform,"Bianca Supports Branch"));Set(helpB,"eventId","D4_BIANCA_SUPPORTS","autoPlayNextEvent",false);
            Pose(helpA.transform,"010_Rise",a,stand,aShadow,shadowPose,.9f);
            Pose(helpB.transform,"010_LeanIn",b,bLean,bShadow,bGround,.25f);Wait(helpB.transform,"020_SteadyHer",.4f);Pose(helpB.transform,"030_Straighten",b,bStand,bShadow,bGround,.25f);
            Set(help,"branches",new Object[]{helpA,helpB});
            var together=Step<ParallelStoryStep>(p,"140_BothFindTheirFeet");
            var aBranch=Component<StoryEvent>(Child(together.transform,"Audere Hop Branch"));Set(aBranch,"eventId","D4_AUDERE_HOP","autoPlayNextEvent",false);
            var bBranch=Component<StoryEvent>(Child(together.transform,"Bianca Hop Branch"));Set(bBranch,"eventId","D4_BIANCA_HOP","autoPlayNextEvent",false);
            Hop(aBranch.transform,"010_Hop",a,stand);Hop(bBranch.transform,"010_Hop",b,bStand);Set(together,"branches",new Object[]{aBranch,bBranch});
            Wait(p,"150_Breathe",.6f);
            Talk(p,"160_NotAnApology",D("SMALL_TALK",DialogueCharacterId.Bianca,"Audere_Tired.png","Bianca/Bianca_Worried.png",
                "L|Tớ tưởng mọi người đang cười.","R|Tớ chỉ thấy cậu ngã.","R|Đồ để đó đã. Mình ngồi xuống nhé?","L|Nhưng còn đồ cô nhờ…","R|Lát nữa mình mang tiếp."));
            Wait(p,"170_LookBeyondBianca",.65f);
            Talk(p,"180_AskTheRoom",D("ASK_FOR_HELP",DialogueCharacterId.None,"Audere_Tired.png",null,
                "L|Mọi người… giúp tớ được không?"));
            Wait(p,"190_LeaveRoomForAnAnswer",1f);
            Fade(p,"200_EveningCover",cover,1f,1.15f);
            Set(Step<SceneLoadStep>(p,"210_ThatEvening"),"sceneName",GameScenes.Day4HomeEvening,"hidePuzzleUiBeforeLoad",true);
            Order(p); AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            CreateEveningIfMissing();
            var scenes=EditorBuildSettings.scenes.ToList();if(!scenes.Any(x=>x.path==EveningPath))scenes.Add(new EditorBuildSettingsScene(EveningPath,true));EditorBuildSettings.scenes=scenes.ToArray();
            EditorSceneManager.OpenScene(ScenePath);
            PolishActive();
            FitStageActive();
        }

        private static CombatEncounterData AuthorCombat()
        {
            var hand=HandPrefab(); var enemyActor=EnemyPrefab();
            var bullet=AssetDatabase.LoadAssetAtPath<CombatBulletView>(Prefabs+"/Bullets/EnemyBullet.prefab");
            var stab=New<GraspingHandsMove>(Folder+"/Move_Palms.asset");
            Set(stab,"handPrefab",hand,"bulletPrefab",bullet,"duration",7f,"warning",.55f,"strike",.44f,"hold",.88f,"retreat",.46f,"rest",.1f,"handsPerBeat",3,"palmVolleys",3,"bulletsPerVolley",14,"bulletSpeed",125f,"sweepDegrees",46f);Save(stab,Folder+"/Move_Palms.asset");
            var gentle=New<GraspingHandsMove>(Folder+"/Move_UncertainHands.asset");
            Set(gentle,"handPrefab",hand,"bulletPrefab",bullet,"duration",8.5f,"warning",1.1f,"strike",1f,"hold",.62f,"retreat",.82f,"rest",.7f,"handsPerBeat",1,"palmVolleys",1,"bulletsPerVolley",7,"bulletSpeed",76f,"sweepDegrees",18f);Save(gentle,Folder+"/Move_UncertainHands.asset");
            var fast=New<LinearProjectilePatternMove>(Folder+"/Move_RushingVoices.asset");
            Set(fast,"duration",5f,"projectilePrefab",bullet,"spawnMode",1,"targetMode",1,"shotInterval",.29f,"projectilesPerShot",5,"spacing",46f,"speed",175f);Save(fast,Folder+"/Move_RushingVoices.asset");
            var slow=New<LinearProjectilePatternMove>(Folder+"/Move_DistantVoices.asset");
            Set(slow,"duration",6f,"projectilePrefab",bullet,"spawnMode",1,"targetMode",1,"shotInterval",1f,"projectilesPerShot",2,"spacing",96f,"speed",92f);Save(slow,Folder+"/Move_DistantVoices.asset");
            var shift=New<ShiftingBattleBoxMove>(Folder+"/Move_RoomSqueezes.asset");Set(shift,"duration",7f,"telegraphDuration",.45f,"squeezeDuration",.65f,"holdDuration",.6f,"returnDuration",.6f);
            var so=new SerializedObject(shift);var poses=so.FindProperty("poses");poses.arraySize=3;
            for(int i=0;i<3;i++){poses.GetArrayElementAtIndex(i).FindPropertyRelative("widthFraction").floatValue=i==1?.75f:.82f;poses.GetArrayElementAtIndex(i).FindPropertyRelative("normalizedX").floatValue=i%2==0?-.65f:.65f;}so.ApplyModifiedPropertiesWithoutUndo();Save(shift,Folder+"/Move_RoomSqueezes.asset");
            var combo=New<CompositeCombatMove>(Folder+"/Move_ShiftingPalms.asset");Set(combo,"duration",7f,"children",new Object[]{stab,shift});Save(combo,Folder+"/Move_ShiftingPalms.asset");
            var dense=MoveSet("Crowded",combo,fast);var quiet=MoveSet("ThereIsRoom",gentle,slow);
            var opening=new[]{
                D("C01_CROWD",DialogueCharacterId.CrowdDistorted,"Audere_Scared.png","Enemyy/Crowd.png","R|Mang có mấy thứ cũng làm rơi.","R|Ai cũng đang nhìn kìa."),
                D("C02_TIMOR",DialogueCharacterId.Timor,"Audere_Crying.png","Timor/TimorLoLangKhongVui.png","R|Tớ đã nói rồi.","R|Đây là điều xảy ra khi cậu tự làm."),
                D("C03_AUDERE",DialogueCharacterId.Timor,"Audere_Scared.png","Timor/TimorLolang.png","L|Tớ chỉ… muốn mang cho xong thôi.","R|Đừng nhìn họ.","R|Để tớ đưa cậu ra khỏi đây."),
                D("C04_CROWD",DialogueCharacterId.CrowdDistorted,"Audere_Crying.png","Enemyy/Crowd.png","R|Lại phải có người giúp rồi.","L|Đừng nhìn tớ nữa…")};
            var turning=new[]{
                D("C10_BIANCA",DialogueCharacterId.Bianca,"Audere_Scared.png","Bianca/Bianca_Worried.png","R|Audere, cậu có bị đau không?"),
                D("C11_AUDERE",DialogueCharacterId.Bianca,"Audere_Tired.png","Bianca/Bianca_Worried.png","L|…Bianca?"),
                D("C12_TIMOR",DialogueCharacterId.Timor,"Audere_Tired.png","Timor/TimorLoLangKhongVui.png","R|Không… nhưng vừa nãy…","R|Họ vẫn đang nhìn cậu mà."),
                D("C13_BIANCA",DialogueCharacterId.Bianca,"Audere_Tired.png","Bianca/Bianca_Worried.png","R|Đừng vội đứng lên. Tớ ở đây."),
                D("C14_AUDERE",DialogueCharacterId.Timor,"Audere_Tired.png","Timor/TimorLolang.png","L|Cậu ấy đang hỏi tớ có đau không.","L|Tớ chưa biết mọi người nghĩ gì…","L|Nhưng tớ muốn nghe cậu ấy nói.")};
            var enemy=New<CombatEnemyDefinition>(Folder+"/Enemy_Crowd.asset");Set(enemy,"enemyId","d4-crowd-pressure","displayName","Đám đông","actorPrefab",enemyActor,"phasePolicy",(int)CombatPhasePolicy.SharedHealthThresholds,"sharedMaxHealth",21);
            so=new SerializedObject(enemy);var phases=so.FindProperty("phases");phases.arraySize=2;
            for(int i=0;i<2;i++)
            {
                var phase=phases.GetArrayElementAtIndex(i);phase.FindPropertyRelative("phaseId").stringValue=i==0?"everyone-is-looking":"one-real-voice";
                phase.FindPropertyRelative("moveSet").objectReferenceValue=i==0?dense:quiet;
                phase.FindPropertyRelative("sharedExitThreshold").intValue=i==0?10:0;
                phase.FindPropertyRelative("playerTimeExitFraction").floatValue=0f;
                phase.FindPropertyRelative("spawnDice").boolValue=true;phase.FindPropertyRelative("allowsPlayerDefeat").boolValue=true;
                var cues=phase.FindPropertyRelative("dialogueCues");cues.arraySize=1;var cue=cues.GetArrayElementAtIndex(0);
                cue.FindPropertyRelative("cueId").stringValue=i==0?"crowd-timor-returns":"crowd-bianca-real-voice";
                cue.FindPropertyRelative("trigger").intValue=0;cue.FindPropertyRelative("presentation").intValue=1;
                cue.FindPropertyRelative("minimumLineDuration").floatValue=1.8f;cue.FindPropertyRelative("charactersPerSecond").floatValue=25f;cue.FindPropertyRelative("interLineGap").floatValue=.2f;
                cue.FindPropertyRelative("requiredBeforeVictory").boolValue=i==1;
                var seq=cue.FindPropertyRelative("sequence");var lines=i==0?opening:turning;seq.arraySize=lines.Length;
                for(int j=0;j<lines.Length;j++)seq.GetArrayElementAtIndex(j).objectReferenceValue=lines[j];
            }
            so.ApplyModifiedPropertiesWithoutUndo();Save(enemy,Folder+"/Enemy_Crowd.asset");
            var encounter=New<CombatEncounterData>(EncounterPath);Set(encounter,"encounterId","d4-crowd-classroom","enemyDefinition",enemy,"encounterDuration",90f,"dicePerBatch",3,"maximumAttacksPerBatch",1,"additionalRerolledAttacksPerBatch",1,"batchRespawnDelay",.26f,"minimumDiceSpeed",105f,"maximumDiceSpeed",158f,"bulletTimePenaltySeconds",2.25f,"playerHitInvulnerability",.65f,"victoryFadeDuration",.75f);
            Save(encounter,EncounterPath);return encounter;
        }

        private static CombatBulletView HandPrefab()
        {
            string path=Prefabs+"/Bullets/Bullet_CrowdHand.prefab";var existing=AssetDatabase.LoadAssetAtPath<CombatBulletView>(path);if(existing!=null)return existing;
            var sprite=Sprite("Enemyy/IMG_1058.png");var material=new Material(Shader.Find("Audere/UI/WrithingHand"));
            Rect r=sprite.textureRect;material.SetVector("_UVRect",new Vector4(r.x/sprite.texture.width,r.y/sprite.texture.height,r.width/sprite.texture.width,r.height/sprite.texture.height));
            EnsureFolder("Assets/_Audere/Materials/Combat");AssetDatabase.CreateAsset(material,"Assets/_Audere/Materials/Combat/CrowdHand.mat");
            var go=new GameObject("Bullet_CrowdHand",typeof(RectTransform),typeof(CombatBulletView));
            ((RectTransform)go.transform).sizeDelta=new Vector2(23f,27f);
            var image=Component<Image>(Child(go.transform,"Arm visual (palm hitbox)"));image.sprite=sprite;image.material=material;image.raycastTarget=false;
            var rt=image.rectTransform;rt.pivot=new Vector2(.5f,.88f);rt.sizeDelta=new Vector2(54f,240f);rt.anchoredPosition=Vector2.zero;
            var prefab=PrefabUtility.SaveAsPrefabAsset(go,path);Object.DestroyImmediate(go);return prefab.GetComponent<CombatBulletView>();
        }
        private static CombatEnemyActor EnemyPrefab()
        {
            string path=Prefabs+"/Enemies/Enemy_Crowd.prefab";var existing=AssetDatabase.LoadAssetAtPath<CombatEnemyActor>(path);if(existing!=null)return existing;
            var go=new GameObject("Enemy_Crowd",typeof(RectTransform),typeof(CombatEnemyActor));((RectTransform)go.transform).sizeDelta=new Vector2(190f,250f);
            var visual=Component<Image>(Child(go.transform,"Crowd visual"));visual.rectTransform.sizeDelta=new Vector2(190f,250f);visual.sprite=Sprite("Enemyy/IMG_1054.png");visual.preserveAspect=true;visual.raycastTarget=false;
            var origin=Child(go.transform,"Projectile Origin");origin.localPosition=new Vector3(0,-110,0);
            Set(go.GetComponent<CombatEnemyActor>(),"visualRoot",visual.transform,"projectileOrigin",origin,"vfxAnchor",visual.transform,"damageAnchor",visual.transform,"graphics",new Object[]{visual});
            var prefab=PrefabUtility.SaveAsPrefabAsset(go,path);Object.DestroyImmediate(go);return prefab.GetComponent<CombatEnemyActor>();
        }
        private static void AddCrowdCatalog()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<DialogueCharacterCatalog>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:DialogueCharacterCatalog").Single()));
            var so=new SerializedObject(catalog);var entries=so.FindProperty("characters");int index=-1;
            for(int i=0;i<entries.arraySize;i++)if(entries.GetArrayElementAtIndex(i).FindPropertyRelative("character").intValue==8)index=i;
            if(index<0){index=entries.arraySize;entries.arraySize++;}var e=entries.GetArrayElementAtIndex(index);e.FindPropertyRelative("character").intValue=8;e.FindPropertyRelative("displayName").stringValue="Đám đông";e.FindPropertyRelative("portrait").objectReferenceValue=Sprite("Enemyy/Crowd.png");so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void CreateEveningIfMissing()
        {
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(EveningPath)!=null)return;
            AssetDatabase.CopyAsset("Assets/_Audere/Scenes/70_D2_Home_Night.unity",EveningPath);
            var scene=EditorSceneManager.OpenScene(EveningPath);var director=All<StoryDirector>(scene).Single();
            foreach(var e in All<StoryEvent>(scene))Object.DestroyImmediate(e.gameObject);
            var arrival=Component<StoryEvent>(Child(director.transform,"D4_EVENING_ARRIVAL"));Set(arrival,"eventId","D4_EVENING_ARRIVAL","autoPlayNextEvent",false);Set(director,"startingEvent",arrival,"playOnStart",true);
            foreach(var t in All<Transform>(scene).Where(x=>x.name.IndexOf("Timor",StringComparison.OrdinalIgnoreCase)>=0 && x.GetComponent<SpriteRenderer>()!=null))t.gameObject.SetActive(false);
            var cover=TransitionFade(scene);cover.alpha=1f;cover.blocksRaycasts=true;cover.interactable=false;
            foreach(var group in All<CanvasGroup>(scene).Where(x=>x!=cover&&x.name.IndexOf("cover",StringComparison.OrdinalIgnoreCase)>=0)){group.alpha=0f;group.blocksRaycasts=false;group.interactable=false;}
            Fade(arrival.transform,"000_CoveredArrival",cover,1f,0f);Wait(arrival.transform,"010_TimePasses",.7f);Fade(arrival.transform,"020_ThatEvening",cover,0f,1.2f);Wait(arrival.transform,"030_Quiet",.8f);
            foreach(var mode in All<WorldModeController>(scene))Set(mode,"enableDebugHotkeys",false);
            EditorSceneManager.SaveScene(scene);
        }
        private static DialogueData D(string id,DialogueCharacterId right,string audere,string portrait,params string[] text)
        {
            string path=DialogueFolder+"/Dialogue_D4_"+id+".asset";var d=New<DialogueData>(path);
            Set(d,"dialogueId","D4_CROWD_"+id,"leftCharacter",1,"rightCharacter",(int)right,"leftPortraitOverride",Sprite("Audere/"+audere),"rightPortraitOverride",portrait==null?null:Sprite(portrait));
            var so=new SerializedObject(d);var lines=so.FindProperty("lines");lines.arraySize=text.Length;
            for(int i=0;i<text.Length;i++){var l=lines.GetArrayElementAtIndex(i);l.FindPropertyRelative("speaker").intValue=text[i][0]=='L'?0:1;l.FindPropertyRelative("text").stringValue=text[i].Substring(2);l.FindPropertyRelative("characterOverride").intValue=0;l.FindPropertyRelative("portraitOverride").objectReferenceValue=null;l.FindPropertyRelative("glitchPortraitTransition").boolValue=false;}so.ApplyModifiedPropertiesWithoutUndo();Save(d,path);return d;
        }
        private static CombatMoveSet MoveSet(string id,params CombatMoveDefinition[] moves)
        {
            string path=Folder+"/MoveSet_"+id+".asset";var set=New<CombatMoveSet>(path);var so=new SerializedObject(set);so.FindProperty("selectionPolicy").intValue=0;var entries=so.FindProperty("entries");entries.arraySize=moves.Length;
            for(int i=0;i<moves.Length;i++){entries.GetArrayElementAtIndex(i).FindPropertyRelative("move").objectReferenceValue=moves[i];entries.GetArrayElementAtIndex(i).FindPropertyRelative("weight").floatValue=1;}so.ApplyModifiedPropertiesWithoutUndo();Save(set,path);return set;
        }
        private static Transform MakeTile(Transform p,string n,Vector3 position,Sprite sprite){var t=Child(p,n);t.position=position;t.localScale=Vector3.one*.25f/p.lossyScale.x;var r=Component<SpriteRenderer>(t);r.sprite=sprite;r.sortingOrder=0;r.color=new Color(.63f,.65f,.72f);return t;}
        private static void Ground(Transform actor,Vector3 tile){var r=actor.GetComponent<SpriteRenderer>();actor.position+=tile-actor.TransformPoint(new Vector3(r.sprite.bounds.center.x,r.sprite.bounds.min.y,0));var p=actor.position;p.z=-.25f;actor.position=p;}
        private static Transform Shadow(Transform a)=>a.GetComponentsInChildren<SpriteRenderer>(true).Single(x=>x.transform!=a&&x.sortingOrder==4).transform;
        private static Transform Anchor(Transform p,string n,Vector3 position,Quaternion rotation){var t=Child(p,n);t.SetPositionAndRotation(position,rotation);return t;}
        private static void Pose(Transform p,string n,Transform a,Transform target,Transform shadow,Transform ground,float duration)=>Set(Step<CharacterPoseStep>(p,n),"actor",a,"targetPose",target,"groundedShadow",shadow,"shadowAnchor",ground,"duration",duration);
        private static void Hop(Transform p,string n,Transform a,Transform target)=>Set(Step<CharacterMotionStep>(p,n),"actor",a,"targetTransform",target,"actorRenderer",a.GetComponent<SpriteRenderer>(),"groundedShadow",Shadow(a),"motionMode",1,"duration",.32f,"arcHeight",.045f,"facingMode",0);
        private static void Fade(Transform p,string n,CanvasGroup g,float a,float d)=>Set(Step<CanvasFadeStep>(p,n),"canvasGroup",g,"targetAlpha",a,"duration",d);
        private static void Wait(Transform p,string n,float d)=>Set(Step<WaitStep>(p,n),"duration",d);
        private static void Talk(Transform p,string n,DialogueData d)=>Set(Step<DialogueStep>(p,n),"dialogueData",d);
        private static CanvasGroup TransitionFade(Scene scene)=>All<CanvasGroup>(scene).Single(x=>x.name=="Fade"&&x.transform.parent!=null&&x.transform.parent.name=="Scene Transition Overlay");
        private static T New<T>(string path)where T:ScriptableObject=>AssetDatabase.LoadAssetAtPath<T>(path)??ScriptableObject.CreateInstance<T>();
        private static void Save(Object a,string path){if(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(a)))AssetDatabase.CreateAsset(a,path);else EditorUtility.SetDirty(a);}
        private static Sprite Sprite(string path)=>AssetDatabase.LoadAllAssetsAtPath("Assets/_Audere/AssetGame/"+path).OfType<Sprite>().First();
        private static Transform Child(Transform p,string n){var t=p.Find(n);if(t!=null)return t;var go=new GameObject(n);go.transform.SetParent(p,false);return go.transform;}
        private static T Component<T>(Transform t)where T:Component{var c=t.GetComponent<T>();return c!=null?c:t.gameObject.AddComponent<T>();}
        private static T Step<T>(Transform p,string n)where T:StoryStep=>Component<T>(Child(p,n));
        private static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<T>(true)).ToArray();
        private static void Order(Transform p){foreach(var t in p.Cast<Transform>().OrderBy(x=>int.Parse(x.name.Split('_')[0])).ToArray())t.SetAsLastSibling();}
        private static void EnsureFolder(string p){if(AssetDatabase.IsValidFolder(p))return;int i=p.LastIndexOf('/');EnsureFolder(p.Substring(0,i));AssetDatabase.CreateFolder(p.Substring(0,i),p.Substring(i+1));}
        private static void Set(Object o,params object[] pairs)
        {
            var so=new SerializedObject(o);for(int i=0;i<pairs.Length;i+=2){var p=so.FindProperty((string)pairs[i]);var v=pairs[i+1];if(p==null)throw new InvalidOperationException(o.name+":"+pairs[i]);
                if(v is Object[] array){p.arraySize=array.Length;for(int j=0;j<array.Length;j++)p.GetArrayElementAtIndex(j).objectReferenceValue=array[j];}
                else if(v is string s)p.stringValue=s;else if(v is int n)p.intValue=n;else if(v is bool b)p.boolValue=b;else if(v is float f)p.floatValue=f;else p.objectReferenceValue=v as Object;}
            so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(o);
        }
    }
}
#endif


