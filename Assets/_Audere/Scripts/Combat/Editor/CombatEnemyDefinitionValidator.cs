#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Audere.Combat.Editor
{
    public static class CombatEnemyDefinitionValidator
    {
        [MenuItem("Audere/Combat/Validate Enemy Definitions")]
        public static void ValidateAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:CombatEnemyDefinition");
            var ids = new Dictionary<string, CombatEnemyDefinition>();
            int errors = 0;
            foreach (string guid in guids)
            {
                CombatEnemyDefinition definition = AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (definition == null) continue;
                if (!definition.Validate(out string error))
                {
                    Debug.LogError($"[CombatEnemyValidation] {error}", definition);
                    errors++;
                    continue;
                }
                if (ids.TryGetValue(definition.EnemyId, out CombatEnemyDefinition duplicate))
                {
                    Debug.LogError(
                        $"[CombatEnemyValidation] Duplicate Enemy ID '{definition.EnemyId}' in '{duplicate.name}' and '{definition.name}'.",
                        definition);
                    errors++;
                }
                else ids.Add(definition.EnemyId, definition);
            }
            Debug.Log($"[CombatEnemyValidation] Checked {guids.Length} definition(s); {errors} error(s).");
        }
    }
}
#endif
