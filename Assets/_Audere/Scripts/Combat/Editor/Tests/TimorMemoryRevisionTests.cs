#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Audere.Combat.Editor.Tests
{
    public sealed class TimorMemoryRevisionTests
    {
        private CombatBoardView board;
        private CombatEnemyActor actor;
        private ICombatMoveExecution execution;
        private CombatEnemyDefinition enemy;
        private CombatEncounterData encounter;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [SetUp] public void Setup()
        {
            board = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>("Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            board.gameObject.SetActive(true); board.PrepareEncounter("Timor QA"); board.ResetPlayer();
            enemy = AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>(TimorFinalPolishAuthoring.Root + "Enemy_TimorReturn.asset");
            encounter = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatEncounterData>(TimorFinalPolishAuthoring.Root + "CombatEncounter_D4_TIMOR_RETURN.asset"));
            actor = board.SpawnEnemyActor(enemy.ActorPrefab, 991, true);
            Canvas.ForceUpdateCanvases();
        }
        [TearDown] public void Cleanup()
        { execution?.Cancel(); execution = null; if(board != null)Object.DestroyImmediate(board.gameObject);Object.DestroyImmediate(encounter); }
        private CombatMoveDefinition Move(string name) => AssetDatabase.LoadAssetAtPath<CombatMoveDefinition>(TimorFinalPolishAuthoring.Moves + "Move_Timor" + name + ".asset");
        private void Start(CombatMoveDefinition move) => execution = move.CreateExecution(new CombatMoveExecutionContext(board,actor,new SystemCombatRandom(37),991,3));
        private void Tick(float dt) { execution.Tick(dt); board.TickHeartFeedback(dt); board.TickBullets(dt,.85f); }
        [Test] public void CorridorOnlyInPhaseTwo_AndSupportFollowsThreeDefeatedMemories()
        {
            for(int p=0;p<3;p++)
            {
                var moves=enemy.GetPhase(p).MoveSet.Entries.Select(x=>x.Move).ToArray();
                Assert.AreEqual(p==1?1:0,moves.Count(m=>m is ScrollingWordCorridorMove));
                Assert.IsFalse(moves.Any(m=>m is TargetedDiceBreakMove));
            }
            var memories=enemy.GetPhase(2).MoveSet.Entries.Select(x=>x.Move).OfType<DefeatableProjectionMove>().ToArray();
            Assert.AreEqual(3,memories.Length);
            foreach(var memory in memories)
            {
                Assert.AreEqual(3,memory.AttacksToDefeat);
                var cue=enemy.GetPhase(2).DialogueCues.Single(c=>c.TriggerMove==memory);
                Assert.AreEqual(CombatDialogueCueTrigger.MoveCompleted,cue.Trigger);Assert.IsTrue(cue.RequiredBeforeVictory);
                foreach(var signature in memory.SignatureMoves)Assert.IsTrue(AssetDatabase.Contains(signature));
            }
        }
        [TestCase("MemoryTeacher")]
        [TestCase("MemoryBianca")]
        [TestCase("MemoryCrowd")]
        public void MemoryNeedsThreeCounters_AndNeverCutsItsSignatureShort(string name)
        {
            var move=(DefeatableProjectionMove)Move(name);Start(move);
            float fullDuration=move.SignatureMoves.Sum(m=>m.Duration);
            int damage=0;float age=0;
            while(!execution.IsComplete && age<fullDuration+8f)
            {
                Tick(.02f); age+=.02f;
                var die=board.GetComponentsInChildren<CombatDieView>().FirstOrDefault(d=>d.CanInteract);
                if(die!=null)
                {
                    board.CatchCursor.position=die.RectTransform.position;
                    ((ICombatMoveInputHandler)execution).HandleInput(true,false);
                    damage+=((ICombatMoveDamageReward)execution).ConsumePendingDamage();
                }
                if(age<fullDuration)Assert.IsFalse(execution.IsComplete,"A hit must not truncate the authored signature.");
            }
            Assert.IsTrue(execution.IsComplete,name);Assert.AreEqual(3*CombatDiceConstants.AttackDamage,damage);
            Assert.AreEqual(0,((ICombatMoveDamageReward)execution).ConsumePendingDamage());
        }
        [Test] public void MemoryCannotExpireWithoutCounters_AndCancelRestoresActor()
        {
            var move=Move("MemoryTeacher");Start(move);
            for(float t=0;t<move.Duration+3;t+=.05f)Tick(.05f);
            Assert.IsFalse(execution.IsComplete);
            var positions=board.GetComponentsInChildren<RectTransform>().ToDictionary(x=>x,x=>x.anchoredPosition);
            execution.Tick(0);
            foreach(var p in positions)Assert.AreEqual(p.Value,p.Key.anchoredPosition);
            execution.Cancel();execution.Cancel();Assert.IsTrue(actor.VisualRoot.gameObject.activeSelf);
            Assert.IsFalse(board.GetComponentsInChildren<CombatDieView>().Any(d=>d.CanInteract));
            Assert.IsFalse(board.IsMountDiveActive);
        }
        [Test] public void CloneVolleyEmitsTwoFiveBulletFansBeforePassingToNextShooter()
        {
            Start(Move("ClockwiseClones"));for(float t=0;t<1.58f;t+=.01f)Tick(.01f);
            var bullets=board.GetComponentsInChildren<CombatBulletView>().Where(b=>b.gameObject.activeInHierarchy).ToArray();
            Assert.AreEqual(10,bullets.Length);
            foreach(var b in bullets)Assert.AreEqual(new Vector2(24,24),b.RectTransform.rect.size);
        }
        [Test] public void SabotageChanceIsThirtyPercent_WithNoEffectBeforeTheCatch()
        {
            int broken=Enumerable.Range(0,10000).Count(i=>ShouldBreak((i+.5f)/10000f,.3f));
            Assert.AreEqual(3000,broken);Assert.IsFalse(ShouldBreak(.3f,.3f));
            var controller=PrepareController(CombatController.State.Playing);
            var dice=Get<List<CombatDieView>>(controller,"activeDice");
            var die=board.SpawnChoiceDie(CombatSymbol.Heal,new Vector2(.5f,.5f));dice.Add(die);
            Assert.IsTrue(die.CanInteract);float before=controller.PlayerTime;
            Call(controller,"CatchDie",die);
            Assert.IsEmpty(dice);Assert.AreEqual(before,controller.PlayerTime);
            Assert.IsTrue(board.GetComponentsInChildren<Transform>().Any(t=>t.name=="Dice catch interception"));
            Call(controller,"StopBatchAndClearDice");
            Assert.IsFalse(board.GetComponentsInChildren<Transform>().Any(t=>t.name=="Dice catch interception"));
        }
        [Test] public void RecoveryQueuesEachHealSmoothly_ProtectsDice_AndClampsAtFull()
        {
            var controller=PrepareController(CombatController.State.PhaseTransition);
            Set(controller,"isRecoveringPlayerTime",true);
            float maximum=controller.ActiveMaximumTime;Set(controller,"encounterTimeRemaining",maximum-9);
            var dice=Get<List<CombatDieView>>(controller,"activeDice");
            for(int i=0;i<2;i++){var die=board.SpawnChoiceDie(CombatSymbol.Heal,new Vector2(.5f,.5f));dice.Add(die);Call(controller,"CatchDie",die);}
            Assert.AreEqual(maximum-9,controller.PlayerTime,"No snap on catch.");
            Call(controller,"TickRecoveryHealing",0f);Assert.AreEqual(maximum-9,controller.PlayerTime);
            Call(controller,"TickRecoveryHealing",.12f);Assert.AreEqual(maximum-6,controller.PlayerTime,.02f);
            Call(controller,"TickRecoveryHealing",1f);Assert.AreEqual(maximum,controller.PlayerTime,.01f);
            Assert.IsFalse(board.GetComponentsInChildren<Transform>().Any(t=>t.name=="Dice catch interception"));
        }
        private static bool ShouldBreak(float roll,float chance) => (bool)typeof(CombatController).Assembly.GetType("Audere.Combat.DiceCatchSabotage").GetMethod("ShouldBreak").Invoke(null,new object[]{roll,chance});
        private CombatController PrepareController(CombatController.State state)
        {
            var controller=board.gameObject.AddComponent<CombatController>();
            Set(controller,"boardView",board);Set(controller,"encounterData",encounter);
            var so=new SerializedObject(encounter);so.FindProperty("diceBreakChance").floatValue=1;so.ApplyModifiedPropertiesWithoutUndo();
            typeof(CombatController).GetProperty("CurrentState").SetValue(controller,state);
            Set(controller,"encounterTimeRemaining",controller.ActiveMaximumTime*.5f);return controller;
        }
        private static T Get<T>(object target,string name)=>(T)target.GetType().GetField(name,Private).GetValue(target);
        private static void Set(object target,string name,object value)=>target.GetType().GetField(name,Private).SetValue(target,value);
        private static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Private).Invoke(target,args);
    }

    public sealed class TimorPhaseRecoveryTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private CombatBoardView board;
        private CombatController controller;
        private CombatEncounterData encounter;
        private CombatEnemyRuntime runtime;

        [SetUp]
        public void SetUp()
        {
            board = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>(
                "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            board.gameObject.SetActive(true);
            board.PrepareEncounter("Recovery QA");
            board.ResetPlayer();
            encounter = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatEncounterData>(
                TimorFinalPolishAuthoring.Root + "CombatEncounter_D4_TIMOR_RETURN.asset"));
            var mount = (Transform)new SerializedObject(board).FindProperty("enemyMount").objectReferenceValue;
            board.BindAuthoredEnemyActor(Object.Instantiate(encounter.EnemyDefinition.ActorPrefab, mount));
            controller = board.gameObject.AddComponent<CombatController>();
            Set("boardView", board);
            Set("encounterData", encounter);
            typeof(CombatController).GetProperty("CurrentState").SetValue(controller, CombatController.State.PhaseTransition);
        }

        [TearDown]
        public void TearDown()
        {
            runtime?.Cancel();
            if (board != null) Object.DestroyImmediate(board.gameObject);
            Object.DestroyImmediate(encounter);
        }

        private void Begin(int phase = 0, float multiplier = 1f, bool playerFull = false)
        {
            Assert.IsTrue(encounter.PhaseRecovery.HealEnemyOnDrop, "Production Timor must author drop-based recovery.");
            runtime = new CombatEnemyRuntime(encounter.EnemyDefinition, board, new SystemCombatRandom(42), 993, true, multiplier);
            runtime.Start(phase);
            foreach (var cue in runtime.CurrentPhase.DialogueCues) runtime.MarkCueResolved(cue);
            Assert.AreEqual(CombatEnemyProgression.PhaseBreak, runtime.ApplyDamage(999, out _));
            Set("enemyRuntime", runtime);
            Set("encounterTimeRemaining", playerFull ? controller.ActiveMaximumTime : 10f);
            Call("BeginPhaseRecovery");
        }

        [TestCase(0, 1f, false)]
        [TestCase(0, 1f, true)]
        [TestCase(1, 1f, false)]
        [TestCase(1, 1f, true)]
        [TestCase(0, 1.36f, false)]
        [TestCase(0, 1.36f, true)]
        [TestCase(1, 1.36f, false)]
        [TestCase(1, 1.36f, true)]
        public void DroppingHealsEnemyAndCompletesWithoutCatches(int phase, float multiplier, bool playerFull)
        {
            Begin(phase, multiplier, playerFull);
            int target = Mathf.CeilToInt(encounter.EnemyDefinition.GetPhase(phase + 1).MaxHealth * multiplier);
            float playerTime = controller.PlayerTime;
            Assert.AreEqual(0, controller.EnemyHealth);
            Assert.AreEqual(target, runtime.CurrentMaxHealth);
            Assert.IsFalse(runtime.AcceptsDamage);
            Call("TickPhaseRecovery", 0f);
            Assert.AreEqual(0, controller.EnemyHealth, "A paused recovery must not spawn or heal.");
            runtime.CompletePhaseBreak();
            Assert.AreEqual(phase, runtime.PhaseIndex, "Cannot advance before the refill completes.");
            bool sawFullWithDice = false;
            for (int tick = 0; tick < 600 && !(bool)Call("IsPhaseRecoveryComplete"); tick++)
            {
                Call("TickPhaseRecovery", .05f);
                int drops = Get<int>("recoveryDropIndex");
                Assert.AreEqual(Mathf.Min(target, drops * encounter.PhaseRecovery.EnemyHealthPerDie), controller.EnemyHealth);
                Assert.LessOrEqual(Dice.Count, encounter.PhaseRecovery.MaximumActiveDice);
                Assert.AreEqual(playerTime, controller.PlayerTime, "An uncaught die never heals Audere.");
                if (controller.EnemyHealth == target && Dice.Count > 0)
                {
                    sawFullWithDice = true;
                    Assert.IsFalse((bool)Call("IsPhaseRecoveryComplete"), "The last dice stay catchable until they expire.");
                }
            }
            Assert.IsTrue(sawFullWithDice);
            Assert.IsTrue((bool)Call("IsPhaseRecoveryComplete"), "No input must still finish recovery within 30 simulated seconds.");
            Assert.AreEqual(target, Get<int>("recoveryDropIndex"));
            int finalDrops = Get<int>("recoveryDropIndex");
            Call("TickPhaseRecovery", 5f);
            Assert.AreEqual(finalDrops, Get<int>("recoveryDropIndex"), "Full enemy HP stops new drops.");
            runtime.CompletePhaseBreak();
            Assert.AreEqual(phase + 1, runtime.PhaseIndex);
            Assert.AreEqual(target, runtime.CurrentHealth);
            Assert.AreEqual(target, runtime.CurrentMaxHealth);
            Assert.IsFalse(runtime.IsRecoveringPhaseHealth);
            Assert.AreEqual(CombatEnemyRuntimeState.Playing, runtime.State);
        }

        [Test]
        public void CatchHealsOnlyPlayerAndSmoothlyClampsAtFull()
        {
            Begin();
            Set("encounterTimeRemaining", controller.ActiveMaximumTime - 2f);
            Call("TickPhaseRecovery", .01f);
            var die = Dice[0];
            die.SetupStationaryChoice(CombatSymbol.Heal, die.RectTransform.anchoredPosition);
            int enemyHealth = controller.EnemyHealth;
            float before = controller.PlayerTime;
            Call("CatchDie", die);
            Assert.AreEqual(enemyHealth, controller.EnemyHealth, "The same die must not heal Timor twice.");
            Assert.AreEqual(before, controller.PlayerTime);
            Call("TickRecoveryHealing", .04f);
            Assert.Greater(controller.PlayerTime, before);
            Assert.Less(controller.PlayerTime, controller.ActiveMaximumTime);
            Call("TickRecoveryHealing", 1f);
            Assert.AreEqual(controller.ActiveMaximumTime, controller.PlayerTime);
            Assert.AreEqual(enemyHealth, controller.EnemyHealth);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CancellationDuringRefillOrFinalDiceClearsPendingWork(bool afterFull)
        {
            Begin();
            Call("TickPhaseRecovery", .01f);
            if (afterFull)
                for (int i = 0; i < 400 && controller.EnemyHealth < runtime.CurrentMaxHealth; i++)
                    Call("TickPhaseRecovery", .05f);
            Call("StopEncounterRuntime", true);
            Assert.IsEmpty(Dice);
            Assert.IsEmpty(Get<Dictionary<CombatDieView, float>>("recoveryDiceAges"));
            Assert.IsFalse(controller.IsRecoveringPlayerTime);
            Assert.IsFalse(runtime.IsRecoveringPhaseHealth);
            Assert.AreEqual(CombatEnemyRuntimeState.Cancelled, runtime.State);
            Assert.AreEqual(0, runtime.HealNextPhaseHealth(5));
            Call("TickPhaseRecovery", 10f);
            Assert.IsEmpty(Dice);
            Assert.AreEqual(0, Get<int>("recoveryDropIndex"));
        }

        private List<CombatDieView> Dice => Get<List<CombatDieView>>("activeDice");
        private T Get<T>(string name) => (T)typeof(CombatController).GetField(name, Private).GetValue(controller);
        private void Set(string name, object value) => typeof(CombatController).GetField(name, Private).SetValue(controller, value);
        private object Call(string name, params object[] args) => typeof(CombatController).GetMethod(name, Private).Invoke(controller, args);
    }
}
#endif
