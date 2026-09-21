#if UNITY_EDITOR
using System;
using Audere.Combat;
using UnityEditor;
using UnityEngine;

namespace Audere.Story.Editor
{
    public static partial class Day4CrowdSetupTool
    {
        [MenuItem("Audere/Combat/Apply Crowd Mount Dive Phase Two")]
        public static void ApplyCrowdMountDivePhaseTwo()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Apply Crowd Mount Dive in ready Edit Mode.");
            var enemy=AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>(Folder+"/Enemy_Crowd.asset");
            if(enemy==null)throw new MissingReferenceException("Author Crowd content before applying phase two.");
            var moves=CreateCrowdMountDiveMoveSet();
            var so=new SerializedObject(enemy);so.FindProperty("sharedMaxHealth").intValue=19;var phases=so.FindProperty("phases");
            if(phases.arraySize<2 || phases.arraySize>3)throw new InvalidOperationException("Crowd must have two or three authored phases.");
            if(phases.arraySize==2)phases.InsertArrayElementAtIndex(1);
            phases.GetArrayElementAtIndex(0).FindPropertyRelative("sharedExitThreshold").intValue=10;
            var pressure=phases.GetArrayElementAtIndex(1);
            pressure.FindPropertyRelative("phaseId").stringValue="nowhere-to-hide";
            pressure.FindPropertyRelative("sharedExitThreshold").intValue=3;
            pressure.FindPropertyRelative("dialogueCues").arraySize=0;
            pressure.FindPropertyRelative("moveSet").objectReferenceValue=moves;
            pressure.FindPropertyRelative("damageReactionMove").objectReferenceValue=AssetDatabase.LoadAssetAtPath<EnemyMountDiveMove>(Folder+"/Move_MountDive_Counter.asset");
            pressure.FindPropertyRelative("damageReactionOnEnter").boolValue=true;
            var quiet=phases.GetArrayElementAtIndex(2);
            quiet.FindPropertyRelative("phaseId").stringValue="one-real-voice";
            quiet.FindPropertyRelative("sharedExitThreshold").intValue=0;
            quiet.FindPropertyRelative("moveSet").objectReferenceValue=CreateCrowdQuietMoveSet();
            quiet.FindPropertyRelative("damageReactionMove").objectReferenceValue=null;
            quiet.FindPropertyRelative("damageReactionOnEnter").boolValue=false;
            so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(enemy);
            ApplyCrowdCatchPolish();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Audere/Combat/Tune Crowd Pressure Rhythm")]
        public static void TuneCrowdPressureRhythm()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stop Play and finish compilation before tuning Crowd.");
            CreateCrowdMountDiveMoveSet();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Audere/Combat/Resize Loaded Crowd Battle Box")]
        public static void ResizeLoadedCrowdBattleBox()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded || scene.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Load saved Scene140 in Edit Mode first.");
            var boards = All<CombatBoardView>(scene);
            if (boards.Length != 1) throw new InvalidOperationException("Scene140 needs one authored combat board.");
            SetCrowdBattleBoxSize(boards[0]);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }

