#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat.Editor.Tests
{
    public sealed class RadialStunTrailTests
    {
        private CombatBoardView board;
        private CombatBulletView Chalk => AssetDatabase.LoadAssetAtPath<CombatBulletView>("Assets/_Audere/Prefabs/Combat/Bullets/Bullet_ChalkRod.prefab");
        private RadialInwardTrailMove Move => AssetDatabase.LoadAssetAtPath<RadialInwardTrailMove>(TeacherRadialTrailSetupTool.MovePath);
        [SetUp] public void Setup()
        {
            board = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>("Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            board.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
        }
        [TearDown] public void Cleanup() { if (board != null) Object.DestroyImmediate(board.gameObject); }
        private CombatMoveExecutionContext Context(int phase = 2) => new CombatMoveExecutionContext(board, null, new SystemCombatRandom(31), 101, phase);
        private CombatBulletView[] ActiveBullets() => board.GetComponentsInChildren<CombatBulletView>(true).Where(b => b.gameObject.activeSelf).ToArray();

        [Test]
        public void Ring_UniformOutsideField_FacesCenter_TelegraphsAndPauses()
        {
            var execution = Move.CreateExecution(Context()); execution.Tick(.01f);
            var bullets = ActiveBullets(); Assert.AreEqual(12, bullets.Length);
            Vector2 center = board.PlayArea.rect.center;
            var positions = bullets.Select(b => b.RectTransform.anchoredPosition).ToArray();
            float radius = (positions[0] - center).magnitude;
            for (int i = 0; i < bullets.Length; i++)
            {
                Assert.AreEqual("Exterior Projectile Root", bullets[i].transform.parent.name);
                Assert.AreEqual(radius, (positions[i] - center).magnitude, .01f);
                Assert.IsFalse(board.PlayArea.rect.Contains(positions[i]));
                Vector2 facing = bullets[i].RectTransform.localRotation * Vector3.right;
                Assert.Greater(Vector2.Dot(facing, (center - positions[i]).normalized), .999f);
                Assert.IsFalse(bullets[i].CollisionActive);
                Assert.AreEqual(0, bullets[i].GetComponent<Image>().color.a, .001f);
            }
            board.TickBullets(.55f, 1f); execution.Tick(.55f);
            Assert.AreEqual(0, board.ActiveStunTrailCount);
            foreach (var b in bullets) Assert.AreEqual(.5f, b.GetComponent<Image>().color.a, .01f);
            execution.Tick(0); board.TickBullets(0, 1f);
            CollectionAssert.AreEqual(positions, bullets.Select(b => b.RectTransform.anchoredPosition).ToArray());
            execution.Cancel(); Assert.IsEmpty(ActiveBullets());
        }

        [Test]
        public void Trail_BlocksForThreePointSixActiveSeconds_ThenFadesWithoutBlocking()
        {
            var owner = new object(); board.CatchCursor.anchoredPosition = Vector2.zero;
            board.EmitStunTrail(owner, 101, 2, Vector2.left * 90, Vector2.right * 90, 14, 3.6f, .3f);
            board.TickBullets(0, 1); Assert.IsTrue(board.IsCursorStunned);
            board.TickBullets(3.59f, 1); Assert.IsTrue(board.IsCursorStunned);
            board.TickBullets(0, 1); Assert.IsTrue(board.IsCursorStunned);
            board.TickBullets(.02f, 1); Assert.IsFalse(board.IsCursorStunned);
            Assert.AreEqual(1, board.ActiveStunTrailCount);
            board.TickBullets(.31f, 1); Assert.AreEqual(0, board.ActiveStunTrailCount);
        }

        [Test]
        public void Trail_ClippedInsideShiftedField_AndCleanupIsOwnerScoped()
        {
            board.SetBattleBoxHorizontalLayout(.65f, .8f); Canvas.ForceUpdateCanvases();
            var a = new object(); var b = new object();
            board.EmitStunTrail(a, 101, 2, new Vector2(-900, -100), new Vector2(900, 100), 14, 3.6f, .3f);
            board.EmitStunTrail(b, 102, 3, new Vector2(-900, 80), new Vector2(900, 80), 14, 3.6f, .3f);
            Assert.AreEqual(2, board.ActiveStunTrailCount);
            foreach (var zone in board.PlayArea.Find("Stun Trail Root").GetComponentsInChildren<CombatStunZoneView>())
            {
                var corners = new Vector3[4]; zone.RectTransform.GetWorldCorners(corners);
                foreach (var corner in corners) Assert.IsTrue(board.PlayArea.rect.Contains(board.PlayArea.InverseTransformPoint(corner)));
            }
            board.ClearRuntimeBullets(101, 2); Assert.AreEqual(1, board.ActiveStunTrailCount);
            board.ClearStunTrails(102, 3, a); Assert.AreEqual(1, board.ActiveStunTrailCount);
            board.ClearStunTrails(102, 3, b); Assert.AreEqual(0, board.ActiveStunTrailCount);
        }

        [TestCase(0f)] [TestCase(.5f)] [TestCase(2.5f)] [TestCase(8.5f)]
        public void Cancel_TelegraphFlightAndCompletion_ReturnHazardsAndTrails(float time)
        {
            var execution = Move.CreateExecution(Context());
            for (float elapsed = 0; elapsed < time; elapsed += .05f) { execution.Tick(.05f); board.TickBullets(.05f, 1); }
            if (time == 2.5f) Assert.Greater(board.ActiveStunTrailCount, 0, "Live inward flight must paint trails inside the field.");
            execution.Cancel(); execution.Cancel();
            execution.Tick(5f); board.TickBullets(5f, 1);
            Assert.IsEmpty(ActiveBullets()); Assert.AreEqual(0, board.ActiveStunTrailCount);
            Assert.IsFalse(board.IsCursorStunned);
            Assert.IsTrue(board.GetComponentsInChildren<CombatBulletView>(true).All(b => !b.CollisionActive));
        }

        [Test]
        public void Pool_ReturnsExteriorToNormalMask_AndStaleLeaseCannotReturnNewBullet()
        {
            var exterior = board.SpawnExteriorEnemyBullet(Chalk, new Vector2(600,0), 101, 2, 1);
            int oldLease = exterior.PoolLeaseVersion;
            exterior.FadeInDuringTelegraph(); board.ReturnEnemyBullet(exterior, oldLease);
            var normal = board.SpawnEnemyBullet(Chalk, Vector2.zero, Vector2.right, 102, 1);
            Assert.AreSame(exterior, normal); Assert.AreEqual("Bullet Root", normal.transform.parent.name);
            Assert.AreEqual(1, normal.GetComponent<Image>().color.a, .001f);
            board.ReturnEnemyBullet(exterior, oldLease);
            Assert.IsTrue(normal.gameObject.activeSelf); Assert.IsTrue(normal.CollisionActive);
            board.ClearRuntimeBullets(); Assert.IsFalse(normal.CollisionActive);
        }

        [Test]
        public void Shield_ClearStopsFutureTrailEmission_AndBoardDisableCleansExistingTrails()
        {
            var execution = Move.CreateExecution(Context()); execution.Tick(.01f);
            // Clear the warning ring using the same board API as Shield, before it fires.
            Assert.AreEqual(12, board.DestroyBulletsNearPlayer(2000f));
            for (int i = 0; i < 50; i++) { execution.Tick(.05f); board.TickBullets(.05f, 1); }
            Assert.AreEqual(0, board.ActiveStunTrailCount);
            board.EmitStunTrail(this, 101, 2, Vector2.left * 90, Vector2.right * 90, 14, 3.6f, .3f);
            // OnDisable is normally raised by Play Mode; invoke it explicitly in EditMode.
            typeof(CombatBoardView).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(board, null);
            Assert.AreEqual(0, board.ActiveStunTrailCount); Assert.IsEmpty(ActiveBullets());
            execution.Cancel();
        }

        [Test]
        public void TeacherData_FifteenHpNinetyTime_RadialAtSeven_AllChalkTrails_NoVerticalImpulse()
        {
            var encounter = AssetDatabase.LoadAssetAtPath<CombatEncounterData>("Assets/_Audere/Data/Combat/Teacher/CombatEncounter_D3_TEACHER_PRESSURE.asset");
            var enemy = encounter.EnemyDefinition;
            Assert.AreEqual(15, enemy.SharedMaxHealth); Assert.AreEqual(90, encounter.EncounterDuration);
            CollectionAssert.AreEqual(new[] { 7, 4, 0 }, Enumerable.Range(0, 3).Select(i => enemy.GetPhase(i).SharedExitThreshold));
            Assert.AreSame(Move, enemy.GetPhase(1).MoveSet.Entries[0].Move);
            var all = Enumerable.Range(0,3).SelectMany(i => enemy.GetPhase(i).MoveSet.Entries.Select(e => e.Move)).SelectMany(Flatten).ToArray();
            Assert.IsFalse(all.Any(m => m is VerticalPlayerImpulseMove));
            Assert.IsTrue(all.Any(m => m is ShiftingBattleBoxMove));
            foreach (var move in all.Where(m => m is ChalkFenceMove || m is ChalkSweepMove || m is RadialInwardTrailMove))
            {
                var trail = new SerializedObject(move).FindProperty("stunTrail");
                Assert.IsTrue(trail.FindPropertyRelative("enabled").boolValue);
                Assert.AreEqual(3.6f, trail.FindPropertyRelative("blockingDuration").floatValue, .001f);
                Assert.IsTrue(move.Validate(out string error), error);
            }
        }

        private static System.Collections.Generic.IEnumerable<CombatMoveDefinition> Flatten(CombatMoveDefinition move)
        {
            yield return move;
            if (!(move is CompositeCombatMove)) yield break;
            var children = new SerializedObject(move).FindProperty("children");
            for (int i = 0; i < children.arraySize; i++)
                foreach (var child in Flatten((CombatMoveDefinition)children.GetArrayElementAtIndex(i).objectReferenceValue)) yield return child;
        }
    }
}
#endif
