using UnityEngine;

namespace ZombieWar.Core
{
    /// <summary>Persisted player options and progress (PlayerPrefs-backed).</summary>
    public static class GameSettings
    {
        private const string MusicKey = "MusicVolume";
        private const string SfxKey = "SFXVolume";
        private const string UnlockKey = "HighestUnlockedLevel";

        /// <summary>Total number of levels shipped with the game.</summary>
        public const int MaxLevel = 2;

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 1f);
            set
            {
                PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, 1f);
            set
            {
                PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        /// <summary>Highest level the player may enter (1 = only Level 1).</summary>
        public static int HighestUnlockedLevel
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(UnlockKey, 1), 1, MaxLevel);
            private set
            {
                PlayerPrefs.SetInt(UnlockKey, Mathf.Clamp(value, 1, MaxLevel));
                PlayerPrefs.Save();
            }
        }

        public static bool IsLevelUnlocked(int level)
        {
            return level >= 1 && level <= HighestUnlockedLevel;
        }

        /// <summary>Unlocks a level. Returns true only when this is a NEW unlock.</summary>
        public static bool UnlockLevel(int level)
        {
            if (level < 2 || level > MaxLevel) return false;
            if (level <= HighestUnlockedLevel) return false;
            HighestUnlockedLevel = level;
            return true;
        }

        /// <summary>Dev-only: wipes level progress (see Editor menu ZombieWar/Reset Level Progress).</summary>
        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(UnlockKey);
            PlayerPrefs.Save();
        }
    }
}
