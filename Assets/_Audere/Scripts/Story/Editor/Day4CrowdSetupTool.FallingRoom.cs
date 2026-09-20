#if UNITY_EDITOR
using System;
using System.Linq;
using Audere.Dialogue;
using Audere.Story.Presentation;
using Audere.Story.Steps;
using Audere.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Audere.Story.Editor
{
    public static partial class Day4CrowdSetupTool
    {
        public const string FallingRoomProfilePath = "Assets/_Audere/Data/Transitions/FallingRoom_Classroom.asset";

        [MenuItem("Audere/Story/Apply Day4 Falling Room")]
        public static void ApplyFallingRoomActive()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || scene.isDirty || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Open saved Scene140 in ready Edit Mode first.");
            var story = All<StoryEvent>(scene).Single(x => x.EventId == "D4_CLASSROOM_CROWD");
            var fallen = story.transform.Find("050_AudereFalls").GetComponent<CharacterPoseStep>();
            var actor = fallen.Actor;
            var shadow = Shadow(actor);
            var stage = All<Transform>(scene).Single(x => x.name == "DAY FOUR TILE CLASSROOM");
            var world = All<WorldModeController>(scene).Single();
            var camera = (Camera)new SerializedObject(world).FindProperty("worldCamera").objectReferenceValue;
            var mask = (GameObject)new SerializedObject(world).FindProperty("puzzleViewportMask").objectReferenceValue;
            var profile = New<FallingRoomProfile>(FallingRoomProfilePath);
            Save(profile, FallingRoomProfilePath);
            var effect = Step<FallingRoomStep>(story.transform, "065_TheFloorFallsAway");
            var continuation = Component<StoryEvent>(Child(effect.transform, "Falling room sequence"));
            Set(continuation, "eventId", "D4_CROWD_FALLING_ROOM", "autoPlayNextEvent", false);
            var anchor = Anchor(fallen.TargetPose.parent, "Audere Falling Into Pressure",
                fallen.TargetPose.position + Vector3.down * .18f, Quaternion.Euler(0, 0, 72));
            var floorAnchor = (Transform)new SerializedObject(fallen).FindProperty("shadowAnchor").objectReferenceValue;
            Pose(continuation.transform, "010_AudereSinks", actor, anchor, shadow, floorAnchor, 1.6f);
            Wait(continuation.transform, "020_OnlyTheFall", .5f);
            Talk(continuation.transform, "030_WordsInTheDark", D("FALLING", DialogueCharacterId.None, "Audere_Scared.png", null,
                "L|Đừng nhìn tớ lúc này…", "L|Tớ chỉ muốn mang đồ tới thôi…"));
            Set(continuation.transform.Find("030_WordsInTheDark").GetComponent<DialogueStep>(),
                "dialogueController", All<DialogueController>(scene).Single());
            var transition = All<FullscreenWorldModeTransitionStep>(scene).Single(x => x.name == "070_TheRoomBecomesPressure");
            transition.transform.SetParent(continuation.transform, true);
            Set(effect, "profile", profile, "worldCamera", camera, "worldMode", world,
                "actor", actor, "groundedShadow", shadow.GetComponent<SpriteRenderer>(), "sequence", continuation,
                "tiles", stage.Cast<Transform>().Select(x => x.GetComponent<SpriteRenderer>()).Where(x => x != null).Cast<Object>().ToArray(),
                "furniture", stage.GetComponentsInChildren<Transform>(true).Where(x => x.name == "Desk centered on tile").Cast<Object>().ToArray(),
                "masks", mask.GetComponentsInChildren<SpriteRenderer>(true).Cast<Object>().ToArray());
            var thought = story.transform.Find("060_TheyMustBeLaughing").GetComponent<DialogueStep>().DialogueData;
            var so = new SerializedObject(thought);
            var lines = so.FindProperty("lines");
            lines.GetArrayElementAtIndex(lines.arraySize - 1).FindPropertyRelative("text").stringValue = "Mọi người… đừng cười mà.";
            so.ApplyModifiedPropertiesWithoutUndo();
            Order(continuation.transform); Order(story.transform);
            AssetDatabase.SaveAssetIfDirty(profile);
            AssetDatabase.SaveAssetIfDirty(thought);
            AssetDatabase.SaveAssetIfDirty(continuation.transform.Find("030_WordsInTheDark").GetComponent<DialogueStep>().DialogueData);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif
