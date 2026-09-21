using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    /// <summary>One field-local mesh keeps shared chalk grain continuous across pooled trail strips.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CombatChalkTrailGraphic : MaskableGraphic
    {
        private CombatBoardView board;

        internal void Bind(CombatBoardView owner)
        {
            board = owner;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            if (board != null) board.PopulateStunTrailMesh(vertices, rectTransform);
        }
    }
}
