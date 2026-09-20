#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using Audere.Story.Steps;
using Audere.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Audere.Story.Editor.Tests
{
    public sealed class FallingRoomTests
    {
        private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(x => x.GetComponentsInChildren<T>(true)).ToArray();
        private static void Call(FallingRoomStep step, string method, params object[] args) => typeof(FallingRoomStep)
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(step, args);

        [Test]
        public void ProductionSequence_FallsAndSpeaksBeforeSharedCombatTransition()
        {
            var scene = EditorSceneManager.OpenPreviewScene(Day4CrowdSetupTool.ScenePath);
            try
            {
                var fall = All<FallingRoomStep>(scene).Single();
                Assert.IsNotNull(fall.Profile);
                CollectionAssert.AreEqual(new[] { "010_AudereSinks", "020_OnlyTheFall", "030_WordsInTheDark", "070_TheRoomBecomesPressure" },
                    fall.Sequence.transform.Cast<Transform>().Select(x => x.name).ToArray());
                var transition = fall.Sequence.GetComponentInChildren<FullscreenWorldModeTransitionStep>();
                Assert.AreEqual("WorldTransition_DreamyDisorientation", transition.TransitionProfile.name);
                Assert.AreEqual(WorldGameplayMode.Combat, transition.TargetMode);
                var story = fall.transform.parent;
                Assert.Less(story.Find("060_TheyMustBeLaughing").GetSiblingIndex(), fall.transform.GetSiblingIndex());
                Assert.Less(fall.transform.GetSiblingIndex(), story.Find("080_TheCrowd").GetSiblingIndex());
                foreach (var e in All<StoryEvent>(scene)) foreach (Transform child in e.transform)
                    Assert.AreEqual(1, child.GetComponents<StoryStep>().Length, child.name);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [TestCase(.1f)]
        [TestCase(1.2f)]
        [TestCase(8f)]
        public void Fall_RisesContinuouslyAndRestoresRoomOnCancelAndReplay(float time)
        {
            var scene = EditorSceneManager.OpenPreviewScene(Day4CrowdSetupTool.ScenePath);
            try
            {
                var fall = All<FallingRoomStep>(scene).Single();
                var world = All<WorldModeController>(scene).Single(); world.ApplyModeImmediate(WorldGameplayMode.Story);
                var camera = All<Camera>(scene).Single(x => x.CompareTag("MainCamera"));
                var originalColor = camera.backgroundColor;
                var desks = All<Transform>(scene).Where(x => x.name == "Desk centered on tile").ToArray();
                var positions = desks.Select(x => x.position).ToArray();
                var masks = All<SpriteRenderer>(scene).Where(x => x.name.StartsWith("Mask ")).ToArray();
                var colors = masks.Select(x => x.color).ToArray();
                var tile = All<SpriteRenderer>(scene).Single(x => x.name == "Audere Tile");
                var tileColor = tile.color;
                for (int replay = 0; replay < 2; replay++)
                {
                    Call(fall, "BeginPresentation"); Call(fall, "SamplePresentation", time);
                    Assert.AreEqual(Color.black, camera.backgroundColor);
                    Assert.IsTrue(masks.All(x => x.color == Color.black));
                    Assert.IsFalse(tile.enabled); Assert.AreEqual(tileColor, tile.color);
                    if (time > .35f) for (int i = 0; i < desks.Length; i++) Assert.Greater(desks[i].position.y, positions[i].y);
                    Call(fall, "SamplePresentation", time + .1f);
                    Assert.Greater(fall.Profile.Rise(time + .1f), fall.Profile.Rise(time));
                    world.ApplyModeImmediate(WorldGameplayMode.Combat); Call(fall, "SamplePresentation", time);
                    Assert.IsFalse(fall.transform.Find("Falling room runtime visuals").gameObject.activeSelf);
                    Call(fall, "OnCancelled");
                    Assert.IsNull(fall.transform.Find("Falling room runtime visuals"));
                    Assert.AreEqual(originalColor, camera.backgroundColor); Assert.IsTrue(tile.enabled);
                    for (int i = 0; i < desks.Length; i++) Assert.AreEqual(positions[i], desks[i].position);
                    for (int i = 0; i < masks.Length; i++) Assert.AreEqual(colors[i], masks[i].color);
                    world.ApplyModeImmediate(WorldGameplayMode.Story);
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
#endif
