#if UNITY_EDITOR
using System;
using System.Linq;
using Audere.Combat;
using Audere.EditorTools;
using Audere.Dialogue;
using Audere.Story.Presentation;
using Audere.Story.Steps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Audere.Story.Editor
{
    // Scoped, repeatable migration. Never regenerate Teacher combat or other story scenes.
    public static class TeacherAfterCombatSetupTool
    {
        public const string DialogueFolder = "Assets/_Audere/Data/Dialogue/Day3/TeacherAfterCombat";

        [MenuItem("Audere/Story/Apply Teacher After Combat To Active Scene")]
        public static void AuthorActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || EditorApplication.isCompiling ||
                scene.path != Day3SchoolSetupTool.TeacherPath || scene.isDirty)
                throw new InvalidOperationException("Open the saved Teacher scene in Edit Mode first.");

            var story = Root(scene, "STORY").transform;
            var main = story.GetComponentsInChildren<StoryEvent>(true)
                .Single(x => x.EventId == "D3_TEACHER_CHECK_IN_PRESSURE").transform;
            var world = Root(scene, "WORLD").transform;
            var audere = world.GetComponentsInChildren<Transform>(true).Single(x => x.name == "Audere");
            var teacher = world.GetComponentsInChildren<Transform>(true).Single(x => x.name == "Teacher PLACEHOLDER");
            var anchors = world.Find("STAGING TARGETS");
            var startA = anchors.Find("Audere_Opening");
            var startT = anchors.Find("Teacher_FacesAudere");
            var tileA = audere.parent.Find("Start Tile");
            var tileT = teacher.parent.Find("Teacher Tile");

            // One tile pitch apart, centered in the existing stage. Keep authored feet offsets.
            float pitch = Mathf.Abs(tileA.lossyScale.x);
            float center = (tileA.position.x + tileT.position.x) * .5f;
            float ax = center - pitch * .5f, tx = center + pitch * .5f;
            SetX(tileA, ax); SetX(tileT, tx);
            SetX(audere, ax); SetX(teacher, tx); SetX(startA, ax); SetX(startT, tx);
            var hugA = Anchor(anchors, "Audere_AcceptsComfort", startA.position + Vector3.right * .025f);
            var hugT = Anchor(anchors, "Teacher_GentleComfort", startT.position + Vector3.left * .10f);

            var reassurance = Dialogue("REASSURANCE", "R|Audere, em không cần trả lời ngay.",
                "R|Cô chỉ muốn biết em có ổn không.", "R|Cô không trách em.");
            var replies = new[] {
                Dialogue("REPLY_WORRIED", "L|Em cứ thấy rất lo…", "L|Em sợ mình làm mọi người thất vọng.", "R|Cảm giác đó chắc hẳn rất mệt."),
                Dialogue("REPLY_TIRED", "L|Em cảm thấy mệt ạ.", "L|Lần đầu em tham gia cùng mọi người…", "L|Em cứ sợ mình không theo kịp.", "R|Cảm giác đó chắc hẳn rất mệt."),
                Dialogue("REPLY_SLEEP", "L|Em ổn ạ. Chắc chỉ do thiếu ngủ.", "R|Ừ. Thiếu ngủ cũng mệt lắm.")
            };
            var support = Dialogue("SUPPORT", "R|Em không phải tự lo hết mọi thứ đâu.", "R|Hôm nay, cứ nghỉ một chút đã nhé.");
            var ask = Dialogue("ASK_HUG", "R|Cô ôm em một chút được không?");
            var consent = Dialogue("ACCEPT_HUG", "L|…Dạ.");
            var encounter = AssetDatabase.LoadAssetAtPath<CombatEncounterData>(Day3SchoolSetupTool.EncounterPath);
            Set(encounter, "victoryPresentation.dialogue", reassurance,
                "victoryPresentation.hazardFadeDuration", .45f, "victoryFadeDuration", .4f);
            AssetDatabase.SaveAssetIfDirty(encounter);

            var choices = Choices(scene);
            var branchesRoot = Child(story, "TEACHER REPLY BRANCHES");
            string[] ids = { "WORRIED", "TIRED", "SLEEP" };
            var branches = new StoryEvent[3];
            for (int i = 0; i < branches.Length; i++)
            {
                var branch = Child(branchesRoot, "D3_TEACHER_REPLY_" + ids[i]);
                branches[i] = Component<StoryEvent>(branch);
                Set(branches[i], "eventId", branch.name, "autoPlayNextEvent", false);
                Set(Step<DialogueStep>(branch, "010_AudereAnswers"), "dialogueData", replies[i]);
            }

            // Normalize under the existing neutral cover before revealing the adjacent tiles.
            Set(Step<MoveActorStep>(main, "112_ResetAudereAfterCombat"), "actor", audere, "targetTransform", startA, "duration", 0f);
            Set(Step<MoveActorStep>(main, "114_ResetTeacherAfterCombat"), "actor", teacher, "targetTransform", startT, "duration", 0f);
            Face(Step<SetActorFacingStep>(main, "116_AudereFacesTeacher"), audere, true);
            Face(Step<SetActorFacingStep>(main, "118_TeacherFacesAudere"), teacher, false);
            var oldWait = main.Find("130_AftermathUnresolved");
            if (oldWait != null) oldWait.name = "130_RoomToAnswer";
            Set(Step<WaitStep>(main, "130_RoomToAnswer"), "duration", .35f);
            Set(Step<StoryChoiceBranchStep>(main, "140_AudereChoosesHerAnswer"), "choiceView", choices,
                "options", new[] { "Em cứ thấy rất lo…", "Em cảm thấy mệt ạ…", "Em ổn ạ. Chắc chỉ do thiếu ngủ." },
                "branches", branches.Cast<Object>().ToArray());
            Set(Step<DialogueStep>(main, "150_NoNeedToSolveEverythingToday"), "dialogueData", support);
            Set(Step<WaitStep>(main, "160_LeaveSomeQuiet"), "duration", .45f);
            Set(Step<DialogueStep>(main, "170_TeacherAsksPermission"), "dialogueData", ask);
            Set(Step<WaitStep>(main, "175_WaitForAudere"), "duration", .3f);
            Set(Step<DialogueStep>(main, "180_AudereAccepts"), "dialogueData", consent);
            Motion(Step<CharacterMotionStep>(main, "190_TeacherMovesCloser"), teacher, hugT, .55f, 0f);
            Motion(Step<CharacterMotionStep>(main, "195_AudereLeansIn"), audere, hugA, .3f, .004f);
            Set(Step<WaitStep>(main, "200_QuietEmbrace"), "duration", 1.6f);
            Motion(Step<CharacterMotionStep>(main, "210_AudereSettles"), audere, startA, .3f, 0f);
            Motion(Step<CharacterMotionStep>(main, "220_TeacherGivesSpace"), teacher, startT, .5f, 0f);
            Set(Step<WaitStep>(main, "230_StayBesideHer"), "duration", .7f);
            // Normalize facing on replay too, without changing surrounding dialogue.
            Face(Step<SetActorFacingStep>(main, "007_AudereFacesTeacher"), audere, true);
            foreach (var t in main.Cast<Transform>().OrderBy(t => t.name, StringComparer.Ordinal).ToArray()) t.SetAsLastSibling();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void Motion(CharacterMotionStep step, Transform actor, Transform target, float duration, float arc)
        {
            var shadow = actor.GetComponentsInChildren<SpriteRenderer>(true).Single(x => x.sortingOrder == 4).transform;
            Set(step, "actor", actor, "actorRenderer", actor.GetComponent<SpriteRenderer>(), "groundedShadow", shadow,
                "targetTransform", target, "duration", duration, "arcHeight", arc, "travelStretch", 0f,
                "landingDuration", 0f, "landingSquash", 0f, "landingWiden", 0f, "facingMode", 0, "useUnscaledTime", true);
        }
        private static void Face(SetActorFacingStep step, Transform actor, bool right) =>
            Set(step, "actorRenderer", actor.GetComponent<SpriteRenderer>(), "faceRight", right, "sourceSpriteFacesLeft", true);
        private static void SetX(Transform t, float x) { var p = t.position; p.x = x; t.position = p; }
        private static Transform Anchor(Transform root, string name, Vector3 position) { var t = Child(root, name); t.position = position; return t; }
        private static T Step<T>(Transform parent, string name) where T : StoryStep => Component<T>(Child(parent, name));
        private static T Component<T>(Transform t) where T : Component { var c = t.GetComponent<T>(); return c != null ? c : t.gameObject.AddComponent<T>(); }
        private static Transform Child(Transform parent, string name)
        { var t = parent.Find(name); if (t != null) return t; t = new GameObject(name).transform; t.SetParent(parent, false); return t; }
        private static GameObject Root(Scene s, string name) => s.GetRootGameObjects().Single(x => x.name == name);
        private static Sprite Portrait(string path, string spriteName) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single(x => x.name == spriteName);

        private static DialogueData Dialogue(string suffix, params string[] lines)
        {
            if (!AssetDatabase.IsValidFolder(DialogueFolder)) AssetDatabase.CreateFolder("Assets/_Audere/Data/Dialogue/Day3", "TeacherAfterCombat");
            string id = "D3_TEACHER_AFTER_" + suffix, path = DialogueFolder + "/Dialogue_" + id + ".asset";
            var data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (data == null) { data = ScriptableObject.CreateInstance<DialogueData>(); AssetDatabase.CreateAsset(data, path); }
            Set(data, "dialogueId", id, "leftCharacter", (int)DialogueCharacterId.Audere, "rightCharacter", (int)DialogueCharacterId.Teacher,
                "leftPortraitOverride", Portrait("Assets/_Audere/AssetGame/Audere/Audere_Tired.png", "Audere_Tired_0"),
                "rightPortraitOverride", Portrait("Assets/_Audere/AssetGame/Giáo viên/Co_giao.png", "Co_giao_0"));
            var so = new SerializedObject(data); var a = so.FindProperty("lines"); a.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                string text = lines[i].Substring(2); if (text.Length > 42) throw new InvalidOperationException(text);
                var line = a.GetArrayElementAtIndex(i);
                line.FindPropertyRelative("speaker").intValue = lines[i][0] == 'L' ? 0 : 1;
                line.FindPropertyRelative("text").stringValue = text;
                line.FindPropertyRelative("portraitOverride").objectReferenceValue = null;
                line.FindPropertyRelative("glitchPortraitTransition").boolValue = false;
            }
            so.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssetIfDirty(data); return data;
        }

        private static StoryChoiceView Choices(Scene scene)
        {
            var root = scene.GetRootGameObjects().SingleOrDefault(x => x.name == "TEACHER CHOICE UI");
            if (root == null)
            {
                const string path = "Assets/_Audere/Scenes/40_Evening.unity";
                var source = SceneManager.GetSceneByPath(path); bool opened = !source.isLoaded;
                if (opened) source = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    root = Object.Instantiate(Root(source, "NIGHT MESSAGE UI")); root.name = "TEACHER CHOICE UI";
                    SceneManager.MoveGameObjectToScene(root, scene);
                    for (int i = root.transform.childCount - 1; i >= 0; i--)
                        if (root.transform.GetChild(i).name != "Reply Choices") Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                }
                finally { if (opened) EditorSceneManager.CloseScene(source, true); }
            }
            var view = root.GetComponentInChildren<StoryChoiceView>(true);
            var ui = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<GameplayUIRoot>(true)).Single();
            Set(view, "inputGate", ui.InputGate);
            var group = view.GetComponent<CanvasGroup>(); group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false;
            return view;
        }
        private static void Set(Object target, params object[] pairs)
        {
            var so = new SerializedObject(target);
            for (int i = 0; i < pairs.Length; i += 2)
            {
                var p = so.FindProperty((string)pairs[i]); var v = pairs[i + 1];
                if (p == null) throw new InvalidOperationException(target.name + ": " + pairs[i]);
                if (v is Object[] refs) { p.arraySize = refs.Length; for (int n = 0; n < refs.Length; n++) p.GetArrayElementAtIndex(n).objectReferenceValue = refs[n]; }
                else if (v is string[] labels) { p.arraySize = labels.Length; for (int n = 0; n < labels.Length; n++) p.GetArrayElementAtIndex(n).stringValue = labels[n]; }
                else if (v is string text) p.stringValue = text;
                else if (v is bool b) p.boolValue = b;
                else if (v is float f) p.floatValue = f;
                else if (v is int number) p.intValue = number;
                else p.objectReferenceValue = v as Object;
            }
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
    }
}
#endif
