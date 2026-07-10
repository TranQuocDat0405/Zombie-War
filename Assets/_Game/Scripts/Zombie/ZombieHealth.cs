using System.Collections;
using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.Zombie
{
    /// <summary>
    /// Zombie hit points and the death sequence: freeze or ragdoll, dissolve,
    /// then return to the pool.
    /// </summary>
    public class ZombieHealth : MonoBehaviour, IDamageable, IPoolable
    {
        [SerializeField] private float maxHealth = 30f;
        [SerializeField] private float dissolveDuration = 1.1f;
        [SerializeField] private float normalDeathDelay = 0.2f;
        [SerializeField] private float ragdollDeathDelay = 1.3f;
        [SerializeField] private AudioClip[] deathClips;

        public static int AliveCount { get; private set; }

        private ZombieAI ai;
        private ZombieRagdoll ragdoll;
        private DissolveEffect dissolve;
        private Animator animator;
        private Collider[] hitColliders;
        private float health;

        public bool IsDead { get; private set; }
        public Transform Transform => transform;
        public float MaxHealth { get => maxHealth; set => maxHealth = value; }

        private void Awake()
        {
            ai = GetComponent<ZombieAI>();
            ragdoll = GetComponent<ZombieRagdoll>();
            dissolve = GetComponent<DissolveEffect>();
            animator = GetComponentInChildren<Animator>();
            hitColliders = GetComponents<Collider>();
            health = maxHealth;
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (IsDead) return;
            health -= amount;
            if (dissolve != null) dissolve.Flash();
            if (health <= 0f) Die(false, Vector3.zero, 0f, 0f);
        }

        public void TakeExplosion(float damage, Vector3 center, float force, float radius)
        {
            if (IsDead) return;
            health -= damage;
            if (dissolve != null) dissolve.Flash();
            if (health <= 0f) Die(true, center, force, radius);
        }

        private void Die(bool explosive, Vector3 center, float force, float radius)
        {
            IsDead = true;
            AliveCount = Mathf.Max(0, AliveCount - 1);

            if (GameManager.Instance != null) GameManager.Instance.RegisterKill();
            if (AudioManager.Instance != null && deathClips != null && deathClips.Length > 0)
            {
                AudioManager.Instance.PlaySfxRandomPitch(
                    deathClips[Random.Range(0, deathClips.Length)], transform.position, 0.8f);
            }

            if (ai != null) ai.DisableAI();
            foreach (var col in hitColliders) col.enabled = false;

            if (explosive && ragdoll != null)
            {
                ragdoll.SetRagdoll(true);
                ragdoll.ApplyExplosion(force, center, radius, 0.6f);
                StartCoroutine(DeathRoutine(ragdollDeathDelay));
            }
            else
            {
                if (animator != null) animator.enabled = false; // freeze last pose
                StartCoroutine(DeathRoutine(normalDeathDelay));
            }
        }

        private IEnumerator DeathRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (dissolve != null) yield return dissolve.PlayDissolve(dissolveDuration);
            ObjectPool.Release(gameObject);
        }

        public void OnSpawned()
        {
            IsDead = false;
            health = maxHealth;
            foreach (var col in hitColliders) col.enabled = true;
            if (animator != null) animator.enabled = true;
            AliveCount++;
        }

        public void OnDespawned()
        {
            StopAllCoroutines();
            if (!IsDead) AliveCount = Mathf.Max(0, AliveCount - 1);
        }
    }
}
