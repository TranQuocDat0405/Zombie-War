using System.Collections.Generic;
using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.Zombie
{
    /// <summary>
    /// Toggles the pre-built bone ragdoll and restores the bind pose when the
    /// zombie is respawned from the pool.
    /// </summary>
    public class ZombieRagdoll : MonoBehaviour, IPoolable
    {
        private Rigidbody[] bodies;
        private Collider[] ragdollColliders;
        private Animator animator;

        private Transform[] bones;
        private Vector3[] bindPositions;
        private Quaternion[] bindRotations;

        public bool IsRagdoll { get; private set; }

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            var list = new List<Rigidbody>(GetComponentsInChildren<Rigidbody>(true));
            list.RemoveAll(rb => rb.transform == transform);
            bodies = list.ToArray();

            var cols = new List<Collider>();
            foreach (var rb in bodies) cols.AddRange(rb.GetComponents<Collider>());
            ragdollColliders = cols.ToArray();

            // Cache bind pose of the whole skeleton for pooling resets.
            var root = animator != null ? animator.transform : transform;
            bones = root.GetComponentsInChildren<Transform>(true);
            bindPositions = new Vector3[bones.Length];
            bindRotations = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                bindPositions[i] = bones[i].localPosition;
                bindRotations[i] = bones[i].localRotation;
            }

            SetRagdoll(false);
        }

        public void SetRagdoll(bool enabled)
        {
            IsRagdoll = enabled;
            foreach (var rb in bodies)
            {
                rb.isKinematic = !enabled;
                if (enabled) rb.velocity = Vector3.zero;
            }
            foreach (var col in ragdollColliders) col.enabled = enabled;
            if (animator != null) animator.enabled = !enabled;
        }

        public void ApplyExplosion(float force, Vector3 center, float radius, float upModifier)
        {
            foreach (var rb in bodies)
            {
                rb.AddExplosionForce(force, center, radius, upModifier, ForceMode.Impulse);
            }
        }

        public void OnSpawned()
        {
            SetRagdoll(false);
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].localPosition = bindPositions[i];
                bones[i].localRotation = bindRotations[i];
            }
        }

        public void OnDespawned() { }
    }
}
