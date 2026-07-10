using UnityEngine;

namespace ZombieWar.Weapons
{
    /// <summary>
    /// Spring-damped kickback: pushes the transform backward AND leans it back
    /// so the whole body visibly jerks with each shot.
    /// </summary>
    public class Recoil : MonoBehaviour
    {
        [SerializeField] private float returnSpeed = 14f;
        [SerializeField] private float maxKick = 0.25f;
        [SerializeField] private float leanDegreesPerMeter = 65f; // rotational part of the jerk

        private Vector3 restLocalPos;
        private Quaternion restLocalRot;
        private float kick;

        private void Awake()
        {
            restLocalPos = transform.localPosition;
            restLocalRot = transform.localRotation;
        }

        public void Kick(float amount)
        {
            kick = Mathf.Min(kick + amount, maxKick);
        }

        private void LateUpdate()
        {
            kick = Mathf.Lerp(kick, 0f, returnSpeed * Time.deltaTime);
            transform.localPosition = restLocalPos - Vector3.forward * kick;
            transform.localRotation = restLocalRot * Quaternion.Euler(-leanDegreesPerMeter * kick, 0f, 0f);
        }
    }
}
