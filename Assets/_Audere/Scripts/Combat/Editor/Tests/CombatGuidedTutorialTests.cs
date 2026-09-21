#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Audere.Combat.Editor.Tests
{
    public sealed class CombatGuidedTutorialTests
    {
        private const string TutorialPath = "Assets/_Audere/Data/Combat/Tutorials/CombatTutorial_D1_CLASSROOM.asset";
        private CombatBoardView board;
        private CombatController controller;
        private CombatTutorialData tutorial;
        private CombatEnemyActor actor;
        private RectTransform frame;
        private Vector2 fieldSize, frameSize;
        private Vector3 fieldPosition, framePosition, actorScale;

        [SetUp]
        public void SetUp()
        {
            tutorial = AssetDatabase.LoadAssetAtPath<CombatTutorialData>(TutorialPath);
            Assert.IsNotNull(tutorial);
            Assert.IsTrue(tutorial.UseGuidedLessons, "Run scoped D1 guided tutorial authoring before these acceptance tests.");
            board = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>(
                "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            board.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            board.PrepareEncounter("Guided tutorial test");
            var serialized = new SerializedObject(board);
            var mount = (Transform)serialized.FindProperty("enemyMount").objectReferenceValue;
            actor = Object.Instantiate(tutorial.EnemyDefinition.ActorPrefab, mount);
            board.BindAuthoredEnemyActor(actor);
            board.SpawnEnemyActor(tutorial.EnemyDefinition.ActorPrefab, 470);
            frame = (RectTransform)serialized.FindProperty("battleBoxFrame").objectReferenceValue;
            fieldSize = board.PlayArea.sizeDelta; fieldPosition = board.PlayArea.anchoredPosition3D;
            frameSize = frame.sizeDelta; framePosition = frame.anchoredPosition3D;
            actorScale = actor.transform.localScale;
            GameObject owner = new GameObject("Guided controller test");
            owner.SetActive(false);
            controller = owner.AddComponent<CombatController>();
            Set(controller, "boardView", board);
            Set(controller, "encounterData", AssetDatabase.LoadAssetAtPath<CombatEncounterData>(
                "Assets/_Audere/Data/Combat/CombatEncounter_D1_CLASSROOM_KHOANG_LANG.asset"));
            owner.SetActive(true);
            Set(controller, "tutorialActive", true);
            Set(controller, "guidedTutorialRunning", true);
            Set(controller, "encounterTimeRemaining", 20f);
            Set(controller, "enemyRuntime", new CombatEnemyRuntime(tutorial.EnemyDefinition, board,
                new SystemCombatRandom(47), 470));
            board.BeginGuidedTutorialPresentation(tutorial.SquareBoardSize);
        }

        [TearDown]
        public void TearDown()
        {
            if (controller != null) Object.DestroyImmediate(controller.gameObject);
            if (board != null) Object.DestroyImmediate(board.gameObject);
        }

        [Test]
        public void AuthoredSequenceTeachesTimeAndDamageBeforeHealWithExplicitHandOff()
        {
            Assert.IsTrue(tutorial.Validate(out string error), error);
            CollectionAssert.AreEqual(new[] {
                CombatTutorialLessonKind.Move, CombatTutorialLessonKind.Time,
                CombatTutorialLessonKind.Damage, CombatTutorialLessonKind.Heal,
                CombatTutorialLessonKind.Attack, CombatTutorialLessonKind.Reroll,
                CombatTutorialLessonKind.Shield, CombatTutorialLessonKind.Dodge,
                CombatTutorialLessonKind.StunCatch, CombatTutorialLessonKind.StunReroll,
                CombatTutorialLessonKind.Finish }, tutorial.GuidedLessons.Select(lesson => lesson.Kind));
            Assert.AreEqual(30f, tutorial.PlayerTime);
            Assert.AreEqual(6, controller.CurrentEncounter.EnemyDefinition.GetPhase(0).MaxHealth);
            Assert.AreNotSame(tutorial.EnemyDefinition, controller.CurrentEncounter.EnemyDefinition);
            Assert.AreEqual(11, tutorial.GuidedLessons.Select(lesson => lesson.Id).Distinct().Count());
            foreach (var lesson in tutorial.GuidedLessons)
            {
                Assert.IsNotNull(lesson.Dialogue, lesson.Id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(lesson.Instruction), lesson.Id);
            }
        }

        [Test]
        public void GuidedDataRejectsUnknownLessonKindAndMissingFinish()
        {
            var copy = Object.Instantiate(tutorial);
            try
            {
                Set(copy.GuidedLessons[0], "kind", (CombatTutorialLessonKind)999);
                Assert.IsFalse(copy.Validate(out _));
                Set(copy.GuidedLessons[0], "kind", CombatTutorialLessonKind.Move);
                Set(copy.GuidedLessons[copy.GuidedLessons.Count - 1], "kind", CombatTutorialLessonKind.Move);
                Assert.IsFalse(copy.Validate(out _));
            }
            finally { Object.DestroyImmediate(copy); }
        }

        [Test]
        public void SquareFieldAndFrameRestoreWithoutChangingActorScale()
        {
            Assert.AreEqual(tutorial.SquareBoardSize, board.PlayArea.rect.width, .01f);
            Assert.Greater(board.PlayArea.rect.height, 0f);
            Assert.Less(board.PlayArea.rect.height, board.PlayArea.rect.width,
                "The square outer board reserves its bottom gutter for TIME.");
            Assert.AreEqual(frame.rect.width, frame.rect.height, .01f);
            Assert.AreEqual(frame.TransformPoint(frame.rect.center).x,
                board.PlayArea.TransformPoint(board.PlayArea.rect.center).x, .001f);
            Assert.AreEqual(actorScale, actor.transform.localScale);
            board.EndGuidedTutorialPresentation();
            board.EndGuidedTutorialPresentation();
            AssertRestored();
        }

        [Test]
        public void LessonsRevealTimeBeforeCatchAndEnemyHealthOnlyAtAttack()
        {
            var serialized = new SerializedObject(board);
            var health = (Slider)serialized.FindProperty("enemyHealthSlider").objectReferenceValue;
            Assert.IsNotNull(health);
            foreach (CombatTutorialLessonKind kind in Enum.GetValues(typeof(CombatTutorialLessonKind)))
            {
                Prepare(kind);
                Assert.AreEqual(kind >= CombatTutorialLessonKind.Time, board.TimerFocusTarget.gameObject.activeSelf, kind.ToString());
                Assert.AreEqual(kind >= CombatTutorialLessonKind.Attack, health.gameObject.activeSelf, kind.ToString());
                Assert.AreEqual(actorScale, actor.transform.localScale);
            }
        }

        [Test]
        public void HealRequiresOverlappingLeftClickAndAppliesItsActualTimeEffectOnce()
        {
            Prepare(CombatTutorialLessonKind.Heal);
            var die = Die;
            board.CatchCursor.anchoredPosition = new Vector2(140f, 140f);
            Click(true, false);
            Assert.IsFalse(Complete); Assert.AreSame(die, Die); Assert.AreEqual(20f, controller.PlayerTime);
            board.CatchCursor.anchoredPosition = die.RectTransform.anchoredPosition;
            Assert.IsTrue(board.CursorOverlaps(die));
            Click(false, true);
            Assert.IsFalse(Complete); Assert.AreSame(die, Die); Assert.IsTrue(die.CanInteract);
            Click(true, false);
            Assert.IsTrue(Complete); Assert.IsNull(Die);
            Assert.AreEqual(20f + CombatDiceConstants.HealTimeSeconds, controller.PlayerTime);
            Click(true, false);
            Assert.AreEqual(20f + CombatDiceConstants.HealTimeSeconds, controller.PlayerTime);
        }

        [Test]
        public void RerollRequiresOverlapAndCannotAdvanceOrBeCaughtWhileAirborne()
        {
            Prepare(CombatTutorialLessonKind.Reroll);
            var original = Die;
            board.CatchCursor.anchoredPosition = new Vector2(140f, 140f);
            Click(false, true);
            Assert.AreSame(original, Die); Assert.IsFalse(Get<bool>(controller, "guidedRerolled"));
            board.CatchCursor.anchoredPosition = original.RectTransform.anchoredPosition;
            Click(true, false);
            Assert.AreSame(original, Die); Assert.IsFalse(Complete);
            Click(false, true);
            Assert.IsTrue(Get<bool>(controller, "guidedRerolled"));
            Assert.AreEqual(CombatSymbol.Shield, Die.Symbol);
            Assert.IsFalse(Die.CanInteract); Assert.IsFalse(Complete);
            var airborne = Die;
            Click(true, false);
            Click(false, true);
            Assert.AreSame(airborne, Die); Assert.IsFalse(Complete);
        }

        [Test]
        public void StunBlocksCatchWithoutConsumingDieThenAllowsRequiredReroll()
        {
            Prepare(CombatTutorialLessonKind.StunCatch);
            var original = Die;
            board.CatchCursor.anchoredPosition = original.RectTransform.anchoredPosition;
            RefreshStun();
            Assert.IsTrue(board.IsCursorStunned); Assert.IsTrue(board.CursorOverlaps(original));
            Click(false, true);
            Assert.IsFalse(Complete); Assert.AreSame(original, Die);
            Click(true, false);
            Assert.IsTrue(Complete); Assert.AreSame(original, Die); Assert.IsTrue(original.CanInteract);
            Prepare(CombatTutorialLessonKind.StunReroll);
            Assert.AreSame(original, Die);
            RefreshStun();
            Click(true, false);
            Assert.IsFalse(Complete); Assert.AreSame(original, Die);
            Click(false, true);
            Assert.IsTrue(Get<bool>(controller, "guidedRerolled"));
            Assert.AreEqual(CombatSymbol.Shield, Die.Symbol); Assert.IsFalse(Die.CanInteract);
            Assert.IsFalse(Complete);
        }

        [Test]
        public void StunReroll_AuthorsShieldFirst_ThenAlwaysChangesAnExistingShield()
        {
            Prepare(CombatTutorialLessonKind.StunReroll);
            board.CatchCursor.anchoredPosition = Die.RectTransform.anchoredPosition;
            RefreshStun();
            Assert.AreEqual(CombatSymbol.Heal, Die.Symbol);
            Click(false, true);
            Assert.AreEqual(CombatSymbol.Shield, Die.Symbol);
            Assert.IsTrue(Get<bool>(controller, "guidedRerolled"));
            Die.SetupStationaryChoice(CombatSymbol.Shield, board.CatchCursor.anchoredPosition);
            Assert.IsTrue(Die.CanInteract);
            Click(false, true);
            Assert.AreNotEqual(CombatSymbol.Shield, Die.Symbol);
            Assert.IsFalse(Die.CanInteract);
            Assert.IsFalse(Complete);
        }

        [Test]
        public void ResetDuringLessonReleasesSquareTargetConstraintAndStun()
        {
            Prepare(CombatTutorialLessonKind.Damage);
            Assert.IsTrue(board.HasPlayerConstraint);
            board.SetGuidedTutorialTarget(Vector2.zero);
            RefreshStun();
            controller.ResetEncounter();
            controller.ResetEncounter();
            Assert.IsFalse(controller.IsGuidedTutorialRunning);
            Assert.AreEqual(-1, controller.GuidedLessonIndex);
            Assert.IsFalse(board.HasPlayerConstraint); Assert.IsFalse(board.HasForcedPlayerControl);
            Assert.IsFalse(board.IsCursorStunned);
            Assert.IsNull(Die);
            Assert.IsFalse(board.GetComponentsInChildren<Transform>(true)
                .Any(t => t.name == "Guided Movement Target (runtime)" && t.gameObject.activeSelf));
            AssertRestored();
        }

        private void Prepare(CombatTutorialLessonKind kind)
        {
            int index = tutorial.GuidedLessons.ToList().FindIndex(lesson => lesson.Kind == kind);
            Assert.GreaterOrEqual(index, 0);
            Set(controller, "guidedLesson", tutorial.GuidedLessons[index]);
            Set(controller, "guidedLessonIndex", index);
            Invoke(controller, "PrepareGuidedLesson");
        }
        private void RefreshStun() => board.SetStunZonePresentation(0,
            new Vector2(.28f, .5f), new Vector2(.16f, .6f), .72f, true);
        private void Click(bool left, bool right) => Invoke(controller, "HandleGuidedTutorialInput", left, right);
        private CombatDieView Die => Get<CombatDieView>(controller, "guidedDie");
        private bool Complete => Get<bool>(controller, "guidedLessonComplete");
        private void AssertRestored()
        {
            Assert.IsFalse(board.IsGuidedTutorialPresentationActive);
            Assert.AreEqual(fieldSize, board.PlayArea.sizeDelta);
            Assert.AreEqual(fieldPosition, board.PlayArea.anchoredPosition3D);
            Assert.AreEqual(frameSize, frame.sizeDelta);
            Assert.AreEqual(framePosition, frame.anchoredPosition3D);
            Assert.AreEqual(actorScale, actor.transform.localScale);
        }
        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
        private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Flags).GetValue(target);
        private static void Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);
    }
}
#endif
