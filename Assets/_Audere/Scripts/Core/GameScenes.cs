namespace Audere.Core
{
    /// <summary>
    /// Single source of truth for scene names. Keep these in sync with the entries in
    /// Build Settings (File &gt; Build Settings). Referencing scenes by these constants
    /// avoids magic strings scattered across the codebase.
    /// </summary>
    public static class GameScenes
    {
        public const string Bootstrap = "00_Bootstrap";
        public const string MainMenu  = "10_MainMenu";
        public const string Game      = "20_Game";
    }
}
