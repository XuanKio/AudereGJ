#if UNITY_EDITOR
using System;
using System.Linq;
using Audere.Combat;
using Audere.Dialogue;
using Audere.Puzzle;
using Audere.Story;
using Audere.Story.Steps;
using Audere.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using static Audere.EditorTools.Day2SchoolMorningSetupTool;

namespace Audere.EditorTools
{
    /// <summary>Design Intent: the supplies mistake and Bianca encounter opening, authored in scene.</summary>
    public static class Day2SchoolSuppliesReturnSetupTool
    {
        public static void Author(Scene scene, StoryEvent previous, WorldModeController mode, CanvasGroup cover,
            Camera camera, GridPlayer audere, GridPlayer bianca, GameplayUIRoot ui)
        {
            // A dedicated encounter owns this event after Bianca combat authoring.
            // Preserve its post-combat staging and direct references on future school setup.
            var authored = AssetDatabase.LoadAssetAtPath<CombatEncounterData>(BiancaCombatAuthoring.EncounterPath);
            var existing = previous.transform.parent.Find("D2_SCHOOL_WRONG_SUPPLIES");
            if (authored != null && authored.EncounterId == "d2-bianca-perceived-judgement" && existing != null)
            {
                Set(previous, "autoPlayNextEvent", true, "nextEvent", existing.GetComponent<StoryEvent>());
                return;
            }
            var roots=scene.GetRootGameObjects();
            Transform school=roots.Single(r=>r.name=="SCHOOL").transform;
            Transform art=school.Find("SCHOOL ART PLACEHOLDER");
            Transform staging=school.Find("STAGING TARGETS");
            Transform returnBoard=Child(art,"Supplies Return Board");
            for(int i=returnBoard.childCount-1;i>=0;i--) Object.DestroyImmediate(returnBoard.GetChild(i).gameObject);
            Transform[] tiles=new Transform[2];
            Transform[] poses=new Transform[2];
            GridPlayer[] actors={audere,bianca};
            for(int i=0;i<2;i++)
            {
                var tile=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PuzzleContentConstants.AssetPaths.GrassPrefab),returnBoard);
                tile.name=i==0?"Tile_Audere_Return":"Tile_Bianca_Return";
                tile.transform.position=new Vector3(i==0?-.25f:.25f,-.17f,0f);
                tile.transform.localScale=Vector3.one;
                tiles[i]=tile.transform;
                poses[i]=Child(staging,i==0?"Audere_SuppliesReturnPose":"Bianca_SuppliesReturnPose");
                var sr=actors[i].GetComponent<SpriteRenderer>();
                Vector3 feetOffset=school.TransformVector(Vector3.down*sr.sprite.bounds.min.y*1.5f);
                poses[i].position=tile.transform.position+feetOffset;
                poses[i].position=new Vector3(poses[i].position.x,poses[i].position.y,actors[i].transform.position.z);
            }
            Transform cameraPose=Child(staging,"Camera_SuppliesReturnPose");
            cameraPose.position=new Vector3(0f,-.04f,-10f);
            var eRoot=Child(previous.transform.parent,"D2_SCHOOL_WRONG_SUPPLIES");
            for(int i=eRoot.childCount-1;i>=0;i--) Object.DestroyImmediate(eRoot.GetChild(i).gameObject);
            var e=eRoot.GetComponent<StoryEvent>() ?? eRoot.gameObject.AddComponent<StoryEvent>();
            Set(e,"eventId",eRoot.name,"autoPlayNextEvent",false,"nextEvent",null);
            Set(previous,"autoPlayNextEvent",true,"nextEvent",e);

