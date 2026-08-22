using Audere.Puzzle.Board;
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
                "This scene/prefab component is the source of truth after baking. " +
                "Changes here are not written back to PuzzleData.",
                MessageType.Info);

            if (tile != null)
                EditorGUILayout.Vector2IntField("Grid Position", tile.GridPosition);

            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dialogueData"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("triggerOnce"));
            serializedObject.ApplyModifiedProperties();

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.Toggle("Triggered", behaviour.Triggered);

            if (GUILayout.Button("Open Legacy PuzzleData Map Editor"))
                PuzzleMapEditorWindow.Open();
        }
    }
}
