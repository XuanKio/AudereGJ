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
        public const string Day1HomeMorning = "20_D1_Home_Morning";
        public const string Classroom = "30_Classroom";
        public const string Evening   = "40_Evening";
        public const string Day2HomeMorning = "50_D2_Home_Morning";
        public const string Day2SchoolMorning = "60_D2_School_Morning";
        public const string Day2HomeNight = "70_D2_Home_Night";
        public const string Day2Dream = "80_D2_Dream";
        public const string Day2HomeAwakening = "90_D2_Home_Awakening";
        public const string Day3HomeMorning = "100_D3_Home_Morning";
        public const string Day3SchoolBoard = "110_D3_School_Board";
        public const string Day3SchoolTeacher = "120_D3_School_Teacher";

        public const string Day4HomeMorning = "130_D4_Home_Morning";
        public const string Day4Classroom = "140_D4_Classroom";
        public const string Day4HomeEvening = "150_D4_Home_Evening";

        // Compatibility alias for older call sites and serialized defaults.
        public const string Game = Day1HomeMorning;
    }
}
