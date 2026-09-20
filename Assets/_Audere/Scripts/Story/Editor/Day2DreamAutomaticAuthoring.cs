#if UNITY_EDITOR
using System;
using System.Linq;
using Audere.Puzzle;
using Audere.Puzzle.Board;
using Audere.Story;
using Audere.Story.Presentation;
using Audere.Story.Steps;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Audere.EditorTools
{
    public static class Day2DreamAutomaticAuthoring
    {
        [MenuItem("Audere/Story/Revise Dream as Automatic Run and Fall")]
        public static void Author()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            Scene scene = SceneManager.GetSceneByPath(Day2NightDreamSetupTool.DreamPath);
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(Day2NightDreamSetupTool.DreamPath, OpenSceneMode.Additive);
            if (scene.isDirty) throw new InvalidOperationException("Save the dream scene before applying this scoped revision.");
            var e = All<StoryEvent>(scene).Single();
            if (e.transform.Find("100_AudereRunsThenSlows") != null)
            {
                RefineFall(scene, e);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                return;
            }
            // Preserve the former puzzle authoring as inactive content; only the new story steps execute.
            foreach (Transform child in e.transform)
                if (child.name == "010_PrepareContinuousPath" ||
                    (int.TryParse(child.name.Substring(0, 3), out int order) && order >= 100 && order <= 260))
                    child.gameObject.SetActive(false);
            var atmosphere = All<DreamAtmosphereView>(scene).Single();
            var atmosphereData = new SerializedObject(atmosphere);
            var start = (Transform)atmosphereData.FindProperty("playerStart").objectReferenceValue;
            var actor = All<GridPlayer>(scene).Single();
            actor.enabled = false;
            actor.CancelMotion();
            var body = actor.GetComponent<SpriteRenderer>();
            var shadow = actor.GetComponentsInChildren<SpriteRenderer>(true).Single(x => x != body && x.sortingOrder == 4);
            var tiles = All<BoardTile>(scene).GroupBy(x => x.GridPosition.x).OrderBy(x => x.Key).Select(g => g.First()).ToArray();
            var levels = All<PuzzleController>(scene);
            foreach (var level in levels) level.PuzzleRoot.gameObject.SetActive(true);
            foreach (var duplicate in All<BoardTile>(scene).Except(tiles)) duplicate.gameObject.SetActive(false);
            foreach (var runtime in All<PuzzleRuntime>(scene)) runtime.gameObject.SetActive(false);
            foreach (var controller in levels) controller.enabled = false;
            foreach (var manager in All<PuzzleManager>(scene)) manager.enabled = false;
            foreach (var coordinator in All<PuzzleRootCoordinator>(scene)) coordinator.enabled = false;
            shadow.transform.position += new Vector3(tiles[0].transform.position.x - shadow.bounds.center.x,
                tiles[0].transform.position.y - shadow.bounds.center.y, 0f);
            body.sortingLayerName = shadow.sortingLayerName = "Player";
            body.sortingOrder = 5; shadow.sortingOrder = 4;

            var anchors = Child(start.parent, "Dream Automatic Route");
            var waypoints = new Transform[15];
            for (int i = 0; i < 14; i++)
            {
                waypoints[i] = Child(anchors, "Audere_Step_" + (i + 1).ToString("00"));
                waypoints[i].position = start.position + tiles[i + 1].transform.position - tiles[0].transform.position;
            }
            waypoints[14] = Child(anchors, "Audere_ReachingForFinalTile");
            waypoints[14].position = Vector3.Lerp(waypoints[13].position,
                start.position + tiles[15].transform.position - tiles[0].transform.position, .36f);
            var fallTarget = Child(anchors, "Audere_InTheVoid");
            fallTarget.position = waypoints[14].position + new Vector3(.16f, -2.4f, 0f);

            Set(Step<SetActiveStep>(e, "008_RestoreGroundedShadow"), "objectsToEnable", new Object[] { shadow.gameObject });
            Set(Step<MoveActorStep>(e, "009_AudereAtDreamStart"), "actor", actor.transform, "targetTransform", start, "duration", 0f);
            Set(e.transform.Find("020_BeginDreamDrift").GetComponent<DreamAtmosphereStep>(), "showPuzzleUi", false);
            Set(Step<DreamWalkStep>(e, "100_AudereRunsThenSlows"), "actor", actor.transform, "actorRenderer", body,
                "groundedShadow", shadow.transform, "waypoints", waypoints, "atmosphere", atmosphere);

            var shardRoot = Child(atmosphere.transform, "Last Tile Fragments - Runtime Presentation");
            var fragments = shardRoot.gameObject.AddComponent<DreamTileFragments>();
            var lastTile = tiles[15].GetComponentInChildren<SpriteRenderer>(true);
            Set(fragments, "owner", e, "tile", lastTile, "tileLayers", tiles[15].GetComponentsInChildren<SpriteRenderer>(true));
            Set(Step<SetActiveStep>(e, "249_NoGroundedShadowInTheVoid"), "objectsToDisable", new Object[] { shadow.gameObject });
            Set(Step<DreamFallStep>(e, "250_LastTileShattersAudereFalls"), "actor", actor.transform,
                "fallTarget", fallTarget, "fragments", fragments, "atmosphere", atmosphere);

            // Design Intent: intrusive dream text, never attributed to Bianca or promoted to character canon.
            string[][] phrases = {
                new[] { "Chạy đi.", "Nhanh lên.", "Đừng đứng lại.", "Sắp muộn rồi.", "Họ đang đợi.", "Còn một đoạn nữa.", "Đừng nhìn lại.", "Đi tiếp đi." },
                new[] { "Lại phải chờ cậu.", "Sao chậm thế?", "Có mỗi việc này thôi mà.", "Lại phải sửa cho cậu.", "Đừng làm sai nữa.", "Ai cũng xong rồi.", "Cậu định đứng đó mãi à?", "Lúc nào cũng phải nhắc." },
                new[] { "Biết ngay lại phải làm hộ.", "Cậu chỉ làm mọi thứ rối thêm.", "Đừng giả vờ là mình giúp được.", "Ai nhờ cậu đến?", "Lại cái vẻ mặt đó.", "Chẳng ai muốn nghe đâu.", "Đến nói cũng không xong.", "Họ cười cậu đấy." },
                new[] { "Con nhỏ phiền phức.", "Không có cậu còn dễ hơn.", "Cậu tưởng họ cần cậu à?", "Họ chỉ thương hại thôi.", "Câm đi.", "Đừng bám theo nữa.", "Đừng làm bộ đáng thương.", "Chẳng ai chờ cậu cả." },
                new[] { "CÚT ĐI.", "KHÔNG AI MUỐN CẬU Ở ĐÂY.", "ĐỒ VÔ DỤNG.", "ĐỪNG LÀM PHIỀN NỮA.", "CẬU CHỈ LÀ GÁNH NẶNG.", "CHẲNG AI CẦN CẬU.", "BIẾN ĐI.", "MỞ MIỆNG RA LÀ HỎNG HẾT." }
            };
            Transform textRoot = atmosphere.transform.Find("Dream Murmurs - NOT Bianca Dialogue");
            var old = textRoot.GetComponentsInChildren<TextMeshPro>(true);
            var texts = new TMP_Text[64];
            var thresholds = new float[64];
            for (int i = 0; i < texts.Length; i++)
            {
                var text = i < old.Length ? old[i] : Object.Instantiate(old[0], textRoot);
                text.name = "Murmur " + i.ToString("00");
                int band = i < 8 ? 0 : i < 20 ? 1 : i < 34 ? 2 : i < 48 ? 3 : 4;
                text.text = phrases[band][(i - (band == 0 ? 0 : band == 1 ? 8 : band == 2 ? 20 : band == 3 ? 34 : 48)) % 8];
                text.fontSize = .82f + (i % 4) * .08f;
                text.rectTransform.sizeDelta = new Vector2(3.2f, .32f);
                text.rectTransform.anchoredPosition3D = new Vector3(-2.15f + (i % 8) * .61f,
                    -1.05f + ((i * 5) % 11) * .205f, 2f);
                text.transform.localRotation = Quaternion.Euler(0, 0, (i % 5 - 2) * 5f);
                text.color = new Color(.72f, .55f, .68f, .24f);
                thresholds[i] = band == 0 ? -.12f : band == 1 ? .14f : band == 2 ? .36f : band == 3 ? .57f : .77f;
                texts[i] = text;
            }
            Set(atmosphere, "murmurs", texts, "murmurThresholds", thresholds);
            var ordered = e.transform.Cast<Transform>().OrderBy(x => x.name, StringComparer.Ordinal).ToArray();
            foreach (var child in ordered) child.SetAsLastSibling();
            RefineFall(scene, e);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void RefineFall(Scene scene, StoryEvent e)
        {
            var walk = All<DreamWalkStep>(scene).Single();
            var actor = walk.Actor;
            var shadow = actor.GetComponentsInChildren<SpriteRenderer>(true).Single(x => x.sortingOrder == 4).transform;
            var atmosphere = All<DreamAtmosphereView>(scene).Single();
            var start = (Transform)new SerializedObject(atmosphere).FindProperty("playerStart").objectReferenceValue;
            var fall = All<DreamFallStep>(scene).Single();
            var target = (Transform)new SerializedObject(fall).FindProperty("fallTarget").objectReferenceValue;
            var last = fall.Fragments.Tile;
            var anchors = walk.Waypoints.Last().parent;
            float stride = walk.Waypoints[13].position.x - walk.Waypoints[12].position.x;
            walk.Waypoints[14].position = new Vector3(last.transform.position.x - stride * .06f, start.position.y, start.position.z);
            target.position = walk.Waypoints[14].position + new Vector3(.12f, -2.4f, 0f);
            target.rotation = start.rotation;
            Set(fall, "breakLead", .07f, "backwardLean", 78f);

            // More path remains ahead: the failing tile must not advertise itself as the end of the road.
            var pathRoot = actor.parent;
            var continuation = pathRoot.Find("Dream Path Continues Beyond Break - Scenery");
            if (continuation == null)
            {
                continuation = Child(pathRoot, "Dream Path Continues Beyond Break - Scenery");
                for (int i = 1; i <= 8; i++)
                {
                    var visual = Object.Instantiate(last.gameObject, continuation).transform;
                    visual.name = "Intact Tile Beyond Break " + i.ToString("00");
                    visual.position = last.transform.position + Vector3.right * (stride * i);
                    visual.rotation = last.transform.rotation;
                    Vector3 parentScale = continuation.lossyScale;
                    visual.localScale = new Vector3(last.transform.lossyScale.x / parentScale.x,
                        last.transform.lossyScale.y / parentScale.y, last.transform.lossyScale.z / parentScale.z);
                }
                var so = new SerializedObject(atmosphere);
                var paths = so.FindProperty("pathRenderers");
                int oldSize = paths.arraySize;
                var renderers = continuation.GetComponentsInChildren<SpriteRenderer>(true);
                paths.arraySize += renderers.Length;
                for (int i = 0; i < renderers.Length; i++) paths.GetArrayElementAtIndex(oldSize + i).objectReferenceValue = renderers[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var shadowStart = anchors.Find("Shadow_Start");
            if (shadowStart == null) shadowStart = Child(anchors, "Shadow_Start");
            // This shadow art has an off-center pivot. Ground the visible ellipse, not its Transform.
            var shadowRenderer = shadow.GetComponent<SpriteRenderer>();
            Vector3 shadowCenterOffset = shadow.TransformVector(shadowRenderer.sprite.bounds.center);
            shadowStart.position = new Vector3(start.position.x - shadowCenterOffset.x,
                last.transform.position.y - shadowCenterOffset.y, shadow.position.z);
            shadowStart.rotation = shadow.rotation;
            shadow.position = shadowStart.position + actor.position - start.position;
            var shadowEnd = anchors.Find("Shadow_VoidPose");
            if (shadowEnd == null) shadowEnd = Child(anchors, "Shadow_VoidPose");
            shadowEnd.position = target.position + shadowStart.position - start.position;
            shadowEnd.rotation = shadowStart.rotation;
            var normalize = e.transform.Find("009_AudereAtDreamStart");
            var move = normalize.GetComponent<MoveActorStep>();
            if (move != null) Object.DestroyImmediate(move);
            var pose = normalize.GetComponent<CharacterPoseStep>() ?? normalize.gameObject.AddComponent<CharacterPoseStep>();
            Set(pose, "actor", actor, "targetPose", start, "groundedShadow", shadow, "shadowAnchor", shadowStart, "duration", 0f);
            // There is no floor in the void. Keep the completed backward fall pose through Timor's lines.
            var recover = e.transform.Find("275_AudereTurnsTowardTimorsVoice");
            if (recover != null) recover.gameObject.SetActive(false);
            e.transform.Find("280_AudereStartlesInPlace").gameObject.SetActive(false);
            var wind = All<FallingWindView>(scene).SingleOrDefault();
            if (wind == null) wind = Child(atmosphere.transform, "Falling Wind - Through Timor Dialogue").gameObject.AddComponent<FallingWindView>();
            var camera = (Camera)new SerializedObject(atmosphere).FindProperty("worldCamera").objectReferenceValue;
            var mask = camera.transform.Find("PuzzleViewportMask");
            var edges = new[] { "Mask Top", "Mask Bottom", "Mask Left", "Mask Right" }
                .Select(name => mask.Find(name).GetComponent<SpriteRenderer>()).ToArray();
            Set(wind, "owner", e, "worldCamera", camera, "apertureEdges", edges,
                "profile", AssetDatabase.LoadAssetAtPath<FallingRoomProfile>("Assets/_Audere/Data/Transitions/FallingRoom_Classroom.asset"));
            Set(fall, "owner", e, "wind", wind);
            foreach (var child in e.transform.Cast<Transform>().OrderBy(x => x.name, StringComparer.Ordinal).ToArray()) child.SetAsLastSibling();
        }

        private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<T>(true)).ToArray();
        private static Transform Child(Transform parent, string name)
        { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }
        private static T Step<T>(StoryEvent e, string name) where T : StoryStep => Child(e.transform, name).gameObject.AddComponent<T>();
        private static void Set(Object target, params object[] values)
        {
            var so = new SerializedObject(target);
            for (int i = 0; i < values.Length; i += 2)
            {
                var p = so.FindProperty((string)values[i]); var value = values[i + 1];
                if (p == null) throw new InvalidOperationException("Missing field " + values[i]);
                if (value is Object[] array)
                { p.arraySize = array.Length; for (int j = 0; j < array.Length; j++) p.GetArrayElementAtIndex(j).objectReferenceValue = array[j]; }
                else if (value is float[] floats)
                { p.arraySize = floats.Length; for (int j = 0; j < floats.Length; j++) p.GetArrayElementAtIndex(j).floatValue = floats[j]; }
                else if (value is bool flag) p.boolValue = flag;
                else if (value is float number) p.floatValue = number;
                else p.objectReferenceValue = (Object)value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
