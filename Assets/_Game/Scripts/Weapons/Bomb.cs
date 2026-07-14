using System.Collections;
using UnityEngine;
using ZombieWar.Core;
using ZombieWar.Zombie;

namespace ZombieWar.Weapons
{
    /// <summary>
    /// Thrown bomb: fuse, explosion VFX, then physics damage — zombies killed
    /// by the blast ragdoll and are flung with AddExplosionForce.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Bomb : MonoBehaviour
    {
        [SerializeField] private float fuseTime = 1.1f;
        [SerializeField] private float damage = 65f;
        [SerializeField] private float radius = 4.5f;
        [SerializeField] private float explosionForce = 9f;
        [SerializeField] private LayerMask zombieMask;
        [SerializeField] private GameObject explosionVfx;
        [SerializeField] private AudioClip explosionClip;

        private Rigidbody rb;
        private readonly Collider[] hits = new Collider[48];

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        public void Launch(Vector3 velocity)
        {
            rb.velocity = velocity;
            rb.angularVelocity = Random.insideUnitSphere * 8f;
            StartCoroutine(FuseRoutine());
        }

        private IEnumerator FuseRoutine()
        {
            yield return new WaitForSeconds(fuseTime);
            Explode();
        }

        private void Explode()
        {
            Vector3 center = transform.position;

            if (explosionVfx != null)
            {
                ObjectPool.Spawn(explosionVfx, center, Quaternion.identity);
            }
            if (explosionClip != null && WorldSoundManager.I != null)
            {
                // 3D for direction, but with a huge loudness plateau so the blast
                // dwarfs the gunfire anywhere near the arena.
                WorldSoundManager.I.PlaySfx(explosionClip, center, 1f, 1f, true, 18f);
            }
            if (CameraShake.I != null) CameraShake.I.Shake(1.6f);

            int count = Physics.OverlapSphereNonAlloc(center, radius, hits, zombieMask);
            for (int i = 0; i < count; i++)
            {
                var zombie = hits[i].GetComponentInParent<ZombieHealth>();
                if (zombie == null || zombie.IsDead) continue;

                float dist = Vector3.Distance(center, zombie.transform.position);
                float falloff = Mathf.Clamp01(1f - dist / radius);
                zombie.TakeExplosion(damage * Mathf.Lerp(0.4f, 1f, falloff), center, explosionForce, radius);
            }

            ObjectPool.Release(gameObject);
        }
    }
}
