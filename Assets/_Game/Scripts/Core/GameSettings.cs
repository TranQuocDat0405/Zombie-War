using UnityEngine;

namespace ZombieWar.Core
{
    /// <summary>Persisted player options (PlayerPrefs-backed).</summary>
    public static class GameSettings
    {
        private const string MusicKey = "MusicVolume";
        private const string SfxKey = "SFXVolume";

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
    }
}
