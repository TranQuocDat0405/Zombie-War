using Cinemachine;
using NFramework;
using UnityEngine;

namespace ZombieWar.Core
{
    /// <summary>
    /// Fires Cinemachine impulses for weapon recoil, hits and explosions.
    /// Attach next to a CinemachineImpulseSource. Lives in each Level scene
    /// (the gameplay camera is per-level), accessed as CameraShake.I.
    /// </summary>
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class CameraShake : SingletonMono<CameraShake>
    {
        private CinemachineImpulseSource impulse;

        protected override void Awake()
        {
            base.Awake();
            impulse = GetComponent<CinemachineImpulseSource>();
        }

        public void Shake(float force)
        {
            if (impulse != null) impulse.GenerateImpulseWithForce(force);
        }
    }
}
