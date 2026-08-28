namespace Audere.Audio
{
    /// <summary>
    /// Stable, permanent identity for every sound in the game. Gameplay refers to audio
    /// ONLY by these ids — never by file name or path. Numbers are explicit and treated
    /// as a permanent identity: once an id is assigned it is never reused for a different
    /// sound, even if the entry is removed. The <see cref="AudioCatalog"/> maps each id to
    /// an actual clip.
    /// </summary>
    public enum AudioId
    {
        None = 0,

        // UI - 1000
        UI_Click = 1001,
        UI_Hover = 1002,
        UI_Back = 1003,
        Dialogue_Text = 1004,

        // Nilah - 2000
        Nilah_Step = 2001,
        // Legacy name kept for existing serialized/content references.
        Nilah_Hurt = 2002,
        Player_Hurt = Nilah_Hurt,
        Player_Fall = 2003,

        // Timor - 3000
        Timor_Meow = 3001,
        Timor_Step = 3002,

        // Exploration - 4000
        Tile_Rotate = 4001,
        Tile_Select = 4002,
        Tile_Connect = 4003,
        Bus_Approach = 4004,
        Classroom_Murmur = 4005,
        Tile_Pop = 4006,
        Actor_Step = 4007,
        School_Bell = 4008,
        Message_Arrive = 4009,

        // Combat - 5000
        Dice_Catch = 5001,
        Dice_Roll = 5002,
        Dice_Hit = 5003,
        Enemy_Hurt = 5004,
        Enemy_BulletVolley = 5005,
        Enemy_LaserVolley = 5006,

        // Music - 9000
        Music_MainMenu = 9001,
        Music_Exploration = 9002,
        Music_Combat = 9003,
        Music_TimorCombat = 9004,
    }
}
