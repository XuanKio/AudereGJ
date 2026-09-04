using System;
using UnityEngine;

namespace Audere.Core
{
    public enum GameDifficulty
    {
        Easy = 0,
        Hard = 1,
    }

    public static class GameplayDifficultySettings
    {
        public const string DifficultyPrefKey = "Audere.Gameplay.Difficulty";
        public const float HardEnemyHealthMultiplier = 1.36f;
        public const float HardPlayerTimeMultiplier = 0.82f;
        public const float GlobalPlayerTimeMultiplier = 0.80f;

        public static GameDifficulty Current
        {
            get
            {
                int stored = PlayerPrefs.GetInt(DifficultyPrefKey, (int)GameDifficulty.Easy);
                return stored == (int)GameDifficulty.Hard
                    ? GameDifficulty.Hard
                    : GameDifficulty.Easy;
            }
            set
            {
                EnsureSupported(value);
                PlayerPrefs.SetInt(DifficultyPrefKey, (int)value);
            }
        }

        public static float GetEnemyHealthMultiplier(GameDifficulty difficulty)
        {
            EnsureSupported(difficulty);
            return difficulty == GameDifficulty.Hard ? HardEnemyHealthMultiplier : 1f;
        }

        public static int ScaleEnemyHealth(int authoredHealth, GameDifficulty difficulty)
        {
            if (authoredHealth <= 0)
                return 0;

            return Mathf.Max(1, Mathf.CeilToInt(
                authoredHealth * GetEnemyHealthMultiplier(difficulty)));
        }

public static float ScalePlayerTime(float authoredTime, GameDifficulty difficulty)
        {
            EnsureSupported(difficulty);
            float difficultyMultiplier = difficulty == GameDifficulty.Hard
                ? HardPlayerTimeMultiplier
                : 1f;
            return Mathf.Max(0f, authoredTime) *
                GlobalPlayerTimeMultiplier *
                difficultyMultiplier;
        }

        private static void EnsureSupported(GameDifficulty difficulty)
        {
            if (difficulty != GameDifficulty.Easy && difficulty != GameDifficulty.Hard)
                throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, "Unsupported game difficulty.");
        }
    }
}
