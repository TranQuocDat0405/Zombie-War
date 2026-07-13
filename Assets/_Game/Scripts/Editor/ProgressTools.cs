using UnityEditor;
using UnityEngine;
using ZombieWar.Core;

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
            GameSettings.ResetProgress();
            Debug.Log("[ZombieWar] Level progress reset — only Level 1 is unlocked now.");
        }

        [MenuItem("ZombieWar/Unlock All Levels")]
        private static void UnlockAll()
        {
            GameSettings.UnlockLevel(GameSettings.MaxLevel);
            Debug.Log("[ZombieWar] All levels unlocked (highest = " + GameSettings.MaxLevel + ").");
        }
    }
}
