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

        // Nilah - 2000
        Nilah_Step = 2001,
        Nilah_Hurt = 2002,

        // Timor - 3000
        Timor_Meow = 3001,
        Timor_Step = 3002,

        // Exploration - 4000
        Tile_Rotate = 4001,
        Tile_Select = 4002,
        Tile_Connect = 4003,

        // Combat - 5000
        Dice_Select = 5001,
        Dice_Roll = 5002,
        Dice_Hit = 5003,

        // Music - 9000
        Music_MainMenu = 9001,
        Music_Exploration = 9002,
        Music_Combat = 9003,
    }
}
