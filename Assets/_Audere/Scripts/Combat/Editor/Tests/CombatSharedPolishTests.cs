#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Audere.Combat.Editor.Tests
{
    public sealed class CombatSharedPolishTests
    {
        private const string BoardPath = "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab";
        private CombatBoardView board;

        private void CreateBoard()
        {
            board = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>(BoardPath));
            board.gameObject.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (board != null) Object.DestroyImmediate(board.gameObject);
        }

        [TestCase("Assets/_Audere/Data/Combat/Moves/Move_AimedFan.asset")]
        [TestCase("Assets/_Audere/Data/Combat/Moves/Move_Rain.asset")]
        [TestCase("Assets/_Audere/Data/Combat/Moves/Move_SideSweep.asset")]
        [TestCase("Assets/_Audere/Data/Combat/BiancaSupplies/Moves/Pattern_Bianca_OpeningBullets.asset")]
        public void LinearVolley_StartPositionsDoNotOverlap(string path)
        {
            CreateBoard();
            LinearProjectilePatternMove move = AssetDatabase.LoadAssetAtPath<LinearProjectilePatternMove>(path);
            Assert.IsNotNull(move, path);
            move.CreateExecution(new CombatMoveExecutionContext(board, null,
                new SystemCombatRandom(123), 1, 1)).Tick(.01f);
            AssertVolleySpacing(move.ProjectilesPerShot,
                move.ProjectilePrefab.GetComponent<RectTransform>().rect.width);
        }

        [TestCase("Assets/_Audere/Data/Combat/TimorNightPressure/Moves/Move_TimorNightPressure_03.asset")]
        [TestCase("Assets/_Audere/Data/Combat/BiancaSupplies/Moves/Pattern_Bianca_5.asset")]
        [TestCase("Assets/_Audere/Data/Combat/TimorReturn/FinalMoves/Move_TimorFinal_P1_SafeRain.asset")]
        public void SafeZoneRain_StartPositionsDoNotOverlap(string path)
        {
            CreateBoard();
            NarrativePressurePatternMove move = AssetDatabase.LoadAssetAtPath<NarrativePressurePatternMove>(path);
            Assert.IsNotNull(move, path);
            for (int seed = 1; seed <= 20; seed++)
            {
                move.CreateExecution(new CombatMoveExecutionContext(board, null,
                    new SystemCombatRandom(seed), 1, 1)).Tick(.01f);
                AssertVolleySpacing(Mathf.Max(3, move.Intensity),
                    move.ProjectilePrefab.GetComponent<RectTransform>().rect.width);
                board.ClearRuntimeBullets();
            }
        }

        [TestCase(7)]
        [TestCase(9)]
        public void HighIntensityFans_FitInsideBoard(int count)
        {
            Rect area = new Rect(-290f, -210f, 580f, 420f);
            foreach (Vector2 axis in new[] { Vector2.up, new Vector2(1f, 1f).normalized })
            {
                float spacing = 42f;
                Vector2 center = CombatVolleySpawnLayout.FitCenter(area,
                    new Vector2(area.xMax - 8f, area.yMax - 8f),
                    axis, count, ref spacing, 12f);
                for (int index = 0; index < count; index++)
                {
                    Vector2 point = center + axis * ((index - (count - 1) * .5f) * spacing);
                    Assert.IsTrue(area.Contains(point), $"{axis}: {point} is outside the board.");
                }
                Assert.GreaterOrEqual(spacing, 24f, $"{axis}: projectile art overlaps.");
            }
        }

        [TestCase(8)]
        [TestCase(12)]
        [TestCase(14)]
        public void RadialBurst_StartsSeparatedAndInsideBoard(int count)
        {
            Rect area = new Rect(-300f, -200f, 600f, 400f);
            float diameter = 24f;
            float radius = CombatVolleySpawnLayout.RadialRadius(count, diameter, 4f);
            Vector2 center = CombatVolleySpawnLayout.FitCircleCenter(area,
                new Vector2(area.xMax - 8f, area.yMax - 8f), radius, diameter * .5f);
            Vector2[] positions = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                positions[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                Assert.IsTrue(area.Contains(positions[i]));
                for (int previous = 0; previous < i; previous++)
                    Assert.GreaterOrEqual(Vector2.Distance(positions[i], positions[previous]),
                        diameter, $"Radial bullets {previous} and {i} overlap.");
            }
        }

        [Test]
        public void NarrativeFan_AllAuthoredWavesStartSeparatedAndInsideBoard()
        {
            CreateBoard();
            NarrativePressurePatternMove move = AssetDatabase.LoadAssetAtPath<NarrativePressurePatternMove>(
                "Assets/_Audere/Data/Combat/BiancaSupplies/Moves/Pattern_Bianca_2.asset");
            Assert.IsNotNull(move);
            ICombatMoveExecution execution = move.CreateExecution(new CombatMoveExecutionContext(
                board, null, new SystemCombatRandom(123), 1, 1));
            int authoredWaves = Mathf.CeilToInt(move.Duration / move.WaveInterval);
            for (int wave = 0; wave < authoredWaves; wave++)
            {
                execution.Tick(wave == 0 ? .01f : move.WaveInterval);
                AssertVolleySpacing(move.Intensity,
                    move.ProjectilePrefab.GetComponent<RectTransform>().rect.width);
                AssertVolleyInsideBoard();
                board.ClearRuntimeBullets();
            }
        }

        [Test]
        public void ProductionScenes_InheritSharedCombatBoard()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoardPath);
            string[] scenes =
            {
                "30_Classroom", "40_Evening", "60_D2_School_Morning",
                "120_D3_School_Teacher", "140_D4_Classroom", "150_D4_Home_Evening",
            };
            foreach (string name in scenes)
            {
                var scene = EditorSceneManager.OpenScene($"Assets/_Audere/Scenes/{name}.unity");
                CombatBoardView sceneBoard = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<CombatBoardView>(true)).Single();
                Assert.AreSame(prefab,
                    PrefabUtility.GetCorrespondingObjectFromSource(sceneBoard.gameObject), name);
                CombatBoardView sourceBoard = PrefabUtility.GetCorrespondingObjectFromSource(sceneBoard);
                string[] bossReferences =
                {
                    "authoredEnemyActor", "authoredEnemyPreview", "enemyVisual", "mechanicHint",
                };
                foreach (PropertyModification modification in
                    PrefabUtility.GetPropertyModifications(sceneBoard.gameObject) ??
                    System.Array.Empty<PropertyModification>())
                {
                    if (modification.target != sourceBoard) continue;
                    CollectionAssert.Contains(bossReferences, modification.propertyPath,
                        $"{name} overrides shared CombatBoardView polish: {modification.propertyPath}");
                }
                if (name != "150_D4_Home_Evening") continue;
                var serialized = new SerializedObject(sceneBoard);
                Assert.IsNotNull(serialized.FindProperty("authoredEnemyActor").objectReferenceValue,
                    "Timor actor was lost during prefab conversion.");
                Assert.IsNotNull(serialized.FindProperty("playerView").objectReferenceValue,
                    "Shared Heart Visual binding was lost during prefab conversion.");
            }
        }

        private void AssertVolleySpacing(int expectedCount, float bulletWidth)
        {
            CombatBulletView[] bullets = board.GetComponentsInChildren<CombatBulletView>()
                .Where(bullet => bullet.gameObject.activeSelf).ToArray();
            Assert.AreEqual(expectedCount, bullets.Length);
            for (int i = 0; i < bullets.Length; i++)
            for (int j = i + 1; j < bullets.Length; j++)
                Assert.GreaterOrEqual(Vector2.Distance(
                    bullets[i].RectTransform.anchoredPosition,
                    bullets[j].RectTransform.anchoredPosition), bulletWidth,
                    $"Bullets {i} and {j} start overlapped.");
        }

        private void AssertVolleyInsideBoard()
        {
            Rect area = board.PlayArea.rect;
            foreach (CombatBulletView bullet in board.GetComponentsInChildren<CombatBulletView>())
                if (bullet.gameObject.activeSelf)
                    Assert.IsTrue(area.Contains(bullet.RectTransform.anchoredPosition),
                        $"Bullet starts outside board at {bullet.RectTransform.anchoredPosition}.");
        }
    }
}
#endif
