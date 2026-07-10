using UnityEngine;
using UnityEngine.AI;
using ZombieWar.Core;

namespace ZombieWar.Zombie
{
    /// <summary>
    /// Spawns pooled zombies on a ring around the PLAYER — far enough to always
    /// be off-screen — cycling through the four compass directions so hordes
    /// keep closing in from all sides. Occasionally spawns screaming runners.
    /// </summary>
    public class ZombieSpawner : MonoBehaviour
    {
        [SerializeField] private WaveConfig config;
        [SerializeField] private GameObject zombiePrefab;
        [SerializeField] private GameObject giantPrefab;
        [SerializeField] private GameObject runnerPrefab;
        [SerializeField] private float spawnRadius = 18f;      // > camera half-view: always off-screen
        [SerializeField] private float minPlayerDistance = 13f;
        [SerializeField] private float arenaLimit = 27f;
        [SerializeField] private float directionJitter = 35f;
        [SerializeField, Range(0f, 1f)] private float runnerChance = 0.18f;
        [SerializeField] private float runnerStartTime = 30f;

        private Transform player;
        private float nextSpawn;
        private int quadrant;
        private int giantPhaseDone = -1;

        private void Awake()
        {
            // The alive counter is static; stale values from the previous run
            // would silently block all spawning after a restart.
            ZombieHealth.ResetAliveCount();
        }

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

                var prefab = zombiePrefab;
                if (runnerPrefab != null && elapsed >= runnerStartTime)
                {
                    // Runners get progressively more common toward the end of the level.
                    float fraction = TimeFraction();
                    float chance = runnerChance * Mathf.Lerp(0.6f, 2.2f, fraction);
                    if (Random.value < chance) prefab = runnerPrefab;
                }
                SpawnOne(prefab);
            }
        }

        private void SpawnOne(GameObject prefab)
        {
            Vector3 center = Vector3.zero;
            if (player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) player = go.transform;
            }
            if (player != null) center = player.position;

            // Cycle N/E/S/W around the player; retry if the arena wall clamps the
            // point back into camera range.
            for (int attempt = 0; attempt < 4; attempt++)
            {
                float baseAngle = quadrant * 90f;
                quadrant = (quadrant + 1) % 4;
                float angle = (baseAngle + Random.Range(-directionJitter, directionJitter)) * Mathf.Deg2Rad;

                Vector3 pos = center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * spawnRadius;
                pos.x = Mathf.Clamp(pos.x, -arenaLimit, arenaLimit);
                pos.z = Mathf.Clamp(pos.z, -arenaLimit, arenaLimit);

                if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 8f, NavMesh.AllAreas)) pos = hit.position;
                if (Vector3.Distance(pos, center) < minPlayerDistance) continue; // clamped too close — try next side

                Vector3 toPlayer = center - pos;
                toPlayer.y = 0f;
                Quaternion look = toPlayer.sqrMagnitude > 0.01f
                    ? Quaternion.LookRotation(toPlayer.normalized)
                    : Quaternion.identity;

                var zombie = ObjectPool.Spawn(prefab, pos, look);

                // Late-game pressure: zombies get faster as the timer runs down.
                var ai = zombie.GetComponent<ZombieAI>();
                if (ai != null) ai.SetSpeedMultiplier(1f + 1.0f * TimeFraction());
                return;
            }
        }

        private static float TimeFraction()
        {
            var gm = GameManager.Instance;
            if (gm == null) return 0f;
            float total = gm.TimeElapsed + gm.TimeRemaining;
            return total > 0f ? gm.TimeElapsed / total : 0f;
        }
    }
}
