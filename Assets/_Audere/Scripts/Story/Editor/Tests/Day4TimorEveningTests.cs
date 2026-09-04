#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Audere.Audio;
using Audere.Combat;
using Audere.Core;
using Audere.Dialogue;

using Audere.UI;
using Audere.Story.Steps;
using Audere.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Audere.Story.Editor.Tests
{
 public sealed class Day4TimorEveningTests
 {
  const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  [Test]
  public void Content_OpeningHasDirectBindingsMaskAndSeparateCombatWithoutDay1Dialogue()
  {
   var s=EditorSceneManager.OpenScene(Day4TimorEveningSetupTool.ScenePath);
   Assert.IsFalse(s.GetRootGameObjects().Any(x=>x.name=="DAY FOUR EVENING COVER"));
   var director=All<StoryDirector>(s).Single();
   Assert.IsTrue(new SerializedObject(director).FindProperty("playOnStart").boolValue,
    "Scene150 production StoryDirector must start automatically after Scene140 loads it.");
   var story=All<StoryEvent>(s).Single();Assert.AreEqual("D4_EVENING_TIMOR_RETURNS",story.EventId);
   foreach(Transform t in story.transform)Assert.AreEqual(1,t.GetComponents<StoryStep>().Length,t.name);
   var mode=All<WorldModeController>(s).Single();Assert.IsTrue(new SerializedObject(mode).FindProperty("storyUsesPuzzleViewportMask").boolValue);
   Assert.IsFalse(new SerializedObject(mode).FindProperty("allowChildFadeFallback").boolValue);
   foreach(var dialogue in All<DialogueStep>(s))
   {
    Assert.IsNotNull(new SerializedObject(dialogue).FindProperty("dialogueController").objectReferenceValue);
    var data=(DialogueData)new SerializedObject(dialogue).FindProperty("dialogueData").objectReferenceValue;
    foreach(var line in data.Lines)Assert.LessOrEqual(line.Text.Length,42,line.Text);
   }
   var transition=All<FullscreenWorldModeTransitionStep>(s).Single();var profile=transition.TransitionProfile;
   Assert.IsTrue(profile.Validate(out string error),error);Assert.AreEqual(Day4TimorEveningSetupTool.ProfilePath,AssetDatabase.GetAssetPath(profile));
   var cover=profile.FloatTracks.Single(t=>t.ShaderProperty=="_Cover").Values;Assert.AreEqual(1f,cover.Evaluate(profile.ModeSwapTime),.0001f);Assert.AreEqual(0f,cover.Evaluate(profile.Duration));
   Assert.IsEmpty(ShaderUtil.GetShaderMessages(profile.Material.shader));
   var combat=All<CombatStep>(s).Single();Assert.IsNotNull(combat.EnemyActorOverride);Assert.AreEqual(Day4TimorEveningSetupTool.EncounterPath,AssetDatabase.GetAssetPath(combat.CombatEncounterData));
   Assert.IsTrue(combat.CombatEncounterData.EnemyDefinition.Validate(out error),error);Assert.AreEqual(AudioId.Music_TimorCombat,combat.CombatEncounterData.Music);
   Assert.IsNull(combat.CombatEncounterData.DefeatPresentation.Dialogue);
   var enemy=combat.CombatEncounterData.EnemyDefinition;
   Assert.AreEqual(CombatPhasePolicy.PerPhaseHealth,enemy.PhasePolicy);
   Assert.AreEqual(3,enemy.PhaseCount);
   Assert.IsTrue(enemy.Phases.All(p=>p.MaxHealth==11&&p.SpawnDice));
   Assert.AreEqual(243.75f,combat.CombatEncounterData.EncounterDuration);
   Assert.AreEqual(195f,
    GameplayDifficultySettings.ScalePlayerTime(
     combat.CombatEncounterData.EncounterDuration,GameDifficulty.Easy),.001f);
   var biancaBoomerang=AssetDatabase.LoadAssetAtPath<ReturningOrbitMove>(
    Day4TimorFinalSetupTool.MoveFolder+"/Move_BiancaMemoryBoomerang.asset");
   Assert.IsNotNull(biancaBoomerang);
   Assert.IsTrue(biancaBoomerang.HorizontalTraversal,
    "Scene150 Bianca boomerang must fly straight out and return along the same lane.");
   Assert.AreEqual(3,combat.CombatEncounterData.DicePerBatch);
   Assert.AreEqual(1,combat.CombatEncounterData.MaximumAttacksPerBatch);
   Assert.AreEqual(1,combat.CombatEncounterData.AdditionalRerolledAttacksPerBatch);
   Assert.IsTrue(combat.CombatEncounterData.OutcomeRules.Allows(CombatResult.Victory));
   Assert.IsTrue(combat.CombatEncounterData.OutcomeRules.Allows(CombatResult.Defeat));
   Assert.IsTrue(enemy.Phases.Take(2).All(p=>p.DialogueCues.Any(c=>c.RequiredBeforePhaseAdvance)));
   Assert.AreEqual(5,enemy.Phases[2].DialogueCues.Count(c=>c.RequiredBeforeVictory));
   Assert.AreEqual(6, enemy.Phases[0].MoveSet.Entries.Count);
   Assert.AreEqual(8, enemy.Phases[1].MoveSet.Entries.Count);
   Assert.AreEqual(11, enemy.Phases[2].MoveSet.Entries.Count);



    var phase1Patterns=enemy.Phases[0].MoveSet.Entries.Select(e=>e.Move)
        .OfType<NarrativePressurePatternMove>().ToArray();
    var phase2Patterns=enemy.Phases[1].MoveSet.Entries.Select(e=>e.Move)
        .OfType<NarrativePressurePatternMove>().ToArray();
    var phase3Patterns=enemy.Phases[2].MoveSet.Entries.Select(e=>e.Move)
        .OfType<NarrativePressurePatternMove>().ToArray();
    Assert.IsTrue(phase1Patterns.All(m=>m.UsesMusicGrid&&m.RhythmBpm==110f&&
        m.Speed>=134f&&m.WaveBeats<=2.8f));
    Assert.IsTrue(phase2Patterns.All(m=>m.UsesMusicGrid&&m.RhythmBpm==110f&&
        m.Speed>=141f&&m.WaveBeats<=2.2f));
    Assert.IsTrue(phase3Patterns.All(m=>m.UsesMusicGrid&&m.RhythmBpm==110f&&
        m.Speed>=158f&&m.WaveBeats<=1.8f));
    Assert.IsTrue(enemy.Phases.SelectMany(p=>p.MoveSet.Entries)
        .All(e=>e.Move.LeadInDuration>=.46f),
        "Every Scene150 Timor attack must begin after a readable hazard-free beat.");
    Assert.IsTrue(phase1Patterns.All(m=>m.SafeGapFraction>=.34f&&m.WavesPerBurst==2&&m.BreatherGridPulses>=1));
    Assert.IsTrue(phase2Patterns.All(m=>m.SafeGapFraction>=.32f&&m.WavesPerBurst==3&&m.BreatherGridPulses>=1));
    Assert.IsTrue(phase3Patterns.All(m=>m.SafeGapFraction>=.30f&&m.WavesPerBurst==3&&m.BreatherGridPulses>=1));
    Assert.GreaterOrEqual(enemy.Phases[1].MoveSet.Entries.Count(e=>e.Move is CompositeCombatMove),3);
    Assert.GreaterOrEqual(enemy.Phases[2].MoveSet.Entries.Count(e=>e.Move is CompositeCombatMove),3);
    Assert.IsFalse(enemy.Phases.SelectMany(p=>p.MoveSet.Entries)
        .Any(e=>e.Move!=null&&e.Move.name=="Move_TimorNightPressure_11"),
        "Scene150 must not reuse Scene40's forced-defeat finale.");
    Assert.IsTrue(enemy.Phases[1].MoveSet.Entries.Any(e=>e.Move is TimorTailThrowMove));
   var projections=enemy.Phases[2].MoveSet.Entries.Select(e=>e.Move).OfType<ProjectionAssaultMove>().ToArray();
   Assert.AreEqual(3,projections.Length);
   CollectionAssert.AreEquivalent(new[]{"IMG_1040_0","IMG_1043_0","IMG_1054_0"},projections.Select(p=>p.ProjectionSprite.name));
   Assert.IsTrue(projections.All(p=>p.Copies==2));
   var board=All<CombatBoardView>(s).Single();
   var field=board.GetComponentsInChildren<RectTransform>(true).Single(x=>x.name=="Dice Field");
   var mount=board.GetComponentsInChildren<RectTransform>(true).Single(x=>x.name=="Enemy Mount");
   Assert.AreEqual(0f,field.anchoredPosition.x,.001f);
   Assert.AreEqual(new Vector2(580f,420f),field.sizeDelta);
   Assert.AreEqual(new Vector2(0f,421f),mount.anchoredPosition);
   Assert.AreEqual(new Vector2(360f,240f),mount.sizeDelta);


   Assert.IsNotNull(combat.CombatEncounterData.VictoryPresentation.Dialogue);
   var finalCanvas=All<CanvasGroup>(s).Single(x=>x.name=="FINAL CUTSCENE");
   var tileRoot=All<Transform>(s).Single(x=>x.name=="ENDING TILES");
   var endingTiles=tileRoot.Cast<Transform>().OrderBy(x=>x.name).ToArray();
   Assert.AreEqual(8,endingTiles.Length);
   float minTileSpacing=endingTiles.SelectMany((a,i)=>endingTiles.Skip(i+1)
    .Select(b=>Vector2.Distance(a.position,b.position))).Min();
   float maxTileDiameter=endingTiles.Select(x=>x.GetComponentInChildren<SpriteRenderer>(true).bounds.size)
    .Max(size=>Mathf.Max(size.x,size.y));
   Assert.GreaterOrEqual(minTileSpacing,.249f,"Ending tiles must keep Scene80 Dream world spacing.");
   Assert.Greater(minTileSpacing,maxTileDiameter,"Ending tiles overlap in world space.");
   Assert.IsNotNull(finalCanvas.GetComponentInChildren<MainMenuBackgroundParallax>(true));
   var finalCanvasComponent=finalCanvas.GetComponent<Canvas>();
   var finalImage=finalCanvas.GetComponentsInChildren<UnityEngine.UI.Image>(true)
    .Single(x=>x.name=="final_cutscene");
   var finalRect=finalImage.rectTransform;
   Assert.AreEqual(Vector2.zero,finalRect.anchorMin);
   Assert.AreEqual(Vector2.one,finalRect.anchorMax);
   Assert.AreEqual(Vector2.zero,finalRect.offsetMin);
   Assert.AreEqual(Vector2.zero,finalRect.offsetMax);
   Assert.IsTrue(finalImage.preserveAspect);
   Assert.AreEqual(16f/9f,finalImage.sprite.rect.width/finalImage.sprite.rect.height,.001f);
   Assert.AreEqual(RenderMode.ScreenSpaceOverlay,finalCanvasComponent.renderMode);
   var mainCamera=All<Camera>(s).Single(x=>x.CompareTag("MainCamera"));
   Assert.AreEqual(new Rect(0f,0f,1f,1f),mainCamera.rect);
   Assert.AreEqual(16f/9f,mainCamera.aspect,.001f);
    var credits=All<CanvasGroup>(s).Single(x=>x.name=="CREDITS");
    Assert.Greater(finalCanvasComponent.sortingOrder,1000);
    Assert.Greater(credits.GetComponent<Canvas>().sortingOrder,finalCanvasComponent.sortingOrder);
    StringAssert.Contains("tuổi nổi loạn",credits.GetComponentInChildren<TMPro.TMP_Text>(true).text);
    var motions=All<CharacterMotionStep>(s).Where(x=>x.transform.IsChildOf(story.transform)).ToArray();
    Assert.AreEqual(5,motions.Length);
    Assert.IsTrue(motions.All(x=>x.Actor!=null&&x.TargetTransform!=null&&x.ActorRenderer!=null&&x.GroundedShadow!=null));
    Assert.AreEqual(4,motions.Count(x=>x.MotionMode==CharacterMotionMode.TravelToTarget));
    Assert.AreEqual("400_EndGameFade",story.transform.GetChild(story.transform.childCount-1).name);
   foreach(var c in All<MonoBehaviour>(s).Where(x=>x!=null))
   {
    var so=new SerializedObject(c);var it=so.GetIterator();while(it.NextVisible(true))if(it.propertyType==SerializedPropertyType.ObjectReference)
    {var other=it.objectReferenceValue as Component;if(other!=null&&!EditorUtility.IsPersistent(other))Assert.AreEqual(s,other.gameObject.scene,c.name+":"+it.propertyPath);}
   }
  }

[Test]
  public void Runtime_PerPhaseDialogueGateStopsAtOneThenAdvances()
  {
   var s=EditorSceneManager.OpenScene(Day4TimorEveningSetupTool.ScenePath);
   var combat=All<CombatController>(s).Single();
   Transform combatRoot=combat.BoardView.transform;
   while(combatRoot.parent!=null&&combatRoot.parent.name!="WORLD")combatRoot=combatRoot.parent;
   combatRoot.gameObject.SetActive(true);combat.gameObject.SetActive(true);
   var runtime=new CombatEnemyRuntime(combat.CurrentEncounter.EnemyDefinition,combat.BoardView,
    new SystemCombatRandom(71),71,true);
   runtime.Start();
   for(int phase=0;phase<2;phase++)
   {
    var cue=runtime.CurrentPhase.DialogueCues.Single(x=>x.RequiredBeforePhaseAdvance);
    runtime.ApplyDamage(99,out int applied);
    Assert.AreEqual(1,runtime.CurrentHealth);
    Assert.AreEqual(CombatEnemyRuntimeState.Playing,runtime.State);
    Assert.IsFalse(runtime.AcceptsDamage);
    runtime.MarkCueResolved(cue);runtime.Tick(0f);
    Assert.AreEqual(CombatEnemyRuntimeState.TransitioningPhase,runtime.State);
    runtime.CompletePhaseBreak();
    Assert.AreEqual(phase+1,runtime.PhaseIndex);
    Assert.AreEqual(11,runtime.CurrentHealth);
   }
   runtime.Cancel();combat.BoardView.ClearCombatRuntime();
  }

[UnityTest]
  public IEnumerator FinalBoss_TailMemoriesVictoryAndCreditsPlayThrough()
  {
   EditorSceneManager.OpenScene(Day4TimorEveningSetupTool.ScenePath);
   yield return new EnterPlayMode();yield return null;
   var s=SceneManager.GetActiveScene();
   var director=All<StoryDirector>(s).Single();
   var combat=All<CombatController>(s).Single();
   yield return Until(()=>combat.IsPlaying&&combat.EnemyRuntime!=null,true,35);
   Assert.AreEqual(0,combat.EnemyRuntime.PhaseIndex);
   combat.EnemyRuntime.ApplyDamage(99,out int p1Damage);
   Assert.AreEqual(1,combat.EnemyHealth);
   yield return Until(()=>combat.EnemyRuntime.PhaseIndex==1,true,20);
   Assert.AreEqual(11,combat.EnemyHealth);
   yield return Until(()=>combat.EnemyRuntime.CurrentMove is TimorTailThrowMove,true,8);
   var actorImage=combat.EnemyRuntime.Actor.Graphics.OfType<UnityEngine.UI.Image>().First();
   Assert.AreEqual("timor no tail",actorImage.sprite.name);
   yield return Capture("150-tail-warning");
   yield return Until(()=>combat.BoardView.HasForcedPlayerControl,true,5);
   yield return Capture("150-tail-caught");
   Assert.IsTrue(combat.BoardView.HasForcedMovementProtection);
   yield return Until(()=>!combat.BoardView.HasForcedPlayerControl,true,5);
   Assert.AreEqual("timor",actorImage.sprite.name);
   combat.EnemyRuntime.ApplyDamage(99,out int p2Damage);
   yield return Until(()=>combat.EnemyRuntime.PhaseIndex==2,true,20);
   combat.EnemyRuntime.ApplyDamage(99,out int p3Damage);
   Assert.AreEqual(1,combat.EnemyHealth);
   string[] memories={"IMG_1040","IMG_1043","IMG_1054"};
   foreach(string memory in memories)
   {
    yield return Until(()=>combat.EnemyRuntime.CurrentMove is ProjectionAssaultMove p&&
     p.ProjectionSprite!=null&&p.ProjectionSprite.name.IndexOf(memory,StringComparison.OrdinalIgnoreCase)>=0,true,20);
    yield return null;
    Assert.AreEqual(2,combat.BoardView.GetComponentsInChildren<UnityEngine.UI.Image>(true)
     .Count(x=>x.name.StartsWith("MEMORY PROJECTION")));
    yield return Capture("150-memory-"+memory.Replace(" ","-"));
    combat.EnemyRuntime.Tick(combat.EnemyRuntime.CurrentMove.Duration+.1f);
    yield return null;
   }
   yield return Until(()=>combat.CurrentState==CombatController.State.Victory||
    !combat.IsPlaying,true,30);
   yield return Until(()=>Step(director)=="200_AudereChoosesToStand",true,25);
   Assert.IsFalse(combat.IsPlaying);
   Assert.IsFalse(combat.BoardView.HasForcedPlayerControl);
   Assert.IsFalse(combat.BoardView.GetComponentsInChildren<CombatBulletView>().Any());
   yield return Until(()=>Step(director)=="210_ThePathOpensOutward",true,8);
   yield return new WaitForSecondsRealtime(.75f);yield return Capture("150-path-opens");
   yield return Until(()=>Step(director)=="230_AudereStepsOntoThePath",true,15);
   var audere=All<SpriteRenderer>(s).Single(x=>x.name=="Audere");
   var shadow=audere.GetComponentsInChildren<SpriteRenderer>(true).Single(x=>x!=audere).transform;
   Vector3 shadowOffset=shadow.position-audere.transform.position;
   yield return Until(()=>Step(director)=="300_TimorMayComeAlong",true,15);
   Assert.Less(Vector3.Distance(audere.transform.position,
    All<Transform>(s).Single(x=>x.name=="Audere_Path_04").position),.001f);
   Assert.Less(Vector3.Distance(shadow.position-audere.transform.position,shadowOffset),.001f);
   yield return Until(()=>Step(director)=="350_HoldFinalImage",true,15);
   var final=All<CanvasGroup>(s).Single(x=>x.name=="FINAL CUTSCENE");
   Assert.IsTrue(final.gameObject.activeInHierarchy);Assert.Greater(final.alpha,.99f);
   Assert.Greater(final.GetComponent<Canvas>().sortingOrder,1000);
   yield return Capture("150-final-cutscene");
   yield return Until(()=>Step(director)=="390_ThankYou",true,15);
   var credits=All<CanvasGroup>(s).Single(x=>x.name=="CREDITS");
   Assert.IsTrue(credits.gameObject.activeInHierarchy);Assert.Greater(credits.alpha,.99f);
   yield return Capture("150-credits");
   yield return Until(()=>director.CurrentEvent==null,true,20);
   Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
   LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
  }


  [UnityTest]
  public IEnumerator Production_SearchSilenceShadowThenTimorCombatAndCancel()
  {
   EditorSceneManager.OpenScene(Day4TimorEveningSetupTool.ScenePath);yield return new EnterPlayMode();yield return null;yield return ProductionChecks();yield return new ExitPlayMode();
  }
  static IEnumerator ProductionChecks()
  {
   Services();
   var s=SceneManager.GetActiveScene();var director=All<StoryDirector>(s).Single();var a=All<SpriteRenderer>(s).Single(x=>x.name=="Audere");
   var position=a.transform.position;var shadow=a.GetComponentsInChildren<SpriteRenderer>().Single(x=>x!=a).transform;var shadowPosition=shadow.position;
   var mode=All<WorldModeController>(s).Single();var transition=All<FullscreenTransitionController>(s).Single();
   yield return Until(()=>Step(director)=="040_ItWorkedOut");yield return new WaitForSecondsRealtime(.85f);yield return Capture("150-alone");
   Assert.IsTrue(All<Transform>(s).Single(x=>x.name=="PuzzleViewportMask").gameObject.activeInHierarchy);
   yield return Until(()=>Step(director)=="080_SearchTheLeft",true);Assert.IsFalse(a.flipX);
   yield return Until(()=>Step(director)=="100_SearchTheRight");Assert.IsTrue(a.flipX);
   Assert.AreEqual(position,a.transform.position);Assert.AreEqual(shadowPosition,shadow.position);
   yield return Until(()=>Step(director)=="120_NoAnswer",true);double start=Time.realtimeSinceStartupAsDouble;
   Assert.IsFalse(GameplayUIRoot.Instance.Dialogue.IsPlaying);yield return Until(()=>Step(director)=="130_FromTheDark");
   Assert.GreaterOrEqual(Time.realtimeSinceStartupAsDouble-start,2.85);yield return new WaitForSecondsRealtime(.85f);yield return Capture("150-timor-reply");
   yield return Until(()=>transition.IsTransitioning,true);
   Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
   yield return new WaitForSecondsRealtime(1.1f);yield return Capture("150-shadow-early");
   yield return new WaitForSecondsRealtime(1.3f);yield return Capture("150-shadow-growing");
   yield return Until(()=>mode.CurrentMode==WorldGameplayMode.Combat);
   Assert.IsFalse(All<CombatController>(s).Single().IsPlaying);Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);yield return Capture("150-shadow-swap");
   var combat=All<CombatController>(s).Single();yield return Until(()=>combat.IsPlaying);yield return new WaitForSecondsRealtime(1.2f);yield return Capture("150-timor-combat");
   Assert.IsFalse(transition.IsTransitioning);Assert.IsFalse(transition.RendererFeature.isActive);Assert.AreEqual(AudioId.Music_TimorCombat,combat.CurrentEncounter.Music);
   Assert.Greater(combat.BoardView.GetComponentsInChildren<CombatDieView>().Length,0);
   director.CancelCurrentEvent();yield return null;Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);Assert.IsFalse(combat.IsPlaying);
   Assert.IsFalse(combat.BoardView.GetComponentsInChildren<CombatBulletView>().Any());LogAssert.NoUnexpectedReceived();
  }
  [UnityTest]
  public IEnumerator ShadowCancel_BeforeAndAfterSwapRestoresStoryAndCanReplay()
  {
   var s=EditorSceneManager.OpenScene(Day4TimorEveningSetupTool.ScenePath);var so=new SerializedObject(All<StoryDirector>(s).Single());so.FindProperty("playOnStart").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
   yield return new EnterPlayMode();yield return null;yield return CancellationChecks();yield return new ExitPlayMode();
  }
  static IEnumerator CancellationChecks()
  {
   Services();var s=SceneManager.GetActiveScene();
   var step=All<FullscreenWorldModeTransitionStep>(s).Single();
   foreach(float time in new[]{1.2f,4.5f})
   {
    Assert.IsTrue(step.Play());yield return new WaitForSecondsRealtime(time);
    Assert.AreEqual(time>4.2f?WorldGameplayMode.Combat:WorldGameplayMode.Story,step.WorldModeController.CurrentMode);
    step.Cancel();yield return null;Assert.AreEqual(WorldGameplayMode.Story,step.WorldModeController.CurrentMode);Assert.IsFalse(step.TransitionController.RendererFeature.isActive);
    Assert.IsNull(typeof(FullscreenTransitionController).GetField("runtimeMaterial",Private).GetValue(step.TransitionController));Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
    Assert.IsTrue(All<Transform>(s).Single(x=>x.name=="PuzzleViewportMask").gameObject.activeInHierarchy);
   }
   Assert.IsTrue(step.Play());yield return Until(()=>!step.IsRunning);Assert.AreEqual(WorldGameplayMode.Combat,step.WorldModeController.CurrentMode);
   LogAssert.NoUnexpectedReceived();
  }
  static string Step(StoryDirector d)=>d.CurrentEvent?.CurrentStep?.name;
  static IEnumerator Capture(string name){System.IO.Directory.CreateDirectory("Temp/Day4Timor");ScreenCapture.CaptureScreenshot("Temp/Day4Timor/"+name+".png");yield return new WaitForSecondsRealtime(.22f);}
