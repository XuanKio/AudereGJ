#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Reflection;
using Audere.Story.Presentation;
using Audere.Story.Steps;
using Audere.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Audere.Story.Editor.Tests
{
    public sealed class TileRippleEndingTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(x => x.GetComponentsInChildren<T>(true)).ToArray();
        private static void Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);

        [Test]
        public void Profile_AcceleratesContinuouslyAndBrightensBeforeArrival()
        {
            var profile = AssetDatabase.LoadAssetAtPath<TileRippleWalkProfile>(Day4EndingRippleAuthoring.ProfilePath);
            Assert.IsNotNull(profile);
            Assert.AreEqual((Color)new Color32(252, 225, 194, 255), profile.Background);
            Assert.AreEqual(0f, profile.DistanceProgress(profile.WalkStart));
            float first = profile.DistanceProgress(profile.WalkStart + 1f) - profile.DistanceProgress(profile.WalkStart + .9f);
            float last = profile.DistanceProgress(profile.Duration - 1f) - profile.DistanceProgress(profile.Duration - 1.1f);
            Assert.Greater(last, first * 2f);
            Assert.That(profile.WhiteAlpha(profile.Duration * .8f), Is.InRange(.1f, .95f));
            Assert.Less(profile.DistanceProgress(profile.Duration * .8f), 1f);
            Assert.AreEqual(1f, profile.WhiteAlpha(profile.Duration));
        }

        [TestCase(1024, 576)]
        [TestCase(1024, 768)]
        [TestCase(1536, 648)]
        public void ProductionPath_HoldsThenOpensOneRowAndAccelerates(int width, int height)
        {
            var scene = EditorSceneManager.OpenPreviewScene(Day4TimorEveningSetupTool.ScenePath);
            RenderTexture output = null;
            try
            {
                All<WorldModeController>(scene).Single().ApplyModeImmediate(WorldGameplayMode.Story);
                var walk = All<ContinuousTileWalkStep>(scene).Single();
                var field = walk.RippleField;
                var camera = All<Camera>(scene).Single(x => x.CompareTag("MainCamera"));
                Color originalBackground = camera.backgroundColor;
                Assert.AreNotEqual(walk.Profile.Background, originalBackground, "Peach is scoped to the ending.");
                camera.scene = scene;
                camera.transform.Find("PuzzleViewportMask").gameObject.SetActive(false);
                foreach (Canvas canvas in All<Canvas>(scene)) canvas.enabled = canvas.name == "ENDING WHITE COVER";
                var cover = All<Canvas>(scene).Single(x => x.name == "ENDING WHITE COVER");
                cover.gameObject.SetActive(true);
                cover.renderMode = RenderMode.ScreenSpaceCamera; cover.worldCamera = camera; cover.planeDistance = 1f;
                output = new RenderTexture(width, height, 24); camera.targetTexture = output;
                field.gameObject.SetActive(true);
                var start = walk.Actor.position;
                var shadow = walk.GroundedShadow.GetComponent<SpriteRenderer>();
                Color shadowColor = shadow.color; Vector3 shadowScale = shadow.transform.lossyScale;
                Vector3 shadowStart = shadow.transform.position;
                Assert.IsTrue((bool)typeof(ContinuousTileWalkStep).GetMethod("BeginMotion", Private).Invoke(walk, null));
                Assert.AreEqual(18, walk.Waypoints.Length);
                Assert.AreEqual(21, field.Tiles.Count);
                Assert.AreEqual(1, field.Tiles.Count(x => x.color.a > .01f), "Begin standing on one tile.");
                var tilePositions = field.Tiles.Select(x => x.transform.position).ToArray();
                var tileAlphas = field.Tiles.Select(x => x.color.a).ToArray();
                Assert.IsNotNull(walk.transform.parent.GetComponent<StoryEvent>());
                Assert.IsNull(walk.transform.parent.Find("320_FadeToWhite"));
                float previousX = walk.Actor.position.x;
                int frameCount = Mathf.CeilToInt(walk.Profile.Duration * 60f);
                for (int frame = 1; frame <= frameCount; frame++)
                {
                    float time = Mathf.Min(frame / 60f, walk.Profile.Duration);
                    Call(walk, "RenderMotion", time);
                    if (time <= walk.Profile.WalkStart) Assert.AreEqual(start, walk.Actor.position, "Hold while the row first opens.");
                    else Assert.Greater(walk.Actor.position.x, previousX, "No stop between tile contacts.");
                    previousX = walk.Actor.position.x;
                    for (int i = 0; i < field.Tiles.Count; i++)
                    {
                        Assert.AreEqual(tilePositions[i], field.Tiles[i].transform.position, "Tiles do not shake.");
                        Assert.GreaterOrEqual(field.Tiles[i].color.a, tileAlphas[i], "Revealed tiles stay visible.");
                        tileAlphas[i] = field.Tiles[i].color.a;
                    }
                    Assert.AreEqual(shadowColor, shadow.color);
                    Assert.Less(Vector3.Distance(shadowScale, shadow.transform.lossyScale), .0001f);
                    Assert.AreEqual((Color)new Color32(252, 225, 194, 255), camera.backgroundColor);
                    var follow = All<StoryCameraFollow2D>(scene).Single();
                    camera.transform.position = (Vector3)typeof(StoryCameraFollow2D).GetMethod("DesiredPosition", Private).Invoke(follow, null);
                    if (frame == 60 || frame == 180 || frame == 300 || frame == 650 || frame == 850 || frame == frameCount)
                        Capture(camera, output, $"row-{width}x{height}-{frame:000}", frame == 300 ? field : null, walk.Actor.position);
                    if (time <= walk.Profile.InitialHold)
                        Assert.AreEqual(1, field.Tiles.Count(x => x.color.a > .01f));
                    if (frame == 300)
                    {
                        Assert.Greater(field.Tiles.Count(x => x.color.a > .05f), 3);
                        Assert.IsTrue(field.Tiles.Any(x => x.color.a < .01f));
                    }
                }
                Assert.Less(Vector3.Distance(walk.Actor.position, walk.Waypoints.Last().position), .0001f);
                Assert.AreEqual(1f, field.CoverAlpha);
                Call(walk, "OnCancelled");
                Assert.IsFalse(field.IsPrepared);
                Assert.AreEqual(originalBackground, camera.backgroundColor);
                Assert.IsFalse(field.gameObject.activeSelf, "Cancelled ripples must not expose the full tile grid.");
                Assert.AreEqual(0f, field.CoverAlpha);
                Assert.AreEqual(start.y, walk.Actor.position.y, .0001f);
                foreach (var tile in field.Tiles) Assert.AreEqual(Color.white, tile.color);
                walk.Actor.position = start;
                // Restore the shadow's authored ground offset before a fresh attempt.
                shadow.transform.position = shadowStart;
                field.gameObject.SetActive(true);
                Assert.IsTrue((bool)typeof(ContinuousTileWalkStep).GetMethod("BeginMotion", Private).Invoke(walk, null));
                Call(walk, "RenderMotion", .3f);
                Call(walk, "OnCancelled");
                Assert.AreEqual(start.y, walk.Actor.position.y, .0001f);
                Assert.AreEqual(shadowColor, shadow.color);
                Assert.AreEqual(0f, field.CoverAlpha);
            }
            finally
            {
                RenderTexture.active = null;
                EditorSceneManager.ClosePreviewScene(scene);
                if (output != null) Object.DestroyImmediate(output);
            }
        }
        private static void Capture(Camera camera, RenderTexture target, string name, TileRippleField field, Vector3 actorPosition)
        {
            Canvas.ForceUpdateCanvases(); camera.Render();
            RenderTexture previous = RenderTexture.active; RenderTexture.active = target;
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Temp/EndingRippleQA");
                File.WriteAllBytes("Temp/EndingRippleQA/" + name + ".png", pixels.EncodeToPNG());
                if (field != null)
                {
                    Color background = pixels.GetPixel(5, target.height - 5);
                    Assert.AreEqual(252f / 255f, background.r, .015f);
                    Assert.AreEqual(225f / 255f, background.g, .015f);
                    Assert.AreEqual(194f / 255f, background.b, .015f);
                    int hidden = 0, visible = 0;
                    foreach (var tile in field.Tiles)
                    {
                        if (Mathf.Abs(tile.bounds.center.x - actorPosition.x) < .25f) continue;
                        Vector3 point = camera.WorldToScreenPoint(tile.bounds.center);
                        if (point.x < 2 || point.x >= target.width - 2 || point.y < 2 || point.y >= target.height - 2) continue;
                        Color rendered = pixels.GetPixel((int)point.x, (int)point.y);
                        if (tile.color.a < .001f)
                        {
                            Assert.AreEqual(background.b, rendered.b, .015f, "Transparent tile must reveal the peach background.");
                            hidden++;
                        }
                        else if (tile.color.a > .4f)
                        {
                            Assert.Greater(rendered.b, background.b + .04f, "Visible tile must brighten towards white.");
                            visible++;
                        }
                    }
                    Assert.Greater(hidden, 0);
                    Assert.Greater(visible, 0);
                }
            }
            finally { RenderTexture.active = previous; Object.DestroyImmediate(pixels); }
        }
    }
}
#endif
