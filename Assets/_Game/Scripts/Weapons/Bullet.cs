using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.Weapons
{
    /// <summary>
    /// Pooled projectile. Sweeps a raycast over the distance covered each frame
    /// instead of using a collider, so it can never tunnel through a zombie and
    /// never shoves the ragdoll physics around.
    /// </summary>
    public class Bullet : MonoBehaviour, IPoolable
    {
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private TrailRenderer trail;

        private Vector3 direction;
        private float speed;
        private float damage;
        private float maxRange;
        private LayerMask hitMask;
        private GameObject impactPrefab;
        private float traveled;
        private float dieAt;
        private bool live;

        public void Launch(Vector3 dir, float speed, float damage, float range,
                           LayerMask hitMask, GameObject impactPrefab)
        {
            direction = dir.normalized;
            this.speed = speed;
            this.damage = damage;
            maxRange = range;
            this.hitMask = hitMask;
            this.impactPrefab = impactPrefab;

            transform.rotation = Quaternion.LookRotation(direction);
            traveled = 0f;
            dieAt = Time.time + lifetime;
            live = true;
        }

        private void Update()
        {
            if (!live) return;

            float step = speed * Time.deltaTime;
            Vector3 from = transform.position;

            RaycastHit hit;
            if (Physics.Raycast(from, direction, out hit, step, hitMask, QueryTriggerInteraction.Ignore))
            {
                Impact(hit.point, hit.normal, hit.collider);
                return;
            }

            transform.position = from + direction * step;
            traveled += step;

            if (traveled >= maxRange || Time.time >= dieAt)
            {
                live = false;
                ObjectPool.Release(gameObject);
            }
        }

        private void Impact(Vector3 point, Vector3 normal, Collider col)
        {
            live = false;

            var damageable = col.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(damage, point, direction);
            }

            if (impactPrefab != null)
            {
                ObjectPool.Spawn(impactPrefab, point, Quaternion.LookRotation(normal));
            }

            transform.position = point;
            ObjectPool.Release(gameObject);
        }

        public void OnSpawned()
        {
            live = false; // stays inert until Launch arms it
            traveled = 0f;
            // A recycled trail would otherwise streak across the map from its last position.
            if (trail != null) trail.Clear();
        }

        public void OnDespawned()
        {
            live = false;
            if (trail != null) trail.Clear();
        }
    }
}
