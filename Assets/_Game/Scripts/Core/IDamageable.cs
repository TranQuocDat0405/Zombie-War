using UnityEngine;

namespace ZombieWar.Core
{
    public interface IDamageable
    {
        bool IsDead { get; }
        Transform Transform { get; }
        void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection);
    }
}
