using UnityEngine;
using UnityEngine.AI;

namespace ZombieWar.Core
{
    /// <summary>
    /// Periodically drops pickups on the NavMesh near the player (never so far
    /// that hunting for ammo breaks the flow of a run).
    /// </summary>
    public class PickupSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject healthPrefab;
        [SerializeField] private GameObject ammoPrefab;
        [SerializeField] private GameObject bombPrefab;
        [SerializeField] private float minInterval = 9f;
        [SerializeField] private float maxInterval = 14f;
        [SerializeField] private float minDistance = 4f;
        [SerializeField] private float maxDistance = 9f;
        [SerializeField] private float arenaLimit = 26f;
        [SerializeField] private int maxActive = 4;

        private Transform player;
        private float nextSpawn;
        private int spawnedCount;
        private readonly System.Collections.Generic.List<GameObject> active = new();

        // ammo appears most often, then health, then bombs
        private static readonly int[] typeCycle = { 0, 1, 0, 2, 0, 1, 2 }; // 0=ammo 1=health 2=bomb

        private void Start()
        {
            nextSpawn = Time.time + 6f; // first drop arrives early
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            if (Time.time < nextSpawn) return;
            nextSpawn = Time.time + Random.Range(minInterval, maxInterval);

            if (player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go == null) return;
                player = go.transform;
            }

            active.RemoveAll(go => go == null || !go.activeInHierarchy);
            if (active.Count >= maxActive) return;

            GameObject prefab = null;
            switch (typeCycle[spawnedCount % typeCycle.Length])
            {
                case 0: prefab = ammoPrefab; break;
                case 1: prefab = healthPrefab; break;
                case 2: prefab = bombPrefab; break;
            }
            spawnedCount++;
            if (prefab == null) return;

            // Ring around the player, clamped inside the arena walls.
            Vector2 dir = Random.insideUnitCircle.normalized;
            float dist = Random.Range(minDistance, maxDistance);
            Vector3 pos = player.position + new Vector3(dir.x, 0f, dir.y) * dist;
            pos.x = Mathf.Clamp(pos.x, -arenaLimit, arenaLimit);
            pos.z = Mathf.Clamp(pos.z, -arenaLimit, arenaLimit);
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 6f, NavMesh.AllAreas)) pos = hit.position;

            active.Add(ObjectPool.Spawn(prefab, pos + Vector3.up * 0.1f, Quaternion.identity));
        }
    }
}
