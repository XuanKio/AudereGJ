#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Audere.Combat.Editor.Tests
{
    public sealed class TeacherArithmeticSelectionTests
    {
        private CombatBoardView board;
        private TeacherMathChoiceMove move;

        private sealed class FixedRandom : ICombatRandom
        {
            public float Value01() => 0f;
            public float Range(float minimum, float maximum) => minimum;
        }

        [SetUp]
        public void SetUp()
        {
            board = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>(
                "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            board.gameObject.SetActive(true);
            board.PrepareEncounter("Arithmetic test");
            board.ResetPlayer();
            move = ScriptableObject.CreateInstance<TeacherMathChoiceMove>();
            var serialized = new SerializedObject(move);
            serialized.FindProperty("monochromeEchoMaterial").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Material>("Assets/_Audere/Materials/Combat/MountRainbowEcho.mat");
            serialized.FindProperty("incorrectAnswerFollowUp").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TeacherGeometrySketchMove>("Assets/_Audere/Data/Combat/Teacher/Moves/Move_TeacherGeometrySketch.asset");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Canvas.ForceUpdateCanvases();
        }

        [TearDown]
        public void TearDown()
        {
            if (board != null) Object.DestroyImmediate(board.gameObject);
            if (move != null) Object.DestroyImmediate(move);
        }

        [Test]
        public void HeartCenteredOnCorrectAnswer_WinsEvenWhenEarlierChoiceAlsoOverlaps()
        {
            var execution = Start();
            execution.Tick(.7f);
            var dice = Choices();
            var correct = Correct(dice);
            Assert.AreSame(dice[2], correct, "This fixture exercises an earlier wrong entry winning the old loop.");
            for (int i = 0; i < dice.Length; i++)
                dice[i].RectTransform.anchoredPosition = new Vector2((i - 1) * 65f, 0f);
            board.CatchCursor.position = correct.RectTransform.position;
            Assert.IsTrue(board.CursorOverlaps(dice[1]));
            Assert.AreEqual(2, board.FindClosestChoiceUnderCursor(dice));
            ((ICombatMoveInputHandler)execution).HandleInput(true, false);
            Assert.AreEqual(3, ((ICombatMoveDamageReward)execution).ConsumePendingDamage());
            Assert.AreEqual(0, ((ICombatMoveDamageReward)execution).ConsumePendingDamage());
            execution.Cancel();
        }

        [Test]
        public void ChoicesHaveSeparateCatchCenters_AndNumbersStayAboveTheHeart()
        {
            var execution = Start();
            execution.Tick(.7f);
            var dice = Choices();
            Assert.AreEqual(360f, board.PlayArea.rect.width, .01f);
            foreach (var die in dice)
            {
                board.CatchCursor.position = die.RectTransform.position;
                Assert.AreEqual(1, dice.Count(board.CursorOverlaps));
                var label = Label(die);
                var corners = new Vector3[4];
                label.rectTransform.GetWorldCorners(corners);
                float labelBottom = corners.Min(c => board.WorldToPlayArea(c).y);
                float labelTop = corners.Max(c => board.WorldToPlayArea(c).y);
                Assert.Greater(labelBottom, board.CatchZoneCenter.y + 14f);
                Assert.Less(labelTop, board.PlayArea.rect.yMax);
            }
            execution.Cancel();
        }

        [Test]
        public void TwentyLessonsUseTwoDigitCarryOrBorrow_UniqueAnswersAndUniqueQuestions()
        {
            var questions = new HashSet<string>();
            var solutions = new HashSet<int>();
            var operations = new HashSet<string>();
            var random = new SystemCombatRandom(901);
            for (int round = 0; round < 20; round++)
            {
                var execution = Start(random: random);
                execution.Tick(.7f);
                string question = Prompt();
                string[] parts = question.Split(' ');
                int left = int.Parse(parts[0]), right = int.Parse(parts[2]);
                Assert.That(left, Is.InRange(10, 99));
                Assert.That(right, Is.InRange(10, 99));
                if (parts[1] == "+") Assert.GreaterOrEqual(left % 10 + right % 10, 10);
                else Assert.Less(left % 10, right % 10);
                int answer = Solve(question);
                Assert.IsTrue(questions.Add(question), question);
                Assert.IsTrue(solutions.Add(answer), question);
                operations.Add(parts[1]);
                var answers = Choices().Select(d => int.Parse(Label(d).text)).ToArray();
                Assert.AreEqual(3, answers.Distinct().Count());
                Assert.AreEqual(1, answers.Count(value => value == answer));
                Assert.IsTrue(answers.All(value => value >= 10 && value <= 99));
                execution.Cancel();
            }
            CollectionAssert.AreEquivalent(new[] { "+", "-" }, operations);
        }

        [Test]
        public void TenIncorrectChoices_ExitToGeometry_UseNewQuestionsAndRestoreBoard()
        {
            Vector2 fullSize = board.PlayArea.rect.size;
            var questions = new HashSet<string>();
            var answers = new HashSet<int>();
            for (int round = 0; round < 10; round++)
            {
                var execution = Start();
                execution.Tick(.7f);
                string question = Prompt();
                Assert.IsTrue(questions.Add(question));
                Assert.IsTrue(answers.Add(Solve(question)));
                var dice = Choices();
                var correct = Correct(dice);
                board.CatchCursor.position = dice.First(d => d != correct).RectTransform.position;
                ((ICombatMoveInputHandler)execution).HandleInput(true, false);
                Assert.IsFalse(board.IsTeacherQuestionVisible);
                Assert.IsInstanceOf<TeacherGeometrySketchMove>(((ICombatMoveFollowUp)execution).NextMove);
                Assert.AreEqual(0, ((ICombatMoveDamageReward)execution).ConsumePendingDamage());
                // Further clicks during exit must never convert a wrong answer into damage.
                ((ICombatMoveInputHandler)execution).HandleInput(true, false);
                Assert.AreEqual(0, ((ICombatMoveDamageReward)execution).ConsumePendingDamage());
                execution.Tick(0f);
                Assert.IsFalse(execution.IsComplete);
                execution.Tick(.25f);
                execution.Tick(.7f);
                Assert.IsTrue(execution.IsComplete);
                Assert.IsEmpty(Choices());
                Assert.IsFalse(board.IsTeacherQuestionVisible);
                Assert.AreEqual(fullSize.x, board.PlayArea.rect.width, .01f);
                Assert.AreEqual(fullSize.y, board.PlayArea.rect.height, .01f);
                execution.Cancel();
                Assert.IsNull(((ICombatMoveFollowUp)execution).NextMove);
            }
        }

        [Test]
        public void RuntimeWrongAnswer_RestartsGeometryThenNewMath_AndCorrectAnswerContinuesSpiral()
        {
            var enemy = AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>(
                "Assets/_Audere/Data/Combat/Teacher/Enemy_Teacher_PLACEHOLDER.asset");
            var runtime = new CombatEnemyRuntime(enemy, board, new SystemCombatRandom(811), 811);
            try
            {
                runtime.Start();
                foreach (var cue in runtime.CurrentPhase.DialogueCues) runtime.MarkCueResolved(cue);
                Assert.AreEqual(CombatEnemyProgression.PhaseBreak, runtime.ApplyDamage(7, out _));
                runtime.CompletePhaseBreak();
                Assert.IsInstanceOf<TeacherGeometrySketchMove>(runtime.CurrentMove);
                runtime.Tick(runtime.CurrentMove.Duration + runtime.CurrentMove.LeadInDuration + .1f);
                Assert.IsInstanceOf<TeacherMathChoiceMove>(runtime.CurrentMove);
                runtime.Tick(1.2f);
                string first = Prompt();
                var dice = Choices();
                var correct = Correct(dice);
                board.CatchCursor.position = dice.First(d => d != correct).RectTransform.position;
                runtime.HandleMoveInput(true, false);
                Assert.AreEqual(0, runtime.ConsumeMoveDamageReward());
                runtime.Tick(.25f);runtime.Tick(.7f);
                Assert.IsInstanceOf<TeacherGeometrySketchMove>(runtime.CurrentMove,
                    "A wrong answer must skip the pending spiral and return to geometry.");
                runtime.Tick(runtime.CurrentMove.Duration + runtime.CurrentMove.LeadInDuration + .1f);
                Assert.IsInstanceOf<TeacherMathChoiceMove>(runtime.CurrentMove);
                runtime.Tick(1.2f);
                Assert.AreNotEqual(first, Prompt());
                board.CatchCursor.position = Correct(Choices()).RectTransform.position;
                runtime.HandleMoveInput(true, false);
                Assert.AreEqual(3, runtime.ConsumeMoveDamageReward());
                runtime.Tick(.25f);runtime.Tick(.7f);
                Assert.IsInstanceOf<TeacherSpiralSketchMove>(runtime.CurrentMove);
            }
            finally { runtime.Cancel(); }
        }

        [Test]
        public void EncounterPrepareAndNewSessionBothResetQuestionHistory()
        {
            string first = StartAndReadQuestion(601);
            Assert.AreNotEqual(first, StartAndReadQuestion(601));
            board.PrepareEncounter("Retry");
            board.ResetPlayer();
            Assert.AreEqual(first, StartAndReadQuestion(601));
            Assert.AreEqual(first, StartAndReadQuestion(602));
        }

        [TestCase(.2f)]
        [TestCase(.8f)]
        [TestCase(1.6f)]
        public void TeacherOrbit_LeavesAnchoredBlackWhiteSilhouettes_AndCancelRestoresMount(float duration)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>(
                "Assets/_Audere/Data/Combat/Teacher/Enemy_Teacher_PLACEHOLDER.asset");
            Transform mount = (Transform)new SerializedObject(board).FindProperty("enemyMount").objectReferenceValue;
            var actor = Object.Instantiate(enemy.ActorPrefab, mount);
            board.BindAuthoredEnemyActor(actor);
            board.SpawnEnemyActor(enemy.ActorPrefab, 601);
            Vector3 home = mount.localPosition;
            Vector3 presentationHome = board.EnemyPresentationRoot.localPosition;
            var execution = Start(actor);
            for (float t = 0f; t < duration; t += .04f) execution.Tick(.04f);
            Assert.Greater(board.ActiveMountEchoes, 1);
            Assert.AreNotEqual(home, mount.localPosition);
            Assert.AreEqual(presentationHome, board.EnemyPresentationRoot.localPosition);
            var echoRoot = mount.parent.Find("Mount Monochrome Echo (runtime)");
            Assert.IsNotNull(echoRoot);
            var images = echoRoot.GetComponentsInChildren<Image>().Where(i => i.isActiveAndEnabled).ToArray();
            Assert.IsTrue(images.Any(i => i.color.r == 0f && i.color.g == 0f && i.color.b == 0f));
            Assert.IsTrue(images.Any(i => i.color.r == 1f && i.color.g == 1f && i.color.b == 1f));
            Assert.IsTrue(images.All(i => !i.maskable && !i.raycastTarget));
            Vector3 snapshot = images[0].transform.position;
            execution.Tick(.005f);
            Assert.AreEqual(snapshot, images[0].transform.position, "Afterimages remain at their emitted world pose.");
            Vector3 paused = mount.position;
            int active = board.ActiveMountEchoes;
            execution.Tick(0f);
            Assert.AreEqual(paused, mount.position);
            Assert.AreEqual(active, board.ActiveMountEchoes);
            execution.Cancel();
            execution.Cancel();
            Assert.AreEqual(home, mount.localPosition);
            Assert.AreEqual(0, board.ActiveMountEchoes);
            Assert.IsFalse(board.IsMountDiveActive);
            Assert.IsEmpty(Choices());
            Assert.IsFalse(board.IsTeacherQuestionVisible);
        }

        private ICombatMoveExecution Start(CombatEnemyActor actor = null, ICombatRandom random = null, int session = 601) =>
            move.CreateExecution(new CombatMoveExecutionContext(board, actor, random ?? new FixedRandom(), session, 2));

        private string StartAndReadQuestion(int session)
        {
            var execution = Start(session: session);
            execution.Tick(.7f);
            string question = Prompt();
            execution.Cancel();
            return question;
        }

        private CombatDieView[] Choices() => board.GetComponentsInChildren<CombatDieView>(true)
            .Where(d => d.gameObject.activeSelf).OrderBy(d => (int)d.Symbol).ToArray();
        private string Prompt() => board.GetComponentsInChildren<TextMeshProUGUI>(true)
            .Single(t => t.name == "Teacher question (runtime)").text;
        private static TMP_Text Label(CombatDieView die) => die.GetComponentsInChildren<TMP_Text>(true)
            .Single(t => t.name == "Choice answer");
        private CombatDieView Correct(IEnumerable<CombatDieView> dice) =>
            dice.Single(d => Label(d).text == Solve(Prompt()).ToString());
        private static int Solve(string question)
        {
            string[] parts = question.Split(' ');
            int left = int.Parse(parts[0]), right = int.Parse(parts[2]);
            return parts[1] == "+" ? left + right : left - right;
        }
    }
}
#endif
