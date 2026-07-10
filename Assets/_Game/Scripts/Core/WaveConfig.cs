using UnityEngine;

namespace ZombieWar.Core
{
    /// <summary>Escalating spawn phases for one level.</summary>
    [CreateAssetMenu(menuName = "ZombieWar/Wave Config", fileName = "WaveConfig")]
    public class WaveConfig : ScriptableObject
    {
        [System.Serializable]
        public class Phase
        {
            public float startTime;
            public float spawnInterval = 2.5f;
            public int zombiesPerTick = 2;
            public int maxAlive = 12;
            public bool spawnGiantAtStart;
        }

        public Phase[] phases;

        public int GetPhaseIndex(float elapsed)
        {
            int index = 0;
            for (int i = 0; i < phases.Length; i++)
            {
                if (elapsed >= phases[i].startTime) index = i;
            }
            return index;
        }
    }
}
