using System;
using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.Player
{
    /// <summary>Player hit points with a short post-hit invulnerability window.</summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float invulnerabilityTime = 0.35f;
        [SerializeField] private AudioClip[] hurtClips;

        private float health;
        private float lastHitTime = -10f;

        public float Health => health;
        public float MaxHealth => maxHealth;
        public bool IsDead { get; private set; }
        public Transform Transform => transform;

        public event Action<float, float> OnHealthChanged;
        public event Action OnDamaged;

        private void Awake()
        {
            health = maxHealth;
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            health = Mathf.Min(maxHealth, health + amount);
            OnHealthChanged?.Invoke(health, maxHealth);
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (IsDead) return;
            if (Time.time - lastHitTime < invulnerabilityTime) return;
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;

            lastHitTime = Time.time;
            health = Mathf.Max(0f, health - amount);
            OnHealthChanged?.Invoke(health, maxHealth);
            OnDamaged?.Invoke();

            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.4f);
            if (AudioManager.Instance != null && hurtClips != null && hurtClips.Length > 0)
            {
                AudioManager.Instance.PlaySfxRandomPitch(
                    hurtClips[UnityEngine.Random.Range(0, hurtClips.Length)], transform.position, 0.8f);
            }

            if (health <= 0f)
            {
                IsDead = true;
                if (GameManager.Instance != null) GameManager.Instance.PlayerDied();
            }
        }
    }
}
