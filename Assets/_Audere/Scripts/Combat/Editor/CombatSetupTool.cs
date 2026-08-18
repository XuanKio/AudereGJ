using Audere.Combat;
using Audere.Dialogue;
using Audere.Puzzle;
using Audere.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Audere.EditorTools
{
    public static class CombatSetupTool
    {
        private const string ScenePath = "Assets/_Audere/Scenes/20_Game.unity";
        private const string BoardPrefabPath = "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab";
        private const string DicePrefabFolder = "Assets/_Audere/Prefabs/Combat/Dice";
        private const string AttackDicePrefabPath = DicePrefabFolder + "/Dice_Attack.prefab";
        private const string ArmorDicePrefabPath = DicePrefabFolder + "/Dice_Armor.prefab";
        private const string HealDicePrefabPath = DicePrefabFolder + "/Dice_Heal.prefab";
        private const string BulletPrefabFolder = "Assets/_Audere/Prefabs/Combat/Bullets";
        private const string EnemyBulletPrefabPath = BulletPrefabFolder + "/EnemyBullet.prefab";
        private const string PlayerPrefabFolder = "Assets/_Audere/Prefabs/Combat/Player";
        private const string HeartVisualPrefabPath = PlayerPrefabFolder + "/HeartVisual.prefab";
        private const string StunZoneShaderPath = "Assets/_Audere/Shaders/UIStunZoneDots.shader";
        private const string StunZoneMaterialPath = "Assets/_Audere/Materials/UI_StunZoneDots.mat";
        private const string EncounterPath = "Assets/_Audere/Data/Combat/CombatEncounter_Sample.asset";
        private const string FontPath = "Assets/_Audere/AssetGame/Font/MTO-Astro-City SDF.asset";
        private const string DamageNumberFontPath = "Assets/_Audere/AssetGame/Font/deltarune SDF.asset";
        private const string AttackIconPath = "Assets/_Audere/AssetGame/IconDice/attack.aseprite";
        private const string ArmorIconPath = "Assets/_Audere/AssetGame/IconDice/gaurd.aseprite";
        private const string HealIconPath = "Assets/_Audere/AssetGame/IconDice/heal.aseprite";
        private const string DiceFramePath = "Assets/_Audere/AssetGame/IconDice/dice (1).aseprite";
        private const string BlockedIconPath = "Assets/_Audere/AssetGame/IconDice/X.aseprite";
        private const string ScratchVfxPath = "Assets/_Audere/AssetGame/Vfx/scratch.aseprite";

        [MenuItem("Audere/Combat/Setup Combat Foundation")]
        public static void SetupCombatFoundation()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[CombatSetup] Stop Play Mode before running the setup tool.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                Debug.LogError($"[CombatSetup] Open '{ScenePath}' before running setup.");
                return;
            }

            MigrateLegacyCombatPrefabs();
            SetupDicePrefabs();
            SetupBulletPrefab();
            SetupHeartVisualPrefab();
            SetupStunZoneMaterial();
            SetupBoardPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameObject world = FindRoot(scene, "WORLD");
            GameObject systems = FindRoot(scene, "SYSTEMS");
            if (world == null || systems == null)
            {
                Debug.LogError("[CombatSetup] WORLD or SYSTEMS root is missing.");
                return;
            }

            Transform puzzleRoot = FindDirectChild(world.transform, "Puzzle Root");
            Transform combatRoot = FindDirectChild(world.transform, "Combat Root");
            if (puzzleRoot == null || combatRoot == null)
            {
                Debug.LogError("[CombatSetup] Puzzle Root or Combat Root is missing under WORLD.");
                return;
            }

            GameObject puzzleSystems = EnsureObject(systems.transform, "Puzzle Systems");
            GameObject combatSystems = EnsureObject(systems.transform, "Combat Systems");
            MoveSystem(systems.transform, puzzleSystems.transform, "Puzzle Manager");
            MoveSystem(systems.transform, puzzleSystems.transform, "Path Placement Controller");
            MoveSystem(systems.transform, puzzleSystems.transform, "Board Controller");

            CombatBoardView boardView = combatRoot.GetComponentInChildren<CombatBoardView>(true);
            if (boardView == null)
            {
                Debug.LogError("[CombatSetup] CombatBoardView did not propagate to the scene prefab instance.");
                return;
            }

            CombatEncounterData encounter = AssetDatabase.LoadAssetAtPath<CombatEncounterData>(EncounterPath);
            if (encounter == null)
            {
                encounter = ScriptableObject.CreateInstance<CombatEncounterData>();
                AssetDatabase.CreateAsset(encounter, EncounterPath);
            }
            ConfigureEncounter(encounter);

            GameObject controllerObject = EnsureObject(combatSystems.transform, "Combat Controller");
            CombatController combatController = GetOrAdd<CombatController>(controllerObject);
            SerializedObject combatSerialized = new SerializedObject(combatController);
            SetObject(combatSerialized, "encounterData", encounter);
            SetObject(combatSerialized, "boardView", boardView);
            combatSerialized.ApplyModifiedPropertiesWithoutUndo();

            CanvasGroup transitionFade = EnsureTransitionOverlay(world.transform);
            Camera mainCamera = Camera.main;
            GameObject puzzleViewportMask = MovePuzzleViewportMaskToCamera(puzzleRoot, mainCamera);
            GridCameraFollow2D cameraFollow = mainCamera != null
                ? mainCamera.GetComponent<GridCameraFollow2D>()
                : null;

            WorldModeController modeController = GetOrAdd<WorldModeController>(world);
            SerializedObject modeSerialized = new SerializedObject(modeController);
            modeSerialized.FindProperty("startingMode").enumValueIndex = (int)WorldGameplayMode.Combat;
            SetObject(modeSerialized, "puzzleRoot", puzzleRoot.gameObject);
            SetObject(modeSerialized, "combatRoot", combatRoot.gameObject);
            SetObject(modeSerialized, "puzzleViewportMask", puzzleViewportMask);
            SetObject(modeSerialized, "puzzleSystemsRoot", puzzleSystems);
            SetObject(modeSerialized, "combatSystemsRoot", combatSystems);
            SetObject(modeSerialized, "transitionFade", transitionFade);
            SetObject(modeSerialized, "worldCamera", mainCamera);
            SetObject(modeSerialized, "puzzleCameraFollow", cameraFollow);
            modeSerialized.ApplyModifiedPropertiesWithoutUndo();

            puzzleRoot.gameObject.SetActive(false);
            combatRoot.gameObject.SetActive(true);
            if (puzzleViewportMask != null)
                puzzleViewportMask.SetActive(false);
            puzzleSystems.SetActive(false);
            combatSystems.SetActive(true);

            GameplayUIRoot gameplayUi = Object.FindFirstObjectByType<GameplayUIRoot>(FindObjectsInactive.Include);
            if (gameplayUi != null && gameplayUi.PuzzleUi != null)
                gameplayUi.PuzzleUi.gameObject.SetActive(false);

            EditorUtility.SetDirty(world);
            EditorUtility.SetDirty(systems);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[CombatSetup] WORLD mode coordinator and data-driven combat foundation are ready.");
        }

        private static GameObject MovePuzzleViewportMaskToCamera(Transform puzzleRoot, Camera mainCamera)
        {
            if (mainCamera == null)
                return null;

            Transform viewportMask = mainCamera.transform.Find("PuzzleViewportMask");
            if (viewportMask == null)
                viewportMask = puzzleRoot.Find("PuzzleViewportMask");
            if (viewportMask == null)
                return null;

            viewportMask.SetParent(mainCamera.transform, false);
            viewportMask.localPosition = new Vector3(0f, 0f, 9f);
            viewportMask.localRotation = Quaternion.identity;
            viewportMask.localScale = Vector3.one * .5814f;
            return viewportMask.gameObject;
        }

        [MenuItem("Audere/Combat/Debug/Switch To Puzzle")]
        public static void DebugSwitchToPuzzle()
        {
            SwitchMode(WorldGameplayMode.Puzzle);
        }

        [MenuItem("Audere/Combat/Debug/Switch To Combat")]
        public static void DebugSwitchToCombat()
        {
            SwitchMode(WorldGameplayMode.Combat);
        }

        [MenuItem("Audere/Combat/Debug/Preview Enemy White Flash")]
        public static void DebugPreviewEnemyWhiteFlash()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[CombatSetup] Enter Play Mode before previewing enemy hit flash.");
                return;
            }

            CombatBoardView boardView = Object.FindFirstObjectByType<CombatBoardView>();
            if (boardView != null)
                boardView.PreviewEnemyWhiteFlash();
        }

        [MenuItem("Audere/Combat/Debug/Apply Attack Dice")]
        public static void DebugApplyAttackDice()
        {
            FindCombatController()?.DebugApplyDiceEffect(CombatSymbol.Attack);
        }

        [MenuItem("Audere/Combat/Debug/Apply Armor Dice")]
        public static void DebugApplyArmorDice()
        {
            FindCombatController()?.DebugApplyDiceEffect(CombatSymbol.Armor);
        }

        [MenuItem("Audere/Combat/Debug/Apply Heal Dice")]
        public static void DebugApplyHealDice()
        {
            FindCombatController()?.DebugApplyDiceEffect(CombatSymbol.Heal);
        }

        [MenuItem("Audere/Combat/Debug/Take Player Hit")]
        public static void DebugTakePlayerHit()
        {
            FindCombatController()?.DebugTakePlayerHit();
        }

        [MenuItem("Audere/Combat/Debug/Expire Timer")]
        public static void DebugExpireTimer()
        {
            FindCombatController()?.DebugExpireTimer();
        }

        [MenuItem("Audere/Combat/Debug/Set Timer To Half")]
        public static void DebugSetTimerHalf()
        {
            CombatController controller = FindCombatController();
            if (controller != null)
                controller.DebugSetTimerHalf();
        }

        [MenuItem("Audere/Combat/Debug/Preview Timer Fill Half")]
        public static void DebugPreviewTimerFillHalf()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[CombatSetup] Enter Play Mode before previewing timer fill.");
                return;
            }

            CombatBoardView boardView = Object.FindFirstObjectByType<CombatBoardView>();
            if (boardView == null)
            {
                Debug.LogError("[CombatSetup] CombatBoardView was not found.");
                return;
            }
            boardView.UpdateTimer(.5f);
            Debug.Log("[CombatSetup] Timer Fill preview set to 50%.");
        }

        private static CombatController FindCombatController()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[CombatSetup] Enter Play Mode before using combat debug actions.");
                return null;
            }
            CombatController controller = Object.FindFirstObjectByType<CombatController>();
            if (controller == null)
                Debug.LogError("[CombatSetup] CombatController was not found.");
            return controller;
        }

        private static void SwitchMode(WorldGameplayMode mode)
        {
            WorldModeController controller = Object.FindFirstObjectByType<WorldModeController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                Debug.LogError("[CombatSetup] WorldModeController was not found.");
                return;
            }

            if (Application.isPlaying)
                controller.SwitchTo(mode);
            else
            {
                controller.ApplyModeImmediate(mode);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }

        private static void SetupStunZoneMaterial()
        {
            EnsureAssetFolder("Assets/_Audere/Materials");
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(StunZoneShaderPath);
            if (shader == null)
                throw new MissingReferenceException($"Stun Zone shader is missing at '{StunZoneShaderPath}'.");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(StunZoneMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "UI_StunZoneDots" };
                AssetDatabase.CreateAsset(material, StunZoneMaterialPath);
            }
            else if (material.shader != shader)
                material.shader = shader;

            material.SetColor("_BackgroundColor", new Color(.13f, .11f, .17f, .78f));
            material.SetColor("_DotColor", new Color(.34f, .26f, .39f, .90f));
            material.SetVector("_Grid", new Vector4(28f, 68f, 0f, 0f));
            material.SetFloat("_DotSize", .12f);
            EditorUtility.SetDirty(material);
        }

        private static CombatCatchCursorView SetupCatchCursorVisual(RectTransform cursor, Image cursorImage)
        {
            RectTransform border = EnsureStretchRect(cursor, "Cursor Border");
            border.SetAsFirstSibling();

            for (int i = border.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(border.GetChild(i).gameObject);

            Image oldBorderImage = border.GetComponent<Image>();
            if (oldBorderImage != null)
                Object.DestroyImmediate(oldBorderImage);
            CombatDashedRingGraphic borderGraphic = border.GetComponent<CombatDashedRingGraphic>();
            if (borderGraphic == null)
            {
                borderGraphic = border.gameObject.AddComponent<CombatDashedRingGraphic>();
                borderGraphic.Configure(8, 8f, .68f, 3f, 22.5f, 8);
            }
            borderGraphic.color = new Color(.95f, .92f, 1f, .96f);
            borderGraphic.raycastTarget = false;

            RectTransform blocked = EnsureRect(cursor, "Blocked X");
            ConfigureRect(blocked, Vector2.zero, new Vector2(100f, 100f));
            CanvasGroup blockedGroup = GetOrAdd<CanvasGroup>(blocked.gameObject);
            blockedGroup.alpha = 0f;
            blockedGroup.blocksRaycasts = false;
            blockedGroup.interactable = false;

            Transform slashA = FindDirectChild(blocked, "Slash A");
            if (slashA != null) Object.DestroyImmediate(slashA.gameObject);
            Transform slashB = FindDirectChild(blocked, "Slash B");
            if (slashB != null) Object.DestroyImmediate(slashB.gameObject);

            RectTransform blockedVisual = EnsureRect(blocked, "Blocked X Visual");
            ConfigureRect(blockedVisual, Vector2.zero, new Vector2(76f, 76f));
            Image blockedImage = GetOrAdd<Image>(blockedVisual.gameObject);
            blockedImage.sprite = LoadFirstSprite(BlockedIconPath);
            blockedImage.color = Color.white;
            blockedImage.preserveAspect = true;
            blockedImage.raycastTarget = false;
            blocked.gameObject.SetActive(false);

            CombatCatchCursorView view = GetOrAdd<CombatCatchCursorView>(cursor.gameObject);
            view.Configure(cursorImage, border, blockedGroup);
            return view;
        }

        private static void SetupBoardPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BoardPrefabPath);
            try
            {
                root.name = "CombatBoard";
                GetOrAdd<CombatBoardView>(root);

                Transform playArea = FindDescendant(root.transform, "Dice Field") ??
                    FindDescendant(root.transform, "Play Area");
                if (playArea == null)
                    throw new MissingReferenceException("CombatBoard needs a Play Area child.");
                playArea.name = "Dice Field";

                Transform stunRoot = FindDescendant(playArea, "Stun Zone Root") ??
                    FindDescendant(playArea, "Hazard Zone Root");
                if (stunRoot == null)
                    stunRoot = EnsureRect(playArea, "Stun Zone Root").transform;
                stunRoot.name = "Stun Zone Root";
                stunRoot.gameObject.SetActive(true);

                RectTransform bulletRoot = EnsureStretchRect(playArea, "Bullet Root");
                RectTransform diceRoot = EnsureStretchRect(playArea, "Dice Root");
                RectTransform playAreaRect = (RectTransform)playArea;
                RectTransform airborneDiceRoot = FindDirectChild(root.transform, "Airborne Dice Overlay") as RectTransform ??
                    EnsureRect(root.transform, "Airborne Dice Overlay");
                airborneDiceRoot.anchorMin = playAreaRect.anchorMin;
                airborneDiceRoot.anchorMax = playAreaRect.anchorMax;
                airborneDiceRoot.pivot = playAreaRect.pivot;
                airborneDiceRoot.anchoredPosition = playAreaRect.anchoredPosition;
                airborneDiceRoot.sizeDelta = playAreaRect.sizeDelta;
                airborneDiceRoot.localScale = Vector3.one;
                airborneDiceRoot.localRotation = Quaternion.identity;
                airborneDiceRoot.SetSiblingIndex(playArea.GetSiblingIndex() + 1);
                Transform existingHeart = FindDescendant(playArea, "Audere Heart Root") ??
                    FindDescendant(playArea, "Player Root");
                RectTransform playerRoot = existingHeart as RectTransform ?? EnsureRect(playArea, "Audere Heart Root");
                playerRoot.name = "Audere Heart Root";
                ConfigureRect(playerRoot, Vector2.zero, new Vector2(28f, 28f));
                CombatPlayerView playerView = GetOrAdd<CombatPlayerView>(playerRoot.gameObject);
                Image playerVisual = EnsureHeartVisualInstance(playerRoot);
                SerializedObject playerSerialized = new SerializedObject(playerView);
                SetObject(playerSerialized, "visual", playerVisual);
                playerSerialized.ApplyModifiedPropertiesWithoutUndo();

                Transform cursorRoot = FindDescendant(playArea, "Catch Cursor Root") ??
                    FindDescendant(playArea, "Catch Zone Root");
                if (cursorRoot == null)
                    cursorRoot = EnsureRect(playArea, "Catch Cursor Root").transform;
                cursorRoot.name = "Catch Cursor Root";

                Transform feedbackRoot = FindDescendant(playArea, "Feedback FX Root") ??
                    FindDescendant(playArea, "Feedback Root");
                if (feedbackRoot == null)
                    feedbackRoot = EnsureStretchRect(playArea, "Feedback FX Root");
                feedbackRoot.name = "Feedback FX Root";

                Transform enemy = FindDescendant(root.transform, "Enemy");
                Transform vfxRoot = FindDirectChild(root.transform, "Vfx") ??
                    FindDescendant(root.transform, "Vfx");
                RectTransform damageNumberRoot = EnsureStretchRect(root.transform, "Damage Number Root");
                damageNumberRoot.SetAsLastSibling();
                TMP_Text enemyNameText = enemy != null
                    ? FindDescendant(enemy, "Enemy Name")?.GetComponent<TMP_Text>()
                    : null;
                Image timerFill = FindDescendant(root.transform, "Timer Fill")?.GetComponent<Image>();
                Image timerDamageFill = null;
                if (timerFill != null)
                {
                    timerFill.type = Image.Type.Simple;
                    timerFill.fillAmount = 1f;
                    RectTransform timerFillRect = timerFill.rectTransform;
                    timerFillRect.anchorMin = Vector2.zero;
                    timerFillRect.anchorMax = Vector2.one;
                    timerFillRect.offsetMin = Vector2.zero;
                    timerFillRect.offsetMax = Vector2.zero;
                    timerFillRect.pivot = new Vector2(0f, .5f);

                    RectTransform damageFillRect = EnsureStretchRect(timerFill.transform.parent, "Timer Damage Fill");
                    damageFillRect.pivot = new Vector2(0f, .5f);
                    timerDamageFill = GetOrAdd<Image>(damageFillRect.gameObject);
                    timerDamageFill.type = Image.Type.Simple;
                    timerDamageFill.color = new Color(1f, 1f, 1f, .96f);
                    timerDamageFill.raycastTarget = false;
                    damageFillRect.SetSiblingIndex(timerFill.transform.GetSiblingIndex());
                    timerFill.transform.SetSiblingIndex(damageFillRect.GetSiblingIndex() + 1);
                }

                RectTransform stunZone = EnsureRect(stunRoot, "Stun Zone");
                ConfigureRect(stunZone, new Vector2(-110f, 0f), new Vector2(170f, 410f));
                Image stunImage = GetOrAdd<Image>(stunZone.gameObject);
                stunImage.color = Color.white;
                stunImage.material = AssetDatabase.LoadAssetAtPath<Material>(StunZoneMaterialPath);
                stunImage.raycastTarget = false;

                RectTransform cursor = EnsureRect(cursorRoot, "Catch Cursor");
                ConfigureRect(cursor, Vector2.zero, new Vector2(100f, 100f));
                Image cursorImage = GetOrAdd<Image>(cursor.gameObject);
                Sprite circularFill = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                if (circularFill != null)
                    cursorImage.sprite = circularFill;
                cursorImage.type = Image.Type.Simple;
                cursorImage.preserveAspect = true;
                cursorImage.color = new Color(1f, 1f, 1f, .015f);
                cursorImage.raycastTarget = false;
                Outline outline = GetOrAdd<Outline>(cursor.gameObject);
                outline.enabled = false;
                CombatCatchCursorView cursorView = SetupCatchCursorVisual(cursor, cursorImage);
                cursor.gameObject.SetActive(false);

                if (playerRoot.parent != cursor)
                    playerRoot.SetParent(cursor, false);
                ConfigureRect(playerRoot, Vector2.zero, new Vector2(28f, 28f));
                playerRoot.SetAsLastSibling();
                Transform blockedX = FindDirectChild(cursor, "Blocked X");
                if (blockedX != null) blockedX.SetAsLastSibling();

                stunRoot.SetSiblingIndex(0);
                bulletRoot.SetSiblingIndex(1);
                diceRoot.SetSiblingIndex(2);
                cursorRoot.SetSiblingIndex(3);
                feedbackRoot.SetSiblingIndex(4);

                CombatBoardView boardView = root.GetComponent<CombatBoardView>();
                SerializedObject serialized = new SerializedObject(boardView);
                SetObject(serialized, "playArea", playArea);
                SetObject(serialized, "stunZoneRoot", stunRoot);
                SetObject(serialized, "bulletRoot", bulletRoot);
                SetObject(serialized, "diceRoot", diceRoot);
                SetObject(serialized, "airborneDiceRoot", airborneDiceRoot);
                SetObject(serialized, "playerRoot", playerRoot);
                SetObject(serialized, "catchCursorRoot", cursorRoot);
                SetObject(serialized, "feedbackRoot", feedbackRoot);
                SetObject(serialized, "catchCursor", cursor);
                SetObject(serialized, "catchCursorView", cursorView);
                SetObject(serialized, "playerView", playerView);
                SetObject(serialized, "enemyNameText", enemyNameText);
                SetObject(serialized, "timerFill", timerFill);
                SetObject(serialized, "timerDamageFill", timerDamageFill);
                SetObject(serialized, "enemyVisual", enemy);
                SetObject(serialized, "vfxRoot", vfxRoot);
                SetObject(serialized, "enemyScratchVfxPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(ScratchVfxPath));
                SetObject(serialized, "damageNumberRoot", damageNumberRoot);
                SetObject(serialized, "damageNumberFont", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DamageNumberFontPath));
                SetObject(serialized, "attackDicePrefab", LoadDicePrefab(AttackDicePrefabPath));
                SetObject(serialized, "armorDicePrefab", LoadDicePrefab(ArmorDicePrefabPath));
                SetObject(serialized, "healDicePrefab", LoadDicePrefab(HealDicePrefabPath));
                SetObject(serialized, "enemyBulletPrefab", LoadBulletPrefab());
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, BoardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupDicePrefabs()
        {
            EnsureAssetFolder(DicePrefabFolder);
            Sprite attackIcon = LoadFirstSprite(AttackIconPath);
            Sprite armorIcon = LoadFirstSprite(ArmorIconPath);
            Sprite healIcon = LoadFirstSprite(HealIconPath);
            Sprite diceFrame = LoadFirstSprite(DiceFramePath);
            SetupDicePrefab(
                AttackDicePrefabPath,
                "Dice_Attack",
                CombatSymbol.Attack,
                "ATK",
                attackIcon,
                diceFrame,
                new Color32(168, 59, 68, 255));
            SetupDicePrefab(
                ArmorDicePrefabPath,
                "Dice_Armor",
                CombatSymbol.Armor,
                "ARM",
                armorIcon,
                diceFrame,
                new Color32(176, 171, 183, 255));
            SetupDicePrefab(
                HealDicePrefabPath,
                "Dice_Heal",
                CombatSymbol.Heal,
                "HEAL",
                healIcon,
                diceFrame,
                new Color32(216, 192, 151, 255));
        }

        private static void SetupDicePrefab(
            string path,
            string prefabName,
            CombatSymbol symbol,
            string labelText,
            Sprite authoredIcon,
            Sprite diceFrame,
            Color activeIconColor)
        {
            bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            GameObject root = prefabExists
                ? PrefabUtility.LoadPrefabContents(path)
                : new GameObject(prefabName, typeof(RectTransform));

            try
            {
                root.name = prefabName;
                RectTransform rootRect = GetOrAdd<RectTransform>(root);
                ConfigureRect(rootRect, Vector2.zero, new Vector2(72f, 72f));
                Image rootImage = GetOrAdd<Image>(root);
                rootImage.color = Color.clear;
                rootImage.raycastTarget = false;
                GetOrAdd<CanvasGroup>(root);

                RectTransform shadow = EnsureRect(root.transform, "Shadow");
                ConfigureRect(shadow, new Vector2(7f, -7f), new Vector2(66f, 66f));
                Image shadowImage = GetOrAdd<Image>(shadow.gameObject);
                shadowImage.color = new Color32(35, 33, 45, 255);
                shadowImage.raycastTarget = false;
                shadow.SetAsFirstSibling();

                RectTransform frame = EnsureRect(root.transform, "Frame");
                ConfigureRect(frame, Vector2.zero, new Vector2(72f, 72f));
                Image frameImage = GetOrAdd<Image>(frame.gameObject);
                frameImage.sprite = diceFrame;
                frameImage.color = Color.white;
                frameImage.preserveAspect = true;
                frameImage.raycastTarget = false;
                frame.SetSiblingIndex(1);

                RectTransform face = EnsureRect(root.transform, "Face");
                ConfigureRect(face, Vector2.zero, new Vector2(60f, 60f));
                Image faceImage = GetOrAdd<Image>(face.gameObject);
                faceImage.color = Color.clear;
                faceImage.raycastTarget = false;
                face.SetSiblingIndex(2);

                RectTransform labelRect = EnsureRect(face, "Symbol");
                ConfigureRect(labelRect, Vector2.zero, new Vector2(60f, 60f));
                TextMeshProUGUI label = GetOrAdd<TextMeshProUGUI>(labelRect.gameObject);
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                if (font != null) label.font = font;
                label.text = labelText;
                label.fontSize = 18f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.color = new Color(.08f, .07f, .11f, 1f);
                label.raycastTarget = false;
                label.textWrappingMode = TextWrappingModes.NoWrap;

                RectTransform iconRect = EnsureRect(face, "Icon");
                ConfigureRect(iconRect, Vector2.zero, new Vector2(50f, 50f));
                Image iconImage = GetOrAdd<Image>(iconRect.gameObject);
                iconImage.sprite = authoredIcon;
                iconImage.color = activeIconColor;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                iconRect.SetAsLastSibling();
                iconImage.gameObject.SetActive(authoredIcon != null);
                label.gameObject.SetActive(authoredIcon == null);

                CombatDieView dieView = GetOrAdd<CombatDieView>(root);
                SerializedObject serialized = new SerializedObject(dieView);
                serialized.FindProperty("prefabSymbol").enumValueIndex = (int)symbol;
                SetObject(serialized, "background", faceImage);
                SetObject(serialized, "shadowImage", shadowImage);
                SetObject(serialized, "shadowRect", shadow);
                SetObject(serialized, "frameRect", frame);
                SetObject(serialized, "faceRect", face);
                SetObject(serialized, "symbolIcon", iconImage);
                SetObject(serialized, "symbolLabel", label);
                serialized.FindProperty("attackColor").colorValue = new Color32(168, 59, 68, 255);
                serialized.FindProperty("armorColor").colorValue = new Color32(176, 171, 183, 255);
                serialized.FindProperty("healColor").colorValue = new Color32(216, 192, 151, 255);
                serialized.FindProperty("inactiveColor").colorValue = new Color32(35, 33, 45, 255);
                serialized.FindProperty("activeIconColor").colorValue = activeIconColor;
                serialized.FindProperty("normalSymbolColor").colorValue = new Color(.08f, .07f, .11f, 1f);
                serialized.FindProperty("launchDelayRange").vector2Value = new Vector2(0f, .12f);
                serialized.FindProperty("bounceCountRange").vector2IntValue = new Vector2Int(2, 3);
                serialized.FindProperty("firstBounceHeight").floatValue = 72f;
                serialized.FindProperty("bounceHeightDecay").floatValue = .48f;
                serialized.FindProperty("firstBounceDuration").floatValue = .38f;
                serialized.FindProperty("bounceDurationDecay").floatValue = .78f;
                serialized.FindProperty("landingSquashDuration").floatValue = .07f;
                serialized.FindProperty("tossTravelSpeedMultiplier").floatValue = 1.2f;
                serialized.FindProperty("shadowScaleAtApex").floatValue = .72f;
                serialized.FindProperty("bodyScaleAtApex").floatValue = .06f;
                serialized.FindProperty("speedMultiplier").floatValue = 1f;
                serialized.FindProperty("rotateWhileMoving").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                if (prefabExists)
                    PrefabUtility.UnloadPrefabContents(root);
                else
                    Object.DestroyImmediate(root);
            }
        }

        private static Sprite LoadFirstSprite(string assetPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                    return sprite;
            }

            Debug.LogWarning($"[CombatSetup] No Sprite sub-asset found at '{assetPath}'.");
            return null;
        }

        private static void SetupBulletPrefab()
        {
            EnsureAssetFolder(BulletPrefabFolder);
            bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyBulletPrefabPath) != null;
            GameObject root = prefabExists
                ? PrefabUtility.LoadPrefabContents(EnemyBulletPrefabPath)
                : new GameObject("EnemyBullet", typeof(RectTransform));

            try
            {
                root.name = "EnemyBullet";
                RectTransform rootRect = GetOrAdd<RectTransform>(root);
                ConfigureRect(rootRect, Vector2.zero, new Vector2(18f, 18f));
                Image frame = GetOrAdd<Image>(root);
                frame.color = new Color(.30f, .20f, .34f, 1f);
                frame.raycastTarget = false;
                rootRect.localRotation = Quaternion.Euler(0f, 0f, 45f);

                RectTransform core = EnsureRect(root.transform, "Core");
                ConfigureRect(core, Vector2.zero, new Vector2(10f, 10f));
                Image coreImage = GetOrAdd<Image>(core.gameObject);
                coreImage.color = new Color(.93f, .38f, .50f, 1f);
                coreImage.raycastTarget = false;
                GetOrAdd<CombatBulletView>(root);
                PrefabUtility.SaveAsPrefabAsset(root, EnemyBulletPrefabPath);
            }
            finally
            {
                if (prefabExists) PrefabUtility.UnloadPrefabContents(root);
                else Object.DestroyImmediate(root);
            }
        }

        private static CombatBulletView LoadBulletPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyBulletPrefabPath);
            return prefab != null ? prefab.GetComponent<CombatBulletView>() : null;
        }

        private static void SetupHeartVisualPrefab()
        {
            EnsureAssetFolder(PlayerPrefabFolder);
            bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(HeartVisualPrefabPath) != null;
            GameObject root = prefabExists
                ? PrefabUtility.LoadPrefabContents(HeartVisualPrefabPath)
                : new GameObject("HeartVisual", typeof(RectTransform));

            try
            {
                root.name = "HeartVisual";
                RectTransform rootRect = GetOrAdd<RectTransform>(root);
                ConfigureRect(rootRect, Vector2.zero, new Vector2(24f, 24f));
                Image image = GetOrAdd<Image>(root);
                image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = new Color(.72f, .95f, .92f, 1f);
                image.raycastTarget = false;
                PrefabUtility.SaveAsPrefabAsset(root, HeartVisualPrefabPath);
            }
            finally
            {
                if (prefabExists) PrefabUtility.UnloadPrefabContents(root);
                else Object.DestroyImmediate(root);
            }
        }

        private static Image EnsureHeartVisualInstance(RectTransform playerRoot)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeartVisualPrefabPath);
            if (prefab == null)
                throw new MissingReferenceException($"Heart visual prefab is missing at '{HeartVisualPrefabPath}'.");

            Transform existing = FindDirectChild(playerRoot, "Heart Visual") ??
                FindDirectChild(playerRoot, "Player Visual");
            GameObject source = existing != null
                ? PrefabUtility.GetCorrespondingObjectFromSource(existing.gameObject)
                : null;

            if (existing == null || source != prefab)
            {
                if (existing != null)
                    Object.DestroyImmediate(existing.gameObject);
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, playerRoot) as GameObject;
                if (instance == null)
                    instance = Object.Instantiate(prefab, playerRoot);
                instance.name = "Heart Visual";
                existing = instance.transform;
            }

            RectTransform rect = existing as RectTransform;
            ConfigureRect(rect, Vector2.zero, new Vector2(24f, 24f));
            return existing.GetComponent<Image>();
        }

        private static void MigrateLegacyCombatPrefabs()
        {
            MoveAssetIfNeeded(DicePrefabFolder + "/Dice_GreenSword.prefab", AttackDicePrefabPath);
            MoveAssetIfNeeded(DicePrefabFolder + "/Dice_Shield.prefab", ArmorDicePrefabPath);
            MoveAssetIfNeeded(DicePrefabFolder + "/Dice_EnemyAttack.prefab", HealDicePrefabPath);
        }

        private static void MoveAssetIfNeeded(string oldPath, string newPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(oldPath) == null ||
                AssetDatabase.LoadAssetAtPath<Object>(newPath) != null)
                return;

            string error = AssetDatabase.MoveAsset(oldPath, newPath);
            if (!string.IsNullOrEmpty(error))
                Debug.LogError($"[CombatSetup] Could not move '{oldPath}' to '{newPath}': {error}");
        }

        private static void ConfigureEncounter(CombatEncounterData encounter)
        {
            SerializedObject serialized = new SerializedObject(encounter);
            serialized.FindProperty("enemyMaxHealth").intValue = 12;
            serialized.FindProperty("encounterDuration").floatValue = 40f;
            serialized.FindProperty("attackDamage").intValue = 1;
            serialized.FindProperty("armorPerDie").intValue = 1;
            serialized.FindProperty("healTimeSeconds").floatValue = 3f;
            serialized.FindProperty("dicePerBatch").intValue = 5;
            serialized.FindProperty("batchRespawnDelay").floatValue = .3f;
            serialized.FindProperty("minimumDiceSpeed").floatValue = 115f;
            serialized.FindProperty("maximumDiceSpeed").floatValue = 185f;
            SerializedProperty weights = serialized.FindProperty("symbolWeights");
            weights.arraySize = 3;
            SetSymbolWeight(weights.GetArrayElementAtIndex(0), CombatSymbol.Attack, 5);
            SetSymbolWeight(weights.GetArrayElementAtIndex(1), CombatSymbol.Armor, 3);
            SetSymbolWeight(weights.GetArrayElementAtIndex(2), CombatSymbol.Heal, 2);
            serialized.FindProperty("playerHitInvulnerability").floatValue = .55f;
            serialized.FindProperty("bulletTimePenaltySeconds").floatValue = 3f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(encounter);
        }

        private static void SetSymbolWeight(SerializedProperty property, CombatSymbol symbol, int weight)
        {
            property.FindPropertyRelative("Symbol").enumValueIndex = (int)symbol;
            property.FindPropertyRelative("Weight").intValue = weight;
        }

        private static CombatDieView LoadDicePrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab != null ? prefab.GetComponent<CombatDieView>() : null;
        }

        private static void EnsureAssetFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static CanvasGroup EnsureTransitionOverlay(Transform world)
        {
            Transform existing = FindDirectChild(world, "World Transition Overlay");
            GameObject canvasObject;
            RectTransform canvasRect;
            if (existing is RectTransform existingRect)
            {
                canvasObject = existing.gameObject;
                canvasRect = existingRect;
            }
            else
            {
                if (existing != null)
                    Object.DestroyImmediate(existing.gameObject);
                canvasObject = new GameObject("World Transition Overlay", typeof(RectTransform));
                canvasRect = canvasObject.GetComponent<RectTransform>();
                canvasRect.SetParent(world, false);
            }
            Canvas canvas = GetOrAdd<Canvas>(canvasObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1000;
            GetOrAdd<CanvasScaler>(canvasObject).uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            RectTransform fadeRect = EnsureStretchRect(canvasRect, "Transition Fade");
            Image fadeImage = GetOrAdd<Image>(fadeRect.gameObject);
            fadeImage.color = Color.black;
            fadeImage.raycastTarget = true;
            CanvasGroup group = GetOrAdd<CanvasGroup>(fadeRect.gameObject);
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            return group;
        }

        private static TMP_Text EnsureText(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            float fontSize,
            TMP_FontAsset font)
        {
            RectTransform rect = EnsureRect(parent, name);
            ConfigureRect(rect, position, size);
            TextMeshProUGUI text = GetOrAdd<TextMeshProUGUI>(rect.gameObject);
            if (font != null) text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(.90f, .85f, .92f, 1f);
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.text = name;
            return text;
        }

        private static RectTransform EnsureStretchRect(Transform parent, string name)
        {
            RectTransform rect = EnsureRect(parent, name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static RectTransform EnsureRect(Transform parent, string name)
        {
            Transform existing = FindDirectChild(parent, name);
            if (existing is RectTransform existingRect)
                return existingRect;

            GameObject child = new GameObject(name, typeof(RectTransform));
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void ConfigureRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(.5f, .5f);
            rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static GameObject EnsureObject(Transform parent, string name)
        {
            Transform existing = FindDirectChild(parent, name);
            if (existing != null) return existing.gameObject;
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void MoveSystem(Transform systemsRoot, Transform destination, string name)
        {
            Transform target = FindDirectChild(systemsRoot, name) ?? FindDirectChild(destination, name);
            if (target != null && target.parent != destination)
                target.SetParent(destination, true);
            if (target != null)
                target.gameObject.SetActive(true);
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == name) return roots[i];
            }
            return null;
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name) return child;
            }
            return null;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDescendant(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void SetObject(SerializedObject serialized, string propertyName, Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }
    }
}
