#if UNITY_EDITOR
using System;
using System.Linq;
using Audere.Combat;
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
    public static partial class Day4CrowdSetupTool
    {
        [MenuItem("Audere/Story/Polish Active Day4 Crowd Hands And Background")]
        public static void PolishActive()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || scene.isDirty || EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Open saved Scene140 in Edit Mode first.");
            string handPath = Prefabs + "/Bullets/Bullet_CrowdHand.prefab";
            var handRoot = PrefabUtility.LoadPrefabContents(handPath);
            try
            {
                ((RectTransform)handRoot.transform).sizeDelta = new Vector2(46f, 54f);
                var handImage = handRoot.GetComponentInChildren<Image>(true);
                handImage.rectTransform.sizeDelta = new Vector2(108f, 480f);
                PrefabUtility.SaveAsPrefabAsset(handRoot, handPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(handRoot); }
            var hand = AssetDatabase.LoadAssetAtPath<CombatBulletView>(handPath);
            var wave = New<OscillatingHandWallMove>(Folder + "/Move_HandWaves.asset");
            Set(wave,"handPrefab",hand,"duration",7.3f,"warning",.8f,"handsPerSide",8,"period",2.3f,"meanDepth",.27f,"amplitude",.14f);
            Save(wave,Folder + "/Move_HandWaves.asset");
            var clasp = New<ConvergingHandsMove>(Folder + "/Move_ClaspAndStab.asset");
            Set(clasp,"handPrefab",hand,"duration",5.1f,"gripHands",3,"warning",.9f,"closeDuration",.45f,"holdDuration",2.4f,"releaseDuration",.55f,"stabInterval",.34f,"stabWarning",.3f);
            Save(clasp,Folder + "/Move_ClaspAndStab.asset");
            MoveSet("Crowded",wave,clasp,
                AssetDatabase.LoadAssetAtPath<CombatMoveDefinition>(Folder+"/Move_ShiftingPalms.asset"),
                AssetDatabase.LoadAssetAtPath<CombatMoveDefinition>(Folder+"/Move_RushingVoices.asset"));

            var mode=All<WorldModeController>(scene).Single();
            Set(mode,"storyUsesPuzzleViewportMask",true);
            var modeSo=new SerializedObject(mode);
            var mask=modeSo.FindProperty("puzzleViewportMask").objectReferenceValue as GameObject;
            if(mask!=null)mask.SetActive(true);
            var combatRoot=(GameObject)modeSo.FindProperty("combatRoot").objectReferenceValue;
            var camera=(Camera)modeSo.FindProperty("worldCamera").objectReferenceValue;
            var backdrop=Child(combatRoot.transform,"Crowd Drifting Mouths Background");
            var canvas=Component<Canvas>(backdrop);
            backdrop=canvas.transform;
            canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=4f;
            canvas.overrideSorting=true; canvas.sortingOrder=-30;
            var image=Component<RawImage>(Child(backdrop,"Warped floating mouths"));
            var rt=image.rectTransform; rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
            image.raycastTarget=false;image.enabled=false;
            var sprite=Sprite("Item/Khong_Co_Tieu_e113_20260829003629.png");image.texture=sprite.texture;
            string matPath="Assets/_Audere/Materials/Combat/CrowdDriftingMouths.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(mat==null){mat=new Material(Shader.Find("Audere/UI/DriftingSpriteField"));AssetDatabase.CreateAsset(mat,matPath);}
            Rect r=sprite.textureRect; mat.SetVector("_UVRect",new Vector4(r.x/sprite.texture.width,r.y/sprite.texture.height,r.width/sprite.texture.width,r.height/sprite.texture.height));
            mat.SetColor("_Color",new Color(.42f,.32f,.48f,1f));EditorUtility.SetDirty(mat);image.material=mat;
            var field=Component<DriftingSpriteField>(backdrop);
            Set(field,"image",image,"fieldMaterial",mat);
            var fieldSo=new SerializedObject(field);var op=fieldSo.FindProperty("phaseOpacity");op.arraySize=2;op.GetArrayElementAtIndex(0).floatValue=.55f;op.GetArrayElementAtIndex(1).floatValue=.18f;fieldSo.ApplyModifiedPropertiesWithoutUndo();
            var board=All<CombatBoardView>(scene).Single();
            var actor=(CombatEnemyActor)new SerializedObject(board).FindProperty("authoredEnemyActor").objectReferenceValue;
            var actorSo=new SerializedObject(actor);var modules=actorSo.FindProperty("mechanicModules");
            bool assigned=false;for(int i=0;i<modules.arraySize;i++)if(modules.GetArrayElementAtIndex(i).objectReferenceValue==field)assigned=true;
            if(!assigned){int i=modules.arraySize;modules.arraySize++;modules.GetArrayElementAtIndex(i).objectReferenceValue=field;}actorSo.ApplyModifiedPropertiesWithoutUndo();

            var story=All<StoryEvent>(scene).Single(x=>x.EventId=="D4_CLASSROOM_CROWD");
            Talk(story.transform,"052_TheFloorFirst",D("FALL",DialogueCharacterId.None,"Audere_Tired.png",null,
                "L|A… đau.","L|Đồ rơi hết rồi…","L|Tớ nhặt lại là được."));
            Set(story.transform.Find("055_OnTheFloor").GetComponent<WaitStep>(),"duration",.65f);
            var thought=D("THOUGHT",DialogueCharacterId.None,"Audere_Tired.png",null,
                "L|Sao tự nhiên im thế…","L|…Có ai vừa cười à?","L|Đừng nhìn tớ lúc này…");
            var thoughtSo=new SerializedObject(thought);var lines=thoughtSo.FindProperty("lines");
            for(int i=1;i<lines.arraySize;i++)lines.GetArrayElementAtIndex(i).FindPropertyRelative("portraitOverride").objectReferenceValue=Sprite("Audere/Audere_Scared.png");
            thoughtSo.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(thought);
            Order(story.transform); AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif

