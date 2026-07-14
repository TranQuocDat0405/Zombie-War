using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.Weapons
{
    /// <summary>
    /// Pooled projectile with three flight modes.
    ///
    /// GUIDED — locked onto one zombie: re-aims at the target's current chest every
    /// frame and detonates on *arrival by distance*, not on collider contact. Earlier
    /// versions relied on a capped-turn homing cast, and a target close to the flight
    /// line could out-turn the bullet — it slid past, whipped around, and buried
    /// itself in the ground. Arrival-by-distance makes a miss structurally
    /// impossible: walls still stop the round mid-flight, and any other zombie that
    /// steps into the line simply absorbs it (damage is never wasted).
    ///
    /// UNGUIDED — shotgun spread pellets: fly dead straight and hit whatever the
    /// sweep touches. These are the fan; the blast's centre pellet is guided.
    ///
    /// COSMETIC — point-blank shots resolve damage instantly in WeaponController;
    /// this just draws the round crossing the short gap.
    /// </summary>
    public class Bullet : MonoBehaviour, IPoolable
    {
        [SerializeField] private float lifetime = 3f;
        [Tooltip("Fat-ray radius for mid-flight sweeps. A hairline ray slips past zombies that are strafing.")]
        [SerializeField] private float castRadius = 0.15f;
        [SerializeField] private TrailRenderer trail;

        private const float ChestHeight = 1.1f;

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
        private Transform guideTarget;
        private Vector3 lastChest; // where the round finishes if the target dies mid-flight

        /// <summary>Guided when target is non-null, unguided spread pellet when null.</summary>
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
            guideTarget = reservedTarget != null ? reservedTarget.Transform : null;
            if (guideTarget != null) lastChest = guideTarget.position + Vector3.up * ChestHeight;
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
            guideTarget = null;
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
                if (guideTarget != null)
                {
                    // Track the chest while the target lives; a corpse keeps its last
                    // chest point so the round finishes there instead of sailing on.
                    if (reservedTarget != null && !reservedTarget.IsDead)
                    {
                        lastChest = guideTarget.position + Vector3.up * ChestHeight;
                    }

                    Vector3 toChest = lastChest - transform.position;
                    float distToChest = toChest.magnitude;

                    if (distToChest > 0.0001f)
                    {
                        // Instant re-aim. Over an 8.5m flight at 32 m/s this shifts by
                        // well under a degree per frame — it still reads as straight.
                        direction = toChest / distToChest;
                        transform.rotation = Quaternion.LookRotation(direction);
                    }

                    if (distToChest <= step)
                    {
                        Arrive();
                        return;
                    }
                }

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

        /// <summary>The guided round reached its target's chest.</summary>
        private void Arrive()
        {
            live = false;
            transform.position = lastChest;

            if (reservedTarget != null && !reservedTarget.IsDead)
            {
                reservedTarget.TakeDamage(damage, lastChest, direction);
            }
            // Target already down (a bomb got there first): the round just buries
            // itself in the corpse — impact puff, no onward flight into the dirt.

            if (impactPrefab != null)
            {
                ObjectPool.Spawn(impactPrefab, lastChest, Quaternion.LookRotation(-direction));
            }

            ReleaseReservation();
            ObjectPool.Release(gameObject);
        }

        /// <summary>Something physical (wall, ground, another zombie) got in the way first.</summary>
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

        /// <summary>Hand the reserved damage back exactly once, however the flight ended.</summary>
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
            guideTarget = null;
            ReleaseReservation(); // safety net if the pool reclaims a bullet mid-flight
            if (trail != null) trail.Clear();
        }
    }
}
