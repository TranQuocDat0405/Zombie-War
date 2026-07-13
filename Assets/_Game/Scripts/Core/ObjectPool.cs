using System.Collections.Generic;
using UnityEngine;

namespace ZombieWar.Core
{
    /// <summary>
    /// Simple prefab pool. Instances are re-parented under the pool object and
    /// recycled via Release(); pooled objects may implement IPoolable.
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        private static readonly Dictionary<GameObject, ObjectPool> pools = new();
        private readonly Stack<GameObject> inactive = new();
        private GameObject prefab;

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!pools.TryGetValue(prefab, out var pool) || pool == null)
            {
                var go = new GameObject($"Pool_{prefab.name}");
                pool = go.AddComponent<ObjectPool>();
                pool.prefab = prefab;
                pools[prefab] = pool;
            }
            return pool.SpawnInternal(position, rotation);
        }

        public static void Release(GameObject instance)
        {
            var marker = instance.GetComponent<PooledInstance>();
            if (marker != null && marker.Owner != null)
            {
                marker.Owner.ReleaseInternal(instance);
            }
            else
            {
                Destroy(instance);
            }
        }

        private GameObject SpawnInternal(Vector3 position, Quaternion rotation)
        {
            GameObject instance = null;
            while (inactive.Count > 0 && instance == null)
            {
                instance = inactive.Pop();
            }

            PooledInstance marker;
            if (instance == null)
            {
                instance = Instantiate(prefab, position, rotation, transform);
                marker = instance.AddComponent<PooledInstance>();
                marker.Owner = this;
                marker.Poolables = instance.GetComponentsInChildren<IPoolable>(true);
            }
            else
            {
                marker = instance.GetComponent<PooledInstance>();
                instance.transform.SetPositionAndRotation(position, rotation);
                instance.SetActive(true);
            }

            var poolables = marker.Poolables;
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnSpawned();
            }
            return instance;
        }

        private void ReleaseInternal(GameObject instance)
        {
            var marker = instance.GetComponent<PooledInstance>();
            var poolables = marker.Poolables;
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnDespawned();
            }
            instance.SetActive(false);
            instance.transform.SetParent(transform, true);
            inactive.Push(instance);
        }

        private void OnDestroy()
        {
            if (prefab != null && pools.TryGetValue(prefab, out var p) && p == this)
            {
                pools.Remove(prefab);
            }
        }
    }

    public class PooledInstance : MonoBehaviour
    {
        public ObjectPool Owner { get; set; }

        /// <summary>Cached once on creation — spawning runs every frame while firing.</summary>
        public IPoolable[] Poolables { get; set; }
    }

    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }
}
