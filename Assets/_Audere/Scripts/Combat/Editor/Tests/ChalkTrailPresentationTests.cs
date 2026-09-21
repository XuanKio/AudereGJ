#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat.Editor.Tests
{
    public sealed class ChalkTrailPresentationTests
    {
        private CombatBoardView board;
        private GameObject targetObject;

        [SetUp]
        public void Setup()
        {
            board = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>(
                "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            board.gameObject.SetActive(true);
            targetObject = new GameObject("Trail motion target", typeof(RectTransform));
            targetObject.transform.SetParent(board.PlayArea, false);
            Canvas.ForceUpdateCanvases();
        }

        [TearDown]
        public void Cleanup()
        {
            if (board != null) Object.DestroyImmediate(board.gameObject);
        }

        [Test]
        public void FirstFrameIncludesSpawnPoint_AndLongFramesUseUniformSegments()
        {
            var target = (RectTransform)targetObject.transform;
            target.anchoredPosition = new Vector2(-80f, 0f);
            var motion = Wrap(new ParametricProjectileMotion(1f,
                t => new Vector2(-80f + 64f * t, 0f), null));
            Assert.IsFalse(motion.Tick(target, 1f));
            var strips = ActiveStrips();
            Assert.AreEqual(4, strips.Length);
            Assert.AreEqual(-80f, strips[0].RectTransform.anchoredPosition.x - 8f, .001f);
            Assert.AreEqual(-16f, strips[3].RectTransform.anchoredPosition.x + 8f, .001f);
            Assert.IsTrue(strips.All(s => Mathf.Abs(s.RectTransform.sizeDelta.x - 16f) < .001f));
        }

        [Test]
        public void LinearTrailGeometryIsIndependentOfFrameRate_AndPauseDoesNotPaint()
        {
            var slow = PaintLinearPath(15);
            board.ClearStunTrails();
            var fast = PaintLinearPath(120);
            Assert.AreEqual(slow.Length, fast.Length);
            for (int index = 0; index < slow.Length; index++)
                Assert.Less((slow[index] - fast[index]).sqrMagnitude, .001f);
        }

        [Test]
        public void SharedSurfaceHasContiguousChalkGeometry_AndPoolStaysBounded()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Audere/Materials/UI/ChalkDrawing.mat");
            var serialized = new SerializedObject(board);
            serialized.FindProperty("stunTrailMaterial").objectReferenceValue = material;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            board.EmitStunTrail(this, 71, 2, new Vector2(-32f, 0f), new Vector2(-16f, 0f), 14f, 3f, .3f);
            board.EmitStunTrail(this, 71, 2, new Vector2(-16f, 0f), Vector2.zero, 14f, 3f, .3f);
            var surface = board.GetComponentInChildren<CombatChalkTrailGraphic>();
            Assert.AreSame(material, surface.material);
            Assert.IsFalse(surface.raycastTarget);
            Assert.IsTrue(ActiveStrips().All(s => s.GetComponent<Image>() == null));
            using (var vertices = new VertexHelper())
            {
                typeof(CombatChalkTrailGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                        null, new[] { typeof(VertexHelper) }, null)
                    .Invoke(surface, new object[] { vertices });
                Assert.AreEqual(8, vertices.currentVertCount);
                var end = UIVertex.simpleVert; var start = UIVertex.simpleVert;
                vertices.PopulateUIVertex(ref end, 2); vertices.PopulateUIVertex(ref start, 5);
                Assert.Less((end.position - start.position).sqrMagnitude, .0001f);
                vertices.PopulateUIVertex(ref end, 3); vertices.PopulateUIVertex(ref start, 4);
                Assert.Less((end.position - start.position).sqrMagnitude, .0001f);
            }
            for (int index = 0; index < 500; index++)
                board.EmitStunTrail(this, 71, 2, new Vector2(-16f, 30f), new Vector2(0f, 30f), 14f, 3f, .3f);
            Assert.AreEqual(384, board.ActiveStunTrailCount);
            board.ClearStunTrails();
            Assert.AreEqual(0, board.ActiveStunTrailCount);
            Assert.IsFalse(board.IsCursorStunned);
        }

        [Test]
        public void ShrinkingFieldClipsExistingTrailCollision_AndFadeReleasesCatchImmediately()
        {
            float oldRight = board.PlayArea.rect.xMax - 30f;
            board.EmitStunTrail(this, 71, 2, new Vector2(-900f, 60f), new Vector2(900f, 60f), 14f, 1f, .5f);
            board.SetBattleBoxHorizontalLayout(.55f, 0f);
            Canvas.ForceUpdateCanvases();
            board.CatchCursor.anchoredPosition = new Vector2(oldRight, 60f);
            board.TickBullets(0f, 1f);
            Assert.IsFalse(board.IsCursorStunned, "The masked-out portion must no longer block catch.");
            board.CatchCursor.anchoredPosition = new Vector2(board.PlayArea.rect.center.x, 60f);
            board.TickBullets(0f, 1f);
            Assert.IsTrue(board.IsCursorStunned);
            board.TickBullets(1f, 1f);
            Assert.IsFalse(board.IsCursorStunned);
            Assert.AreEqual(1, board.ActiveStunTrailCount);
            board.TickBullets(.51f, 1f);
            Assert.AreEqual(0, board.ActiveStunTrailCount);
        }

        private ICombatProjectileMotion Wrap(ICombatProjectileMotion motion)
        {
            var settings = new CombatProjectileTrailSettings();
            typeof(CombatProjectileTrailSettings).GetField("enabled", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(settings, true);
            return settings.Wrap(motion, new CombatMoveExecutionContext(board, null, new SystemCombatRandom(7), 71, 2), this);
        }

        private Vector4[] PaintLinearPath(int ticks)
        {
            var target = (RectTransform)targetObject.transform;
            target.anchoredPosition = new Vector2(-100f, 0f);
            var motion = Wrap(new ParametricProjectileMotion(1f, t => new Vector2(-100f + 200f * t, 0f), null));
            motion.Tick(target, 0f);
            Assert.AreEqual(0, board.ActiveStunTrailCount);
            for (int tick = 0; tick < ticks - 1; tick++) motion.Tick(target, 1f / ticks);
            motion.Tick(target, 1f);
            var strips = ActiveStrips();
            Assert.AreEqual(13, strips.Length);
            return strips.Select(s => new Vector4(s.RectTransform.anchoredPosition.x, s.RectTransform.anchoredPosition.y,
                s.RectTransform.sizeDelta.x, s.RectTransform.sizeDelta.y)).ToArray();
        }

        private CombatStunZoneView[] ActiveStrips() => board.PlayArea.Find("Stun Trail Root")
            .GetComponentsInChildren<CombatStunZoneView>().OrderBy(s => s.RectTransform.anchoredPosition.x).ToArray();
    }
}
#endif
