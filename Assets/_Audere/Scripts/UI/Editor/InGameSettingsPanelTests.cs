#if UNITY_EDITOR
using System.Linq;
using Audere.GameplayInput;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.UI.Editor.Tests
{
    public sealed class InGameSettingsPanelTests
    {
        private const string PrefabPath = "Assets/_Audere/Prefabs/UI/GameplayUIRoot.prefab";
        private const string IconPath = "Assets/_Audere/AssetGame/UI/InGameSettingsIcon.png";

        [Test]
        public void GameplayUiPrefab_InGameSettingsKeepsAudioAndReplacesDifficultyWithMainMenu()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                InGameSettingsPanel settings = root.GetComponent<InGameSettingsPanel>();
                Assert.IsNotNull(settings);

                SerializedObject serialized = new SerializedObject(settings);
                string[] requiredReferences =
                {
                    "panelRoot",
                    "settingsButton",
                    "closeButton",
                    "mainMenuButton",
                    "musicSlider",
                    "sfxSlider",
                    "musicValueText",
                    "sfxValueText",
                };

                foreach (string propertyName in requiredReferences)
                    Assert.IsNotNull(
                        serialized.FindProperty(propertyName).objectReferenceValue,
                        propertyName + " must be a direct prefab reference.");

                GameObject panelRoot = (GameObject)serialized.FindProperty("panelRoot").objectReferenceValue;
                Assert.IsFalse(panelRoot.activeSelf);
                Assert.AreEqual("InGameSettingsOverlay", panelRoot.name);

                Button mainMenu = (Button)serialized.FindProperty("mainMenuButton").objectReferenceValue;
                Assert.AreEqual("MainMenuButton", mainMenu.name);
                Assert.AreEqual("Thoát ra Main Menu", mainMenu.GetComponentInChildren<TMPro.TextMeshProUGUI>(true).text);

                string[] forbiddenDifficultyObjects =
                {
                    "DifficultyLabel",
                    "DifficultyEasy",
                    "DifficultyHard",
                    "DifficultyDescription",
                };
                string[] names = panelRoot.GetComponentsInChildren<Transform>(true).Select(x => x.name).ToArray();
                foreach (string forbidden in forbiddenDifficultyObjects)
                    CollectionAssert.DoesNotContain(names, forbidden);

                Button opener = (Button)serialized.FindProperty("settingsButton").objectReferenceValue;
                Assert.AreEqual("InGameSettingsButton", opener.name);
                RectTransform openerRect = (RectTransform)opener.transform;
                Assert.AreEqual(Vector2.one, openerRect.anchorMin);
                Assert.AreEqual(Vector2.one, openerRect.anchorMax);

                Image icon = opener.transform.Find("Icon").GetComponent<Image>();
                Assert.IsNotNull(icon.sprite);
                Assert.AreEqual(IconPath, AssetDatabase.GetAssetPath(icon.sprite));
                Assert.IsTrue(icon.preserveAspect);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

[Test]
        public void NestedPauseReleasedBehindOpenPanel_CloseRestoresLiveScale()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            float originalTimeScale = Time.timeScale;
            try
            {
                InGameSettingsPanel settings = root.GetComponent<InGameSettingsPanel>();
                Assert.IsNotNull(settings);

                Time.timeScale = 1f;
                settings.Open();
                Assert.IsTrue(settings.IsOpen);
                Assert.AreEqual(0f, Time.timeScale);
                settings.Close();
                Assert.AreEqual(1f, Time.timeScale);

                Time.timeScale = 0f;
                settings.Open();
                Assert.IsTrue(settings.IsOpen);

                // Simulate dialogue releasing its own pause while Settings is still open.
                Time.timeScale = .25f;
                var maintainPause = typeof(InGameSettingsPanel).GetMethod(
                    "MaintainSettingsPause",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.IsNotNull(maintainPause);
                maintainPause.Invoke(settings, null);

                Assert.AreEqual(0f, Time.timeScale);
                Assert.IsTrue(InGameSettingsPanel.TryGetResumeTimeScale(out float dialogueResumeScale));
                Assert.AreEqual(.25f, dialogueResumeScale,
                    "Dialogue started behind Settings must inherit the real resume scale, not zero.");
                settings.Close();
                Assert.AreEqual(.25f, Time.timeScale,
                    "Settings must restore the live scale released by the previous pause owner.");
            }
            finally
            {
                Time.timeScale = originalTimeScale;
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void ModalPanel_BlocksDialogueAndCombatClaims_ThenRestoresTheirOwnership()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            GameObject dialogueOwner = new GameObject("Dialogue input owner");
            GameObject combatOwner = new GameObject("Combat input owner");
            float originalTimeScale = Time.timeScale;
            try
            {
                InGameSettingsPanel settings = root.GetComponent<InGameSettingsPanel>();
                Audere.Dialogue.GameplayUIRoot gameplayUi = root.GetComponent<Audere.Dialogue.GameplayUIRoot>();
                GameplayInputGate gate = gameplayUi != null ? gameplayUi.InputGate : null;
                Assert.IsNotNull(settings);
                Assert.IsNotNull(gameplayUi);
                Assert.IsNotNull(gate);

                GameplayInputToken combat = gate.PushMode(combatOwner, GameplayInputMode.Combat);
                Assert.IsTrue(combat.IsValid);
                Assert.IsTrue(gate.Allows(GameplayInputMode.Combat));

                GameplayInputToken dialogue = gate.PushMode(dialogueOwner, GameplayInputMode.Dialogue);
                Assert.IsTrue(dialogue.IsValid);
                Assert.IsTrue(gate.Allows(GameplayInputMode.Dialogue));

                Time.timeScale = 1f;
                settings.Open();

                Assert.IsTrue(settings.IsOpen);
                Assert.AreEqual(GameplayInputMode.Modal, gate.CurrentMode);
                Assert.IsFalse(gate.Allows(GameplayInputMode.Dialogue));
                Assert.IsFalse(gate.Allows(GameplayInputMode.Combat));

                settings.Close();

                Assert.AreEqual(GameplayInputMode.Dialogue, gate.CurrentMode);
                Assert.IsFalse(gate.Allows(GameplayInputMode.Dialogue),
                    "The click that closes a modal must not advance dialogue in the same frame.");
                Assert.IsTrue(gate.Release(dialogue));
                Assert.AreEqual(GameplayInputMode.Combat, gate.CurrentMode);
                Assert.IsFalse(gate.Allows(GameplayInputMode.Combat),
                    "The click that closes a modal must not reach combat in the same frame.");
                Assert.IsTrue(gate.Release(combat));
            }
            finally
            {
                Time.timeScale = originalTimeScale;
                Object.DestroyImmediate(dialogueOwner);
                Object.DestroyImmediate(combatOwner);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

    }
}
#endif
