#if UNITY_INCLUDE_TESTS
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Audere.Puzzle.PathPieces.Editor.Tests
{
    public sealed class PathPreviewRotationTests
    {
        private const string PreviewPrefabPath =
            "Assets/_Audere/Prefabs/Puzzle/World/PathPreviewWorld.prefab";
        private static readonly MethodInfo TickPresentation = typeof(PathPreview).GetMethod(
            "TickPresentation",
            BindingFlags.Instance | BindingFlags.NonPublic);

        [Test]
        public void Rotation_UsesIntermediateAnglesThenSettlesAtTarget()
        {
            GameObject instance = Object.Instantiate(
                AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPrefabPath));
            PathPreview preview = instance.GetComponent<PathPreview>();
            var serializedPreview = new SerializedObject(preview);
            var endpointB = serializedPreview.FindProperty("endpointB")
                .objectReferenceValue as SpriteRenderer;

            try
            {
                Assert.IsNotNull(TickPresentation);
                Assert.IsNotNull(endpointB);

                Vector3[] initialPath =
                {
                    Vector3.zero,
                    new Vector3(.25f, 0f),
                    new Vector3(.25f, .25f)
                };
                Vector3[] rotatedPath =
                {
                    Vector3.zero,
                    new Vector3(0f, .25f),
                    new Vector3(-.25f, .25f)
                };

                preview.Setup();
                preview.Show(initialPath, .25f, GridRotation.Degrees0, false);
                Tick(.25f, preview);
                Vector3 start = endpointB.transform.position;

                preview.Show(rotatedPath, .25f, GridRotation.Degrees90);
                Tick(1f / 60f, preview);
                Vector3 intermediate = endpointB.transform.position;

                Assert.Greater(Vector3.Distance(intermediate, start), .0001f);
                Assert.Greater(Vector3.Distance(intermediate, rotatedPath[2]), .0001f);

                for (int i = 0; i < 90; i++)
                    Tick(1f / 60f, preview);

                Assert.That(endpointB.transform.position.x, Is.EqualTo(rotatedPath[2].x).Within(.001f));
                Assert.That(endpointB.transform.position.y, Is.EqualTo(rotatedPath[2].y).Within(.001f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void Tick(float deltaTime, PathPreview preview) =>
            TickPresentation.Invoke(preview, new object[] { deltaTime });
    }
}
#endif
