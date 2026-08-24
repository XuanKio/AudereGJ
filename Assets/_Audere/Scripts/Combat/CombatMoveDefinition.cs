using UnityEngine;

namespace Audere.Combat
{
    public abstract class CombatMoveDefinition : ScriptableObject
    {
        [SerializeField, Min(.01f)] private float duration = 4f;
        public float Duration => duration;
        public virtual bool Validate(out string error)
        {
            if (duration <= 0f)
            {
                error = $"Move '{name}' requires Duration greater than zero.";
                return false;
            }
            error = null;
            return true;
        }
        public abstract ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context);
    }
}
