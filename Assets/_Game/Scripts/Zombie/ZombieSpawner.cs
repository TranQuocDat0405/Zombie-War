using UnityEngine;
using UnityEngine.AI;
using ZombieWar.Core;

namespace ZombieWar.Zombie
{
    /// <summary>
    /// Spawns pooled zombies on a ring around the arena, cycling through the
    /// four compass directions so hordes visibly close in from all sides.
    /// </summary>
    public class ZombieSpawner : MonoBehaviour
    {
        [SerializeField] private WaveConfig config;
        [SerializeField] private GameObject zombiePrefab;
        [SerializeField] private GameObject giantPrefab;
        [SerializeField] private float spawnRadius = 26f;
        [SerializeField] private float directionJitter = 35f;

        private float nextSpawn;
        private int quadrant;
        private int giantPhaseDone = -1;

        private void Update()
        {
            if (config == null || zombiePrefab == null) return;
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            float elapsed = GameManager.Instance.TimeElapsed;
            int phaseIndex = config.GetPhaseIndex(elapsed);
            var phase = config.phases[phaseIndex];

            if (phase.spawnGiantAtStart && giantPhaseDone < phaseIndex && giantPrefab != null)
            {
                giantPhaseDone = phaseIndex;
                SpawnOne(giantPrefab);
            }

            if (Time.time < nextSpawn) return;
            nextSpawn = Time.time + phase.spawnInterval;

            for (int i = 0; i < phase.zombiesPerTick; i++)
            {
                if (ZombieHealth.AliveCount >= phase.maxAlive) break;
                SpawnOne(zombiePrefab);
            }
        }

        private void SpawnOne(GameObject prefab)
        {
            // Cycle N/E/S/W so zombies keep arriving from all four directions.
            float baseAngle = quadrant * 90f;
            quadrant = (quadrant + 1) % 4;
            float angle = (baseAngle + Random.Range(-directionJitter, directionJitter)) * Mathf.Deg2Rad;

            Vector3 pos = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * spawnRadius;
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 8f, NavMesh.AllAreas))
            {
                pos = hit.position;
            }

            Quaternion look = Quaternion.LookRotation(-pos.normalized);
            ObjectPool.Spawn(prefab, pos, look);
        }
    }
}
