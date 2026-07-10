using UnityEngine;

namespace ZombieWar.Weapons
{
    /// <summary>Spring-damped kickback applied to the held gun model.</summary>
    public class Recoil : MonoBehaviour
    {
        [SerializeField] private float returnSpeed = 18f;

        private Vector3 restLocalPos;
        private float kick;

        private void Awake()
        {
            restLocalPos = transform.localPosition;
        }

        public void Kick(float amount)
        {
            kick = Mathf.Min(kick + amount, 0.18f);
        }

        private void LateUpdate()
        {
            kick = Mathf.Lerp(kick, 0f, returnSpeed * Time.deltaTime);
            transform.localPosition = restLocalPos - Vector3.forward * kick;
        }
    }
}
