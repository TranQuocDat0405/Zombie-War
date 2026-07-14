using System;
using NFramework;
using Newtonsoft.Json;
using UnityEngine;

namespace ZombieWar
{
    /// <summary>
    /// Player progression (replaces the old PlayerPrefs-based GameSettings).
    /// Volume lives in NFramework SoundManager, which is its own ISaveable.
    /// </summary>
    public class UserData : SingletonMono<UserData>, ISaveable
    {
        public static event Action<int> OnHighestUnlockedLevelChanged = delegate { };

        public const int MaxLevel = 2;

        [SerializeField] private SaveData _saveData = new SaveData();

        public int HighestUnlockedLevel
        {
            get => Mathf.Clamp(_saveData.highestUnlockedLevel, 1, MaxLevel);
            private set
            {
                var clamped = Mathf.Clamp(value, 1, MaxLevel);
                if (_saveData.highestUnlockedLevel != clamped)
                {
                    _saveData.highestUnlockedLevel = clamped;
                    DataChanged = true;
                    OnHighestUnlockedLevelChanged?.Invoke(clamped);
                }
            }
        }

        public bool HasSeenTutorial
        {
            get => _saveData.hasSeenTutorial;
            set
            {
                if (_saveData.hasSeenTutorial != value)
                {
                    _saveData.hasSeenTutorial = value;
                    DataChanged = true;
                }
            }
        }

        public bool IsLevelUnlocked(int level) => level >= 1 && level <= HighestUnlockedLevel;

        /// <summary>Same semantics as the old GameSettings.UnlockLevel: true ONLY on a new unlock.</summary>
        public bool UnlockLevel(int level)
        {
            if (level < 2 || level > MaxLevel) return false;
            if (level <= HighestUnlockedLevel) return false;
            HighestUnlockedLevel = level;
            return true;
        }

        #region ISaveable
        [Serializable]
        public class SaveData
        {
            public int highestUnlockedLevel = 1;
            public bool hasSeenTutorial;
        }

        public string SaveKey => "UserData";
        public bool DataChanged { get; set; }
        public object GetData() => _saveData;

        public void SetData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                _saveData = new SaveData();
                DataChanged = true;
            }
            else
            {
                _saveData = JsonConvert.DeserializeObject<SaveData>(data);
            }
        }

        public void OnAllDataLoaded() { }
        #endregion
    }
}
