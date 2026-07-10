using UnityEngine;
using ZombieWar.Core;
using ZombieWar.Player;

namespace ZombieWar.Weapons
{
    /// <summary>Lobs a bomb toward the current aim target (or ahead) on a cooldown.</summary>
    public class BombThrower : MonoBehaviour
    {
        [SerializeField] private GameObject bombPrefab;
        [SerializeField] private float cooldown = 5f;
        [SerializeField] private float throwDistance = 6f;
        [SerializeField] private float flightTime = 0.8f;

        private AutoAim autoAim;
        private float nextThrowTime;

        public float Cooldown => cooldown;
        public float CooldownRemaining => Mathf.Max(0f, nextThrowTime - Time.time);
        public bool CanThrow =>
            Time.time >= nextThrowTime &&
            (GameManager.Instance == null || GameManager.Instance.State == GameState.Playing);

        private void Awake()
        {
            autoAim = GetComponent<AutoAim>();
        }

        public void ThrowBomb()
        {
            if (!CanThrow || bombPrefab == null) return;
            nextThrowTime = Time.time + cooldown;

            Vector3 targetPos = autoAim != null && autoAim.CurrentTarget != null
                ? autoAim.CurrentTarget.position
                : transform.position + transform.forward * throwDistance;

            Vector3 origin = transform.position + Vector3.up * 1.3f + transform.forward * 0.4f;
            var bomb = ObjectPool.Spawn(bombPrefab, origin, Random.rotation);

            // Ballistic velocity to land on the target after flightTime.
            Vector3 d = targetPos - origin;
            Vector3 velocity = d / flightTime - 0.5f * Physics.gravity * flightTime;
            bomb.GetComponent<Bomb>().Launch(velocity);
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.B)) ThrowBomb();
        }
#endif
    }
}
