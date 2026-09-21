#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Audere.Audio;
using Audere.Core;
using Audere.Dialogue;
using Audere.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Audere.Combat.Editor
{
    // Explicitly started diagnostic; never runs automatically and never saves a scene.
    public static class TimorFinalQAProbe
    {
        private const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        private const string Folder="Temp/TimorPolishQA/";
        private static CombatController combat;
        private static double deadline;
        public static string Status {get;private set;}="idle";
        public static void Start()
        {
            if(!Application.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            var scene=SceneManager.GetActiveScene();
            if(scene.name!="150_D4_Home_Evening")throw new InvalidOperationException("Open Scene150 first.");
            combat=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<CombatController>(true)).Single();
            Application.runInBackground=true;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            if(SceneFlow.Instance==null)
            {
                var go=new GameObject("Timor QA temporary services");UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<SceneFlow>().Initialize();
                if(AudioService.Instance==null)
                {
                    var audio=go.AddComponent<AudioService>();typeof(AudioService).GetField("catalog",Private).SetValue(audio,
                        AssetDatabase.LoadAssetAtPath<AudioCatalog>("Assets/_Audere/Data/Audio/AudioCatalog.asset"));audio.Initialize();
                }
            }
            System.IO.Directory.CreateDirectory(Folder);deadline=EditorApplication.timeSinceStartup+240;
            EditorApplication.isPaused=false;
            SceneFlow.Instance.StartCoroutine(Guarded(Run()));
        }
        private static IEnumerator Guarded(IEnumerator root)
        {
            var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(root);
            while(stack.Count>0)
            {
                object yielded=null;bool advanced=false;Exception error=null;
                try{advanced=stack.Peek().MoveNext();if(advanced)yielded=stack.Peek().Current;}
                catch(Exception e){error=e;}
                if(error!=null){Status="FAIL "+error;System.IO.File.WriteAllText(Folder+"play-result.txt",Status);yield break;}
                if(!advanced){stack.Pop();continue;}
                if(yielded is IEnumerator nested){stack.Push(nested);continue;}
                yield return yielded;
            }
            Status="PASS production opening, all 3 phases, Heal recovery, checkpoint and Victory handoff. Debug damage/TIME used; not a human balance test.";
            System.IO.File.WriteAllText(Folder+"play-result.txt",Status);
        }
        private static IEnumerator Run()
        {
            Status="opening";
            yield return Until(()=>combat.EnemyRuntime!=null&&combat.CurrentState==CombatController.State.Playing);
            Damage();yield return Recovery();
            Check(combat.RetryCheckpointPhaseIndex==1,"Phase2 checkpoint");
            Status="dice break";yield return Until(()=>combat.EnemyRuntime.CurrentMove is TargetedDiceBreakMove);
            yield return Delay(2.2f);yield return Capture("dice-break");
            Status="orbit clones";yield return Until(()=>combat.EnemyRuntime.CurrentMove is OrbitingCloneVolleyMove);
            yield return Delay(3.1f);yield return Capture("orbit-clones");Damage();
            Status="pressure waves";yield return Until(()=>combat.EnemyRuntime.CurrentMove is TimorPressureWaveMove);
            yield return Delay(3f);yield return Capture("pressure-waves");
            yield return Recovery();Check(combat.RetryCheckpointPhaseIndex==2,"Phase3 checkpoint");
            Status="corridor";Damage();yield return Until(()=>combat.EnemyRuntime.CurrentMove is ScrollingWordCorridorMove);
            yield return Delay(4.1f);yield return Capture("word-corridor");
            Status="peak";yield return Until(()=>combat.EnemyRuntime.CurrentMove!=null&&combat.EnemyRuntime.CurrentMove.name=="Move_TimorPeakClones");
            yield return Delay(3.2f);yield return Capture("peak-clones");
            Status="support";yield return Until(()=>combat.EnemyRuntime.CurrentMove!=null&&combat.EnemyRuntime.CurrentMove.name=="Move_TimorSupportedWaves");
            yield return Delay(6.5f);yield return Capture("supported-waves");
            Status="victory";yield return Until(()=>combat.CurrentState==CombatController.State.Victory||!combat.IsPlaying);
            yield return Until(()=>!combat.IsPlaying);
            Check(!combat.BoardView.HasForcedPlayerControl,"Control released at Victory");
            Check(!combat.BoardView.GetComponentsInChildren<CombatBulletView>().Any(x=>x.CollisionActive),"No active Victory hazard");
            yield return Delay(1.5f);yield return Capture("victory-handoff");
        }
        private static IEnumerator Recovery()
        {
            yield return Until(()=>combat.IsRecoveringPlayerTime);
            float value=combat.PlayerTime;yield return new WaitForSecondsRealtime(.2f);
            Check(Mathf.Abs(value-combat.PlayerTime)<.02f,"TIME paused during recovery");
            while(combat.IsRecoveringPlayerTime)
            {
                var dice=((IList)typeof(CombatController).GetField("activeDice",Private).GetValue(combat)).Cast<CombatDieView>();
                var die=dice.FirstOrDefault(x=>x!=null&&x.CanInteract);
                if(die!=null)typeof(CombatController).GetMethod("CatchDie",Private).Invoke(combat,new object[]{die});
                yield return null;
            }
            yield return Until(()=>combat.CurrentState==CombatController.State.Playing);
        }
        private static void Damage()=>typeof(CombatController).GetMethod("ApplyEnemyDamage",Private).Invoke(combat,new object[]{999});
        private static void Advance()
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException(Status);
            var dialogue=GameplayUIRoot.Instance?.Dialogue;
            if(dialogue!=null&&dialogue.IsPlaying)typeof(DialogueController).GetMethod("EndPlayback",Private).Invoke(dialogue,new object[]{DialogueResult.Completed,true});
            if(combat.CurrentState==CombatController.State.Playing&&!combat.IsRecoveringPlayerTime)
                typeof(CombatController).GetField("encounterTimeRemaining",Private).SetValue(combat,combat.ActiveMaximumTime);
        }
        private static IEnumerator Until(Func<bool> condition){while(!condition()){Advance();yield return null;}}
        private static IEnumerator Delay(float duration){float end=Time.unscaledTime+duration;while(Time.unscaledTime<end){Advance();yield return null;}}
        private static IEnumerator Capture(string name){ScreenCapture.CaptureScreenshot(Folder+name+".png");yield return new WaitForSecondsRealtime(.3f);}
        private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    }
}
#endif
