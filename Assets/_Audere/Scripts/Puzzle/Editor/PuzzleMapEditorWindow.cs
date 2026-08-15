using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Audere.Dialogue;
using Audere.Puzzle.Board;
using Audere.Puzzle.PathPieces;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Audere.Puzzle.Editor
{
    public sealed class PuzzleMapEditorWindow : EditorWindow
    {
        private struct DialogueTileSettings
        {
            public DialogueData Data;
            public bool TriggerOnce;

            public DialogueTileSettings(DialogueData data, bool triggerOnce)
            {
                Data = data;
                TriggerOnce = triggerOnce;
            }
        }

        private enum PaintTool
        {
            Tile,
            Erase,
            Player
        }

        private const float CellSize = 34f;
        private const float CellGap = 2f;
        private const string DefaultFolder = "Assets/_Audere/Data/Puzzle/Levels";

        private readonly Dictionary<Vector2Int, PuzzleTileType> boardTiles = new Dictionary<Vector2Int, PuzzleTileType>();
        private readonly Dictionary<Vector2Int, DialogueTileSettings> dialogueTiles =
            new Dictionary<Vector2Int, DialogueTileSettings>();
        private readonly List<PathPieceData> pathPieces = new List<PathPieceData>();

        private PuzzleData targetData;
        private PuzzleManager sceneManager;
        private PuzzleTileCatalog tileCatalog;
        private string puzzleId = "puzzle-new";
        private Vector2Int gridOrigin = new Vector2Int(-4, -4);
        private Vector2Int gridSize = new Vector2Int(9, 9);
        private Vector2Int playerPosition;
        private bool hasPlayer;
        private bool hasSelectedCell;
        private Vector2Int selectedCell;
        private PuzzleTileType selectedTileType = PuzzleTileType.Grass;
        private PaintTool activeTool;
        private Vector2 gridScroll;
        private Vector2 windowScroll;
        private GUIStyle centeredCellStyle;

        [MenuItem("Audere/Puzzle/Map Editor")]
        public static void Open()
        {
            PuzzleMapEditorWindow window = GetWindow<PuzzleMapEditorWindow>();
            window.titleContent = new GUIContent("Puzzle Map Editor");
            window.minSize = new Vector2(720f, 620f);
            window.Show();
        }

        public static void OpenFor(PuzzleData data)
        {
            Open();
            PuzzleMapEditorWindow window = GetWindow<PuzzleMapEditorWindow>();
            window.targetData = data;
            window.LoadFromTarget();
            window.Focus();
        }

        private void OnEnable()
        {
            sceneManager = FindFirstObjectByType<PuzzleManager>();
            tileCatalog = AssetDatabase.LoadAssetAtPath<PuzzleTileCatalog>(
                PuzzleContentConstants.AssetPaths.TileCatalog);
        }

        private void OnGUI()
        {
            EnsureStyles();
            windowScroll = EditorGUILayout.BeginScrollView(windowScroll);

            DrawHeader();
            EditorGUILayout.Space(6f);
            DrawAssetControls();
            EditorGUILayout.Space(8f);
            DrawGridSettings();
            EditorGUILayout.Space(6f);
            DrawToolbar();
            EditorGUILayout.Space(4f);
            DrawGrid();
            DrawSelectedTileSettings();
            EditorGUILayout.Space(10f);
            DrawPathPieceList();
            EditorGUILayout.Space(10f);
            DrawValidation();
            DrawActions();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Steptile-style Puzzle Map Editor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Left-click or drag to paint prefab-backed tile types. Goal is a real tile type with its own prefab. " +
                "Player is the only actor marker; right-click always erases.",
                MessageType.Info);

            tileCatalog = (PuzzleTileCatalog)EditorGUILayout.ObjectField(
                "Tile Prefab Catalog",
                tileCatalog,
                typeof(PuzzleTileCatalog),
                false);
        }

        private void DrawAssetControls()
        {
            EditorGUI.BeginChangeCheck();
            PuzzleData newTarget = (PuzzleData)EditorGUILayout.ObjectField(
                "Puzzle Data",
                targetData,
                typeof(PuzzleData),
                false);

            if (EditorGUI.EndChangeCheck())
            {
                targetData = newTarget;
                if (targetData != null)
                    LoadFromTarget();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                puzzleId = EditorGUILayout.TextField("Puzzle Id", puzzleId);

                if (GUILayout.Button("Load", GUILayout.Width(72f)))
                    LoadFromTarget();

                if (GUILayout.Button("Ping", GUILayout.Width(72f)) && targetData != null)
                    EditorGUIUtility.PingObject(targetData);
            }
        }

        private void DrawGridSettings()
        {
            EditorGUILayout.LabelField("Canvas", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                gridOrigin = EditorGUILayout.Vector2IntField("Bottom-left", gridOrigin);
                gridSize = EditorGUILayout.Vector2IntField("Size", gridSize);
            }

            gridSize.x = Mathf.Clamp(gridSize.x, 2, 30);
            gridSize.y = Mathf.Clamp(gridSize.y, 2, 30);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fit Around Map"))
                    FitCanvasAroundMap();

                if (GUILayout.Button("Frame 9 x 9"))
                {
                    gridOrigin = new Vector2Int(-4, -4);
                    gridSize = new Vector2Int(9, 9);
                }

                if (GUILayout.Button("Clear Map"))
                {
                    if (EditorUtility.DisplayDialog("Clear puzzle map?", "This clears the unsaved editor canvas.", "Clear", "Cancel"))
                    {
                        boardTiles.Clear();
                        dialogueTiles.Clear();
                        hasPlayer = false;
                        hasSelectedCell = false;
                    }
                }
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.LabelField("Paint Tool", EditorStyles.boldLabel);
            selectedTileType = (PuzzleTileType)EditorGUILayout.EnumPopup("Tile Type", selectedTileType);
            activeTool = (PaintTool)GUILayout.Toolbar(
                (int)activeTool,
                new[] { selectedTileType.ToString(), "Erase", "Player" },
                GUILayout.Height(28f));
        }

        private void DrawGrid()
        {
            float width = gridSize.x * (CellSize + CellGap) + 34f;
            float height = gridSize.y * (CellSize + CellGap) + 30f;

            gridScroll = EditorGUILayout.BeginScrollView(
                gridScroll,
                GUILayout.Height(Mathf.Min(height + 16f, 430f)));

            Rect canvasRect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false));
            GUI.Box(canvasRect, GUIContent.none, EditorStyles.helpBox);

            Event currentEvent = Event.current;

            for (int row = gridSize.y - 1; row >= 0; row--)
            {
                for (int column = 0; column < gridSize.x; column++)
                {
                    Vector2Int coordinate = gridOrigin + new Vector2Int(column, row);
                    Rect cellRect = new Rect(
                        canvasRect.x + 28f + column * (CellSize + CellGap),
                        canvasRect.y + 4f + (gridSize.y - 1 - row) * (CellSize + CellGap),
                        CellSize,
                        CellSize);

                    DrawCell(cellRect, coordinate);
                    HandleCellInput(currentEvent, cellRect, coordinate);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawCell(Rect rect, Vector2Int coordinate)
        {
            bool isTile = boardTiles.TryGetValue(coordinate, out PuzzleTileType tileType);
            GUI.Box(rect, GUIContent.none, isTile ? GUI.skin.button : GUI.skin.box);

            if (isTile)
                DrawTilePrefabPreview(rect, tileType);

            if (hasPlayer && playerPosition == coordinate)
            {
                GUI.Label(rect, "P", centeredCellStyle);
            }

            GUI.Label(
                new Rect(rect.x + 2f, rect.y + rect.height - 12f, rect.width - 4f, 10f),
                $"{coordinate.x},{coordinate.y}",
                EditorStyles.miniLabel);

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
        }

        private void HandleCellInput(Event currentEvent, Rect rect, Vector2Int coordinate)
        {
            if (!rect.Contains(currentEvent.mousePosition))
                return;

            bool isPaintEvent = currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag;
            if (!isPaintEvent || (currentEvent.button != 0 && currentEvent.button != 1))
                return;

            PaintTool tool = currentEvent.button == 1 ? PaintTool.Erase : activeTool;

            switch (tool)
            {
                case PaintTool.Tile:
                    boardTiles[coordinate] = selectedTileType;
                    if (selectedTileType == PuzzleTileType.Dialogue)
                    {
                        if (!dialogueTiles.ContainsKey(coordinate))
                            dialogueTiles[coordinate] = new DialogueTileSettings(null, true);
                    }
                    else
                    {
                        dialogueTiles.Remove(coordinate);
                    }

                    selectedCell = coordinate;
                    hasSelectedCell = true;
                    break;
                case PaintTool.Erase:
                    boardTiles.Remove(coordinate);
                    dialogueTiles.Remove(coordinate);
                    if (hasPlayer && playerPosition == coordinate) hasPlayer = false;
                    if (hasSelectedCell && selectedCell == coordinate) hasSelectedCell = false;
                    break;
                case PaintTool.Player:
                    playerPosition = coordinate;
                    hasPlayer = true;
                    if (!boardTiles.ContainsKey(coordinate))
                        boardTiles[coordinate] = PuzzleTileType.Grass;
                    break;
            }

            currentEvent.Use();
            Repaint();
        }

        private void DrawSelectedTileSettings()
        {
            if (!hasSelectedCell)
                return;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField($"Selected Cell ({selectedCell.x}, {selectedCell.y})", EditorStyles.boldLabel);

            if (!boardTiles.TryGetValue(selectedCell, out PuzzleTileType tileType) ||
                tileType != PuzzleTileType.Dialogue)
            {
                EditorGUILayout.HelpBox(
                    "Paint or click a Dialogue tile to assign its dialogue data.",
                    MessageType.Info);
                return;
            }

            if (!dialogueTiles.TryGetValue(selectedCell, out DialogueTileSettings settings))
                settings = new DialogueTileSettings(null, true);

            settings.Data = (DialogueData)EditorGUILayout.ObjectField(
                "Dialogue Data",
                settings.Data,
                typeof(DialogueData),
                false);
            settings.TriggerOnce = EditorGUILayout.Toggle("Trigger Once", settings.TriggerOnce);
            dialogueTiles[selectedCell] = settings;
        }

        private void DrawTilePrefabPreview(Rect cellRect, PuzzleTileType tileType)
        {
            if (tileCatalog == null || !tileCatalog.TryGetPrefab(tileType, out BoardTile prefab))
            {
                GUI.Label(cellRect, tileType.ToString(), EditorStyles.centeredGreyMiniLabel);
                return;
            }

            Texture preview = AssetPreview.GetAssetPreview(prefab.gameObject);
            if (preview == null)
            {
                preview = AssetPreview.GetMiniThumbnail(prefab.gameObject);
                if (AssetPreview.IsLoadingAssetPreview(prefab.gameObject.GetInstanceID()))
                    Repaint();
            }

            if (preview != null)
                GUI.DrawTexture(
                    new Rect(cellRect.x + 2f, cellRect.y + 2f, cellRect.width - 4f, cellRect.height - 4f),
                    preview,
                    ScaleMode.ScaleToFit,
                    true);
        }

        private void DrawPathPieceList()
        {
            EditorGUILayout.LabelField("Available Path Pieces", EditorStyles.boldLabel);

            for (int index = 0; index < pathPieces.Count; index++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    pathPieces[index] = (PathPieceData)EditorGUILayout.ObjectField(
                        $"Card {index + 1}",
                        pathPieces[index],
                        typeof(PathPieceData),
                        false);

                    GUI.enabled = index > 0;
                    if (GUILayout.Button("Up", GUILayout.Width(36f)))
                        SwapPathPieces(index, index - 1);

                    GUI.enabled = index < pathPieces.Count - 1;
                    if (GUILayout.Button("Down", GUILayout.Width(48f)))
                        SwapPathPieces(index, index + 1);

                    GUI.enabled = true;
                    if (GUILayout.Button("X", GUILayout.Width(28f)))
                    {
                        pathPieces.RemoveAt(index);
                        break;
                    }
                }
            }

            GUI.enabled = pathPieces.Count < PuzzleContentConstants.Hand.MaxSlots;
            if (GUILayout.Button($"+ Add Path Piece ({pathPieces.Count}/{PuzzleContentConstants.Hand.MaxSlots})"))
                pathPieces.Add(null);
            GUI.enabled = true;
        }

        private void DrawValidation()
        {
            List<string> issues = GetValidationIssues();

            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    $"Ready: {boardTiles.Count} board tiles, {pathPieces.Count} cards.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(string.Join("\n", issues), MessageType.Warning);
        }

        private void DrawActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create New Data", GUILayout.Height(34f)))
                    CreateNewData();

                GUI.enabled = targetData != null && GetValidationIssues().Count == 0;
                if (GUILayout.Button("Save Data", GUILayout.Height(34f)))
                    SaveData();
                GUI.enabled = true;
            }

            sceneManager = (PuzzleManager)EditorGUILayout.ObjectField(
                "Scene Puzzle Manager",
                sceneManager,
                typeof(PuzzleManager),
                true);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = targetData != null && sceneManager != null && GetValidationIssues().Count == 0;

                if (GUILayout.Button("Apply to Scene", GUILayout.Height(34f)))
                    ApplyToScene(false);

                if (GUILayout.Button("Apply & Play", GUILayout.Height(34f)))
                    ApplyToScene(true);

                GUI.enabled = true;
            }
        }

        private void LoadFromTarget()
        {
            if (targetData == null)
                return;

            boardTiles.Clear();
            dialogueTiles.Clear();
            foreach (PuzzleTileData tile in targetData.BoardTiles)
            {
                boardTiles[tile.Position] = tile.TileType;
                if (tile.TileType == PuzzleTileType.Dialogue)
                    dialogueTiles[tile.Position] = new DialogueTileSettings(
                        tile.Dialogue,
                        tile.TriggerDialogueOnce);
            }

            hasSelectedCell = false;

            playerPosition = targetData.PlayerStartPosition;
            hasPlayer = boardTiles.ContainsKey(playerPosition);
            puzzleId = targetData.PuzzleId;

            pathPieces.Clear();
            pathPieces.AddRange(targetData.AvailablePathPieces);
            FitCanvasAroundMap();
            Repaint();
        }

        private void CreateNewData()
        {
            if (GetValidationIssues().Count > 0)
            {
                EditorUtility.DisplayDialog("Puzzle is incomplete", string.Join("\n", GetValidationIssues()), "OK");
                return;
            }

            EnsureAssetFolder(DefaultFolder);
            string defaultName = string.IsNullOrWhiteSpace(puzzleId) ? "Puzzle_New" : $"Puzzle_{SanitizeFileName(puzzleId)}";
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Puzzle Data",
                defaultName,
                "asset",
                "Choose where to save this level.",
                DefaultFolder);

            if (string.IsNullOrEmpty(path))
                return;

            targetData = CreateInstance<PuzzleData>();
            AssetDatabase.CreateAsset(targetData, path);
            SaveData();
            Selection.activeObject = targetData;
            EditorGUIUtility.PingObject(targetData);
        }

        private void SaveData()
        {
            if (targetData == null)
                return;

            List<string> issues = GetValidationIssues();
            if (issues.Count > 0)
            {
                EditorUtility.DisplayDialog("Puzzle is incomplete", string.Join("\n", issues), "OK");
                return;
            }

            Undo.RecordObject(targetData, "Save Puzzle Map");
            SerializedObject serializedData = new SerializedObject(targetData);
            serializedData.FindProperty("puzzleId").stringValue = puzzleId.Trim();

            List<KeyValuePair<Vector2Int, PuzzleTileType>> sortedTiles = boardTiles
                .OrderBy(tile => tile.Key.y)
                .ThenBy(tile => tile.Key.x)
                .ToList();

            SerializedProperty tilesProperty = serializedData.FindProperty("boardTiles");
            tilesProperty.arraySize = sortedTiles.Count;
            for (int index = 0; index < sortedTiles.Count; index++)
            {
                SerializedProperty tileProperty = tilesProperty.GetArrayElementAtIndex(index);
                tileProperty.FindPropertyRelative("position").vector2IntValue = sortedTiles[index].Key;
                tileProperty.FindPropertyRelative("tileType").enumValueIndex = (int)sortedTiles[index].Value;

                bool isDialogueTile = sortedTiles[index].Value == PuzzleTileType.Dialogue;
                DialogueTileSettings settings = isDialogueTile && dialogueTiles.TryGetValue(
                    sortedTiles[index].Key,
                    out DialogueTileSettings configuredSettings)
                    ? configuredSettings
                    : new DialogueTileSettings(null, false);

                tileProperty.FindPropertyRelative("dialogue").objectReferenceValue = settings.Data;
                tileProperty.FindPropertyRelative("triggerDialogueOnce").boolValue = settings.TriggerOnce;
            }

            // Keep the legacy coordinate-only list synchronized while older branches still consume it.
            SerializedProperty legacyCellsProperty = serializedData.FindProperty("boardCells");
            legacyCellsProperty.arraySize = sortedTiles.Count;
            for (int index = 0; index < sortedTiles.Count; index++)
                legacyCellsProperty.GetArrayElementAtIndex(index).vector2IntValue = sortedTiles[index].Key;

            serializedData.FindProperty("playerStartPosition").vector2IntValue = playerPosition;
            Vector2Int serializedGoalPosition = sortedTiles
                .First(tile => tile.Value == PuzzleTileType.Goal)
                .Key;
            serializedData.FindProperty("goalPosition").vector2IntValue = serializedGoalPosition;

            SerializedProperty piecesProperty = serializedData.FindProperty("availablePathPieces");
            piecesProperty.arraySize = pathPieces.Count;
            for (int index = 0; index < pathPieces.Count; index++)
                piecesProperty.GetArrayElementAtIndex(index).objectReferenceValue = pathPieces[index];

            serializedData.ApplyModifiedProperties();
            EditorUtility.SetDirty(targetData);
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent($"Saved {targetData.name}"));
        }

        private void ApplyToScene(bool enterPlayMode)
        {
            if (targetData == null || sceneManager == null)
                return;

            List<string> issues = GetValidationIssues();
            if (issues.Count > 0)
            {
                EditorUtility.DisplayDialog("Puzzle is incomplete", string.Join("\n", issues), "OK");
                return;
            }

            SaveData();
            Undo.RecordObject(sceneManager, "Apply Puzzle Data");
            SerializedObject serializedManager = new SerializedObject(sceneManager);
            serializedManager.FindProperty("puzzleData").objectReferenceValue = targetData;
            serializedManager.ApplyModifiedProperties();
            EditorUtility.SetDirty(sceneManager);
            EditorSceneManager.MarkSceneDirty(sceneManager.gameObject.scene);
            EditorSceneManager.SaveScene(sceneManager.gameObject.scene);
            Selection.activeObject = sceneManager;

            if (enterPlayMode)
                EditorApplication.isPlaying = true;
            else
                ShowNotification(new GUIContent("Applied to scene Puzzle Manager"));
        }

        private List<string> GetValidationIssues()
        {
            List<string> issues = new List<string>();

            if (string.IsNullOrWhiteSpace(puzzleId)) issues.Add("- Puzzle Id is required.");
            if (boardTiles.Count == 0) issues.Add("- Paint at least one board tile.");
            if (!hasPlayer) issues.Add("- Place the Player start.");
            if (hasPlayer && !boardTiles.ContainsKey(playerPosition)) issues.Add("- Player must stand on a board tile.");
            int goalCount = boardTiles.Count(tile => tile.Value == PuzzleTileType.Goal);
            if (goalCount == 0) issues.Add("- Paint one Goal tile.");
            if (goalCount > 1) issues.Add("- A puzzle can only contain one Goal tile.");
            if (hasPlayer && boardTiles.TryGetValue(playerPosition, out PuzzleTileType playerTileType) &&
                playerTileType == PuzzleTileType.Goal)
                issues.Add("- Player and Goal must use different cells.");
            if (pathPieces.Count == 0) issues.Add("- Add at least one Path Piece card.");
            if (pathPieces.Count > PuzzleContentConstants.Hand.MaxSlots)
                issues.Add($"- A hand can contain at most {PuzzleContentConstants.Hand.MaxSlots} Path Piece cards.");
            if (pathPieces.Any(piece => piece == null)) issues.Add("- Remove or assign empty Path Piece slots.");

            foreach (KeyValuePair<Vector2Int, PuzzleTileType> tile in boardTiles)
            {
                if (tile.Value != PuzzleTileType.Dialogue)
                    continue;

                if (!dialogueTiles.TryGetValue(tile.Key, out DialogueTileSettings settings) || settings.Data == null)
                    issues.Add($"- Dialogue tile at ({tile.Key.x}, {tile.Key.y}) needs Dialogue Data.");
            }

            return issues;
        }

        private void FitCanvasAroundMap()
        {
            List<Vector2Int> points = boardTiles.Keys.ToList();
            if (hasPlayer) points.Add(playerPosition);

            if (points.Count == 0)
                return;

            int minX = points.Min(point => point.x) - 1;
            int minY = points.Min(point => point.y) - 1;
            int maxX = points.Max(point => point.x) + 1;
            int maxY = points.Max(point => point.y) + 1;

            gridOrigin = new Vector2Int(minX, minY);
            gridSize = new Vector2Int(
                Mathf.Clamp(maxX - minX + 1, 2, 30),
                Mathf.Clamp(maxY - minY + 1, 2, 30));
        }

        private void SwapPathPieces(int first, int second)
        {
            PathPieceData temporary = pathPieces[first];
            pathPieces[first] = pathPieces[second];
            pathPieces[second] = temporary;
        }

        private void EnsureStyles()
        {
            if (centeredCellStyle != null)
                return;

            centeredCellStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14
            };
        }

        private static string SanitizeFileName(string value)
        {
            string sanitized = value.Trim().Replace(' ', '_');
            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
                sanitized = sanitized.Replace(invalidCharacter, '_');
            return sanitized;
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];

            for (int index = 1; index < segments.Length; index++)
            {
                string next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }
    }

    [CustomEditor(typeof(PuzzleData))]
    public sealed class PuzzleDataInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(8f);

            if (GUILayout.Button("Open in Puzzle Map Editor", GUILayout.Height(32f)))
                PuzzleMapEditorWindow.OpenFor((PuzzleData)target);
        }
    }
}
