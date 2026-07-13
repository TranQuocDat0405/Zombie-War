using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.Weapons
{
    /// <summary>
    /// Pooled projectile. Sweeps the distance covered each frame instead of using a
    /// collider, so it can never tunnel through a zombie and never shoves the
    /// ragdoll physics around.
    /// </summary>
    public class Bullet : MonoBehaviour, IPoolable
    {
        [SerializeField] private float lifetime = 3f;
        [Tooltip("Fat-ray radius. A hairline ray slips past zombies that are strafing.")]
        [SerializeField] private float castRadius = 0.15f;
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
        private bool cosmetic;

        private IDamageable reservedTarget;
        private float reservedAmount;

        /// <summary>
        /// Aiming and collision both start at the muzzle, so the sweep always follows
        /// the line the bullet is actually drawn along. (An earlier version aimed from
        /// the muzzle but swept from the chest 0.57m to the side; at point-blank range
        /// that parallax threw the sweep nearly a metre wide of the target, so close
        /// zombies could not be hit at all.) Point-blank shots never reach this class:
        /// WeaponController resolves them directly, because a cast that begins inside
        /// a collider can never report a hit.
        /// </summary>
        public void Launch(Vector3 dir, float speed, float damage, float range,
                           LayerMask hitMask, GameObject impactPrefab,
                           IDamageable reservedTarget, float reservedAmount)
        {
            direction = dir.normalized;
            this.speed = speed;
            this.damage = damage;
            maxRange = range;
            this.hitMask = hitMask;
            this.impactPrefab = impactPrefab;
            this.reservedTarget = reservedTarget;
            this.reservedAmount = reservedAmount;
            cosmetic = false;

            transform.rotation = Quaternion.LookRotation(direction);
            traveled = 0f;
            dieAt = Time.time + lifetime;
            live = true;
        }

        /// <summary>
        /// A round that only exists to be seen. The damage was already applied by
        /// WeaponController; this just flies the given distance and vanishes.
        /// </summary>
        public void LaunchCosmetic(Vector3 dir, float speed, float distance)
        {
            direction = dir.normalized;
            this.speed = speed;
            maxRange = distance;
            damage = 0f;
            reservedTarget = null;
            reservedAmount = 0f;
            cosmetic = true;

            transform.rotation = Quaternion.LookRotation(direction);
            traveled = 0f;
            dieAt = Time.time + lifetime;
            live = true;
        }

        private void Update()
        {
            if (!live) return;

            float step = speed * Time.deltaTime;

            if (!cosmetic)
            {
                RaycastHit hit;
                if (Physics.SphereCast(transform.position, castRadius, direction, out hit, step,
                                       hitMask, QueryTriggerInteraction.Ignore))
                {
                    Impact(hit.point, hit.normal, hit.collider);
                    return;
                }
            }

            transform.position += direction * step;
            traveled += step;

            if (traveled >= maxRange || Time.time >= dieAt)
            {
                Expire();
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
            ReleaseReservation();
            ObjectPool.Release(gameObject);
        }

        private void Expire()
        {
            live = false;
            ReleaseReservation();
            ObjectPool.Release(gameObject);
        }

        /// <summary>Hand the reserved damage back exactly once, whether we hit or missed.</summary>
        private void ReleaseReservation()
        {
            if (reservedTarget == null) return;
            reservedTarget.ReleasePending(reservedAmount);
            reservedTarget = null;
            reservedAmount = 0f;
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
            ReleaseReservation(); // safety net if the pool reclaims a bullet mid-flight
            if (trail != null) trail.Clear();
        }
    }
}