        private static void SetCrowdBattleBoxSize(CombatBoardView board)
        {
            var serialized = new SerializedObject(board);
            var frame = (RectTransform)serialized.FindProperty("battleBoxFrame").objectReferenceValue;
            var overlay = (RectTransform)serialized.FindProperty("airborneDiceRoot").objectReferenceValue;
            var field = board.PlayArea;
            foreach (var rect in new[] { field, frame, overlay })
            {
                if (rect == null) continue;
                Undo.RecordObject(rect, "Resize Crowd battle box");
                rect.sizeDelta = rect == frame ? new Vector2(760f, 440f) : new Vector2(720f, 360f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
                EditorUtility.SetDirty(rect);
            }
        }

        private static CombatMoveSet CreateCrowdMountDiveMoveSet()
        {
            const string materialPath="Assets/_Audere/Materials/Combat/MountRainbowEcho.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Shader shader=Shader.Find("Audere/UI/Combat Rainbow Echo");
            if(shader==null)throw new MissingReferenceException("Compile the Combat Rainbow Echo shader first.");
            if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,materialPath);}
            else{material.shader=shader;EditorUtility.SetDirty(material);}
            var alternating=New<EnemyMountDiveMove>(Folder+"/Move_MountDive_Alternating.asset");
            ConfigureMountDive(alternating,material,false,new[]{0f,-.18f,.18f},.65f,.4f,2f,.95f,.55f);
            Save(alternating,Folder+"/Move_MountDive_Alternating.asset");
            var tracking=New<EnemyMountDiveMove>(Folder+"/Move_MountDive_Tracking.asset");
            ConfigureMountDive(tracking,material,true,new[]{0f,0f},.78f,.36f,2f,.95f,.6f);
            Save(tracking,Folder+"/Move_MountDive_Tracking.asset");
            var counter=New<EnemyMountDiveMove>(Folder+"/Move_MountDive_Counter.asset");
            ConfigureMountDive(counter,material,true,new[]{0f},.48f,.34f,2f,.85f,.42f);
            Save(counter,Folder+"/Move_MountDive_Counter.asset");
            var hand=AssetDatabase.LoadAssetAtPath<CombatBulletView>(Prefabs+"/Bullets/Bullet_CrowdHand.prefab");
            var aisle=New<ProcessionGateMove>(Folder+"/Move_WatchingAisle.asset");
            Set(aisle,"projectilePrefab",hand,"fromBothSides",false,"gapOffsets",new[]{-.23f,0f,.23f,0f,-.23f},
                "gapWidth",114f,"visualHalfWidth",54f,"spacing",58f,"speed",235f,"volleyInterval",2f,"entryDelay",.65f,"duration",11f,"leadInDuration",.45f);
            Save(aisle,Folder+"/Move_WatchingAisle.asset");
            var press=New<ProcessionGateMove>(Folder+"/Move_PressureCorridor.asset");
            Set(press,"projectilePrefab",hand,"fromBothSides",true,"gapOffsets",new[]{-.19f,.19f,0f,-.19f},
                "gapWidth",114f,"visualHalfWidth",54f,"spacing",46f,"speed",235f,"volleyInterval",3.2f,"entryDelay",.7f,"duration",13.2f,"leadInDuration",.5f);
            Save(press,Folder+"/Move_PressureCorridor.asset");
            var diagonal=New<ProcessionGateMove>(Folder+"/Move_DiagonalHands.asset");
            Set(diagonal,"projectilePrefab",hand,"fromBothSides",true,"gapOffsets",new[]{0f,-.16f,.16f,0f},
                "gapWidth",116f,"visualHalfWidth",54f,"spacing",46f,"speed",235f,"volleyInterval",3.2f,
                "entryDelay",.7f,"diagonalSlope",.32f,"duration",13.2f,"leadInDuration",.5f);
            Save(diagonal,Folder+"/Move_DiagonalHands.asset");
            return MoveSet("MountPressure",aisle,press,diagonal);
        }

        private static void ApplyCrowdCatchPolish()
        {
            var clasp=AssetDatabase.LoadAssetAtPath<ConvergingHandsMove>(Folder+"/Move_ClaspAndStab.asset");
            if(clasp==null)return;
            Set(clasp,"duration",7.2f,"gripHands",3,"startDistance",215f,"chaseSpeed",310f,"warningDuration",.4f,
                "chaseDuration",.9f,"trackingDuration",.28f,"appearDuration",.14f,
                "fadeDuration",.38f,"gapDuration",.3f,"captureHoldDuration",1.02f,
                "stabCount",3,"stabInterval",.15f,"stabWarning",.22f,"recoveryDuration",.4f);
            Save(clasp,Folder+"/Move_ClaspAndStab.asset");
        }

        private static CombatMoveSet CreateCrowdQuietMoveSet() => MoveSet("ThereIsRoom",
            AssetDatabase.LoadAssetAtPath<GraspingHandsMove>(Folder+"/Move_UncertainHands.asset"),
            AssetDatabase.LoadAssetAtPath<LinearProjectilePatternMove>(Folder+"/Move_DistantVoices.asset"));

        private static void ConfigureMountDive(EnemyMountDiveMove move, Material material, bool aimed,
            float[] offsets,float windup,float plunge,float hold,float rise,float recovery)
        {
            var so=new SerializedObject(move);
            so.FindProperty("duration").floatValue=offsets.Length*(windup+plunge+hold+rise+recovery);
            so.FindProperty("leadInDuration").floatValue=.25f;
            so.FindProperty("aimAtPlayer").boolValue=aimed;
            so.FindProperty("windup").floatValue=windup;so.FindProperty("plunge").floatValue=plunge;
            so.FindProperty("hold").floatValue=hold;so.FindProperty("returnDuration").floatValue=rise;
            so.FindProperty("recovery").floatValue=recovery;
            so.FindProperty("separationFraction").floatValue=.11f;
            so.FindProperty("bodyWidthFraction").floatValue=.22f;so.FindProperty("bodyHeight").floatValue=148f;
            so.FindProperty("rainbowEchoMaterial").objectReferenceValue=material;
            so.FindProperty("impactBulletPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<CombatBulletView>(Prefabs+"/Bullets/EnemyBullet.prefab");
            so.FindProperty("impactBulletsPerSide").intValue=5;
            so.FindProperty("impactBulletSpeed").floatValue=160f;
            so.FindProperty("impactBulletWarning").floatValue=.22f;
            var positions=so.FindProperty("impactOffsets");positions.arraySize=offsets.Length;
            for(int i=0;i<offsets.Length;i++)positions.GetArrayElementAtIndex(i).floatValue=offsets[i];
            so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(move);
        }
    }
}
#endif
