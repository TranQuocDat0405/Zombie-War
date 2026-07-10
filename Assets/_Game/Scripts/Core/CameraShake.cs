using Cinemachine;
using UnityEngine;

namespace ZombieWar.Core
{
    /// <summary>
    /// Fires Cinemachine impulses for weapon recoil, hits and explosions.
    /// Attach next to a CinemachineImpulseSource.
    /// </summary>
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        private CinemachineImpulseSource impulse;

        private void Awake()
        {
            Instance = this;
            impulse = GetComponent<CinemachineImpulseSource>();
        }

        public void Shake(float force)
        {
            if (impulse != null) impulse.GenerateImpulseWithForce(force);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
