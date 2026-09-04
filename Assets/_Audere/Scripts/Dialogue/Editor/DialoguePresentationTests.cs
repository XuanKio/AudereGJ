#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Audere.Dialogue.Editor.Tests
{
    public sealed class DialoguePresentationTests
    {
        private const string Root = "Assets/_Audere/Data/Dialogue/";
        private const string TeacherScene = "Assets/_Audere/Scenes/120_D3_School_Teacher.unity";
        private static DialogueCharacterCatalog Catalog => AssetDatabase.LoadAssetAtPath<DialogueCharacterCatalog>(Root + "DialogueCharacterCatalog.asset");
        private static DialogueCharacterCatalog.Entry Entry(DialogueCharacterId id)
        {
            if (id == DialogueCharacterId.None) return default;
            Assert.IsTrue(Catalog.TryGet(id, out var entry), id.ToString());
            return entry;
        }
        private static DialogueData.Line Line(DialogueCharacterId id, Sprite portrait = null)
        {
            var data = ScriptableObject.CreateInstance<DialogueData>();
            try
            {
                var so = new SerializedObject(data);
                var lines = so.FindProperty("lines"); lines.arraySize = 1;
                var line = lines.GetArrayElementAtIndex(0);
                line.FindPropertyRelative("characterOverride").intValue = (int)id;
                line.FindPropertyRelative("portraitOverride").objectReferenceValue = portrait;
                so.ApplyModifiedPropertiesWithoutUndo();
                return data.Lines[0];
            }
            finally { Object.DestroyImmediate(data); }
        }

        [Test]
        public void Catalog_StableDistinctIdentitiesAndVerifiedPortraits()
        {
            Assert.AreEqual(3, (int)DialogueCharacterId.Teacher);
            Assert.AreEqual(4, (int)DialogueCharacterId.Bianca);
            Assert.AreEqual(5, (int)DialogueCharacterId.KhoangLang);
            Assert.AreEqual(6, (int)DialogueCharacterId.BiancaDistorted);
            Assert.AreEqual(7, (int)DialogueCharacterId.TeacherDistorted);
            Assert.AreEqual("Co_giao_0", Entry(DialogueCharacterId.Teacher).Portrait.name);
            Assert.AreEqual("Bianca_0", Entry(DialogueCharacterId.Bianca).Portrait.name);
            Assert.AreEqual("Co_giao_Creepy_0", Entry(DialogueCharacterId.TeacherDistorted).Portrait.name);
            Assert.AreEqual("Bianca_Creepy_0", Entry(DialogueCharacterId.BiancaDistorted).Portrait.name);
            Assert.AreEqual(Entry(DialogueCharacterId.Teacher).DisplayName, Entry(DialogueCharacterId.TeacherDistorted).DisplayName);
            Assert.AreEqual(Entry(DialogueCharacterId.Bianca).DisplayName, Entry(DialogueCharacterId.BiancaDistorted).DisplayName);
        }

        [TestCase(DialogueCharacterId.Teacher, DialogueCharacterId.TeacherDistorted)]
        [TestCase(DialogueCharacterId.Bianca, DialogueCharacterId.BiancaDistorted)]
        public void IdentityChange_ResetsOldExpression_NullLineHolds(DialogueCharacterId normal, DialogueCharacterId distorted)
        {
            var state = new DialoguePresentationState(Entry(normal), Entry(DialogueCharacterId.Audere).Portrait);
            Assert.IsTrue(state.TryApply(Line(distorted), Catalog));
            Assert.AreSame(Entry(distorted).Portrait, state.Portrait);
            Assert.IsTrue(state.TryApply(default, Catalog));
            Assert.AreSame(Entry(distorted).Portrait, state.Portrait);
            Assert.IsTrue(state.TryApply(Line(normal), Catalog));
            Assert.AreSame(Entry(normal).Portrait, state.Portrait);
            Assert.IsNull(state.PortraitOverride);
        }

        [Test]
        public void SameIdentity_KeepsExpression_ExplicitPortraitWins()
        {
            var expression = Entry(DialogueCharacterId.Audere).Portrait;
            var state = new DialoguePresentationState(Entry(DialogueCharacterId.Bianca), expression);
            Assert.IsTrue(state.TryApply(Line(DialogueCharacterId.Bianca), Catalog));
            Assert.AreSame(expression, state.Portrait);
            Assert.IsTrue(state.TryApply(Line(DialogueCharacterId.BiancaDistorted, expression), Catalog));
            Assert.AreEqual(DialogueCharacterId.BiancaDistorted, state.Character.Character);
            Assert.AreSame(expression, state.Portrait);
        }

        [Test]
        public void InvalidIdentity_DoesNotMutateState()
        {
            var state = new DialoguePresentationState(Entry(DialogueCharacterId.Teacher));
            Assert.IsFalse(state.TryApply(Line((DialogueCharacterId)999), Catalog));
            Assert.AreEqual(DialogueCharacterId.Teacher, state.Character.Character);
            Assert.AreSame(Entry(DialogueCharacterId.Teacher).Portrait, state.Portrait);
        }

        [Test]
        public void IndependentSlotsAndReplay_DoNotInheritPreviousIdentity()
        {
            var left = new DialoguePresentationState(Entry(DialogueCharacterId.Audere));
            var right = new DialoguePresentationState(Entry(DialogueCharacterId.Teacher));
            right.TryApply(Line(DialogueCharacterId.TeacherDistorted), Catalog);
            Assert.AreEqual(DialogueCharacterId.Audere, left.Character.Character);
            var replay = new DialoguePresentationState(Entry(DialogueCharacterId.Teacher));
            Assert.AreSame(Entry(DialogueCharacterId.Teacher).Portrait, replay.Portrait);
        }

        [Test]
        public void AllDialogueAssets_ResolveIdentities_AndCreepyArtOnlyBelongsToDistortedSpeaker()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:DialogueData", new[] { Root.TrimEnd('/') }))
            {
                var data = AssetDatabase.LoadAssetAtPath<DialogueData>(AssetDatabase.GUIDToAssetPath(guid));
                if (!data.HasLines) continue;
                var left = new DialoguePresentationState(Entry(data.LeftCharacter), data.LeftPortraitOverride);
                var right = new DialoguePresentationState(Entry(data.RightCharacter), data.RightPortraitOverride);
                foreach (var line in data.Lines)
                {
                    var state = line.Speaker == DialogueSpeakerSide.Left ? left : right;
                    Assert.IsTrue(state.TryApply(line, Catalog), data.name);
                    if (state.Portrait == Entry(DialogueCharacterId.BiancaDistorted).Portrait)
                        Assert.AreEqual(DialogueCharacterId.BiancaDistorted, state.Character.Character, data.name);
                    if (state.Portrait == Entry(DialogueCharacterId.TeacherDistorted).Portrait)
                        Assert.AreEqual(DialogueCharacterId.TeacherDistorted, state.Character.Character, data.name);
                }
            }
        }

        [Test]
        public void RepriseAndAftercare_KeepRealPeople_NotDistortedVariants()
        {
            foreach (var folder in new[] { "Day3/BiancaReprise", "Day3/TeacherAfterCombat" })
                foreach (var guid in AssetDatabase.FindAssets("t:DialogueData", new[] { Root + folder }))
                {
                    var data = AssetDatabase.LoadAssetAtPath<DialogueData>(AssetDatabase.GUIDToAssetPath(guid));
                    Assert.IsFalse(data.RightCharacter == DialogueCharacterId.TeacherDistorted || data.RightCharacter == DialogueCharacterId.BiancaDistorted, data.name);
                    Assert.IsTrue(data.Lines.All(l => l.CharacterOverride == DialogueCharacterId.None), data.name);
                }
        }

        [UnityTest]
        public IEnumerator RealDialogueUI_MixedPortraits_AutoPlaybackCancelAndReplay()
        {
            var scene = EditorSceneManager.OpenScene(TeacherScene);
            var director = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Audere.Story.StoryDirector>(true)).Single();
            var so = new SerializedObject(director);
            so.FindProperty("playOnStart").boolValue = false; so.ApplyModifiedPropertiesWithoutUndo();
            yield return new EnterPlayMode();
            var root = GameplayUIRoot.Instance;
            Assert.IsNotNull(root);
            var controller = root.Dialogue;
            var fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var left = (DialogueCharacterSlotView)typeof(DialogueController).GetField("leftSlot", fields).GetValue(controller);
            var right = (DialogueCharacterSlotView)typeof(DialogueController).GetField("rightSlot", fields).GetValue(controller);
            var rightImage = (Image)typeof(DialogueCharacterSlotView).GetField("characterImage", fields).GetValue(right);
            var leftImage = (Image)typeof(DialogueCharacterSlotView).GetField("characterImage", fields).GetValue(left);
            foreach (var pair in new[] {
                new { Path = "Day3/TeacherCombat/Dialogue_D3_TEACHER_PERCEIVED_SMALL_TASK.asset", Id = DialogueCharacterId.TeacherDistorted, Normal = DialogueCharacterId.Teacher },
                new { Path = "Day2/School/Combat/Dialogue_D2_BIANCA_TAUNT_02.asset", Id = DialogueCharacterId.BiancaDistorted, Normal = DialogueCharacterId.Bianca }
            })
            {
                var data = AssetDatabase.LoadAssetAtPath<DialogueData>(Root + pair.Path);
                int callbacks = 0; bool sawCreepy = false, sawNormalAfter = false;
                float previousScale = Time.timeScale;
                int claims = root.InputGate.ActiveClaimCount;
                Assert.IsTrue(controller.PlayAuto(data, r => { Assert.AreEqual(DialogueResult.Completed, r); callbacks++; }, .8f, 120f, .05f));
                double deadline = EditorApplication.timeSinceStartup + 20;
                while (controller.IsPlaying && EditorApplication.timeSinceStartup < deadline)
                {
                    sawCreepy |= rightImage.sprite == Entry(pair.Id).Portrait;
                    sawNormalAfter |= sawCreepy && rightImage.sprite == Entry(pair.Normal).Portrait;
                    Assert.AreSame(data.LeftPortraitOverride ?? Entry(DialogueCharacterId.Audere).Portrait, leftImage.sprite);
                    Assert.AreEqual(claims, root.InputGate.ActiveClaimCount);
                    EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                }
                Assert.IsFalse(controller.IsPlaying);
                Assert.AreEqual(1, callbacks); Assert.IsTrue(sawCreepy); Assert.IsTrue(sawNormalAfter);
                Assert.AreEqual(previousScale, Time.timeScale);
                int cancelled = 0;
                Assert.IsTrue(controller.PlayAuto(data, r => { Assert.AreEqual(DialogueResult.Cancelled, r); cancelled++; }, 1, 120, 0));
                yield return null;
                controller.ForceClose(); controller.ForceClose();
                Assert.AreEqual(1, cancelled);
                Assert.AreEqual(claims, root.InputGate.ActiveClaimCount);
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            EditorSceneManager.OpenScene(TeacherScene);
        }
    }
}
#endif

