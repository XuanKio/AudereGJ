#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Audere.Combat.Editor.Tests
{
    public sealed class CombatDiceRandomSelectionTests
    {
        [Test]
        public void NormalRolls_AllThreeFacesHaveEqualChance()
        {
            var counts = new int[3];
            for (int i = 0; i < 3000; i++)
                counts[(int)CombatDiceConstants.RollSymbol((i + .5f) / 3000f)]++;
            CollectionAssert.AreEqual(new[] { 1000, 1000, 1000 }, counts);
            foreach (CombatSymbol symbol in new[] { CombatSymbol.Attack, CombatSymbol.Shield, CombatSymbol.Heal })
                Assert.AreEqual(1f / 3f, CombatDiceConstants.GetRollChance(symbol), .00001f);
            Assert.AreEqual(CombatSymbol.Attack, CombatDiceConstants.RollSymbol(0f));
            Assert.AreEqual(CombatSymbol.Heal, CombatDiceConstants.RollSymbol(1f));
        }

        [Test]
        public void LowTimeRolls_ReduceShieldToSixteenPercentAndRaiseBothOtherFaces()
        {
            var counts = new int[3];
            for (int i = 0; i < 10000; i++)
                counts[(int)CombatDiceConstants.RollSymbol((i + .5f) / 10000f, true)]++;
            CollectionAssert.AreEqual(new[] { 4200, 1600, 4200 }, counts);
            Assert.AreEqual(.16f, CombatDiceConstants.GetRollChance(CombatSymbol.Shield, true));
            Assert.AreEqual(.42f, CombatDiceConstants.GetRollChance(CombatSymbol.Attack, true));
            Assert.AreEqual(.42f, CombatDiceConstants.GetRollChance(CombatSymbol.Heal, true));
        }

        [Test]
        public void LowTimeRerolls_NeverKeepCurrentFaceOrExceedSixteenPercentShield()
        {
            foreach (CombatSymbol current in new[] { CombatSymbol.Attack, CombatSymbol.Shield, CombatSymbol.Heal })
            {
                var counts = new int[3];
                for (int i = 0; i < 10000; i++)
                {
                    CombatSymbol next = CombatDiceConstants.RerollSymbol(current, (i + .5f) / 10000f, true);
                    Assert.AreNotEqual(current, next);
                    counts[(int)next]++;
                }
                Assert.LessOrEqual(counts[(int)CombatSymbol.Shield], 1600);
                Assert.AreEqual(10000, counts.Sum());
            }
        }

        [Test]
        public void NormalThreeDice_AllTwentySevenCombinationsIncludingZeroOneAndThreeAttacksArePossible()
        {
            var outcomes = new HashSet<string>();
            var attackCounts = new HashSet<int>();
            for (int first = 0; first < 3; first++)
            for (int second = 0; second < 3; second++)
            for (int third = 0; third < 3; third++)
            {
                var dice = new[] { first, second, third }
                    .Select(face => CombatDiceConstants.RollSymbol((face + .5f) / 3f)).ToArray();
                outcomes.Add(string.Join(",", dice));
                attackCounts.Add(dice.Count(symbol => symbol == CombatSymbol.Attack));
            }
            Assert.AreEqual(27, outcomes.Count);
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, attackCounts);
        }

        [TestCase(CombatSymbol.Attack)]
        [TestCase(CombatSymbol.Shield)]
        [TestCase(CombatSymbol.Heal)]
        public void Reroll_AlwaysChangesFace_AndSplitsRemainingFacesEqually(CombatSymbol current)
        {
            var counts = new int[3];
            for (int i = 0; i < 1000; i++)
            {
                CombatSymbol next = CombatDiceConstants.RerollSymbol(current, (i + .5f) / 1000f);
                Assert.AreNotEqual(current, next);
                counts[(int)next]++;
            }
            Assert.AreEqual(0, counts[(int)current]);
            CollectionAssert.AreEquivalent(new[] { 0, 500, 500 }, counts);
            Assert.AreNotEqual(current, CombatDiceConstants.RerollSymbol(current, 0f));
            Assert.AreNotEqual(current, CombatDiceConstants.RerollSymbol(current, 1f));
        }

        [Test]
        public void ControllerSelection_IgnoresLegacyCapsAndLiveAttacks_AndNeverRerollsToCurrentFace()
        {
            var owner = new GameObject("Dice selection controller");
            owner.SetActive(false);
            var controller = owner.AddComponent<CombatController>();
            var dice = new List<CombatDieView>();
            Random.State randomState = Random.state;
            try
            {
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(CombatController).GetField("encounterData", flags).SetValue(controller,
                    AssetDatabase.LoadAssetAtPath<CombatEncounterData>(
                        "Assets/_Audere/Data/Combat/Teacher/CombatEncounter_D3_TEACHER_PRESSURE.asset"));
                typeof(CombatController).GetField("encounterTimeRemaining", flags)
                    .SetValue(controller, controller.ActiveMaximumTime);
                var active = (List<CombatDieView>)typeof(CombatController).GetField("activeDice", flags).GetValue(controller);
                for (int i = 0; i < 3; i++)
                {
                    var die = new GameObject("Existing Attack " + i, typeof(RectTransform)).AddComponent<CombatDieView>();
                    die.SetSymbol(CombatSymbol.Attack);
                    dice.Add(die); active.Add(die);
                }
                var batchRoll = typeof(CombatController).GetMethod("RollBatchSymbol", flags);
                var reroll = typeof(CombatController).GetMethod("RollRerollSymbol", flags);
                var observedAttackCounts = new HashSet<int>();
                for (int seed = 0; seed < 120; seed++)
                {
                    Random.InitState(seed);
                    int count = 0;
                    for (int i = 0; i < 3; i++)
                        if ((CombatSymbol)batchRoll.Invoke(controller, null) == CombatSymbol.Attack) count++;
                    observedAttackCounts.Add(count);
                }
                CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, observedAttackCounts);
                foreach (CombatSymbol current in new[] { CombatSymbol.Attack, CombatSymbol.Shield, CombatSymbol.Heal })
                {
                    dice[0].SetSymbol(current);
                    var observed = new HashSet<CombatSymbol>();
                    for (int i = 0; i < 100; i++)
                    {
                        var next = (CombatSymbol)reroll.Invoke(controller, new object[] { dice[0] });
                        Assert.AreNotEqual(current, next);
                        observed.Add(next);
                    }
                    Assert.AreEqual(2, observed.Count);
                }
            }
            finally
            {
                Random.state = randomState;
                Object.DestroyImmediate(owner);
                foreach (var die in dice) Object.DestroyImmediate(die.gameObject);
            }
        }

        [Test]
        public void LowTimeAssist_OnlyStartsBelowThirtyPercentOnEasy()
        {
            var owner = new GameObject("Low TIME dice policy");
            owner.SetActive(false);
            var controller = owner.AddComponent<CombatController>();
            try
            {
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(CombatController).GetField("encounterData", flags).SetValue(controller,
                    AssetDatabase.LoadAssetAtPath<CombatEncounterData>(
                        "Assets/_Audere/Data/Combat/Teacher/CombatEncounter_D3_TEACHER_PRESSURE.asset"));
                var difficulty = typeof(CombatController).GetField("activeDifficulty", flags);
                var remaining = typeof(CombatController).GetField("encounterTimeRemaining", flags);
                var assist = typeof(CombatController).GetMethod("UseLowTimeDiceAssist", flags);
                difficulty.SetValue(controller, Audere.Core.GameDifficulty.Easy);
                float maximum = controller.ActiveMaximumTime;
                remaining.SetValue(controller, maximum * .30f);
                Assert.IsFalse((bool)assist.Invoke(controller, null));
                remaining.SetValue(controller, maximum * .29f);
                Assert.IsTrue((bool)assist.Invoke(controller, null));
                difficulty.SetValue(controller, Audere.Core.GameDifficulty.Hard);
                remaining.SetValue(controller, 0f);
                Assert.IsFalse((bool)assist.Invoke(controller, null));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void ScriptedSpawnsAndTutorialOpeningKeepTheirAuthoredFaces()
        {
            var board = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>(
                "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            try
            {
                board.gameObject.SetActive(true);
                board.PrepareEncounter("Scripted dice test");
                var tutorial = AssetDatabase.LoadAssetAtPath<CombatTutorialData>(
                    "Assets/_Audere/Data/Combat/Tutorials/CombatTutorial_D1_CLASSROOM.asset");
                CollectionAssert.AreEqual(new[] { CombatSymbol.Attack, CombatSymbol.Shield, CombatSymbol.Heal }, tutorial.OpeningDice);
                foreach (CombatSymbol symbol in tutorial.OpeningDice)
                    Assert.AreEqual(symbol, board.SpawnDie(symbol, 0f).Symbol);
                for (int i = 0; i < 3; i++)
                {
                    var forcedHeal = new CombatScriptedDieSpawn(CombatSymbol.Heal,
                        new Vector2(.25f + i * .25f, .91f), Vector2.down);
                    Assert.AreEqual(CombatSymbol.Heal, board.SpawnDie(forcedHeal, 30f).Symbol);
                }
            }
            finally { Object.DestroyImmediate(board.gameObject); }
        }
    }
}
#endif
