#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Audere.Combat.Editor.Tests
{
    public sealed class CombatTutorialPolishTests
    {
        private CombatBoardView board;
        private CombatTutorialView tutorial;
        private RectTransform frame;
        private float authoredInset;

        [SetUp]
        public void SetUp()
        {
            board = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>(
                "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            board.gameObject.SetActive(true);
            board.PrepareEncounter("Tutorial layout test");
            frame = (RectTransform)new SerializedObject(board).FindProperty("battleBoxFrame").objectReferenceValue;
            authoredInset = (frame.rect.width - BoundsInFrame(board.PlayArea).width) * .5f;
            tutorial = new GameObject("Tutorial instruction test", typeof(RectTransform), typeof(CanvasGroup))
                .AddComponent<CombatTutorialView>();
            ((RectTransform)tutorial.transform).sizeDelta = new Vector2(1920f, 1080f);
        }

        [TearDown]
        public void TearDown()
        {
            if (tutorial != null) Object.DestroyImmediate(tutorial.gameObject);
            if (board != null) Object.DestroyImmediate(board.gameObject);
        }

        [Test]
        public void SuccessStrikesTheWholeOriginalTaskAndKeepsItVisible()
        {
            const string task = "LEFT CLICK: catch the die.\nRestore TIME.";
            tutorial.ShowGuidedInstruction(task, 4, 11);
            Assert.IsTrue(tutorial.IsVisible);
            tutorial.ShowGuidedSuccess();
            tutorial.ShowGuidedSuccess();

            Assert.AreEqual(task, tutorial.CurrentInstruction,
                "The completed task must remain readable instead of being replaced by a success message.");
            Assert.IsTrue((ActionText.fontStyle & FontStyles.Strikethrough) != 0,
                "Strike every line of the original task.");
            Assert.IsTrue(tutorial.IsVisible);
            Assert.IsTrue(ActionText.gameObject.activeInHierarchy);
            Assert.IsFalse(tutorial.GetComponent<CanvasGroup>().blocksRaycasts);
        }

        [Test]
        public void NextTaskAndReopenedTutorialClearTheCompletedDecoration()
        {
            tutorial.ShowGuidedInstruction("First task", 1, 11);
            tutorial.ShowGuidedSuccess();
            tutorial.ShowGuidedInstruction("Second task", 2, 11);
            Assert.AreEqual("Second task", tutorial.CurrentInstruction);
            Assert.IsFalse((ActionText.fontStyle & FontStyles.Strikethrough) != 0);

            tutorial.ShowGuidedSuccess();
            tutorial.ForceHide();
            Assert.IsFalse(tutorial.IsVisible);
            tutorial.ShowGuidedInstruction("Restarted task", 1, 11);
            Assert.AreEqual("Restarted task", tutorial.CurrentInstruction);
            Assert.IsFalse((ActionText.fontStyle & FontStyles.Strikethrough) != 0);
        }

        [Test]
        public void SquareFrameReservesTimeGutterAndRestoresWithoutScalingHeart()
        {
            Vector2 originalFrameSize = frame.sizeDelta;
            Vector3 originalFramePosition = frame.anchoredPosition3D;
            Vector2 originalFieldSize = board.PlayArea.sizeDelta;
            Vector3 originalFieldPosition = board.PlayArea.anchoredPosition3D;
            Vector3 boardScale = board.transform.localScale;
            Vector3 frameScale = frame.localScale;
            Vector3 heartScale = board.GuidedHeartTarget.localScale;
            board.BeginGuidedTutorialPresentation(400f);
            board.SyncTimerToBoard();

            Assert.AreEqual(frame.rect.width, frame.rect.height, .01f);
            AssertTimerInsideFrame();
            Rect fieldBounds = BoundsInFrame(board.PlayArea);
            Rect timerBounds = BoundsInFrame(board.TimerFocusTarget);
            Assert.AreEqual(fieldBounds.xMin, timerBounds.xMin, .01f);
            Assert.AreEqual(fieldBounds.xMax, timerBounds.xMax, .01f);
            Assert.GreaterOrEqual(fieldBounds.yMin - timerBounds.yMax, 11.9f,
                "TIME needs a visible separator from the playable field.");

            board.EndGuidedTutorialPresentation();
            board.SyncTimerToBoard();
            AssertTimerInsideFrame();
            Assert.AreEqual(originalFrameSize, frame.sizeDelta);
            Assert.AreEqual(originalFramePosition, frame.anchoredPosition3D);
            Assert.AreEqual(originalFieldSize, board.PlayArea.sizeDelta);
            Assert.AreEqual(originalFieldPosition, board.PlayArea.anchoredPosition3D);
            Assert.AreEqual(boardScale, board.transform.localScale);
            Assert.AreEqual(frameScale, frame.localScale);
            Assert.AreEqual(heartScale, board.GuidedHeartTarget.localScale);
        }

        [Test]
        public void ResizingAndMovingFrameUpdatesWholeTimeTrackWithoutChangingRemainingFraction()
        {
            board.UpdateTimer(.4f);
            Image fill = (Image)new SerializedObject(board).FindProperty("timerFill").objectReferenceValue;
            Vector3 timerScale = board.TimerFocusTarget.localScale;
            foreach (Vector2 size in new[] { new Vector2(440f, 440f), new Vector2(930f, 500f), new Vector2(620f, 500f) })
            {
                frame.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                frame.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
                frame.anchoredPosition += new Vector2(37f, -13f);
                board.SyncTimerToBoard();
                AssertTimerInsideFrame();
                Assert.AreEqual(.4f, BoundsInFrame(fill.rectTransform).width /
                    BoundsInFrame(board.TimerFocusTarget).width, .001f);
                Assert.AreEqual(timerScale, board.TimerFocusTarget.localScale);
            }
        }

        [Test]
        public void NarrowingOnlyPlayableFieldKeepsTimeMatchedToOuterBoard()
        {
            board.SyncTimerToBoard();
            Rect originalTrack = BoundsInFrame(board.TimerFocusTarget);
            board.SetBattleBoxHorizontalLayout(.5f, 1f);
            board.SyncTimerToBoard();
            Assert.Less(BoundsInFrame(board.PlayArea).width, originalTrack.width);
            AssertSameTrack(originalTrack);
            AssertTimerInsideFrame();
            board.ResetBattleBoxLayout();
            board.SyncTimerToBoard();
            AssertSameTrack(originalTrack);
        }

        private TMP_Text ActionText => (TMP_Text)typeof(CombatTutorialView).GetField(
            "guidedInstructionText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tutorial);

        private void AssertTimerInsideFrame()
        {
            Rect timer = BoundsInFrame(board.TimerFocusTarget);
            Assert.AreEqual(frame.rect.xMin + authoredInset, timer.xMin, .01f);
            Assert.AreEqual(frame.rect.xMax - authoredInset, timer.xMax, .01f);
            Assert.AreEqual(frame.rect.yMin + authoredInset, timer.yMin, .01f);
            Assert.Greater(timer.height, 0f);
            Assert.Less(timer.yMax, frame.rect.yMax);
        }

        private void AssertSameTrack(Rect expected)
        {
            Rect actual = BoundsInFrame(board.TimerFocusTarget);
            Assert.AreEqual(expected.xMin, actual.xMin, .01f);
            Assert.AreEqual(expected.xMax, actual.xMax, .01f);
            Assert.AreEqual(expected.yMin, actual.yMin, .01f);
            Assert.AreEqual(expected.yMax, actual.yMax, .01f);
        }

        private Rect BoundsInFrame(RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (Vector3 corner in corners)
            {
                Vector2 point = frame.InverseTransformPoint(corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
#endif
