using UnityEngine;

namespace ZombieWar.Player
{
    /// <summary>
    /// Drives the upper-body layer weight so the soldier can run while
    /// shooting or reloading. WeaponController calls NotifyFiring()/PlayReload().
    /// </summary>
    public class PlayerAnimation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float blendSpeed = 8f;
        [SerializeField] private float holdTime = 0.35f;
        [SerializeField] private float reloadClipLength = 3.1f; // Mixamo Reloading clip seconds

        private int upperBodyLayer = -1;
        private float lastFireTime = -10f;
        private float reloadUntil = -10f;
        private float weight;

        private static readonly int ReloadHash = Animator.StringToHash("Reload");
        private static readonly int ReloadSpeedHash = Animator.StringToHash("ReloadSpeed");

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null) upperBodyLayer = animator.GetLayerIndex("UpperBody");
        }

        /// <summary>Call whenever the weapon fires (or keeps firing).</summary>
        public void NotifyFiring()
        {
            lastFireTime = Time.time;
        }

        public void SetShootSpeed(float speedMultiplier)
        {
            if (animator != null) animator.SetFloat("ShootSpeed", speedMultiplier);
        }

        /// <summary>Plays the reload animation stretched to the weapon's reload time.</summary>
        public void PlayReload(float duration)
        {
            if (animator == null || upperBodyLayer < 0 || duration <= 0f) return;
            reloadUntil = Time.time + duration;
            animator.SetFloat(ReloadSpeedHash, reloadClipLength / duration);
            animator.SetTrigger(ReloadHash);
        }

        /// <summary>Weapon switch interrupts the reload animation.</summary>
        public void CancelReload()
        {
            if (reloadUntil <= Time.time) return;
            reloadUntil = -10f;
            if (animator != null && upperBodyLayer >= 0)
            {
                animator.ResetTrigger(ReloadHash);
                animator.CrossFade("Shoot", 0.15f, upperBodyLayer);
            }
        }

        private void Update()
        {
            if (animator == null || upperBodyLayer < 0) return;

            bool active = Time.time - lastFireTime < holdTime || Time.time < reloadUntil;
            weight = Mathf.MoveTowards(weight, active ? 1f : 0f, blendSpeed * Time.deltaTime);
            animator.SetLayerWeight(upperBodyLayer, weight);
        }
    }
}
