#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Audere.Combat.Editor.Tests
{
    public sealed class CombatFollowingVfxTests
    {
        [Test]
        public void ScratchFollowsEveryAuthoredEnemyWithoutChangingSizeAndCleansUp()
        {
            const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            var boardPrefab = AssetDatabase.LoadAssetAtPath<CombatBoardView>(
                "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab");
            Assert.IsNotNull(boardPrefab);
            string[] enemyPaths = AssetDatabase.FindAssets("t:Prefab",
                    new[] { "Assets/_Audere/Prefabs/Combat/Enemies" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToArray();
            Assert.IsNotEmpty(enemyPaths);
            MethodInfo playScratch = typeof(CombatBoardView).GetMethod("PlayAttackHitVfxRoutine", privateInstance);
            MethodInfo prepareFlash = typeof(CombatBoardView).GetMethod("PrepareEnemyFlash", privateInstance);
            FieldInfo flashes = typeof(CombatBoardView).GetField("enemySpriteRenderers", privateInstance);
            FieldInfo activeEffects = typeof(CombatBoardView).GetField("activeHitVfx", privateInstance);
            Assert.IsNotNull(playScratch);
            Assert.IsNotNull(prepareFlash);
            Assert.IsNotNull(flashes);
            Assert.IsNotNull(activeEffects);

            foreach (string path in enemyPaths)
            {
                var actorPrefab = AssetDatabase.LoadAssetAtPath<CombatEnemyActor>(path);
                Assert.IsNotNull(actorPrefab, path);
                CombatBoardView board = Object.Instantiate(boardPrefab);
                try
                {
                    board.gameObject.SetActive(true);
                    board.PrepareEncounter("Following VFX test");
                    var serialized = new SerializedObject(board);
                    var mount = (Transform)serialized.FindProperty("enemyMount").objectReferenceValue;
                    var authoredVfxRoot = (Transform)serialized.FindProperty("vfxRoot").objectReferenceValue;
                    float calibratedScale = serialized.FindProperty("enemyScratchVfxUiScale").floatValue;
                    Assert.IsNotNull(mount, path);
                    Assert.IsNotNull(authoredVfxRoot, path);
                    var actor = Object.Instantiate(actorPrefab, mount);
                    actor.transform.localScale *= 1.37f;
                    Vector3 actorScale = actor.transform.localScale;
                    board.BindAuthoredEnemyActor(actor);
                    Assert.AreSame(actor, board.SpawnEnemyActor(actorPrefab, 717), path);
                    var visual = actor.VisualRoot != null ? actor.VisualRoot : actor.transform;
                    Vector3 visualScale = visual.localScale;
                    Vector3 expectedWorldScale = authoredVfxRoot.lossyScale * Mathf.Max(1f, calibratedScale);

                    var routine = (IEnumerator)playScratch.Invoke(board, null);
                    Assert.IsTrue(routine.MoveNext(), path);
                    var effects = (IList)activeEffects.GetValue(board);
                    Assert.AreEqual(1, effects.Count, path);
                    var scratch = (GameObject)effects[0];
                    Assert.IsTrue(scratch.transform.IsChildOf(visual), path + ": scratch must follow the visual hierarchy.");
                    Transform effectAnchor = scratch.transform.parent;
                    Assert.IsTrue(effectAnchor == actor.VfxAnchor || effectAnchor.IsChildOf(visual), path);
                    Assert.Less(Vector3.Distance(expectedWorldScale, scratch.transform.lossyScale), .001f,
                        path + ": parenting must preserve the calibrated Canvas-to-sprite size.");
                    SpriteRenderer[] scratchRenderers = scratch.GetComponentsInChildren<SpriteRenderer>(true);
                    Assert.IsNotEmpty(scratchRenderers, path);
                    Bounds bounds = scratchRenderers[0].bounds;
                    foreach (SpriteRenderer renderer in scratchRenderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                    Assert.Less(Vector2.Distance(bounds.center, effectAnchor.position), .001f, path + ": scratch must start centered on its anchor.");

                    Vector3 before = scratch.transform.position;
                    Vector3 visualShift = new Vector3(37f, -29f, 0f);
                    Vector3 expectedShift = visual.parent.TransformVector(visualShift);
                    visual.localPosition += visualShift;
                    Assert.Less(Vector3.Distance(before + expectedShift, scratch.transform.position), .001f,
                        path + ": later visual movement must carry the effect.");
                    before = scratch.transform.position;
                    Vector3 mountShift = new Vector3(-61f, -103f, 0f);
                    expectedShift = mount.parent.TransformVector(mountShift);
                    mount.localPosition += mountShift;
                    Assert.Less(Vector3.Distance(before + expectedShift, scratch.transform.position), .001f,
                        path + ": later Enemy Mount movement must carry the effect.");
                    Assert.Less(Vector3.Distance(expectedWorldScale, scratch.transform.lossyScale), .001f, path);
                    Assert.AreEqual(actorScale, actor.transform.localScale, path);
                    Assert.AreEqual(visualScale, visual.localScale, path);

                    prepareFlash.Invoke(board, null);
                    var flashRenderers = (SpriteRenderer[])flashes.GetValue(board);
                    Assert.IsNotNull(flashRenderers, path);
                    foreach (SpriteRenderer renderer in scratchRenderers)
                        CollectionAssert.DoesNotContain(flashRenderers, renderer, path + ": hit flash must not recolor scratch VFX.");
                    serialized.Update();
                    Assert.AreSame(authoredVfxRoot, serialized.FindProperty("vfxRoot").objectReferenceValue, path);

                    board.ClearEnemyActor();
                    Assert.IsTrue(scratch == null, path + ": clearing the enemy must destroy its active scratch.");
                    Assert.AreEqual(0, effects.Count, path);
                    Assert.IsNotNull(actor, path + ": the authored actor must survive cleanup.");
                    Assert.IsFalse(actor.gameObject.activeSelf, path);
                    Assert.IsFalse(actor.GetComponentsInChildren<Transform>(true)
                        .Any(child => child.name == "Following VFX Anchor (runtime)"), path);
                    serialized.Update();
                    Assert.AreSame(authoredVfxRoot, serialized.FindProperty("vfxRoot").objectReferenceValue, path);
                }
                finally
                {
                    Object.DestroyImmediate(board.gameObject);
                }
            }
        }
    }
}
#endif
