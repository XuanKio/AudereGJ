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
using Object=UnityEngine.Object;
namespace Audere.Story.Editor
{
 public static class Day4TimorEveningSetupTool
 {
  public const string ScenePath="Assets/_Audere/Scenes/150_D4_Home_Evening.unity";
  public const string Folder="Assets/_Audere/Data/Combat/TimorReturn";
  public const string DialogueFolder="Assets/_Audere/Data/Dialogue/Day4/TimorEvening";
  public const string ProfilePath="Assets/_Audere/Data/Transitions/WorldTransition_TimorShadow.asset";
  public const string EncounterPath=Folder+"/CombatEncounter_D4_TIMOR_RETURN.asset";
  [MenuItem("Audere/Story/Author Active Day4 Timor Evening")]
  public static void AuthorActive()
  {
   var scene=SceneManager.GetActiveScene();
   if(scene.path!=ScenePath||scene.isDirty||EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Open saved Scene150 in Edit Mode.");
   EnsureFolder(Folder);EnsureFolder(DialogueFolder);
   var world=scene.GetRootGameObjects().Single(x=>x.name=="WORLD");
   var camera=All<Camera>(scene).Single();
   var stage=world.transform.Find("Home Stage PLACEHOLDER_NO_ART");
   var a=stage.GetComponentsInChildren<SpriteRenderer>(true).Single(x=>x.name=="Audere");
   a.flipX=true;a.sortingLayerName="Player";a.sortingOrder=5;
   var mask=camera.transform.Find("PuzzleViewportMask").gameObject;mask.SetActive(true);
   var mode=Component<WorldModeController>(world.transform);
   var transition=Component<FullscreenTransitionController>(world.transform);
   var systems=scene.GetRootGameObjects().FirstOrDefault(x=>x.name=="SYSTEMS");if(systems==null)systems=new GameObject("SYSTEMS");
   var combatRoot=world.transform.Find("Combat Root");
   if(combatRoot==null)
   {
    var reference=EditorSceneManager.OpenScene("Assets/_Audere/Scenes/40_Evening.unity",OpenSceneMode.Additive);
    try
    {
     var source=All<WorldModeController>(reference).Single();
     var sourceRoot=(GameObject)new SerializedObject(source).FindProperty("combatRoot").objectReferenceValue;
     var clone=Object.Instantiate(sourceRoot,world.transform);clone.name="Combat Root";combatRoot=clone.transform;
     Set(transition,"rendererFeature",new SerializedObject(All<FullscreenTransitionController>(reference).Single()).FindProperty("rendererFeature").objectReferenceValue);
    }
    finally {EditorSceneManager.CloseScene(reference,true);SceneManager.SetActiveScene(scene);}
   }
   var board=combatRoot.GetComponentInChildren<CombatBoardView>(true);
   var combat=Component<CombatController>(Child(systems.transform,"Combat Systems"));
   var encounter=AuthorEncounter();
   var actor=board.GetComponentsInChildren<CombatEnemyActor>(true).Single();actor.name="Enemy_Timor_Return";
   // Preserve the scene40 authored visual, not its obsolete Audere prefab placeholder.
   foreach(var graphic in actor.GetComponentsInChildren<Image>(true))if(graphic.sprite!=null)graphic.sprite=Sprite("Enemyy/timor.png");
   Set(board,"authoredEnemyActor",actor);Set(combat,"boardView",board,"encounterData",encounter,"playOnStart",false);
   Set(mode,"startingMode",2,"storyRoot",stage.gameObject,"combatRoot",combatRoot.gameObject,"combatSystemsRoot",combat.gameObject,
    "worldCamera",camera,"puzzleViewportMask",mask,"storyUsesPuzzleViewportMask",true,"allowChildFadeFallback",false,
    "revealStartingModeOnStart",false,"enableDebugHotkeys",false,"storyOrthographicSize",1.05f);
   var modeSO=new SerializedObject(mode);modeSO.FindProperty("storyCameraPosition").vector3Value=new Vector3(0,-.04f,-10);modeSO.ApplyModifiedPropertiesWithoutUndo();
   Set(transition,"worldCamera",camera);
   stage.gameObject.SetActive(true);combatRoot.gameObject.SetActive(false);combat.gameObject.SetActive(false);actor.gameObject.SetActive(false);
   camera.orthographicSize=1.05f;
   foreach(var scaler in All<CanvasScaler>(scene).Where(x=>x.name=="GameplayUIRoot"))Set(scaler,"m_ScreenMatchMode",(int)CanvasScaler.ScreenMatchMode.Expand);
   var director=All<StoryDirector>(scene).Single();
   foreach(var e in All<StoryEvent>(scene).Where(x=>x.GetComponentsInParent<StoryEvent>(true).Length==1).ToArray())Object.DestroyImmediate(e.gameObject);
   var story=Component<StoryEvent>(Child(director.transform,"D4_EVENING_TIMOR_RETURNS"));Set(story,"eventId","D4_EVENING_TIMOR_RETURNS","autoPlayNextEvent",false);Set(director,"startingEvent",story,"playOnStart",true);
   var cover=All<CanvasGroup>(scene).Single(x=>x.name=="Fade"&&x.transform.parent!=null&&x.transform.parent.name=="Scene Transition Overlay");cover.alpha=1;cover.blocksRaycasts=true;cover.interactable=false;
   var p=story.transform;
   Fade(p,"000_ArriveUnderCover",cover,1,0);
   Set(Step<WorldModeStep>(p,"002_AloneOnTheTile"),"worldModeController",mode,"targetMode",2);
   Facing(p,"004_FacingIntoTheRoom",a,true);Wait(p,"010_TimePasses",.7f);
   Fade(p,"020_ThatEvening",cover,0,1.2f);Wait(p,"030_Quiet",.9f);
   Talk(p,"040_ItWorkedOut",D("OPENING",DialogueCharacterId.None,"Audere_Tired.png",null,
    "L|Hôm nay không có Timor nhắc…","L|May là mọi chuyện rồi cũng ổn.","L|Lúc tớ ngã, mọi người đã giúp.","L|Tớ không phải làm hết một mình."));
   Wait(p,"050_A_Breath",.8f);
   Talk(p,"060_A_Voice",D("VOICE",DialogueCharacterId.Timor,"Audere_Tired.png","Timor/TimorBuon.png","R|Vậy à…"));
   Facing(p,"070_LookLeft",a,false);Wait(p,"080_SearchTheLeft",.65f);
   Facing(p,"090_LookRight",a,true);Wait(p,"100_SearchTheRight",.8f);
   Talk(p,"110_Timor",D("CALL",DialogueCharacterId.None,"Audere_Scared.png",null,"L|Timor?"));
   Wait(p,"120_NoAnswer",3f);
   Talk(p,"130_FromTheDark",D("UNNEEDED",DialogueCharacterId.Timor,"Audere_Scared.png","Timor/TimorLoLangKhongVui.png","R|Tớ tưởng cậu không cần tớ nữa."));
   Wait(p,"140_HoldTheWords",.45f);
   Set(Step<FullscreenWorldModeTransitionStep>(p,"150_HisShadowFillsTheRoom"),"transitionController",transition,"worldModeController",mode,"transitionProfile",AuthorProfile(),"sourceMode",2,"targetMode",1);
   Set(Step<CombatStep>(p,"160_TimorAgain"),"combatController",combat,"combatEncounterData",encounter,"enemyActorOverride",actor,"defeatBehaviour",0,"victoryBehaviour",0);
   foreach(var d in All<DialogueStep>(scene))Set(d,"dialogueController",All<DialogueController>(scene).Single());
   AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
  }
  static CombatEncounterData AuthorEncounter()
  {
   const string old="Assets/_Audere/Data/Combat/TimorNightPressure/";
   var enemy=New<CombatEnemyDefinition>(Folder+"/Enemy_TimorReturn.asset");
   EditorUtility.CopySerialized(AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>(old+"Enemy_TimorNightPressure.asset"),enemy);
   Set(enemy,"enemyId","d4-timor-return");
   var so=new SerializedObject(enemy);var phases=so.FindProperty("phases");
   for(int i=0;i<phases.arraySize;i++)
   {
    var phase=phases.GetArrayElementAtIndex(i);phase.FindPropertyRelative("phaseId").stringValue="timor-return-"+(i+1);
    // Day1-specific mother/message barks must never leak into Day4.
    phase.FindPropertyRelative("dialogueCues").arraySize=0;
   }
   so.ApplyModifiedPropertiesWithoutUndo();Save(enemy,Folder+"/Enemy_TimorReturn.asset");
   var data=New<CombatEncounterData>(EncounterPath);
   EditorUtility.CopySerialized(AssetDatabase.LoadAssetAtPath<CombatEncounterData>(old+"CombatEncounter_D1_TIMOR_NIGHT_PRESSURE.asset"),data);
   Set(data,"encounterId","d4-timor-return","enemyDefinition",enemy);
   so=new SerializedObject(data);so.FindProperty("defeatPresentation").FindPropertyRelative("dialogue").objectReferenceValue=null;so.ApplyModifiedPropertiesWithoutUndo();Save(data,EncounterPath);
   // Re-entry currently preserves the original fight rules. Final outcome is explicitly unresolved.
   return data;
  }
  static FullscreenTransitionProfile AuthorProfile()
  {
   const string matPath="Assets/_Audere/Materials/PostProcess/FullscreenTimorShadow.mat";
   var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(material==null){material=new Material(Shader.Find("Hidden/Audere/TimorShadowTransition"));AssetDatabase.CreateAsset(material,matPath);}
   var sprite=Sprite("Enemyy/timor.png");material.SetTexture("_ShadowTex",sprite.texture);var r=sprite.textureRect;
   material.SetVector("_ShadowUVRect",new Vector4(r.x/sprite.texture.width,r.y/sprite.texture.height,r.width/sprite.texture.width,r.height/sprite.texture.height));EditorUtility.SetDirty(material);
   var profile=New<FullscreenTransitionProfile>(ProfilePath);Set(profile,"profileId","timor-shadow-encroachment","displayName","Timor Shadow Encroachment","material",material,"duration",5.4f,"modeSwapTime",4.2f,"usesFocusRenderer",false);
   var so=new SerializedObject(profile);var tracks=so.FindProperty("floatTracks");tracks.arraySize=3;
   Track(tracks,0,"_ShadowExtent",new Keyframe(0,.5f),new Keyframe(.8f,.65f),new Keyframe(2.2f,1.45f),new Keyframe(3.9f,5.4f),new Keyframe(5.4f,5.4f));
   Track(tracks,1,"_ShadowOpacity",new Keyframe(0,0),new Keyframe(.5f,.85f),new Keyframe(3.9f,1),new Keyframe(4.4f,1),new Keyframe(5.4f,0));
   Track(tracks,2,"_Cover",new Keyframe(0,0),new Keyframe(3f,0),new Keyframe(4f,1),new Keyframe(4.4f,1),new Keyframe(5.4f,0));
   so.ApplyModifiedPropertiesWithoutUndo();Save(profile,ProfilePath);return profile;
  }
  static void Track(SerializedProperty tracks,int i,string name,params Keyframe[] keys){var track=tracks.GetArrayElementAtIndex(i);track.FindPropertyRelative("shaderProperty").stringValue=name;track.FindPropertyRelative("values").animationCurveValue=new AnimationCurve(keys);}
  static DialogueData D(string id,DialogueCharacterId right,string audere,string portrait,params string[] text)
  {
   string path=DialogueFolder+"/Dialogue_D4_TIMOR_"+id+".asset";var d=New<DialogueData>(path);
   Set(d,"dialogueId","D4_TIMOR_"+id,"leftCharacter",1,"rightCharacter",(int)right,"leftPortraitOverride",Sprite("Audere/"+audere),"rightPortraitOverride",portrait==null?null:Sprite(portrait));
   var so=new SerializedObject(d);var lines=so.FindProperty("lines");lines.arraySize=text.Length;
   for(int i=0;i<text.Length;i++){var l=lines.GetArrayElementAtIndex(i);l.FindPropertyRelative("speaker").intValue=text[i][0]=='L'?0:1;l.FindPropertyRelative("text").stringValue=text[i].Substring(2);l.FindPropertyRelative("characterOverride").intValue=0;l.FindPropertyRelative("portraitOverride").objectReferenceValue=null;l.FindPropertyRelative("glitchPortraitTransition").boolValue=false;}so.ApplyModifiedPropertiesWithoutUndo();Save(d,path);return d;
  }
  static void Facing(Transform p,string n,SpriteRenderer a,bool right)=>Set(Step<SetActorFacingStep>(p,n),"actorRenderer",a,"faceRight",right,"sourceSpriteFacesLeft",true);
  static void Fade(Transform p,string n,CanvasGroup g,float a,float d)=>Set(Step<CanvasFadeStep>(p,n),"canvasGroup",g,"targetAlpha",a,"duration",d);
  static void Wait(Transform p,string n,float d)=>Set(Step<WaitStep>(p,n),"duration",d);
  static void Talk(Transform p,string n,DialogueData d)=>Set(Step<DialogueStep>(p,n),"dialogueData",d);
  static T New<T>(string path)where T:ScriptableObject=>AssetDatabase.LoadAssetAtPath<T>(path)??ScriptableObject.CreateInstance<T>();
  static void Save(Object a,string path){if(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(a)))AssetDatabase.CreateAsset(a,path);else EditorUtility.SetDirty(a);}
  static Sprite Sprite(string path)=>AssetDatabase.LoadAllAssetsAtPath("Assets/_Audere/AssetGame/"+path).OfType<Sprite>().First();
  static Transform Child(Transform p,string n){var t=p.Find(n);if(t!=null)return t;var go=new GameObject(n);go.transform.SetParent(p,false);return go.transform;}
  static T Component<T>(Transform t)where T:Component{var c=t.GetComponent<T>();return c!=null?c:t.gameObject.AddComponent<T>();}
  static T Step<T>(Transform p,string n)where T:StoryStep=>Component<T>(Child(p,n));
  static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<T>(true)).ToArray();
  static void EnsureFolder(string p){if(AssetDatabase.IsValidFolder(p))return;int i=p.LastIndexOf('/');EnsureFolder(p.Substring(0,i));AssetDatabase.CreateFolder(p.Substring(0,i),p.Substring(i+1));}
  static void Set(Object o,params object[] pairs)
  {
   var so=new SerializedObject(o);for(int i=0;i<pairs.Length;i+=2){var p=so.FindProperty((string)pairs[i]);var v=pairs[i+1];if(p==null)throw new InvalidOperationException(o.name+":"+pairs[i]);
    if(v is string s)p.stringValue=s;else if(v is int n)p.intValue=n;else if(v is bool b)p.boolValue=b;else if(v is float f)p.floatValue=f;else p.objectReferenceValue=v as Object;}
   so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(o);
  }
 }
}
#endif
