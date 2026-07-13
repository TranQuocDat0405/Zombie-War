using UnityEngine;

namespace ZombieWar.Player
{
    /// <summary>
    /// Periodically finds the closest alive zombie inside range and exposes it
    /// as the current aim target.
    /// </summary>
    public class AutoAim : MonoBehaviour
    {
        [SerializeField] private float range = 12f;
        [SerializeField] private float scanInterval = 0.05f;
        [SerializeField] private LayerMask zombieMask;

        private readonly Collider[] hits = new Collider[32];
        private float nextScanTime;

        public Transform CurrentTarget { get; private set; }
        public float Range => range;

        private void Update()
        {
            if (Time.time < nextScanTime) return;
            nextScanTime = Time.time + scanInterval;
            Scan();
        }

        private void Scan()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, range, hits, zombieMask);
            Transform best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var damageable = hits[i].GetComponentInParent<ZombieWar.Core.IDamageable>();
                if (damageable == null || damageable.IsDead) continue;

                // Bullets already in the air will finish this one — treat it as dead
                // and move on now, instead of emptying another burst into it.
                var zombie = damageable as ZombieWar.Zombie.ZombieHealth;
                if (zombie != null && zombie.EffectiveHealth <= 0f) continue;

                float sqr = (hits[i].transform.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = damageable.Transform;
                }
            }

            CurrentTarget = best;
        }
    }
}
