using UnityEngine;
using UnityEngine.AI;
using ZombieWar.Core;

namespace ZombieWar.Zombie
{
    /// <summary>
    /// NavMesh chase + melee attack. Locomotion feeds the animator blend tree
    /// through the normalized agent velocity.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class ZombieAI : MonoBehaviour, IPoolable
    {
        [SerializeField] private float attackRange = 1.7f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float damageDelay = 0.45f;
        [SerializeField] private float repathInterval = 0.25f;
        [SerializeField] private Animator animator;
        [SerializeField] private AudioClip[] growlClips;
        [SerializeField] private AudioClip attackClip;

        private NavMeshAgent agent;
        private ZombieHealth health;
        private Transform target;
        private IDamageable targetDamageable;
        private float nextRepath;
        private float nextAttack;
        private float nextGrowl;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<ZombieHealth>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (health != null && health.IsDead) return;
            if (!agent.enabled || !agent.isOnNavMesh) return;

            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
            {
                agent.isStopped = true;
                if (animator != null) animator.SetFloat(SpeedHash, 0f);
                return;
            }

            if (target == null)
            {
                AcquireTarget();
                return;
            }

            float dist = Vector3.Distance(transform.position, target.position);
            if (dist <= attackRange)
            {
                agent.isStopped = true;
                FaceTarget();
                if (Time.time >= nextAttack)
                {
                    nextAttack = Time.time + attackCooldown;
                    if (animator != null) animator.SetTrigger(AttackHash);
                    if (attackClip != null && AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySfxRandomPitch(attackClip, transform.position, 0.7f);
                    }
                    Invoke(nameof(ApplyDamage), damageDelay);
                }
            }
            else
            {
                agent.isStopped = false;
                if (Time.time >= nextRepath)
                {
                    nextRepath = Time.time + repathInterval;
                    agent.SetDestination(target.position);
                }
            }

            if (animator != null)
            {
                float normalized = agent.velocity.magnitude / Mathf.Max(agent.speed, 0.01f);
                animator.SetFloat(SpeedHash, normalized, 0.1f, Time.deltaTime);
            }

            if (Time.time >= nextGrowl && growlClips != null && growlClips.Length > 0)
            {
                nextGrowl = Time.time + Random.Range(5f, 10f);
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySfxRandomPitch(
                        growlClips[Random.Range(0, growlClips.Length)], transform.position, 0.5f);
                }
            }
        }

        private void AcquireTarget()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
            {
                target = go.transform;
                targetDamageable = go.GetComponent<IDamageable>();
            }
        }

        private void ApplyDamage()
        {
            if (health != null && health.IsDead) return;
            if (target == null || targetDamageable == null || targetDamageable.IsDead) return;
            if (Vector3.Distance(transform.position, target.position) <= attackRange + 0.6f)
            {
                Vector3 dir = (target.position - transform.position).normalized;
                targetDamageable.TakeDamage(attackDamage, target.position, dir);
            }
        }

        private void FaceTarget()
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        }

        public void DisableAI()
        {
            CancelInvoke();
            if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false;
        }

        public void SetStats(float speed, float damage)
        {
            agent.speed = speed;
            attackDamage = damage;
        }

        public void OnSpawned()
        {
            nextAttack = 0f;
            nextRepath = 0f;
            nextGrowl = Time.time + Random.Range(0f, 4f);
            if (!agent.enabled) agent.enabled = true;
            agent.Warp(transform.position);
            agent.isStopped = false;
            AcquireTarget();
        }

        public void OnDespawned()
        {
            CancelInvoke();
            if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
        }
    }
}