static void Services()
  {
   Application.runInBackground=true;
   var gameViewType=Type.GetType("UnityEditor.GameView,UnityEditor");
   if(gameViewType!=null)EditorWindow.GetWindow(gameViewType).Focus();
   if(SceneFlow.Instance!=null)return;
   var go=new GameObject("TEST Timor Services");Object.DontDestroyOnLoad(go);
   go.AddComponent<SceneFlow>().Initialize();
   var audio=go.AddComponent<AudioService>();
   typeof(AudioService).GetField("catalog",Private).SetValue(audio,
    AssetDatabase.LoadAssetAtPath<AudioCatalog>("Assets/_Audere/Data/Audio/AudioCatalog.asset"));
   audio.Initialize();
  }
  static IEnumerator Until(Func<bool> condition,bool advance=false,float timeout=20)
  {
   double end=EditorApplication.timeSinceStartup+timeout;while(!condition()&&EditorApplication.timeSinceStartup<end)
   {var d=GameplayUIRoot.Instance?.Dialogue;if(advance&&d!=null&&d.IsPlaying)typeof(DialogueController).GetMethod("EndPlayback",Private).Invoke(d,new object[]{DialogueResult.Completed,true});EditorApplication.QueuePlayerLoopUpdate();yield return null;}
   Assert.IsTrue(condition(),"Timeout in Scene150");
  }
  static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<T>(true)).ToArray();
  [UnityTearDown]
  public IEnumerator Cleanup()
  {
   if(EditorApplication.isPlaying)yield return new ExitPlayMode();
   var scene=EditorSceneManager.OpenScene(Day4TimorEveningSetupTool.ScenePath);
   var director=All<StoryDirector>(scene).Single();
   var serialized=new SerializedObject(director);
   serialized.FindProperty("playOnStart").boolValue=true;
   serialized.ApplyModifiedPropertiesWithoutUndo();
   EditorSceneManager.SaveScene(scene);
  }
 }
}
#endif


