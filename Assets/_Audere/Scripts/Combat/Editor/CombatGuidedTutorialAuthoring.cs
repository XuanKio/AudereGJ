#if UNITY_EDITOR
using System;
using System.Globalization;
using Audere.Dialogue;
using UnityEditor;
using UnityEngine;

namespace Audere.Combat.Editor
{
    /// <summary>
    /// Scoped D1 lesson authoring. The encounter, production enemy, legacy cues,
    /// scene references and shared UI prefab remain owned by their existing tools.
    /// New wording is Design Intent for this first classroom combat.
    /// </summary>
    public static class CombatGuidedTutorialAuthoring
    {
        public const string MenuPath = "Audere/Combat/Apply D1 Guided Combat Tutorial";
        public const string TutorialPath =
            "Assets/_Audere/Data/Combat/Tutorials/CombatTutorial_D1_CLASSROOM.asset";
        private const string EncounterPath =
            "Assets/_Audere/Data/Combat/CombatEncounter_D1_CLASSROOM_KHOANG_LANG.asset";
        private const string SourceMovePath = "Assets/_Audere/Data/Combat/Moves/Move_AimedFan.asset";
        private const string SourceDialogueFolder = "Assets/_Audere/Data/Dialogue/Day1/Classroom/Combat";
        private const string DialogueFolder = SourceDialogueFolder + "/Guided";

        private readonly struct LineSpec
        {
            public readonly DialogueSpeakerSide Speaker;
            public readonly string Text;

            public LineSpec(DialogueSpeakerSide speaker, string text)
            {
                Speaker = speaker;
                Text = text;
            }
        }

        private readonly struct LessonSpec
        {
            public readonly CombatTutorialLessonKind Kind;
            public readonly string Id;
            public readonly string PortraitSource;
            public readonly string Instruction;
            public readonly LineSpec[] Lines;

            public LessonSpec(CombatTutorialLessonKind kind, string id, string portraitSource,
                string instruction, params LineSpec[] lines)
            {
                Kind = kind;
                Id = id;
                PortraitSource = portraitSource;
                Instruction = instruction;
                Lines = lines;
            }
        }

        [MenuItem(MenuPath)]
        public static void ApplyD1GuidedCombatTutorial()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring the D1 guided tutorial.");

            CombatTutorialData tutorial = RequireAsset<CombatTutorialData>(TutorialPath);
            CombatEncounterData encounter = RequireAsset<CombatEncounterData>(EncounterPath);
            if (encounter.TutorialData != tutorial)
                throw new InvalidOperationException("The D1 encounter must directly reference the existing D1 tutorial.");
            if (tutorial.EnemyDefinition == null || !tutorial.EnemyDefinition.Validate(out string enemyError))
                throw new InvalidOperationException("The existing tutorial enemy is missing or invalid.");
            if (encounter.EnemyDefinition == null || encounter.EnemyDefinition.PhaseCount != 1 ||
                encounter.EnemyDefinition.GetPhase(0).MaxHealth != 6)
                throw new InvalidOperationException("The existing D1 production encounter must retain its single 6 HP phase.");

            // The demonstration uses the same directly authored bullet as the
            // classroom's source move, so imported art and hit footprint agree.
            CombatMoveDefinition sourceMove = RequireAsset<CombatMoveDefinition>(SourceMovePath);
            var sourceMoveSerialized = new SerializedObject(sourceMove);
            CombatBulletView bullet = RequiredProperty(sourceMoveSerialized, "projectilePrefab")
                .objectReferenceValue as CombatBulletView;
            if (bullet == null || !PrefabUtility.IsPartOfPrefabAsset(bullet))
                throw new MissingReferenceException("The D1 source move must reference an authored CombatBulletView prefab.");

            LessonSpec[] specs = CreateLessons(encounter);
            ValidateDraft(specs);
            // Validate every source before creating assets; no broad tutorial
            // regeneration is needed to adopt the action-gated lesson sequence.
            for (int i = 0; i < specs.Length; i++)
                RequireAsset<DialogueData>(SourceDialoguePath(specs[i].PortraitSource));

            var serialized = new SerializedObject(tutorial);
            SerializedProperty guidedLessons = RequiredProperty(serialized, "guidedLessons");
            SerializedProperty enabled = RequiredProperty(serialized, "useGuidedLessons");
            SerializedProperty boardSize = RequiredProperty(serialized, "squareBoardSize");
            SerializedProperty demonstrationBullet = RequiredProperty(serialized, "demonstrationBullet");
            SerializedProperty playerTime = RequiredProperty(serialized, "playerTime");

            if (!AssetDatabase.IsValidFolder(DialogueFolder))
                AssetDatabase.CreateFolder(SourceDialogueFolder, "Guided");

