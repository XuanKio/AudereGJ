using System;
using UnityEngine;

namespace Audere.Puzzle
{
    /// <summary>
    /// Legacy component kept only so older scenes deserialize safely. Gameplay
    /// bounds now come from PuzzleData/BoardManager; viewport bounds come from Camera.pixelRect.
    /// </summary>
    [Obsolete("Use BoardManager for map bounds and Camera.pixelRect for viewport bounds.")]
    [DisallowMultipleComponent]
    public sealed class GameplayBounds2D : MonoBehaviour
    {
    }
}
