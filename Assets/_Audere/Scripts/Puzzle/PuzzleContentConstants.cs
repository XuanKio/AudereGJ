namespace Audere.Puzzle
{
    /// <summary>
    /// Stable identifiers used by saved level data, analytics and content lookup.
    /// Add every new tile and path piece here before creating its asset.
    /// </summary>
    public static class PuzzleContentConstants
    {
        public static class Tiles
        {
            public const string Grass = "grass";
            public const string Goal = "goal";
        }

        public static class Pieces
        {
            public const string Line2 = "line-2";
            public const string LCorner = "l-corner";
            public const string Line4 = "line-4";
        }

        public static class Hand
        {
            public const int MaxSlots = 3;
        }

        public static class AssetPaths
        {
            public const string GrassPrefab = "Assets/_Audere/Prefabs/Puzzle/Tiles/Grass.prefab";
            public const string GoalPrefab = "Assets/_Audere/Prefabs/Puzzle/Tiles/Goal.prefab";
            public const string PlayerPrefab = "Assets/_Audere/Prefabs/Puzzle/Actors/Player.prefab";
            public const string PathPieceSlotArt = "Assets/_Audere/AssetGame/Step Tile/slot.aseprite";
            public const string TileCatalog = "Assets/_Audere/Data/Puzzle/PuzzleTileCatalog.asset";
        }

        public static string GetTileId(PuzzleTileType type)
        {
            return type switch
            {
                PuzzleTileType.Grass => Tiles.Grass,
                PuzzleTileType.Goal => Tiles.Goal,
                _ => type.ToString().ToLowerInvariant()
            };
        }

        public static string GetPieceId(PathPieceType type)
        {
            return type switch
            {
                PathPieceType.Line2 => Pieces.Line2,
                PathPieceType.LCorner => Pieces.LCorner,
                PathPieceType.Line4 => Pieces.Line4,
                _ => type.ToString().ToLowerInvariant()
            };
        }
    }

    public enum PuzzleTileType
    {
        Grass = 0,
        Goal = 1
    }

    public enum PathPieceType
    {
        Line2 = 0,
        LCorner = 1,
        Line4 = 2
    }
}