            guidedLessons.arraySize = specs.Length;
            for (int i = 0; i < specs.Length; i++)
            {
                LessonSpec spec = specs[i];
                DialogueData dialogue = EnsureDialogue(spec);
                SerializedProperty lesson = guidedLessons.GetArrayElementAtIndex(i);
                lesson.FindPropertyRelative("id").stringValue = spec.Id;
                lesson.FindPropertyRelative("kind").enumValueIndex = (int)spec.Kind;
                lesson.FindPropertyRelative("dialogue").objectReferenceValue = dialogue;
                lesson.FindPropertyRelative("instruction").stringValue = spec.Instruction;
            }
            boardSize.floatValue = 400f;
            demonstrationBullet.objectReferenceValue = bullet;
            playerTime.floatValue = 30f;
            enabled.boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tutorial);
            if (!tutorial.Validate(out string tutorialError))
                throw new InvalidOperationException(tutorialError);
            AssetDatabase.SaveAssetIfDirty(tutorial);
            Debug.Log("[CombatGuidedTutorialAuthoring] D1 guided tutorial: 11 action lessons, 400 × 400 board, 30 TIME. Production encounter and legacy cues preserved.");
        }

        private static LessonSpec[] CreateLessons(CombatEncounterData encounter)
        {
            string hit = Number(encounter.BulletTimePenaltySeconds);
            string heal = Number(CombatDiceConstants.HealTimeSeconds);
            string attack = CombatDiceConstants.AttackDamage.ToString(CultureInfo.InvariantCulture);
            return new[]
            {
                new LessonSpec(CombatTutorialLessonKind.Move, "guided-move", "CATCH",
                    "RÊ CHUỘT: đưa Heart vào vòng sáng.",
                    R("Cứ đưa trái tim theo tay cậu trước nhé."),
                    R("Chạm vào chỗ sáng kia.")),
                new LessonSpec(CombatTutorialLessonKind.Time, "guided-time", "CATCH",
                    "TIME LÀ MÁU CỦA CẬU\nMỗi giây trôi qua: −1 TIME. Về 0: thua.\nQuan sát thanh TIME giảm.",
                    R("Nhìn thanh TIME này."),
                    R("Nó giảm cả khi cậu đứng yên.")),
                new LessonSpec(CombatTutorialLessonKind.Damage, "guided-damage", "PLAYER_HIT",
                    "XEM MỘT LẦN TRÚNG ĐẠN\nTrúng đạn: −" + hit + " TIME.\nBài tập này không thể thua.",
                    R("Tớ cho cậu xem một lần nhé."),
                    R("Sau đó mình tập né.")),
                new LessonSpec(CombatTutorialLessonKind.Heal, "guided-heal", "HEAL",
                    "RÊ CHUỘT: cho viên HỒI NHỊP vào vòng bắt.\nCHUỘT TRÁI: bắt viên để hồi +" + heal + " TIME.",
                    R("Mình lấy lại phần vừa mất nhé."),
                    R("Đưa vòng bắt tới viên này.")),
                new LessonSpec(CombatTutorialLessonKind.Attack, "guided-attack", "ATTACK",
                    "CHUỘT TRÁI: bắt viên TẤN CÔNG trong vòng.\nMỗi viên: −" + attack + " HP đối thủ. Về 0: thắng.",
                    L("Tớ có thể làm nó yếu đi sao?"),
                    R("Ừ. Cậu thử viên này nhé.")),
                new LessonSpec(CombatTutorialLessonKind.Reroll, "guided-reroll", "REROLL",
                    "Đưa viên vào vòng rồi CHUỘT PHẢI: gieo lại.\nBình thường, mặt mới là ngẫu nhiên.\nTrong bài tập, lần này sẽ ra KHIÊN.",
                    R("Không phải mặt cậu cần à?"),
                    R("Thử gieo lại viên đó nhé.")),
                new LessonSpec(CombatTutorialLessonKind.Shield, "guided-shield", "SHIELD",
                    "CHUỘT TRÁI: bắt viên KHIÊN.\nXóa đạn gần Heart.\nĐạn ở xa vẫn còn: tiếp tục né.",
                    R("Giờ bắt mặt khiên vừa gieo được."),
                    R("Nó cho cậu một khoảng thở.")),
                new LessonSpec(CombatTutorialLessonKind.Dodge, "guided-dodge", "CATCH",
                    "RÊ CHUỘT: đưa Heart qua chỗ trống.\nNé liên tục 4 giây để hoàn thành.",
                    R("Cứ nhìn chỗ trống trước đã."),
                    R("Đưa trái tim qua đó nhé.")),
                new LessonSpec(CombatTutorialLessonKind.StunCatch, "guided-stun-catch", "STUN_ZONE",
                    "Đưa Heart vào vùng nhiễu, viên ở trong vòng.\nThử CHUỘT TRÁI: không bắt được ở đây.",
                    R("Có một chỗ cản lúc cậu bắt."),
                    R("Thử ở trong vùng này nhé.")),
                new LessonSpec(CombatTutorialLessonKind.StunReroll, "guided-stun-reroll", "STUN_ZONE",
                    "RÊ CHUỘT: đặt viên ở mép phải vòng bắt.\nCHUỘT PHẢI: gieo viên sang bên phải.\nRa khỏi vùng nhiễu rồi CHUỘT TRÁI: bắt.",
                    R("Ở đây, mình vẫn gieo lại được."),
                    R("Đưa nó ra chỗ thoáng rồi bắt nhé.")),
                new LessonSpec(CombatTutorialLessonKind.Finish, "guided-finish", "FINAL",
                    "CHUỘT TRÁI: bắt đầu trận thật.\nTIME được hồi đầy.\nHạ HP đối thủ về 0 để thắng.",
                    L("Tớ muốn thử."),
                    R("Vậy đừng để mất câu đó.")),
            };
        }

        private static void ValidateDraft(LessonSpec[] specs)
        {
            if (specs.Length != 11)
                throw new InvalidOperationException("D1 guided tutorial requires all 11 authored lessons.");
            for (int i = 0; i < specs.Length; i++)
            {
                LessonSpec spec = specs[i];
                if ((int)spec.Kind != i || string.IsNullOrWhiteSpace(spec.Instruction))
                    throw new InvalidOperationException("D1 guided lesson order or instruction is invalid: " + spec.Id);
                if (spec.Lines.Length < 1 || spec.Lines.Length > 2)
                    throw new InvalidOperationException("Each D1 guided lesson needs one or two short speech beats.");
                foreach (LineSpec line in spec.Lines)
                {
                    if (string.IsNullOrWhiteSpace(line.Text) ||
                        new StringInfo(line.Text).LengthInTextElements > 42)
                        throw new InvalidOperationException("Dialogue bubble exceeds 42 visible characters: " + line.Text);
                }
            }
        }

        private static DialogueData EnsureDialogue(LessonSpec spec)
        {
            string path = DialogueFolder + "/Dialogue_D1_COMBAT_" +
                spec.Id.Replace('-', '_').ToUpperInvariant() + ".asset";
            DialogueData source = RequireAsset<DialogueData>(SourceDialoguePath(spec.PortraitSource));
            DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            // Scene setup seeds missing dialogue; authored wording and expressions own existing assets.
            if (data != null) return data;
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<DialogueData>();
                AssetDatabase.CreateAsset(data, path);
            }
            var serialized = new SerializedObject(data);
            serialized.FindProperty("dialogueId").stringValue = "d1-combat-" + spec.Id;
            serialized.FindProperty("leftCharacter").enumValueIndex = (int)DialogueCharacterId.Audere;
            serialized.FindProperty("rightCharacter").enumValueIndex = (int)DialogueCharacterId.Timor;
            serialized.FindProperty("leftPortraitOverride").objectReferenceValue = source.LeftPortraitOverride;
            serialized.FindProperty("rightPortraitOverride").objectReferenceValue = source.RightPortraitOverride;
            SerializedProperty lines = serialized.FindProperty("lines");
            lines.arraySize = spec.Lines.Length;
            for (int i = 0; i < spec.Lines.Length; i++)
            {
                LineSpec specLine = spec.Lines[i];
                SerializedProperty line = lines.GetArrayElementAtIndex(i);
                line.FindPropertyRelative("speaker").enumValueIndex = (int)specLine.Speaker;
                line.FindPropertyRelative("text").stringValue = specLine.Text;
                line.FindPropertyRelative("characterOverride").enumValueIndex = (int)DialogueCharacterId.None;
                line.FindPropertyRelative("portraitOverride").objectReferenceValue =
                    ExistingPortraitForExactLine(source, specLine);
                line.FindPropertyRelative("glitchPortraitTransition").boolValue = false;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);
            return data;
        }

        private static Sprite ExistingPortraitForExactLine(DialogueData source, LineSpec spec)
        {
            if (source.Lines == null) return null;
            foreach (DialogueData.Line line in source.Lines)
                if (line.Speaker == spec.Speaker && string.Equals(line.Text, spec.Text, StringComparison.Ordinal))
                    return line.PortraitOverride;
            return null;
        }

        private static string SourceDialoguePath(string suffix) =>
            SourceDialogueFolder + "/Dialogue_D1_COMBAT_TUTORIAL_" + suffix + ".asset";

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new MissingReferenceException("Missing " + typeof(T).Name + " at '" + path + "'.");
            return asset;
        }

        private static SerializedProperty RequiredProperty(SerializedObject serialized, string name)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null)
                throw new InvalidOperationException("Missing guided tutorial field '" + name + "'; compile the updated runtime first.");
            return property;
        }

        private static string Number(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        private static LineSpec L(string text) => new LineSpec(DialogueSpeakerSide.Left, text);
        private static LineSpec R(string text) => new LineSpec(DialogueSpeakerSide.Right, text);
    }
}
#endif
