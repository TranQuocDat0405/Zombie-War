using NFramework;
using UnityEditor;
using UnityEngine;

namespace ZombieWar.EditorTools
{
    /// <summary>
    /// Dev-only progress utilities. Lives in an Editor folder, so none of this
    /// ships in a build — players can never reset or cheat their progress.
    /// </summary>
    public static class ProgressTools
    {
        [MenuItem("ZombieWar/Reset Level Progress")]
        private static void ResetProgress()
        {
            // Progress now lives in the NFramework save file (UserData + SoundManager).
            SaveManager.DeleteSave();
            Debug.Log("[ZombieWar] Save deleted — progress, tutorial flag and volumes reset.");
        }

        [MenuItem("ZombieWar/Unlock All Levels")]
        private static void UnlockAll()
        {
            if (!Application.isPlaying || !UserData.IsSingletonAlive)
            {
                Debug.LogWarning("[ZombieWar] Enter Play Mode (Main scene) first — UserData must be alive to unlock.");
                return;
            }
            UserData.I.UnlockLevel(UserData.MaxLevel);
            Debug.Log("[ZombieWar] All levels unlocked (highest = " + UserData.MaxLevel + ").");
        }
    }
}
