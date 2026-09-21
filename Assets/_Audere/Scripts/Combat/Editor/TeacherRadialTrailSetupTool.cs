#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat.Editor
{
    public static class TeacherRadialTrailSetupTool
    {
        public const string MovePath = "Assets/_Audere/Data/Combat/Teacher/Moves/Move_TeacherRadialInwardTrails.asset";
        private const string Folder = "Assets/_Audere/Data/Combat/Teacher/";
        private const string BoardPath = "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab";

        [MenuItem("Audere/Combat/Author Teacher Radial Trails")]
        public static void Author()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play before authoring Teacher radial trails.");
            AuthorBoardRoots();
            var radial = AssetDatabase.LoadAssetAtPath<RadialInwardTrailMove>(MovePath);
            if (radial == null)
            {
                radial = ScriptableObject.CreateInstance<RadialInwardTrailMove>();
                var so = new SerializedObject(radial);
                so.FindProperty("duration").floatValue = 8.2f;
                so.FindProperty("projectilePrefab").objectReferenceValue = Required<CombatBulletView>("Assets/_Audere/Prefabs/Combat/Bullets/Bullet_ChalkRod.prefab");
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(radial, MovePath);
            }
            EnableTrail(radial);
            var fence = Required<ChalkFenceMove>(Folder + "Moves/Move_ChalkFence.asset");
            var sweep = Required<ChalkSweepMove>(Folder + "Moves/Move_ChalkSweep.asset");
            EnableTrail(fence); EnableTrail(sweep);
            TeacherBossPolishAuthoring.Author();
            Debug.Log("[TeacherRadialTrails] Exterior roots and trails refreshed; Teacher boss balance reapplied.");
        }

        public static void EnableTrail(CombatMoveDefinition move)
        {
            var so = new SerializedObject(move); var trail = so.FindProperty("stunTrail");
            trail.FindPropertyRelative("enabled").boolValue = true;
            trail.FindPropertyRelative("blockingDuration").floatValue = 2.8f;
            so.ApplyModifiedPropertiesWithoutUndo(); if (AssetDatabase.Contains(move)) AssetDatabase.SaveAssetIfDirty(move);
        }

        private static void AuthorBoardRoots()
        {
            const string materialPath = "Assets/_Audere/Materials/UI_StunTrailDots.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Required<Material>("Assets/_Audere/Materials/UI_StunZoneDots.mat"));
                material.SetVector("_Grid", new Vector4(4, 3, 0, 0));
                material.SetColor("_DotColor", new Color(.68f, .48f, .64f, .95f));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            var root = PrefabUtility.LoadPrefabContents(BoardPath);
            try
            {
                var board = root.GetComponent<CombatBoardView>(); var so = new SerializedObject(board);
                var field = (RectTransform)so.FindProperty("playArea").objectReferenceValue;
                var exterior = root.transform.Find("Exterior Projectile Root") as RectTransform;
                if (exterior == null)
                {
                    exterior = new GameObject("Exterior Projectile Root", typeof(RectTransform)).GetComponent<RectTransform>();
                    exterior.SetParent(root.transform, false);
                    exterior.anchorMin = exterior.anchorMax = new Vector2(.5f, .5f);
                    exterior.sizeDelta = field.rect.size;
                    exterior.anchoredPosition = field.anchoredPosition;
                    exterior.SetSiblingIndex(root.transform.Find("Enemy").GetSiblingIndex());
                }
                var trails = field.Find("Stun Trail Root") as RectTransform;
                if (trails == null)
                {
                    trails = new GameObject("Stun Trail Root", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
                    trails.SetParent(field, false); trails.SetAsFirstSibling();
                    trails.anchorMin = Vector2.zero; trails.anchorMax = Vector2.one;
                    trails.offsetMin = trails.offsetMax = Vector2.zero;
                }
                so.FindProperty("exteriorProjectileRoot").objectReferenceValue = exterior;
                so.FindProperty("stunTrailRoot").objectReferenceValue = trails;
                so.FindProperty("stunTrailMaterial").objectReferenceValue = material;
                so.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(root, BoardPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static T Required<T>(string path) where T : UnityEngine.Object
        { var value = AssetDatabase.LoadAssetAtPath<T>(path); if (value == null) throw new MissingReferenceException(path); return value; }
    }
}
#endif
