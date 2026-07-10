using UnityEngine;

namespace ZombieWar.Player
{
    /// <summary>
    /// Drives the upper-body shoot layer weight so the soldier can run and
    /// shoot at the same time. WeaponController calls NotifyFiring() each shot.
    /// </summary>
    public class PlayerAnimation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float blendSpeed = 8f;
        [SerializeField] private float holdTime = 0.35f;

        private int upperBodyLayer = -1;
        private float lastFireTime = -10f;
        private float weight;

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

        private void Update()
        {
            if (animator == null || upperBodyLayer < 0) return;

            float target = Time.time - lastFireTime < holdTime ? 1f : 0f;
            weight = Mathf.MoveTowards(weight, target, blendSpeed * Time.deltaTime);
            animator.SetLayerWeight(upperBodyLayer, weight);
        }
    }
}
