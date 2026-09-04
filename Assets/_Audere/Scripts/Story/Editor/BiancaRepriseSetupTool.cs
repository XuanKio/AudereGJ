#if UNITY_EDITOR
using System;
using System.Linq;
using Audere.Combat;
using Audere.Dialogue;
using Audere.EditorTools;
using Audere.Story.Presentation;
using Audere.Story.Steps;
using Audere.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Audere.Story.Editor
{
    // Scoped sequel to TeacherAfterCombatSetupTool. Does not regenerate previous beats.
    public static class BiancaRepriseSetupTool
    {
        public const string Folder = "Assets/_Audere/Data/Combat/BiancaReprise";
        public const string DialogueFolder = "Assets/_Audere/Data/Dialogue/Day3/BiancaReprise";
        public const string EncounterPath = Folder + "/CombatEncounter_D3_BIANCA_REPRISE.asset";
        public const string EventId = "D3_BIANCA_REPRISE_AND_SILENCE";

        [MenuItem("Audere/Story/Apply Bianca Reprise To Active Scene")]
        public static void AuthorActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || scene.isDirty || scene.path != Day3SchoolSetupTool.TeacherPath)
                throw new InvalidOperationException("Open the saved Teacher scene in Edit Mode first.");
            EnsureFolder(Folder); EnsureFolder(DialogueFolder);
            var story = Root(scene, "STORY").transform;
            var world = Root(scene, "WORLD").transform;
            var main = story.GetComponentsInChildren<StoryEvent>(true).Single(x => x.EventId == "D3_TEACHER_CHECK_IN_PRESSURE");
            var e = Component<StoryEvent>(Child(story, EventId)); Set(e, "eventId", EventId, "autoPlayNextEvent", false);
            Set(main, "autoPlayNextEvent", true, "nextEvent", e);
            var a = world.GetComponentsInChildren<Transform>(true).Single(x => x.name == "Audere");
            var t = world.GetComponentsInChildren<Transform>(true).Single(x => x.name == "Teacher PLACEHOLDER");
            var tileA = a.parent.Find("Start Tile"); var tileT = t.parent.Find("Teacher Tile");
            var anchors = world.Find("STAGING TARGETS");
            var openingA = anchors.Find("Audere_Opening");
            var b = a.parent.Find("Bianca");
            if (b == null)
            {
                b = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Audere/Prefabs/Story/Characters/Bianca.prefab"), a.parent)).transform;
                b.name = "Bianca";
            }
            b.gameObject.SetActive(false);
            float pitch = Mathf.Abs(tileA.lossyScale.x);
            var tiles = new Transform[4];
            for (int i = 0; i < tiles.Length; i++)
            {
                string name = "Bianca Arrival Tile " + i;
                tiles[i] = a.parent.Find(name);
                if (tiles[i] == null) { tiles[i] = Object.Instantiate(tileA.gameObject, a.parent).transform; tiles[i].name = name; }
                tiles[i].position = tileT.position + Vector3.right * pitch * (i + 1);
                tiles[i].gameObject.SetActive(false);
            }
            // Match GridPlayer's foot baseline. Keep the prefab shadow offset intact.
            var body = b.GetComponent<SpriteRenderer>();
            Vector3 foot = new Vector3(body.sprite.bounds.center.x, body.sprite.bounds.min.y, 0f);
            b.position += tiles[3].position - b.TransformPoint(foot);
            var bp = b.position; bp.z = a.position.z; b.position = bp;
            var entry = Anchor(anchors, "Bianca_FromRight", b.position);
            var stops = new Transform[3];
            for (int i = 0; i < 3; i++) stops[i] = Anchor(anchors, "Bianca_Approach_" + i, b.position + Vector3.left * pitch * (i + 1));
            var near = Anchor(anchors, "Audere_ChoosesCompany", openingA.position + Vector3.right * pitch);
            var endBody = b.GetComponent<SpriteRenderer>(); endBody.flipX = false;
            var encounter = AuthorEncounter();
            var teacherStep = main.GetComponentsInChildren<CombatStep>(true).Single();
            var board = teacherStep.CombatController.BoardView;
            var teacherActor = board.GetComponentsInChildren<CombatEnemyActor>(true).Single(x => x.name == "Enemy_Teacher_PLACEHOLDER");
            var enemy = teacherActor.transform.parent.Find("Enemy_Bianca_Reprise");
            if (enemy == null)
            {
                enemy = ((GameObject)PrefabUtility.InstantiatePrefab(encounter.EnemyDefinition.ActorPrefab.gameObject, teacherActor.transform.parent)).transform;
                enemy.name = "Enemy_Bianca_Reprise";
            }
            enemy.gameObject.SetActive(false);
            Set(teacherStep, "enemyActorOverride", teacherActor);
            var mode = world.GetComponentsInChildren<WorldModeController>(true).FirstOrDefault() ?? scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<WorldModeController>(true)).Single();
            var fade = Cover(scene);
            var choice = Root(scene, "TEACHER CHOICE UI").GetComponentInChildren<StoryChoiceView>(true);
            var title = Title(scene, choice);

            // Replay normalization runs under the existing opening cover.
            Set(Step<SetActiveStep>(main.transform, "001_ResetBiancaEndingVisibility"), "objectsToEnable", new Object[] { t.gameObject },
                "objectsToDisable", new Object[] { b.gameObject }.Concat(tiles.Select(x => (Object)x.gameObject)).ToArray());
            Fade(Step<CanvasFadeStep>(main.transform, "002_ResetDay3Title"), title, 0f, 0f);
            Set(Step<MoveActorStep>(main.transform, "003_ResetBiancaEntry"), "actor", b, "targetTransform", entry, "duration", 0f);
            Fade(Step<CanvasFadeStep>(main.transform, "004_ResetBiancaCover"), fade, 0f, 0f);

            Wait(e.transform, "000_OneBeatAfterComfort", .55f);
            Set(Step<SetActiveStep>(e.transform, "010_TheRightHandPath"), "objectsToEnable", tiles.Select(x => (Object)x.gameObject).ToArray());
            Set(Step<MoveActorStep>(e.transform, "012_ResetBiancaAtPath"), "actor", b, "targetTransform", entry, "duration", 0f);
            Set(Step<SetActiveStep>(e.transform, "015_BiancaAppears"), "objectsToEnable", new Object[] { b.gameObject });
            for (int i = 0; i < 3; i++) Motion(Step<CharacterMotionStep>(e.transform, (20 + i * 10).ToString("000") + "_BiancaStepsLeft"), b, stops[i], .36f, .045f, 2);
            Talk(e.transform, "050_TimorFillsThePause", D("ARRIVAL", DialogueCharacterId.Timor, "Timor/TimorLolang.png",
                "R|Cô ấy đến vì giáo viên bảo.", "R|Cô ấy chỉ đang thương hại cậu.", "R|Cô ấy chỉ đang nghĩ là…"));
            Set(Step<WorldModeStep>(e.transform, "060_TheFamiliarBattle"), "worldModeController", mode, "targetMode", (int)WorldGameplayMode.Combat);
            Set(Step<CombatStep>(e.transform, "070_PlayFadingBianca"), "combatController", teacherStep.CombatController,
                "combatEncounterData", encounter, "enemyActorOverride", enemy.GetComponent<CombatEnemyActor>(), "defeatBehaviour", (int)CombatResultBehaviour.Fail);
            Fade(Step<CanvasFadeStep>(e.transform, "080_CoverTheAfterimage"), fade, 1f, .7f);
            Set(Step<WorldModeStep>(e.transform, "090_ReturnToCompany"), "worldModeController", mode, "targetMode", (int)WorldGameplayMode.Story);
            Set(Step<SetActiveStep>(e.transform, "100_OnlyAudereAndBianca"), "objectsToDisable", new Object[] { t.gameObject }.Concat(tiles.Skip(1).Select(x => (Object)x.gameObject)).ToArray());
            Set(Step<MoveActorStep>(e.transform, "105_BiancaWaits"), "actor", b, "targetTransform", stops[2], "duration", 0f);
            Fade(Step<CanvasFadeStep>(e.transform, "110_RevealTheQuiet"), fade, 0f, .7f);
            Wait(e.transform, "120_AudereLooksAtHer", .45f);
            Motion(Step<CharacterMotionStep>(e.transform, "130_AudereApproaches"), a, near, .48f, .015f, 3);
            Motion(Step<CharacterMotionStep>(e.transform, "140_AudereFindsAWord"), a, near, .22f, .035f, 3);
            Set(e.transform.Find("140_AudereFindsAWord").GetComponent<CharacterMotionStep>(), "motionMode", (int)CharacterMotionMode.VerticalInPlace);
            Wait(e.transform, "145_LetTheHopSettle", .16f);

            var branchesRoot = Child(story, "BIANCA REPRISE REPLIES");
            string[] labels = { "Cậu… ngồi đây một lúc được không?", "Tớ ổn… Cảm ơn cậu.", "Lát nữa mình cùng về lớp nhé?" };
            string[] answers = { "Ừ. Tớ ở đây.", "Ừ. Không có gì đâu.", "Ừ. Khi nào cậu muốn thì mình đi." };
            var branches = new StoryEvent[3];
            for (int i = 0; i < branches.Length; i++)
            {
                var branch = Child(branchesRoot, "D3_BIANCA_REPLY_" + i); branches[i] = Component<StoryEvent>(branch);
                Set(branches[i], "eventId", branch.name, "autoPlayNextEvent", false);
                Talk(branch, "010_AudereMakesRoom", D("REPLY_" + i, DialogueCharacterId.Bianca, "Bianca/Bianca.png", "L|" + labels[i], "R|" + answers[i]));
            }
            Set(Step<StoryChoiceBranchStep>(e.transform, "150_AudereChoosesCompany"), "choiceView", choice, "options", labels, "branches", branches.Cast<Object>().ToArray());
            Wait(e.transform, "160_TheUnansweredSpace", .65f);
            Talk(e.transform, "170_TimorFeelsLeftBehind", D("LEFT_BEHIND", DialogueCharacterId.Timor, "Timor/TimorBuon.png",
                "R|Trước đây chỉ có tớ ở đây.", "L|Tớ vẫn cần cậu, nhưng—"));
            Talk(e.transform, "180_TimorInterrupts", D("INTERRUPTS", DialogueCharacterId.Timor, "Timor/TimorTucGian.png", "R|Không. Cậu chỉ cần họ thôi."));
            Wait(e.transform, "190_HurtBehindTheAnger", .4f);
            Talk(e.transform, "200_TimorWithdraws", D("WITHDRAWS", DialogueCharacterId.Timor, "Timor/TimorBuon.png",
                "R|Được. Ngày mai tớ sẽ không nhắc nữa.", "R|Để xem cậu có thật sự làm được không."));
            Wait(e.transform, "210_NoAnswerFromTimor", 1.4f);
            Fade(Step<CanvasFadeStep>(e.transform, "215_KeepTitleHidden"), title, 0f, 0f);
            Fade(Step<CanvasFadeStep>(e.transform, "220_FadeOutDayThree"), fade, 1f, 1.1f);
            Wait(e.transform, "225_HoldBlackBeforeTitle", .45f);
            Set(Step<StoryTitleCardStep>(e.transform, "230_DayThreeEnds"), "overlay", title, "titleText", title.GetComponentInChildren<TMP_Text>(true),
                "title", "Ngày 3 - Kết thúc", "fadeDuration", .85f, "holdDuration", 2f, "waitForConfirm", false, "allowConfirmSkip", true, "leaveVisible", true);
            Set(Step<SceneLoadStep>(e.transform, "240_BeginDayFour"), "sceneName", "130_D4_Home_Morning", "hidePuzzleUiBeforeLoad", true);
            Order(main.transform); Order(e.transform);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }

        private static CombatEncounterData AuthorEncounter()
        {
            var first = AssetDatabase.LoadAssetAtPath<CombatEncounterData>("Assets/_Audere/Data/Combat/BiancaSupplies/CombatEncounter_D2_BIANCA_SUPPLIES_PLACEHOLDER.asset");
            var moves = new FadingPressureMove[2];
            for (int i = 0; i < moves.Length; i++)
            {
                moves[i] = Asset<FadingPressureMove>(Folder + "/Move_Fading_" + i + ".asset");
                Set(moves[i], "projectilePrefab", AssetDatabase.LoadAssetAtPath<CombatBulletView>("Assets/_Audere/Prefabs/Combat/Bullets/" + (i == 0 ? "EnemyBullet" : "Bullet_Bianca_Returning") + ".prefab"),
                    "returningOrbit", i == 1, "duration", 7f, "beatInterval", i == 0 ? .85f : 1.8f, "speed", 80f, "deflectRadius", i == 0 ? 36f : 44f, "dissolveRadius", 3f);
                AssetDatabase.SaveAssetIfDirty(moves[i]);
            }
            var set = Asset<CombatMoveSet>(Folder + "/MoveSet_FadingPressure.asset");
            var so = new SerializedObject(set); so.FindProperty("selectionPolicy").intValue = 0;
            var entries = so.FindProperty("entries"); entries.arraySize = moves.Length;
            for (int i = 0; i < moves.Length; i++) { var p = entries.GetArrayElementAtIndex(i); p.FindPropertyRelative("move").objectReferenceValue = moves[i]; p.FindPropertyRelative("weight").floatValue = 1f; }
            so.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssetIfDirty(set);
            var lines = new[] {
                D("C01_TIMOR", DialogueCharacterId.Timor, "Timor/TimorLolang.png", "R|Cô ấy chỉ muốn làm tròn việc thôi."),
                D("C02_BIANCA", DialogueCharacterId.Bianca, "Bianca/Bianca_Worried.png", "R|Audere…", "R|Tớ không biết cậu muốn có người ở đây…", "R|Hay muốn ở một mình."),
                D("C03_TIMOR", DialogueCharacterId.Timor, "Timor/TimorLoLangKhongVui.png", "R|Nghe chưa? Cô ấy cũng không muốn ở lại."),
                D("C04_BIANCA", DialogueCharacterId.Bianca, "Bianca/Bianca_Worried.png", "R|Nên tớ chỉ xuống hỏi thôi.", "R|Cậu không cần nói gì cả."),
                D("C05_TIMOR", DialogueCharacterId.Timor, "Timor/TimorLoLangKhongVui.png", "R|Cậu đâu biết cô ấy đang nghĩ gì…"),
                D("C06_AUDERE", DialogueCharacterId.Timor, "Timor/TimorBuon.png", "L|Được rồi mà, Timor.", "L|Cậu biết Bianca không phải người như thế.")
            };
            var enemy = Asset<CombatEnemyDefinition>(Folder + "/Enemy_Bianca_FadingPressure.asset");
            so = new SerializedObject(enemy);
            so.FindProperty("enemyId").stringValue = "d3-bianca-fading-pressure";
            so.FindProperty("displayName").stringValue = "Bianca";
            so.FindProperty("actorPrefab").objectReferenceValue = first.EnemyDefinition.ActorPrefab;
            so.FindProperty("phasePolicy").intValue = (int)CombatPhasePolicy.PerPhaseHealth;
            so.FindProperty("passiveHealthDecayInterval").floatValue = 3.5f; var phases = so.FindProperty("phases"); phases.arraySize = 1; var phase = phases.GetArrayElementAtIndex(0);
            phase.FindPropertyRelative("phaseId").stringValue = "listen-to-her"; phase.FindPropertyRelative("maxHealth").intValue = 6;
            phase.FindPropertyRelative("duration").floatValue = 60f; phase.FindPropertyRelative("moveSet").objectReferenceValue = set;
            phase.FindPropertyRelative("spawnDice").boolValue = false; phase.FindPropertyRelative("allowsPlayerDefeat").boolValue = false;
            var cues = phase.FindPropertyRelative("dialogueCues"); cues.arraySize = 1; var cue = cues.GetArrayElementAtIndex(0);
            cue.FindPropertyRelative("cueId").stringValue = "reprise-real-voices"; cue.FindPropertyRelative("trigger").intValue = 0;
            cue.FindPropertyRelative("presentation").intValue = 1; cue.FindPropertyRelative("requiredBeforeVictory").boolValue = true;
            cue.FindPropertyRelative("minimumLineDuration").floatValue = 2.1f; cue.FindPropertyRelative("charactersPerSecond").floatValue = 24f;
            cue.FindPropertyRelative("interLineGap").floatValue = .3f;
            var sequence = cue.FindPropertyRelative("sequence"); sequence.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++) sequence.GetArrayElementAtIndex(i).objectReferenceValue = lines[i];
            so.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssetIfDirty(enemy);
            var encounter = Asset<CombatEncounterData>(EncounterPath);
            Set(encounter, "encounterId", "d3-bianca-reprise", "enemyDefinition", enemy, "encounterDuration", 90f,
                "music", (int)first.Music, "outcomeRules.allowedOutcomes", (int)CombatAllowedOutcome.Victory,
                "outcomeRules.showRetryOnDefeat", false, "victoryFadeDuration", .9f);
            AssetDatabase.SaveAssetIfDirty(encounter);
            if (!enemy.Validate(out string error)) throw new InvalidOperationException(error);
            return encounter;
        }

        private static DialogueData D(string suffix, DialogueCharacterId right, string portrait, params string[] lines)
        {
            var d = Asset<DialogueData>(DialogueFolder + "/Dialogue_D3_REPRISE_" + suffix + ".asset");
            Set(d, "dialogueId", "D3_REPRISE_" + suffix, "leftCharacter", (int)DialogueCharacterId.Audere, "rightCharacter", (int)right,
                "leftPortraitOverride", Portrait("Audere/Audere_Tired.png"), "rightPortraitOverride", Portrait(portrait));
            var so = new SerializedObject(d); var a = so.FindProperty("lines"); a.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                string text = lines[i].Substring(2); if (text.Length > 42) throw new InvalidOperationException(text);
                var p = a.GetArrayElementAtIndex(i); p.FindPropertyRelative("speaker").intValue = lines[i][0] == 'L' ? 0 : 1;
                p.FindPropertyRelative("text").stringValue = text; p.FindPropertyRelative("portraitOverride").objectReferenceValue = null;
                p.FindPropertyRelative("glitchPortraitTransition").boolValue = false;
            }
            so.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssetIfDirty(d); return d;
        }
        private static CanvasGroup Title(Scene scene, StoryChoiceView choice)
        {
            var go = scene.GetRootGameObjects().SingleOrDefault(x => x.name == "DAY THREE END TITLE");
            if (go == null)
            {
                go = new GameObject("DAY THREE END TITLE", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
                var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1400;
                var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080);
                var textGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI)); textGo.transform.SetParent(go.transform, false);
                var rt = (RectTransform)textGo.transform; rt.anchorMin = new Vector2(.1f,.3f); rt.anchorMax = new Vector2(.9f,.7f); rt.offsetMin = rt.offsetMax = Vector2.zero;
                var text = textGo.GetComponent<TextMeshProUGUI>(); text.font = choice.GetComponentInChildren<TMP_Text>(true).font;
                text.fontSize = 56; text.alignment = TextAlignmentOptions.Center; text.color = new Color(.87f,.88f,.91f,1); text.raycastTarget = false;
            }
            var group = go.GetComponent<CanvasGroup>(); group.alpha = 0; group.interactable = group.blocksRaycasts = false;
            group.GetComponentInChildren<TMP_Text>(true).text = "Ngày 3 - Kết thúc"; return group;
        }
        private static CanvasGroup Cover(Scene scene)
        {
            var go=scene.GetRootGameObjects().SingleOrDefault(x=>x.name=="DAY THREE STORY COVER");
            if(go==null)
            {
                go=new GameObject("DAY THREE STORY COVER",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
                var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1350;
                var image=new GameObject("Neutral Cover",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(CanvasGroup));
                image.transform.SetParent(go.transform,false);var rt=(RectTransform)image.transform;
                rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
                image.GetComponent<Image>().color=Color.black;
            }
            var group=go.GetComponentInChildren<CanvasGroup>(true);group.alpha=0f;group.blocksRaycasts=false;group.interactable=false;
            return group;
        }
        private static void Wait(Transform p, string n, float time) => Set(Step<WaitStep>(p,n), "duration",time);
        private static void Talk(Transform p, string n, DialogueData d) => Set(Step<DialogueStep>(p,n), "dialogueData",d);
        private static void Fade(CanvasFadeStep step, CanvasGroup group, float alpha, float time) => Set(step,"canvasGroup",group,"targetAlpha",alpha,"duration",time);
        private static void Motion(CharacterMotionStep step, Transform actor, Transform target, float duration, float arc, int facing)
        {
            Set(step, "actor", actor, "targetTransform", target, "actorRenderer", actor.GetComponent<SpriteRenderer>(),
                "groundedShadow", actor.GetComponentsInChildren<SpriteRenderer>(true).Single(x => x.sortingOrder == 4).transform,
                "duration",duration,"arcHeight",arc,"travelStretch",0f,"landingDuration",0f,"landingSquash",0f,"landingWiden",0f,"facingMode",facing,"useUnscaledTime",true);
        }
        private static Sprite Portrait(string path) => AssetDatabase.LoadAllAssetsAtPath("Assets/_Audere/AssetGame/" + path).OfType<Sprite>().First();
        private static void EnsureFolder(string path) { if (AssetDatabase.IsValidFolder(path)) return; int n=path.LastIndexOf('/'); EnsureFolder(path.Substring(0,n)); AssetDatabase.CreateFolder(path.Substring(0,n),path.Substring(n+1)); }
        private static T Asset<T>(string path) where T : ScriptableObject { var v=AssetDatabase.LoadAssetAtPath<T>(path); if(v==null) { v=ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(v,path); } return v; }
        private static Transform Anchor(Transform parent, string name, Vector3 pos) { var t=Child(parent,name);t.position=pos;return t; }
        private static T Step<T>(Transform p,string n) where T:StoryStep => Component<T>(Child(p,n));
        private static T Component<T>(Transform t) where T:Component { var c=t.GetComponent<T>();return c!=null?c:t.gameObject.AddComponent<T>(); }
        private static Transform Child(Transform p,string n) { var t=p.Find(n);if(t!=null)return t;t=new GameObject(n).transform;t.SetParent(p,false);return t; }
        private static GameObject Root(Scene s,string n) => s.GetRootGameObjects().Single(x=>x.name==n);
        private static void Order(Transform root) { foreach(var t in root.Cast<Transform>().OrderBy(x=>x.name,StringComparer.Ordinal).ToArray())t.SetAsLastSibling(); }
        private static void Set(Object target, params object[] pairs)
        {
            var so=new SerializedObject(target);
            for(int i=0;i<pairs.Length;i+=2)
            {
                var p=so.FindProperty((string)pairs[i]);var v=pairs[i+1];if(p==null)throw new InvalidOperationException(target.name+":"+pairs[i]);
                if(v is Object[] refs) { p.arraySize=refs.Length;for(int n=0;n<refs.Length;n++)p.GetArrayElementAtIndex(n).objectReferenceValue=refs[n]; }
                else if(v is string[] labels) { p.arraySize=labels.Length;for(int n=0;n<labels.Length;n++)p.GetArrayElementAtIndex(n).stringValue=labels[n]; }
                else if(v is string text)p.stringValue=text;else if(v is bool b)p.boolValue=b;else if(v is float f)p.floatValue=f;else if(v is int number)p.intValue=number;else p.objectReferenceValue=v as Object;
            }
            so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(target);
        }
    }
}
#endif
