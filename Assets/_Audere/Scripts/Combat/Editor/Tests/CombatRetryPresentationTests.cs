#if UNITY_INCLUDE_TESTS
using System.IO;
using System.Linq;
using System.Reflection;
using Audere.GameplayInput;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Audere.Combat.Editor.Tests
{
    public sealed class CombatRetryPresentationTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject root;
        private GameplayInputGate gate;
        private CombatRetryView view;

        [SetUp]
        public void Setup()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CombatRetryPresentationAuthoring.UiPath);
            root = Object.Instantiate(prefab.transform.Find("CombatRetryUI").gameObject);
            gate = root.AddComponent<GameplayInputGate>();
            view = root.GetComponent<CombatRetryView>();
            typeof(CombatRetryView).GetField("inputGate", Private).SetValue(view, gate);
            view.ForceHide();
        }

        [TearDown]
        public void Teardown() { if (root != null) Object.DestroyImmediate(root); }

        private void Tick(float dt) => typeof(CombatRetryView).GetMethod("TickPresentation", Private).Invoke(view, new object[] { dt });
        private void Click() => typeof(CombatRetryView).GetMethod("HandleRetryClicked", Private).Invoke(view, null);

        [Test]
        public void Intro_BlocksEarlyClick_ThenRetriesExactlyOnceAndReleasesInput()
        {
            int calls = 0;
            Assert.IsTrue(view.Show(view, () => calls++));
            Assert.AreEqual(GameplayInputMode.Modal, gate.CurrentMode);
            Click();
            Assert.AreEqual(0, calls);
            Assert.IsFalse(view.IsReadyToRetry);
            Tick(.65f);
            Assert.IsFalse(root.transform.Find("Retry Panel/Retry Content/Broken Heart/Heart Visual").gameObject.activeSelf);
            Assert.IsTrue(root.transform.Find("Retry Panel/Retry Content/Broken Heart/Left Half").gameObject.activeSelf);
            Click();
            Assert.AreEqual(0, calls);
            Tick(10f);
            Assert.IsTrue(view.IsReadyToRetry);
            Click(); Click();
            Assert.AreEqual(1, calls);
            Assert.IsFalse(view.IsShowing);
            Assert.AreEqual(0, gate.ActiveClaimCount);
        }

        [TestCase(0f)]
        [TestCase(.65f)]
        [TestCase(10f)]
        public void CancelAtAnyStage_ClearsOwnershipAndReplayStartsWithAnIntactHeart(float elapsed)
        {
            int calls = 0;
            Assert.IsTrue(view.Show(view, () => calls++));
            Tick(elapsed);
            view.ForceHide(); view.ForceHide();
            Tick(20f); Click();
            Assert.AreEqual(0, calls);
            Assert.IsFalse(view.IsShowing);
            Assert.IsNull(view.ActiveOwner);
            Assert.AreEqual(0, gate.ActiveClaimCount);
            Assert.IsFalse(root.GetComponent<AudioSource>().isPlaying);
            Assert.IsTrue(view.Show(view, () => calls++));
            Assert.IsFalse(view.IsReadyToRetry);
            Transform heart = root.transform.Find("Retry Panel/Retry Content/Broken Heart");
            Assert.IsTrue(heart.Find("Heart Visual").gameObject.activeSelf);
            Assert.IsFalse(heart.Find("Left Half").gameObject.activeSelf);
            Assert.AreEqual(Vector2.zero, ((RectTransform)heart.Find("Left Half")).anchoredPosition);
            Assert.AreEqual(1, gate.ActiveClaimCount);
        }

        [Test]
        public void SharedPresentation_FadesCoverAfterBreakAndKeepsRequestedCopy()
        {
            Image cover = root.transform.Find("Retry Panel/Fullscreen Blocker").GetComponent<Image>();
            Assert.AreEqual(0f, cover.color.a);
            Assert.IsTrue(cover.raycastTarget);
            Image heart = root.transform.Find("Retry Panel/Retry Content/Broken Heart/Heart Visual").GetComponent<Image>();
            Image source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Audere/Prefabs/Combat/Player/HeartVisual.prefab").GetComponent<Image>();
            Assert.AreEqual(source.sprite, heart.sprite);
            Assert.AreEqual(Color.white, heart.color);
            var profile = AssetDatabase.LoadAssetAtPath<CombatRetryPresentationProfile>(CombatRetryPresentationAuthoring.ProfilePath);
            Assert.AreEqual("Audere, cùng thử lại nào.\nMình tin cậu sẽ làm được mà.", profile.Message);
            var catalog = AssetDatabase.LoadAssetAtPath<Audere.Audio.AudioCatalog>("Assets/_Audere/Data/Audio/AudioCatalog.asset");
            Assert.IsTrue(catalog.TryGet(Audere.Audio.AudioId.Player_HeartBreak, out var crack));
            Assert.IsNotNull(crack.clip);
            Assert.That(crack.clip.length, Is.InRange(.1f, .5f));
            Assert.IsTrue(view.Show(view, () => { }));
            Tick(profile.CrackDelay + .05f);
            Assert.IsFalse(heart.gameObject.activeSelf);
            Assert.AreEqual(0f, cover.color.a, "The icon must break before the world darkens.");
            Tick(profile.BlackoutStart + profile.BlackoutDuration * .5f - profile.CrackDelay - .05f);
            Assert.That(cover.color.a, Is.InRange(.45f, .55f));
            Assert.AreEqual(0, root.GetComponentInChildren<TMP_Text>(true).maxVisibleCharacters);
            Tick(10f);
            Assert.AreEqual(Color.black, cover.color);
        }

        [Test]
        public void BrokenHeart_KeepsFallingAndFadingUntilHiddenBeforeRetryContentAppears()
        {
            var profile = AssetDatabase.LoadAssetAtPath<CombatRetryPresentationProfile>(CombatRetryPresentationAuthoring.ProfilePath);
            Transform content = root.transform.Find("Retry Panel/Retry Content");
            var whole = content.Find("Broken Heart/Heart Visual").GetComponent<Image>();
            var left = content.Find("Broken Heart/Left Half").GetComponent<CombatHeartHalfGraphic>();
            var right = content.Find("Broken Heart/Right Half").GetComponent<CombatHeartHalfGraphic>();
            var message = content.Find("Retry Message").GetComponent<TMP_Text>();
            var button = content.Find("Retry Button").GetComponent<Button>();
            var buttonGroup = button.GetComponent<CanvasGroup>();
            int calls = 0;
            Assert.IsTrue(view.Show(view, () => calls++));
            Assert.That(profile.MessageStart, Is.GreaterThanOrEqualTo(profile.HeartHiddenTime));

            Tick(profile.HeartFadeStart);
            Assert.IsFalse(whole.gameObject.activeSelf);
            Assert.IsTrue(left.gameObject.activeSelf);
            Assert.IsTrue(right.gameObject.activeSelf);
            Assert.That(left.color.a, Is.EqualTo(1f).Within(.001f));
            float initialY = left.rectTransform.anchoredPosition.y;

            Tick((profile.HeartHiddenTime - profile.HeartFadeStart) * .5f);
            float halfwayY = left.rectTransform.anchoredPosition.y;
            float halfwayAlpha = left.color.a;
            Assert.That(halfwayY, Is.LessThan(initialY), "The broken Heart should continue falling during the blackout.");
            Assert.That(halfwayAlpha, Is.InRange(.1f, .9f));
            Assert.That(right.color.a, Is.EqualTo(halfwayAlpha).Within(.001f));
            Assert.AreEqual(0, message.maxVisibleCharacters);
            Assert.AreEqual(0f, buttonGroup.alpha);

            Tick((profile.HeartHiddenTime - profile.HeartFadeStart) * .5f - .001f);
            Assert.IsTrue(left.gameObject.activeSelf);
            Assert.IsTrue(right.gameObject.activeSelf);
            Assert.That(left.rectTransform.anchoredPosition.y, Is.LessThan(halfwayY));
            Assert.That(left.color.a, Is.LessThan(halfwayAlpha));
            Assert.AreEqual(0, message.maxVisibleCharacters);
            Assert.AreEqual(0f, buttonGroup.alpha);
            Assert.IsFalse(button.interactable);
            Click();
            Assert.AreEqual(0, calls);

            Tick(.002f);
            Assert.IsFalse(whole.gameObject.activeSelf);
            Assert.IsFalse(left.gameObject.activeSelf);
            Assert.IsFalse(right.gameObject.activeSelf);
            Assert.AreEqual(0, message.maxVisibleCharacters);
            Assert.AreEqual(0f, buttonGroup.alpha);
            Tick(10f);
            Assert.IsTrue(view.IsReadyToRetry);
            Assert.Greater(message.maxVisibleCharacters, 0);
            Assert.AreEqual(1f, buttonGroup.alpha);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FrozenCombatBackdrop_IsBehindBlackoutAndReleasedOnCompletionOrCancel(bool completeBlackout)
        {
            var profile = AssetDatabase.LoadAssetAtPath<CombatRetryPresentationProfile>(CombatRetryPresentationAuthoring.ProfilePath);
            var deathFrame = new Texture2D(4, 4) { name = "Retry Test Death Frame" };
            var nextFrame = new Texture2D(4, 4) { name = "Retry Test Next Frame" };
            try
            {
                int calls = 0;
                Assert.IsTrue(view.Show(view, () => calls++, default, deathFrame));
                var backdrop = root.transform.Find("Retry Panel/Frozen Combat Background").GetComponent<RawImage>();
                var cover = root.transform.Find("Retry Panel/Fullscreen Blocker").GetComponent<Image>();
                Assert.AreSame(deathFrame, backdrop.texture);
                Assert.IsTrue(backdrop.gameObject.activeSelf);
                Assert.IsFalse(backdrop.raycastTarget);
                Assert.AreSame(cover.transform.parent, backdrop.transform.parent);
                Assert.Less(backdrop.transform.GetSiblingIndex(), cover.transform.GetSiblingIndex());
                Assert.AreEqual(Vector2.zero, backdrop.rectTransform.anchorMin);
                Assert.AreEqual(Vector2.one, backdrop.rectTransform.anchorMax);
                Assert.AreEqual(Vector2.zero, backdrop.rectTransform.offsetMin);
                Assert.AreEqual(Vector2.zero, backdrop.rectTransform.offsetMax);
                Tick(profile.HeartFadeStart + .01f);
                Assert.IsTrue(deathFrame != null, "The original combat frame must remain while the Heart is breaking.");
                if (completeBlackout)
                {
                    Tick(profile.HeartHiddenTime - profile.HeartFadeStart);
                    Assert.AreEqual(1f, cover.color.a);
                }
                else view.ForceHide();
                Assert.IsTrue(deathFrame == null, "The Retry view must release its captured frame when hidden or fully covered.");
                Assert.IsTrue(backdrop == null || backdrop.texture == null);

                view.ForceHide();
                Tick(20f); Click();
                Assert.AreEqual(0, calls, "Cancelling the presentation must never start another combat attempt.");
                Assert.AreEqual(0, gate.ActiveClaimCount);
                Assert.IsTrue(view.Show(view, () => calls++, default, nextFrame));
                var nextBackdrop = root.transform.Find("Retry Panel/Frozen Combat Background").GetComponent<RawImage>();
                Assert.AreSame(nextFrame, nextBackdrop.texture);
                Assert.AreEqual(0f, cover.color.a);
                view.ForceHide();
                Assert.IsTrue(nextFrame == null);
                Assert.AreEqual(0, calls);
            }
            finally
            {
                if (deathFrame != null) Object.DestroyImmediate(deathFrame);
                if (nextFrame != null) Object.DestroyImmediate(nextFrame);
            }
        }

        [Test]
        public void DeathPose_StaysAtCapturedViewportPositionAcrossSplitAndReplay()
        {
            Image source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Audere/Prefabs/Combat/Player/HeartVisual.prefab").GetComponent<Image>();
            RectTransform heart = (RectTransform)root.transform.Find("Retry Panel/Retry Content/Broken Heart");
            var pose = new CombatHeartScreenPose(source.sprite, new Vector2(.72f, .31f), new Vector2(.03f, .04f), 12f);
            Assert.IsTrue(view.Show(view, () => { }, pose));
            Assert.AreEqual(pose.ViewportCenter, heart.anchorMin);
            Assert.AreEqual(Vector2.zero, heart.anchoredPosition);
            Assert.That(Mathf.DeltaAngle(12f, heart.localEulerAngles.z), Is.EqualTo(0f).Within(.01f));
            Tick(10f);
            Assert.AreEqual(pose.ViewportCenter, heart.anchorMin);
            view.ForceHide();
            Assert.IsTrue(view.Show(view, () => { }, new CombatHeartScreenPose(source.sprite,
                new Vector2(.2f, .4f), pose.ViewportSize, 0f)));
            Assert.AreEqual(new Vector2(.2f, .4f), heart.anchorMin);
            Assert.AreEqual(0f, root.transform.Find("Retry Panel/Fullscreen Blocker").GetComponent<Image>().color.a);
        }

        [Test]
        public void AllCombatScenes_HaveWhiteHeartsAndConfiguredLocalRetryViews()
        {
            string boardGuid = AssetDatabase.AssetPathToGUID("Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab");
            int scenes = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Audere/Scenes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string yaml = File.ReadAllText(path);
                if (!yaml.Contains(boardGuid) && !yaml.Contains("32ae51753931fc94cb63331956372d09")) continue;
                var scene = EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    var players = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<CombatPlayerView>(true)).ToArray();
                    Assert.IsNotEmpty(players, path);
                    foreach (var player in players)
                    {
                        Assert.AreEqual(Color.white, new SerializedObject(player).FindProperty("normalColor").colorValue, path);
                        foreach (Image image in player.GetComponentsInChildren<Image>(true))
                            Assert.AreEqual(Color.white, image.color, path + "/" + image.name);
                    }
                    var retries = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<CombatRetryView>(true)).ToArray();
                    Assert.IsNotEmpty(retries, path);
                    foreach (var retry in retries)
                        Assert.IsTrue(CombatRetryPresentationAuthoring.HasCompleteReferences(retry), path);
                    scenes++;
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
            Assert.GreaterOrEqual(scenes, 6);
        }

        [TestCase(1920, 1080)]
        [TestCase(1024, 768)]
        [TestCase(2560, 1080)]
        public void Presentation_IsReadableAtSupportedAspects(int width, int height)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var texture = new RenderTexture(width, height, 24);
            try
            {
                SceneManager.MoveGameObjectToScene(root, scene);
                var cameraObject = new GameObject("Retry QA Camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>();
                camera.scene = scene;
                camera.targetTexture = texture;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.08f, .12f, .17f);
                camera.orthographic = true;
                camera.orthographicSize = 540f;
                camera.nearClipPlane = .01f;
                camera.farClipPlane = 100f;
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                root.GetComponent<CanvasScaler>().enabled = false;
                canvas.scaleFactor = Mathf.Sqrt(width / 1920f * height / 1080f);
                Image source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Audere/Prefabs/Combat/Player/HeartVisual.prefab").GetComponent<Image>();
                Assert.IsTrue(view.Show(view, () => { }, new CombatHeartScreenPose(source.sprite,
                    new Vector2(.72f, .32f), new Vector2(.045f, .08f), 0f)));
                Capture(camera, texture, width, height, "intact");
                Tick(.8f);
                Capture(camera, texture, width, height, "broken");
                var profile = AssetDatabase.LoadAssetAtPath<CombatRetryPresentationProfile>(CombatRetryPresentationAuthoring.ProfilePath);
                float fallingTime = profile.HeartFadeStart + (profile.HeartHiddenTime - profile.HeartFadeStart) * .35f;
                Tick(fallingTime - .8f);
                Capture(camera, texture, width, height, "falling");
                Tick(profile.HeartHiddenTime - fallingTime + .001f);
                Capture(camera, texture, width, height, "hidden");
                Tick(10f);
                Capture(camera, texture, width, height, "ready");
                TMP_Text text = root.transform.Find("Retry Panel/Retry Content/Retry Message").GetComponent<TMP_Text>();
                var button = (RectTransform)root.transform.Find("Retry Panel/Retry Content/Retry Button");
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                Assert.IsFalse(text.isTextOverflowing);
                Assert.AreEqual(2, text.textInfo.lineCount);
                Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(51.9f));
                Assert.That(label.fontSize, Is.GreaterThanOrEqualTo(47.9f));
                Rect messageBounds = ViewportBounds(text.rectTransform, camera);
                Rect buttonBounds = ViewportBounds(button, camera);
                Assert.That(messageBounds.xMin, Is.GreaterThanOrEqualTo(.075f));
                Assert.That(messageBounds.xMax, Is.LessThanOrEqualTo(.925f));
                Assert.That(messageBounds.yMin, Is.GreaterThan(buttonBounds.yMax), "Retry text and button must not overlap.");
                Rect groupBounds = Rect.MinMaxRect(Mathf.Min(messageBounds.xMin, buttonBounds.xMin),
                    Mathf.Min(messageBounds.yMin, buttonBounds.yMin), Mathf.Max(messageBounds.xMax, buttonBounds.xMax),
                    Mathf.Max(messageBounds.yMax, buttonBounds.yMax));
                Assert.That(groupBounds.center.x, Is.EqualTo(.5f).Within(.01f));
                Assert.That(groupBounds.center.y, Is.EqualTo(.5f).Within(.02f), "The complete Retry group should sit in the middle of the screen.");
                Assert.IsTrue(view.IsReadyToRetry);
            }
            finally
            {
                RenderTexture.active = null;
                EditorSceneManager.ClosePreviewScene(scene);
                Object.DestroyImmediate(texture);
            }
        }

        private static Rect ViewportBounds(RectTransform rect, Camera camera)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 min = Vector2.one * float.PositiveInfinity;
            Vector2 max = Vector2.one * float.NegativeInfinity;
            foreach (Vector3 corner in corners)
            {
                Vector2 point = camera.WorldToViewportPoint(corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static void Capture(Camera camera, RenderTexture target, int width, int height, string stage)
        {
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory("Temp/CombatRetryQA");
                File.WriteAllBytes($"Temp/CombatRetryQA/{width}x{height}-{stage}.png", pixels.EncodeToPNG());
                if (stage == "ready" || stage == "hidden") Assert.That(pixels.GetPixel(2, 2).r, Is.LessThan(.01f));
                else Assert.That(pixels.GetPixel(2, 2).r, Is.GreaterThan(.01f));
                if (stage == "hidden")
                    Assert.IsFalse(pixels.GetPixels32().Any(c => c.r > 15 || c.g > 15 || c.b > 15),
                        "The Heart must disappear completely before any Retry text or button renders.");
                else Assert.IsTrue(pixels.GetPixels32().Any(c => c.r > 100), "Heart/text must render on the black cover.");
            }
            finally { RenderTexture.active = previous; Object.DestroyImmediate(pixels); }
        }
    }
}
#endif
