#if UNITY_EDITOR
using System;
using System.Linq;
using Audere.Dialogue;
using Audere.Story.Steps;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat.Editor
{
    public static class CombatEnemyPrototypeAuthoring
    {
        private const string DataFolder = "Assets/_Audere/Data/Combat/Enemies";
        private const string MoveFolder = "Assets/_Audere/Data/Combat/Moves";
        private const string ActorFolder = "Assets/_Audere/Prefabs/Combat/Enemies";
        private const string BoardPath = "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab";
        private const string UiPath = "Assets/_Audere/Prefabs/UI/GameplayUIRoot.prefab";
        private const string BulletPath = "Assets/_Audere/Prefabs/Combat/Bullets/EnemyBullet.prefab";
        private const string VisualPath = "Assets/_Audere/AssetGame/Audere/audere mid.aseprite";
        private const string ActorPath = ActorFolder + "/Enemy_KhoangLang_PLACEHOLDER.prefab";
        private const string SampleActorPath = ActorFolder + "/Enemy_Sample_PLACEHOLDER.prefab";
        private const string ClassroomOldEncounter = "Assets/_Audere/Data/Combat/CombatEncounter_D1_CLASSROOM_PROTOTYPE.asset";
        private const string ClassroomEncounter = "Assets/_Audere/Data/Combat/CombatEncounter_D1_CLASSROOM_KHOANG_LANG.asset";
        private const string SampleEncounter = "Assets/_Audere/Data/Combat/CombatEncounter_Sample.asset";

        [MenuItem("Audere/Combat/Setup Multi-Phase Enemy Prototype")]
        public static void Setup()
        {
            EnsureFolder("Assets/_Audere/Data/Combat", "Enemies");
            EnsureFolder("Assets/_Audere/Data/Combat", "Moves");
            EnsureFolder("Assets/_Audere/Prefabs/Combat", "Enemies");

            TMP_FontAsset retryFont = MigrateBoardAndCreateActor();
            CreateRetryOverlay(retryFont);

            CombatBulletView bullet = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPath).GetComponent<CombatBulletView>();
            CombatEnemyActor actor = AssetDatabase.LoadAssetAtPath<GameObject>(ActorPath).GetComponent<CombatEnemyActor>();
            CombatEnemyActor sampleActor = AssetDatabase.LoadAssetAtPath<GameObject>(SampleActorPath).GetComponent<CombatEnemyActor>();
            LinearProjectilePatternMove aimed = CreateMove(
                "Move_AimedFan", bullet, LinearProjectileSpawnMode.ActorAnchor,
                LinearProjectileTargetMode.AimAtHeart, 8f, 1.15f, 3, 145f, 24f, 42f);
            LinearProjectilePatternMove sweep = CreateMove(
                "Move_SideSweep", bullet, LinearProjectileSpawnMode.AlternatingSides,
                LinearProjectileTargetMode.HorizontalIntoBoard, 9f, .85f, 2, 165f, 0f, 42f);
            LinearProjectilePatternMove rain = CreateMove(
                "Move_Rain", bullet, LinearProjectileSpawnMode.RandomTop,
                LinearProjectileTargetMode.Down, 8f, .72f, 3, 155f, 16f, 42f);

            CombatMoveSet aimedSet = CreateMoveSet("MoveSet_KhoangLang_P1_AimedFan", aimed);
            CombatMoveSet sweepSet = CreateMoveSet("MoveSet_KhoangLang_P2_SideSweep", sweep);
            CombatMoveSet rainSet = CreateMoveSet("MoveSet_KhoangLang_P3_Rain", rain);
            CombatMoveSet sampleSet = CreateMoveSet("MoveSet_Sample_DebugCycle", aimed, sweep, rain);

            CombatEnemyDefinition khoangLang = CreateEnemy(
                "Enemy_KhoangLang", "d1-classroom-khoang-lang", "Khoảng Lặng", actor,
                CombatPhasePolicy.PerPhaseHealth,
                new[] { "phase-1-placeholder", "phase-2-placeholder", "phase-3-placeholder" },
                new[] { aimedSet, sweepSet, rainSet }, new[] { 2, 2, 2 });
            CombatEnemyDefinition sample = CreateEnemy(
                "Enemy_Sample", "sample-debug-enemy", "Sample Enemy", sampleActor,
                CombatPhasePolicy.PerPhaseHealth,
                new[] { "sample-phase" }, new[] { sampleSet }, new[] { 12 });

            MigrateEncounter(ClassroomOldEncounter, ClassroomEncounter, "d1-classroom-khoang-lang", khoangLang, 45f);
            MigrateEncounter(SampleEncounter, SampleEncounter, "combat-sample", sample, 40f);
            AddKhoangLangCatalogEntry();
            MigrateScenes(actor, sampleActor);
            CombatTutorialAuthoring.ApplyD1ClassroomTutorial();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            CombatEnemyDefinitionValidator.ValidateAll();
            Debug.Log("[CombatEnemyPrototypeAuthoring] Multi-phase prototype, Retry overlay and scenes migrated.");
        }

        private static TMP_FontAsset MigrateBoardAndCreateActor()
        {
            GameObject board = PrefabUtility.LoadPrefabContents(BoardPath);
            try
            {
                TMP_Text oldMessage = board.transform.Find("Retry Panel/Retry Message")?.GetComponent<TMP_Text>();
                TMP_FontAsset font = oldMessage != null ? oldMessage.font : null;
                Transform oldVisual = board.transform.Find("Enemy/Enemy");
                CreateActorPrefab(ActorPath, "Enemy_KhoangLang_PLACEHOLDER");
                CreateActorPrefab(SampleActorPath, "Enemy_Sample_PLACEHOLDER");

                CombatRetryView retry = board.GetComponent<CombatRetryView>();
                if (retry != null) UnityEngine.Object.DestroyImmediate(retry, true);
                Transform retryPanel = board.transform.Find("Retry Panel");
                if (retryPanel != null) UnityEngine.Object.DestroyImmediate(retryPanel.gameObject, true);
                if (oldVisual != null) UnityEngine.Object.DestroyImmediate(oldVisual.gameObject, true);

                Transform enemyContainer = board.transform.Find("Enemy");
                Transform existingMount = enemyContainer.Find("Enemy Mount");
                GameObject mountObject = existingMount != null
                    ? existingMount.gameObject
                    : new GameObject("Enemy Mount", typeof(RectTransform));
                RectTransform mount = mountObject.GetComponent<RectTransform>();
                mount.SetParent(enemyContainer, false);
                mount.anchorMin = mount.anchorMax = new Vector2(.5f, .5f);
                mount.pivot = new Vector2(.5f, .5f);
                // Keep the enemy portrait in the header gap between HP and name,
                // leaving the dice field unobstructed.
                mount.anchoredPosition = new Vector2(0f, 350f);
                mount.sizeDelta = new Vector2(360f, 240f);
                mount.SetSiblingIndex(0);

                RectTransform nameRoot = enemyContainer.Find("Name") as RectTransform;
                RectTransform nameBackground = nameRoot != null ? nameRoot.Find("Image") as RectTransform : null;
                TMP_Text enemyName = nameRoot != null ? nameRoot.Find("Enemy Name")?.GetComponent<TMP_Text>() : null;
                if (nameRoot != null && nameBackground != null && enemyName != null)
                {
                    // The Image is the authored name container. Keep both background
                    // and text in one rect so longer enemy names wrap inside it.
                    nameRoot.anchorMin = nameRoot.anchorMax = new Vector2(.5f, .5f);
                    nameRoot.pivot = new Vector2(.5f, .5f);
                    nameRoot.anchoredPosition = new Vector2(256f, 431f);
                    nameRoot.sizeDelta = new Vector2(420f, 120f);

                    nameBackground.anchorMin = Vector2.zero;
                    nameBackground.anchorMax = Vector2.one;
                    nameBackground.pivot = new Vector2(.5f, .5f);
                    nameBackground.anchoredPosition = Vector2.zero;
                    nameBackground.sizeDelta = Vector2.zero;
                    Image backgroundImage = nameBackground.GetComponent<Image>();
                    if (backgroundImage != null)
                        backgroundImage.raycastTarget = false;

                    RectTransform nameTextRect = enemyName.rectTransform;
                    nameTextRect.anchorMin = Vector2.zero;
                    nameTextRect.anchorMax = Vector2.one;
                    nameTextRect.pivot = new Vector2(.5f, .5f);
                    nameTextRect.anchoredPosition = Vector2.zero;
                    nameTextRect.sizeDelta = new Vector2(-24f, -12f);
                    enemyName.fontSize = 57f;
                    enemyName.enableAutoSizing = false;
                    enemyName.textWrappingMode = TextWrappingModes.Normal;
            // The current handwritten font has no ellipsis glyph. The widened
            // two-line frame already fits the production name, so truncate only
            // as a final containment guard instead of producing a missing-glyph warning.
            enemyName.overflowMode = TextOverflowModes.Truncate;
                    enemyName.alignment = TextAlignmentOptions.Center;
                }

                CombatBoardView view = board.GetComponent<CombatBoardView>();
                SerializedObject serialized = new SerializedObject(view);
                serialized.FindProperty("enemyMount").objectReferenceValue = mount;
                serialized.FindProperty("enemyVisual").objectReferenceValue = null;
                serialized.FindProperty("vfxRoot").objectReferenceValue = null;
                serialized.FindProperty("damageNumberFontSize").floatValue = 52f;
                serialized.FindProperty("damageNumberDuration").floatValue = .68f;
                serialized.FindProperty("damageNumberRiseDistance").floatValue = 52f;
                serialized.FindProperty("damageNumberSpawnSpread").floatValue = 10f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(board, BoardPath);
                return font;
            }
            finally { PrefabUtility.UnloadPrefabContents(board); }
        }

        private static void CreateActorPrefab(string assetPath, string rootName)
        {
            GameObject root = new GameObject(rootName, typeof(RectTransform), typeof(CombatEnemyActor));
            try
            {
                RectTransform rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(360f, 240f);
                GameObject visual = new GameObject(
                    "Visual_audere-mid_PLACEHOLDER",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform visualRect = visual.GetComponent<RectTransform>();
                visualRect.SetParent(root.transform, false);
                visualRect.anchorMin = visualRect.anchorMax = new Vector2(.5f, .5f);
                visualRect.pivot = new Vector2(.5f, .5f);
                visualRect.anchoredPosition = new Vector2(0f, 2f);
                visualRect.sizeDelta = new Vector2(90f, 186f);
                Image visualImage = visual.GetComponent<Image>();
                visualImage.sprite = AssetDatabase.LoadAllAssetsAtPath(VisualPath)
                    .OfType<Sprite>()
                    .FirstOrDefault(sprite => sprite.name == "audere mid") ??
                    AssetDatabase.LoadAllAssetsAtPath(VisualPath).OfType<Sprite>().FirstOrDefault();
                visualImage.preserveAspect = true;
                visualImage.raycastTarget = false;
                visualImage.color = Color.white;

                RectTransform vfx = new GameObject("VFX Anchor_PLACEHOLDER", typeof(RectTransform))
                    .GetComponent<RectTransform>();
                vfx.SetParent(root.transform, false);
                vfx.anchorMin = vfx.anchorMax = new Vector2(.5f, .5f);
                vfx.anchoredPosition = new Vector2(0f, 8f);

                RectTransform projectile = new GameObject("Projectile Origin_PLACEHOLDER", typeof(RectTransform))
                    .GetComponent<RectTransform>();
                projectile.SetParent(root.transform, false);
                projectile.anchorMin = projectile.anchorMax = new Vector2(.5f, .5f);
                projectile.anchoredPosition = new Vector2(0f, 92f);

                RectTransform damage = new GameObject("Damage Anchor_PLACEHOLDER", typeof(RectTransform))
                    .GetComponent<RectTransform>();
                damage.SetParent(root.transform, false);
                damage.anchorMin = damage.anchorMax = new Vector2(.5f, .5f);
                damage.anchoredPosition = new Vector2(54f, 66f);

                CombatEnemyActor actor = root.GetComponent<CombatEnemyActor>();
                SerializedObject serialized = new SerializedObject(actor);
                serialized.FindProperty("visualRoot").objectReferenceValue = visual.transform;
                serialized.FindProperty("projectileOrigin").objectReferenceValue = projectile;
                serialized.FindProperty("vfxAnchor").objectReferenceValue = vfx;
                serialized.FindProperty("damageAnchor").objectReferenceValue = damage;
                Renderer[] renderers = Array.Empty<Renderer>();
                SerializedProperty rendererArray = serialized.FindProperty("renderers");
                rendererArray.arraySize = renderers.Length;
                for (int i = 0; i < renderers.Length; i++) rendererArray.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                Graphic[] graphics = visual.GetComponentsInChildren<Graphic>(true);
                SerializedProperty graphicArray = serialized.FindProperty("graphics");
                graphicArray.arraySize = graphics.Length;
                for (int i = 0; i < graphics.Length; i++) graphicArray.GetArrayElementAtIndex(i).objectReferenceValue = graphics[i];
                serialized.FindProperty("mechanicModules").arraySize = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void CreateRetryOverlay(TMP_FontAsset font)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(UiPath);
            try
            {
                Transform existing = root.transform.Find("CombatRetryUI");
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject, true);

                GameObject retryUi = UiObject("CombatRetryUI", root.transform);
                Stretch(retryUi.GetComponent<RectTransform>());
                Canvas canvas = retryUi.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 1200;
                CanvasScaler scaler = retryUi.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = .5f;
                retryUi.AddComponent<GraphicRaycaster>();
                CombatRetryView retryView = retryUi.AddComponent<CombatRetryView>();

                GameObject panel = UiObject("Retry Panel", retryUi.transform);
                Stretch(panel.GetComponent<RectTransform>());
                GameObject blocker = UiObject("Fullscreen Blocker", panel.transform, typeof(Image));
                Stretch(blocker.GetComponent<RectTransform>());
                Image blockerImage = blocker.GetComponent<Image>();
                blockerImage.color = new Color(.035f, .025f, .055f, .94f);
                blockerImage.raycastTarget = true;

                GameObject content = UiObject("Retry Content", panel.transform, typeof(Image));
                RectTransform contentRect = content.GetComponent<RectTransform>();
                contentRect.anchorMin = contentRect.anchorMax = contentRect.pivot = new Vector2(.5f, .5f);
                contentRect.sizeDelta = new Vector2(680f, 330f);
                content.GetComponent<Image>().color = new Color(.12f, .09f, .16f, 1f);

                GameObject messageObject = UiObject("Retry Message", content.transform, typeof(TextMeshProUGUI));
                RectTransform messageRect = messageObject.GetComponent<RectTransform>();
                messageRect.anchorMin = new Vector2(.1f, .48f);
                messageRect.anchorMax = new Vector2(.9f, .88f);
                messageRect.offsetMin = messageRect.offsetMax = Vector2.zero;
                TextMeshProUGUI message = messageObject.GetComponent<TextMeshProUGUI>();
                if (font != null) message.font = font;
                message.fontSize = 42f;
                message.alignment = TextAlignmentOptions.Center;
                message.color = Color.white;
                message.text = "Không sao. Tớ vẫn ở đây.\nMình thử lại nhé.";
                message.raycastTarget = false;

                GameObject buttonObject = UiObject("Retry Button", content.transform, typeof(Image), typeof(Button));
                RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
                buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(.5f, .25f);
                buttonRect.sizeDelta = new Vector2(300f, 88f);
                Image buttonImage = buttonObject.GetComponent<Image>();
                buttonImage.color = new Color(.52f, .34f, .58f, 1f);
                Button button = buttonObject.GetComponent<Button>();
                button.targetGraphic = buttonImage;

                GameObject labelObject = UiObject("Label", buttonObject.transform, typeof(TextMeshProUGUI));
                Stretch(labelObject.GetComponent<RectTransform>());
                TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
                if (font != null) label.font = font;
                label.fontSize = 38f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.white;
                label.text = "Thử lại";
                label.raycastTarget = false;

                SerializedObject retrySerialized = new SerializedObject(retryView);
                retrySerialized.FindProperty("retryRoot").objectReferenceValue = panel;
                retrySerialized.FindProperty("messageText").objectReferenceValue = message;
                retrySerialized.FindProperty("retryButton").objectReferenceValue = button;
                retrySerialized.ApplyModifiedPropertiesWithoutUndo();

                GameplayUIRoot uiRoot = root.GetComponent<GameplayUIRoot>();
                SerializedObject rootSerialized = new SerializedObject(uiRoot);
                rootSerialized.FindProperty("combatRetry").objectReferenceValue = retryView;
                rootSerialized.ApplyModifiedPropertiesWithoutUndo();
                panel.SetActive(false);
                retryUi.transform.SetAsLastSibling();
                PrefabUtility.SaveAsPrefabAsset(root, UiPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static LinearProjectilePatternMove CreateMove(string name, CombatBulletView bullet,
            LinearProjectileSpawnMode spawn, LinearProjectileTargetMode target, float duration,
            float interval, int count, float speed, float spread, float spacing)
        {
            string path = MoveFolder + "/" + name + ".asset";
            LinearProjectilePatternMove asset = LoadOrCreate<LinearProjectilePatternMove>(path);
            SerializedObject serialized = new SerializedObject(asset);
            Set(serialized, "duration", duration);
            Set(serialized, "projectilePrefab", bullet);
            Set(serialized, "spawnMode", (int)spawn);
            Set(serialized, "targetMode", (int)target);
            Set(serialized, "shotInterval", interval);
            Set(serialized, "projectilesPerShot", count);
            Set(serialized, "speed", speed);
            Set(serialized, "spreadDegrees", spread);
            Set(serialized, "spacing", spacing);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static CombatMoveSet CreateMoveSet(string name, params CombatMoveDefinition[] moves)
        {
            CombatMoveSet asset = LoadOrCreate<CombatMoveSet>(MoveFolder + "/" + name + ".asset");
            SerializedObject serialized = new SerializedObject(asset);
            Set(serialized, "selectionPolicy", (int)CombatMoveSelectionPolicy.OrderedLoop);
            SerializedProperty entries = serialized.FindProperty("entries");
            entries.arraySize = moves.Length;
            for (int i = 0; i < moves.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("move").objectReferenceValue = moves[i];
                entry.FindPropertyRelative("weight").floatValue = 1f;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static CombatEnemyDefinition CreateEnemy(string assetName, string id, string displayName,
            CombatEnemyActor actor, CombatPhasePolicy policy, string[] phaseIds, CombatMoveSet[] moveSets, int[] health)
        {
            CombatEnemyDefinition asset = LoadOrCreate<CombatEnemyDefinition>(DataFolder + "/" + assetName + ".asset");
            SerializedObject serialized = new SerializedObject(asset);
            Set(serialized, "enemyId", id);
            Set(serialized, "displayName", displayName);
            Set(serialized, "actorPrefab", actor);
            Set(serialized, "phasePolicy", (int)policy);
            Set(serialized, "sharedMaxHealth", health.Sum());
            SerializedProperty phases = serialized.FindProperty("phases");
            phases.arraySize = phaseIds.Length;
            for (int i = 0; i < phaseIds.Length; i++)
            {
                SerializedProperty phase = phases.GetArrayElementAtIndex(i);
                phase.FindPropertyRelative("phaseId").stringValue = phaseIds[i];
                phase.FindPropertyRelative("maxHealth").intValue = health[i];
                phase.FindPropertyRelative("sharedExitThreshold").intValue = i == phaseIds.Length - 1 ? 0 : health.Skip(i + 1).Sum();
                phase.FindPropertyRelative("duration").floatValue = 8f;
                phase.FindPropertyRelative("moveSet").objectReferenceValue = moveSets[i];
                phase.FindPropertyRelative("dialogueCues").arraySize = 0;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void MigrateEncounter(string oldPath, string targetPath, string id, CombatEnemyDefinition enemy, float duration)
        {
            if (oldPath != targetPath && AssetDatabase.LoadAssetAtPath<CombatEncounterData>(targetPath) == null)
            {
                string moveError = AssetDatabase.MoveAsset(oldPath, targetPath);
                if (!string.IsNullOrEmpty(moveError)) throw new InvalidOperationException(moveError);
            }
            CombatEncounterData encounter = AssetDatabase.LoadAssetAtPath<CombatEncounterData>(targetPath);
            SerializedObject serialized = new SerializedObject(encounter);
            Set(serialized, "encounterId", id);
            Set(serialized, "enemyDefinition", enemy);
            Set(serialized, "encounterDuration", duration);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(encounter);
        }

        private static void AddKhoangLangCatalogEntry()
        {
            DialogueCharacterCatalog catalog = AssetDatabase.LoadAssetAtPath<DialogueCharacterCatalog>(
                "Assets/_Audere/Data/Dialogue/DialogueCharacterCatalog.asset");
            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty entries = serialized.FindProperty("characters");
            for (int i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("character").enumValueIndex == (int)DialogueCharacterId.KhoangLang)
                    return;
            entries.arraySize++;
            SerializedProperty entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            entry.FindPropertyRelative("character").enumValueIndex = (int)DialogueCharacterId.KhoangLang;
            entry.FindPropertyRelative("displayName").stringValue = "Khoảng Lặng";
            entry.FindPropertyRelative("portrait").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void MigrateScenes(CombatEnemyActor classroomActor, CombatEnemyActor sampleActor)
        {
            string activePath = EditorSceneManager.GetActiveScene().path;
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene("Assets/_Audere/Scenes/30_Classroom.unity", OpenSceneMode.Single);
            Transform story = scene.GetRootGameObjects().First(go => go.name == "STORY").transform;
            Transform step = story.Find("D1_CLASSROOM_RECESS_BIANCA/210_PlayCombatPrototype");
            if (step != null) step.name = "210_PlayKhoangLangPrototype";
            BindEnemyAuthoringPreview(scene, classroomActor);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            UnityEngine.SceneManagement.Scene sampleScene = EditorSceneManager.OpenScene("Assets/_Audere/Scenes/20_Game.unity", OpenSceneMode.Single);
            BindEnemyAuthoringPreview(sampleScene, sampleActor);
            EditorSceneManager.MarkSceneDirty(sampleScene);
            EditorSceneManager.SaveScene(sampleScene);
            if (!string.IsNullOrEmpty(activePath) && activePath != sampleScene.path)
                EditorSceneManager.OpenScene(activePath, OpenSceneMode.Single);
        }

        private static void BindEnemyAuthoringPreview(
            UnityEngine.SceneManagement.Scene scene,
            CombatEnemyActor actorPrefab)
        {
            if (actorPrefab == null)
                throw new MissingReferenceException($"Scene '{scene.name}' has no enemy actor prefab for its preview.");

            CombatBoardView board = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<CombatBoardView>(true))
                .FirstOrDefault();
            if (board == null)
            {
                Debug.LogWarning(
                    $"[CombatEnemyPrototypeAuthoring] Scene '{scene.name}' creates its board at runtime; " +
                    "the EnemyDefinition still references its dedicated actor prefab, so no edit-mode preview was added.");
                return;
            }

            SerializedObject boardSerialized = new SerializedObject(board);
            Transform mount = boardSerialized.FindProperty("enemyMount").objectReferenceValue as Transform;
            if (mount == null)
                throw new MissingReferenceException($"CombatBoardView in '{scene.name}' has no Enemy Mount.");

            for (int i = mount.childCount - 1; i >= 0; i--)
            {
                Transform child = mount.GetChild(i);
                if (child.name.EndsWith("__AUTHORING_PREVIEW", StringComparison.Ordinal))
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            GameObject preview = (GameObject)PrefabUtility.InstantiatePrefab(actorPrefab.gameObject, scene);
            preview.name = actorPrefab.name + "__AUTHORING_PREVIEW";
            preview.transform.SetParent(mount, false);
            preview.SetActive(true);
            boardSerialized.Update();
            boardSerialized.FindProperty("authoredEnemyPreview").objectReferenceValue =
                preview.GetComponent<CombatEnemyActor>();
            boardSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }

        private static GameObject UiObject(string name, Transform parent, params Type[] components)
        {
            Type[] all = new[] { typeof(RectTransform) }.Concat(components).ToArray();
            GameObject result = new GameObject(name, all);
            result.transform.SetParent(parent, false);
            return result;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Set(SerializedObject serialized, string name, object value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            switch (value)
            {
                case string text: property.stringValue = text; break;
                case int integer: property.intValue = integer; break;
                case float number: property.floatValue = number; break;
                case UnityEngine.Object reference: property.objectReferenceValue = reference; break;
                default: throw new InvalidOperationException($"Unsupported serialized value for '{name}'.");
            }
        }
    }
}
#endif
