using UnityEngine;

namespace ZombieWar.Core
{
    public interface IDamageable
    {
        bool IsDead { get; }
        Transform Transform { get; }
        void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection);

        /// <summary>
        /// Damage from bullets already in the air. Aiming code subtracts this from
        /// health so the soldier stops pouring rounds into a target that is already
        /// dead on arrival — bullets take longer to land than the rifle takes to
        /// fire again, so without this every kill wastes a shot.
        /// </summary>
        float PendingDamage { get; }
        void ReservePending(float amount);
        void ReleasePending(float amount);
    }
}
