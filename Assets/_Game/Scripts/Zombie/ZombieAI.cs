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
        [Tooltip("How long the zombie roots itself while committing to a swing.")]
        [SerializeField] private float swingLock = 0.25f;
        [SerializeField] private float repathInterval = 0.25f;
        [SerializeField] private Animator animator;
        [SerializeField] private AudioClip[] growlClips;
        [SerializeField] private AudioClip attackClip;
        [Tooltip("Runner variant: freeze and play the Scream state for this long after spawning.")]
        [SerializeField] private float spawnScreamDuration = 0f;
        [Tooltip("Base animator playback speed — lets big/slow variants match feet to velocity.")]
        [SerializeField] private float animatorBaseSpeed = 1f;

        private NavMeshAgent agent;
        private ZombieHealth health;
        private Transform target;
        private IDamageable targetDamageable;
        private float nextRepath;
        private float nextAttack;
        private float nextGrowl;
        private float baseSpeed;
        private float frozenUntil;
        private float swingLockUntil;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int ScreamHash = Animator.StringToHash("Scream");

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<ZombieHealth>();
            baseSpeed = agent.speed;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null) animator.speed = animatorBaseSpeed;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            agent.speed = baseSpeed * multiplier;
            // Keep feet in sync with the faster movement so zombies never glide.
            if (animator != null) animator.speed = animatorBaseSpeed * multiplier;
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

            // Spawn scream: rooted in place, facing the player, before charging.
            if (Time.time < frozenUntil)
            {
                agent.isStopped = true;
                FaceTarget();
                if (animator != null) animator.SetFloat(SpeedHash, 0f);
                return;
            }

            float dist = Vector3.Distance(transform.position, target.position);
            bool inRange = dist <= attackRange;

            // Only root the zombie for the brief moment it commits to a swing.
            // Hard-stopping for the whole time it was in range let a running player
            // (5 m/s) slip straight back out, so the swing always whiffed.
            bool committed = Time.time < swingLockUntil;
            agent.isStopped = committed;

            if (!committed)
            {
                // Close in, the player outruns a 0.25s repath — track them tightly.
                float interval = dist < 3f ? 0.08f : repathInterval;
                if (Time.time >= nextRepath)
                {
                    nextRepath = Time.time + interval;
                    agent.SetDestination(target.position);
                }
            }

            if (inRange)
            {
                FaceTarget();
                if (Time.time >= nextAttack && FacingTarget(50f))
                {
                    nextAttack = Time.time + attackCooldown;
                    swingLockUntil = Time.time + swingLock;
                    if (animator != null) animator.SetTrigger(AttackHash);
                    if (attackClip != null && AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySfxRandomPitch(attackClip, transform.position, 0.7f);
                    }
                    // animator.speed is scaled by SetSpeedMultiplier, so a fixed delay
                    // would drift out of sync with the swing as zombies speed up.
                    float animSpeed = animator != null ? Mathf.Max(0.1f, animator.speed) : 1f;
                    Invoke(nameof(ApplyDamage), damageDelay / animSpeed);
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

            // Small grace only — the old 0.9m slop let a player who merely brushed
            // past an idle zombie take a hit the swing never came close to landing.
            if (Vector3.Distance(transform.position, target.position) > attackRange + 0.35f) return;
            if (!FacingTarget(60f)) return;

            Vector3 dir = (target.position - transform.position).normalized;
            targetDamageable.TakeDamage(attackDamage, target.position, dir);
        }

        private bool FacingTarget(float maxAngle)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return true;
            return Vector3.Angle(transform.forward, dir) <= maxAngle;
        }

        private void FaceTarget()
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir), 16f * Time.deltaTime);
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
            swingLockUntil = 0f;
            nextGrowl = Time.time + Random.Range(0f, 4f);
            agent.speed = baseSpeed; // spawner re-applies the time-based multiplier
            if (animator != null) animator.speed = animatorBaseSpeed;
            if (!agent.enabled) agent.enabled = true;
            agent.Warp(transform.position);
            agent.isStopped = false;
            AcquireTarget();

            frozenUntil = 0f;
            if (spawnScreamDuration > 0f)
            {
                frozenUntil = Time.time + spawnScreamDuration;
                if (animator != null) animator.SetTrigger(ScreamHash);
                if (growlClips != null && growlClips.Length > 0 && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySfx(
                        growlClips[Random.Range(0, growlClips.Length)], transform.position, 0.9f, 1.1f);
                }
            }
        }

        public void OnDespawned()
        {
            CancelInvoke();
            if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
        }
    }
}