            CombatEncounterData encounter=Encounter(bianca.GetComponent<SpriteRenderer>().sprite);
            Transform combatRoot=Child(mode.transform,"Combat Root");
            var board= combatRoot.GetComponentInChildren<CombatBoardView>(true);
            if(board==null) board=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"),combatRoot)).GetComponent<CombatBoardView>();
            board.transform.localPosition=new Vector3(0f,-.42f,0f);board.transform.localScale=Vector3.one*.0025f;
            Transform mount=board.transform.Find("Enemy/Enemy Mount");
            var enemy=mount.GetComponentInChildren<CombatEnemyActor>(true);
            if(enemy==null || PrefabUtility.GetCorrespondingObjectFromSource(enemy)!=encounter.EnemyDefinition.ActorPrefab)
            {
                for(int i=mount.childCount-1;i>=0;i--) Object.DestroyImmediate(mount.GetChild(i).gameObject);
                enemy=((GameObject)PrefabUtility.InstantiatePrefab(encounter.EnemyDefinition.ActorPrefab.gameObject,mount)).GetComponent<CombatEnemyActor>();
            }
            Set(board,"authoredEnemyActor",enemy,"enemyVisual",enemy.VisualRoot);
            foreach(var label in board.GetComponentsInChildren<TMPro.TMP_Text>(true)) if(label.name=="Enemy Name")label.text="Bianca";
            Transform systems=Child(mode.transform,"Combat Systems");
            var controller=systems.GetComponent<CombatController>() ?? systems.gameObject.AddComponent<CombatController>();
            Set(controller,"encounterData",encounter,"boardView",board,"playOnStart",false);
            Set(mode,"combatRoot",combatRoot.gameObject,"combatSystemsRoot",systems.gameObject,"worldCamera",camera,
                "storyAndPuzzleRoots",new[]{school.gameObject},"combatOrthographicSize",1.25f,"storyOrthographicSize",1.25f);
            var ms=new SerializedObject(mode);
            ms.FindProperty("combatCameraPosition").vector3Value=new Vector3(0,0,-10);
            ms.FindProperty("storyCameraPosition").vector3Value=new Vector3(0,-.04f,-10);
            ms.ApplyModifiedPropertiesWithoutUndo();
            var fullscreen=mode.GetComponent<FullscreenTransitionController>() ?? mode.gameObject.AddComponent<FullscreenTransitionController>();
            var feature=AssetDatabase.FindAssets("t:ScriptableRendererData").SelectMany(g=>AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(g)))
                .OfType<FullScreenPassRendererFeature>().Single(f=>f.name=="Audere Fullscreen World Transition");
            Set(fullscreen,"worldCamera",camera,"rendererFeature",feature);

            Fade(Step<CanvasFadeStep>(e,"000_FadeFromLastPuzzle"),cover,1f,.65f);
            Set(Step<WorldModeStep>(e,"010_ReturnToStory"),"worldModeController",mode,"targetMode",(int)WorldGameplayMode.Story,"waitUntilTransitionFinished",true);
            Toggle(e,"015_NoCameraFollow",camera.GetComponent<UnityEngine.Animations.PositionConstraint>(),false);
            Active(e,"020_StageSuppliesReturn",new[]{returnBoard.gameObject,audere.gameObject,bianca.gameObject},
                new[]{school.Find("COOP PUZZLES").gameObject,art.Find("Board").gameObject,art.Find("Classroom Board").gameObject,art.Find("Actors/Teacher_PLACEHOLDER").gameObject});
            Move(e,"030_AudereOnOwnTile",audere.transform,poses[0],0f);
            Move(e,"040_BiancaOnOwnTile",bianca.transform,poses[1],0f);
            SpriteFade(e,"050_RestoreAudere",audere.transform,1f,0f);
            SpriteFade(e,"060_RestoreBianca",bianca.transform,1f,0f);
            Facing(e,"070_AudereFacesBianca",audere.transform,true);
            Facing(e,"080_BiancaFacesAudere",bianca.transform,false);
            Move(e,"090_FrameTheTwoTiles",camera.transform,cameraPose,0f);
            Fade(Step<CanvasFadeStep>(e,"100_RevealAtClassroom"),cover,0f,.55f);
            Talk(e,"110_PuttingDownSupplies",D("RETURN_SETTLE",DialogueCharacterId.Bianca,"R|Tới rồi. Để hộp xuống đây nhé.","L|Ừ…"),ui.Dialogue);
            Wait(e,"120_AudereNoticesTheLabel",.4f);
            Set(Step<CharacterMotionStep>(e,"130_AudereRealizesTheMistake"),"actor",audere.transform,"targetTransform",poses[0],
                "actorRenderer",audere.GetComponent<SpriteRenderer>(),"groundedShadow",audere.GetComponentsInChildren<SpriteRenderer>(true).Single(r=>r.sortingOrder==4).transform,
                "motionMode",(int)CharacterMotionMode.VerticalInPlace,"duration",.19f,"arcHeight",.09f,"landingDuration",.08f,
                "facingMode",(int)CharacterFacingMode.Preserve,"useUnscaledTime",true);
            Talk(e,"140_WrongClassLabel",D("RETURN_WRONG_LABEL",DialogueCharacterId.Bianca,"L|Khoan… đây không phải lớp mình."),ui.Dialogue);
            Talk(e,"150_BiancaChecksTheBox",D("RETURN_BIANCA_CHECKS",DialogueCharacterId.Bianca,"R|À… chắc là hộp bên cạnh.","R|Hộp này là đồ lớp khác rồi."),ui.Dialogue);
            Wait(e,"160_AudereStaysStill",.85f);
            Talk(e,"170_TimorUsesTheMistake",D("RETURN_TIMOR_BLAME",DialogueCharacterId.Timor,
                "R|Thấy chưa?","L|…","R|Cậu làm phiền cậu ấy rồi.","R|Tớ đã bảo cậu ở lại lớp mà."),ui.Dialogue);
            Talk(e,"180_BiancaCallsOnce",D("RETURN_BIANCA_CALL",DialogueCharacterId.Bianca,"R|Audere?"),ui.Dialogue);
            Wait(e,"190_HeDoesNotAnswer",.5f);
            Talk(e,"200_TimorInvitesTheFear",D("RETURN_TIMOR_PROJECTION",DialogueCharacterId.Timor,
                "R|Nghĩ xem…","R|Giờ cậu ấy đang nghĩ gì về cậu nhỉ?"),ui.Dialogue);
            Wait(e,"210_HoldBeforePressure",.3f);
            Set(Step<FullscreenWorldModeTransitionStep>(e,"220_EnterBiancaPressure"),"transitionController",fullscreen,"worldModeController",mode,
                "transitionProfile",AssetDatabase.LoadAssetAtPath<FullscreenTransitionProfile>("Assets/_Audere/Data/Transitions/WorldTransition_DreamyDisorientation.asset"),
                "focusRenderer",audere.GetComponent<SpriteRenderer>(),"sourceMode",(int)WorldGameplayMode.Story,"targetMode",(int)WorldGameplayMode.Combat);
            Set(Step<CombatStep>(e,"230_BiancaCombat_PLACEHOLDER"),"combatController",controller,"combatEncounterData",encounter,
                "victoryBehaviour",(int)CombatResultBehaviour.Complete,"defeatBehaviour",(int)CombatResultBehaviour.Retry);
            Fade(Step<CanvasFadeStep>(e,"240_CoverCombatResolution"),cover,1f,.45f);
            Set(Step<WorldModeStep>(e,"250_ReturnAfterCombat"),"worldModeController",mode,"targetMode",(int)WorldGameplayMode.Story,"waitUntilTransitionFinished",true);
            Fade(Step<CanvasFadeStep>(e,"260_RevealStory"),cover,0f,.45f);
            returnBoard.gameObject.SetActive(false);combatRoot.gameObject.SetActive(false);systems.gameObject.SetActive(false);
            foreach(var old in new[]{"D2_SCHOOL_BIANCA_MORNING","D2_CLASSROOM_SUPPLIES"})
            {
                var normalize=eRoot.parent.Find(old).GetComponentsInChildren<SetActiveStep>(true).First(x=>x.name.Contains("NormalizeSchool")||x.name.Contains("StageClassroomUnderFade"));
                Set(normalize,"objectsToDisable",normalize.ObjectsToDisable.Concat(new[]{returnBoard.gameObject}).Distinct().ToArray());
            }
        }

        private static DialogueData D(string id,DialogueCharacterId partner,params string[] lines) => Day2SchoolMorningSetupTool.Dialogue(id,partner,lines);

        private static CombatEncounterData Encounter(Sprite sprite)
        {
            const string folder="Assets/_Audere/Data/Combat/BiancaSupplies";
            const string actorPath="Assets/_Audere/Prefabs/Combat/Enemies/Enemy_BiancaSupplies_PLACEHOLDER.prefab";
            Folder(folder);
            if(AssetDatabase.LoadAssetAtPath<GameObject>(actorPath)==null)
            {
                var source=PrefabUtility.LoadPrefabContents("Assets/_Audere/Prefabs/Combat/Enemies/Enemy_Sample_PLACEHOLDER.prefab");
                try {
                    source.name="Enemy_BiancaSupplies_PLACEHOLDER";
                    foreach(var image in source.GetComponentsInChildren<Image>(true)){image.name="Visual_Bianca_PLACEHOLDER";image.sprite=sprite;image.preserveAspect=true;}
                    PrefabUtility.SaveAsPrefabAsset(source,actorPath);
                } finally {PrefabUtility.UnloadPrefabContents(source);}
            }
            var actor=AssetDatabase.LoadAssetAtPath<GameObject>(actorPath).GetComponent<CombatEnemyActor>();
            string enemyPath=folder+"/Enemy_BiancaSupplies_PLACEHOLDER.asset";
            var enemy=AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>(enemyPath);
            if(enemy==null){enemy=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>("Assets/_Audere/Data/Combat/Enemies/Enemy_Sample.asset"));AssetDatabase.CreateAsset(enemy,enemyPath);}
            Set(enemy,"enemyId","d2-school-bianca-supplies-placeholder","displayName","Bianca","actorPrefab",actor,"phasePolicy",(int)CombatPhasePolicy.PerPhaseHealth);
            var es=new SerializedObject(enemy);var phases=es.FindProperty("phases");phases.arraySize=1;
            var phase=phases.GetArrayElementAtIndex(0);phase.FindPropertyRelative("phaseId").stringValue="bianca-supplies-placeholder";
            phase.FindPropertyRelative("maxHealth").intValue=12;phase.FindPropertyRelative("dialogueCues").arraySize=0;
            phase.FindPropertyRelative("spawnDice").boolValue=true;phase.FindPropertyRelative("allowsPlayerDefeat").boolValue=true;
            es.ApplyModifiedPropertiesWithoutUndo();
            string encounterPath=folder+"/CombatEncounter_D2_BIANCA_SUPPLIES_PLACEHOLDER.asset";
            var encounter=AssetDatabase.LoadAssetAtPath<CombatEncounterData>(encounterPath);
            if(encounter==null){encounter=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatEncounterData>("Assets/_Audere/Data/Combat/CombatEncounter_Sample.asset"));AssetDatabase.CreateAsset(encounter,encounterPath);}
            Set(encounter,"encounterId","d2-school-bianca-supplies-placeholder","enemyDefinition",enemy,"tutorialData",null,"encounterDuration",45f);
            if(!enemy.Validate(out string error))throw new InvalidOperationException(error);
            return encounter;
        }
    }
}
#endif
