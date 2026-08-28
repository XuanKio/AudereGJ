#if UNITY_EDITOR
using Audere.Puzzle.PathPieces;
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
        public void SharedWorldPreview_UsesDenserSmallerEndpointRatio()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPrefabPath);
            PathPreview preview = prefab.GetComponent<PathPreview>();
            SerializedObject serialized = new SerializedObject(preview);

            Assert.AreEqual(.82f, serialized.FindProperty("endpointScaleToBoardTile").floatValue, .001f);
            Assert.AreEqual(.22f, serialized.FindProperty("connectorScaleToBoardTile").floatValue, .001f);
            Assert.AreEqual(.28f, serialized.FindProperty("connectorSpacingToBoardTile").floatValue, .001f);
        }
    }
}
#endif
