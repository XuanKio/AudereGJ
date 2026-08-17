using Audere.Puzzle.Board;
using Audere.Dialogue;
using UnityEditor;
using UnityEngine;

namespace Audere.Puzzle.Editor
{
    [CustomEditor(typeof(DialogueTileBehaviour))]
    public sealed class DialogueTileBehaviourEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DialogueTileBehaviour behaviour = (DialogueTileBehaviour)target;
            BoardTile tile = behaviour.GetComponent<BoardTile>();

            EditorGUILayout.HelpBox(
                Application.isPlaying
                    ? "Editing these fields updates the matching cell in PuzzleData and the live tile."
                    : "Dialogue Data is stored per map cell in PuzzleData. Use the Puzzle Map Editor to assign it.",
                MessageType.Info);

            if (tile != null)
                EditorGUILayout.Vector2IntField("Grid Position", tile.GridPosition);

            SerializedObject puzzleObject = null;
            SerializedProperty dialogueProperty = null;
            SerializedProperty triggerOnceProperty = null;
            bool hasEditableSource = Application.isPlaying &&
                TryGetCellProperties(
                    tile,
                    out puzzleObject,
                    out dialogueProperty,
                    out triggerOnceProperty);

            if (hasEditableSource)
            {
                puzzleObject.Update();
                DialogueData data = (DialogueData)EditorGUILayout.ObjectField(
                    "Dialogue Data",
                    dialogueProperty.objectReferenceValue,
                    typeof(DialogueData),
                    false);
                bool triggerOnce = EditorGUILayout.Toggle(
                    "Trigger Once",
                    triggerOnceProperty.boolValue);

                if (data != dialogueProperty.objectReferenceValue ||
                    triggerOnce != triggerOnceProperty.boolValue)
                {
                    Undo.RecordObject(puzzleObject.targetObject, "Change Dialogue Tile Data");
                    dialogueProperty.objectReferenceValue = data;
                    triggerOnceProperty.boolValue = triggerOnce;
                    puzzleObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(puzzleObject.targetObject);
                    behaviour.ConfigureData(data, triggerOnce);
                }
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(
                        "Dialogue Data",
                        behaviour.DialogueData,
                        typeof(DialogueData),
                        false);
                    EditorGUILayout.Toggle("Trigger Once", behaviour.TriggerOnce);
                }
            }

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.Toggle("Triggered", behaviour.Triggered);

            if (GUILayout.Button("Open Puzzle Map Editor"))
            {
                PuzzleManager manager = Object.FindFirstObjectByType<PuzzleManager>();
                if (manager != null && manager.PuzzleData != null)
                    PuzzleMapEditorWindow.OpenFor(manager.PuzzleData);
                else
                    PuzzleMapEditorWindow.Open();
            }
        }

        private static bool TryGetCellProperties(
            BoardTile tile,
            out SerializedObject puzzleObject,
            out SerializedProperty dialogueProperty,
            out SerializedProperty triggerOnceProperty)
        {
            puzzleObject = null;
            dialogueProperty = null;
            triggerOnceProperty = null;
            if (tile == null || tile.TileType != PuzzleTileType.Dialogue)
                return false;

            PuzzleManager manager = Object.FindFirstObjectByType<PuzzleManager>();
            if (manager == null || manager.PuzzleData == null)
                return false;

            puzzleObject = new SerializedObject(manager.PuzzleData);
            SerializedProperty tiles = puzzleObject.FindProperty("boardTiles");
            for (int index = 0; index < tiles.arraySize; index++)
            {
                SerializedProperty cell = tiles.GetArrayElementAtIndex(index);
                if (cell.FindPropertyRelative("position").vector2IntValue != tile.GridPosition)
                    continue;
                if ((PuzzleTileType)cell.FindPropertyRelative("tileType").enumValueIndex !=
                    PuzzleTileType.Dialogue)
                    return false;

                dialogueProperty = cell.FindPropertyRelative("dialogue");
                triggerOnceProperty = cell.FindPropertyRelative("triggerDialogueOnce");
                return dialogueProperty != null && triggerOnceProperty != null;
            }

            return false;
        }
    }
}
