#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Audere.Puzzle.PathPieces;
using UnityEditor.SceneManagement;
using UnityEngine.TestTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Audere.Puzzle.Editor.Tests
{
    public sealed class PathPiecePresentationTests
    {
        private const string HandPrefabPath = "Assets/_Audere/Prefabs/Puzzle/UI/PathPieceHandUI.prefab";
        private const string CardPrefabPath = "Assets/_Audere/Prefabs/Puzzle/UI/PathPieceCardUI.prefab";
        private const string PreviewPrefabPath = "Assets/_Audere/Prefabs/Puzzle/World/PathPreviewWorld.prefab";
        private const string GameplayUiPrefabPath = "Assets/_Audere/Prefabs/UI/GameplayUIRoot.prefab";

        [Test]
        public void SharedBottomHand_KeepsEstablishedLayout()
        {
            GameObject handPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HandPrefabPath);
            RectTransform handRect = handPrefab.GetComponent<RectTransform>();
            PathPieceHand hand = handPrefab.GetComponent<PathPieceHand>();
            SerializedObject serialized = new SerializedObject(hand);

            Assert.AreEqual(172f, handRect.sizeDelta.y, .001f);
            Assert.AreEqual(24f, handRect.anchoredPosition.y, .001f);
            Assert.AreEqual(
                new Vector2(128f, 128f),
                serialized.FindProperty("cardSize").vector2Value);
            Assert.AreEqual(24f, serialized.FindProperty("cardSpacing").floatValue, .001f);

            GameObject gameplayUi = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayUiPrefabPath);
            RectTransform sceneHandRect = gameplayUi.GetComponentInChildren<PathPieceHand>(true)
                .GetComponent<RectTransform>();
            Assert.AreEqual(190f, sceneHandRect.sizeDelta.y, .001f);
            Assert.AreEqual(28f, sceneHandRect.anchoredPosition.y, .001f);
        }

        [Test]
        public void PathPieceCard_KeepsEstablishedPresentation()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            RectTransform cardRect = prefab.GetComponent<RectTransform>();
            PathPieceCardUI card = prefab.GetComponent<PathPieceCardUI>();
            SerializedObject serialized = new SerializedObject(card);

            Assert.AreEqual(new Vector2(128f, 128f), cardRect.sizeDelta);
            Assert.AreEqual(14f, serialized.FindProperty("endpointNodeSize").floatValue, .001f);
            Assert.AreEqual(14f, serialized.FindProperty("middleNodeSize").floatValue, .001f);
            Assert.AreEqual(24f, serialized.FindProperty("nodeSpacing").floatValue, .001f);
            Assert.AreEqual(9f, serialized.FindProperty("hoverLift").floatValue, .001f);
            Assert.AreEqual(18f, serialized.FindProperty("selectedLift").floatValue, .001f);
        }

        [Test]
        public void PathHand_TraversalLockKeepsTheReservedPieceSelected()
        {
            GameObject host = new GameObject("Path Hand Selection Lock Test");
            PathPieceHand hand = host.AddComponent<PathPieceHand>();
            PathPieceData first = ScriptableObject.CreateInstance<PathPieceData>();
            PathPieceData second = ScriptableObject.CreateInstance<PathPieceData>();
            try
            {
                var pieces = (List<PathPieceData>)typeof(PathPieceHand)
                    .GetField("pieces", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(hand);
                pieces.Add(first);
                pieces.Add(second);

                hand.Select(0);
                hand.SetSelectionEnabled(false);
                hand.Select(1);
                hand.ToggleSelection(0);

                Assert.AreSame(first, hand.SelectedPiece);
                hand.SetSelectionEnabled(true);
                hand.Select(1);
                Assert.AreSame(second, hand.SelectedPiece);
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void SharedWorldPreview_KeepsEndpointsAndUsesSmallRegularConnectors()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPrefabPath);
            PathPreview preview = prefab.GetComponent<PathPreview>();
            SerializedObject serialized = new SerializedObject(preview);

            Assert.AreEqual(.82f, serialized.FindProperty("endpointScaleToBoardTile").floatValue, .001f);
            Assert.AreEqual(.14f, serialized.FindProperty("connectorScaleToBoardTile").floatValue, .001f);
            Assert.AreEqual(.25f, serialized.FindProperty("connectorSpacingToBoardTile").floatValue, .001f);
        }

        private static readonly MethodInfo RenderPreview = typeof(PathPreview).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly Vector3[] ShortPath = { Vector3.zero, new Vector3(.25f,0), new Vector3(.25f,.25f) };
        private static readonly Vector3[] LongPath = { Vector3.zero, new Vector3(.25f,0), new Vector3(.5f,0), new Vector3(.5f,.25f) };
        private static SpriteRenderer[] Connectors(PathPreview preview) => preview.GetComponentsInChildren<SpriteRenderer>()
            .Where(x=>x.name.StartsWith("Middle Tile ") && x.name!="Middle Tile Template").OrderBy(x=>x.name).ToArray();
        private static PathPreview CreatePreview() => Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPrefabPath)).GetComponent<PathPreview>();

        [TestCase(0,false)]
        [TestCase(1,false)]
        [TestCase(2,false)]
        [TestCase(3,false)]
        [TestCase(0,true)]
        [TestCase(1,true)]
        public void WorldPreview_CornerHasOneSquareAndEqualQuarterCellGaps(int turns,bool reverse)
        {
            var preview=CreatePreview();
            try
            {
                Quaternion rotation=Quaternion.Euler(0,0,turns*90f);
                Vector3[] path=ShortPath.Select(x=>rotation*x).ToArray();
                Vector3[] expected={rotation*new Vector3(.1875f,0),rotation*new Vector3(.25f,0),rotation*new Vector3(.25f,.0625f)};
                if(reverse) { System.Array.Reverse(path);System.Array.Reverse(expected); }
                preview.Show(path,.25f);RenderPreview.Invoke(preview,null);
                var squares=Connectors(preview);Assert.AreEqual(3,squares.Length);
                for(int i=0;i<squares.Length;i++)
                {
                    Assert.Less(Vector3.Distance(expected[i],squares[i].bounds.center),.0001f);
                    Assert.AreEqual(.035f,squares[i].bounds.size.x,.0001f);
                    Assert.AreEqual(.035f,squares[i].bounds.size.y,.0001f);
                    if(i>0)Assert.AreEqual(.0625f,Vector3.Distance(squares[i-1].bounds.center,squares[i].bounds.center),.0001f);
                }
            }
            finally { Object.DestroyImmediate(preview.gameObject); }
        }

        [Test]
        public void WorldPreview_NewAndReusedSquaresNeverGrowFromTemplateSize()
        {
            var preview=CreatePreview();
            try
            {
                preview.transform.localScale=new Vector3(.25f,.4f,1f);
                preview.Show(ShortPath,.25f);RenderPreview.Invoke(preview,null);
                foreach(var state in new[]{PathPreview.PresentationState.Valid,PathPreview.PresentationState.Invalid,PathPreview.PresentationState.Dangerous})
                {
                    preview.SetState(state);
                    preview.Show(LongPath,.25f);RenderPreview.Invoke(preview,null);
                    var squares=Connectors(preview);Assert.AreEqual(7,squares.Length);
                    foreach(var square in squares)
                    {
                        Assert.LessOrEqual(square.bounds.size.x,.0351f,"New pool entries must not tween from one full tile.");
                        Assert.AreEqual(squares[0].bounds.size.x,square.bounds.size.x,.0001f);
                        Assert.AreEqual(square.bounds.size.x,square.bounds.size.y,.0001f);
                    }
                    for(int i=1;i<squares.Length;i++)
                        Assert.AreEqual(.0625f,Vector3.Distance(squares[i-1].bounds.center,squares[i].bounds.center),.0001f,
                            "Changing path length must not briefly mix old and new corner positions.");
                    preview.Show(ShortPath,.25f);RenderPreview.Invoke(preview,null);Assert.AreEqual(3,Connectors(preview).Length);
                }
                preview.Clear();
                for(int i=0;i<300;i++)RenderPreview.Invoke(preview,null);
                preview.Show(LongPath,.5f);RenderPreview.Invoke(preview,null);
                var resized=Connectors(preview);Assert.AreEqual(resized[0].bounds.size.x,resized.Last().bounds.size.x,.0001f);
                Assert.LessOrEqual(resized[0].bounds.size.x,.0701f);
            }
            finally { Object.DestroyImmediate(preview.gameObject); }
        }

        [Test]
        public void WorldPreview_ShortPathsClearAndLongPathsRespectPoolBudget()
        {
            var preview=CreatePreview();
            try
            {
                preview.Show(new[]{Vector3.zero,new Vector3(.25f,0)},.25f);RenderPreview.Invoke(preview,null);
                Assert.IsEmpty(Connectors(preview),"A single-cell path has no space between the endpoint squares.");
                preview.Show(Enumerable.Range(0,50).Select(i=>Vector3.right*i*.25f).ToArray(),.25f);RenderPreview.Invoke(preview,null);
                Assert.LessOrEqual(Connectors(preview).Length,32);
            }
            finally { Object.DestroyImmediate(preview.gameObject); }
        }

        [Test]
        public void WorldPreview_ClearBeforeFirstRenderHidesEverySquare()
        {
            var preview=CreatePreview();
            try
            {
                preview.Show(ShortPath,.25f);preview.Clear();
                Assert.IsEmpty(preview.GetComponentsInChildren<SpriteRenderer>());
            }
            finally { Object.DestroyImmediate(preview.gameObject); }
        }

        [UnityTest]
        public IEnumerator WorldPreview_PlayModeRotationGrowthAndClear()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            yield return new EnterPlayMode();yield return null;
            yield return PreviewPlayChecks();
            yield return new ExitPlayMode();
        }
        private static IEnumerator PreviewPlayChecks()
        {
            var cameraObject=new GameObject("Preview QA Camera",typeof(Camera));
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=.65f;
            camera.backgroundColor=new Color(.047f,.035f,.071f);camera.clearFlags=CameraClearFlags.SolidColor;
            cameraObject.transform.position=new Vector3(.25f,.125f,-10f);
            var preview=CreatePreview();preview.SetState(PathPreview.PresentationState.Valid);
            preview.Show(ShortPath,.25f);yield return new WaitForSecondsRealtime(.25f);
            System.IO.Directory.CreateDirectory("Temp/PathPreviewPolish");
            ScreenCapture.CaptureScreenshot("Temp/PathPreviewPolish/short-even.png");
            yield return new WaitForSecondsRealtime(.12f);
            preview.Show(LongPath,.25f);yield return null;
            var squares=Connectors(preview);Assert.AreEqual(7,squares.Length);
            foreach(var square in squares)Assert.AreEqual(squares[0].bounds.size.x,square.bounds.size.x,.0001f);
            yield return new WaitForSecondsRealtime(.35f);ScreenCapture.CaptureScreenshot("Temp/PathPreviewPolish/long-even.png");
            yield return new WaitForSecondsRealtime(.12f);
            preview.Show(LongPath.Select(x=>Quaternion.Euler(0,0,90)*x).ToArray(),.25f);
            yield return new WaitForSecondsRealtime(.4f);
            foreach(var square in Connectors(preview))Assert.AreEqual(.035f,square.bounds.size.x,.001f);
            preview.Clear();yield return new WaitForSecondsRealtime(.6f);
            Assert.IsEmpty(preview.GetComponentsInChildren<SpriteRenderer>());
            Object.Destroy(preview.gameObject);Object.Destroy(cameraObject);yield return null;
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
