using UnityEngine;

namespace ZombieWar.Core
{
    /// <summary>Returns a pooled object (VFX, tracer...) to its pool after a delay.</summary>
    public class PooledAutoRelease : MonoBehaviour
    {
        [SerializeField] private float lifetime = 2f;

        public float Lifetime
        {
            get => lifetime;
            set => lifetime = value;
        }

        private void OnEnable()
        {
            Invoke(nameof(Release), lifetime);
        }

        private void OnDisable()
        {
            CancelInvoke();
        }

        private void Release()
        {
            ObjectPool.Release(gameObject);
        }
    }
}
