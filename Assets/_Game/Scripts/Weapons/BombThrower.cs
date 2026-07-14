using System;
using UnityEngine;
using ZombieWar.Core;
using ZombieWar.Player;

namespace ZombieWar.Weapons
{
    /// <summary>Lobs a bomb toward the current aim target (or ahead). Bombs are a
    /// limited resource collected from pickups.</summary>
    public class BombThrower : MonoBehaviour
    {
        [SerializeField] private GameObject bombPrefab;
        [SerializeField] private float cooldown = 5f;
        [SerializeField] private float throwDistance = 6f;
        [SerializeField] private float flightTime = 0.8f;
        [SerializeField] private int startBombs = 1;
        [SerializeField] private int maxBombs = 3;

        private AutoAim autoAim;
        private float nextThrowTime;

        public int BombCount { get; private set; }
        public int MaxBombs => maxBombs;
        public float Cooldown => cooldown;
        public float CooldownRemaining => Mathf.Max(0f, nextThrowTime - Time.time);
        public bool CanThrow =>
            BombCount > 0 &&
            Time.time >= nextThrowTime &&
            (LevelManager.Instance == null || LevelManager.Instance.State == GameState.Playing);

        public event Action OnBombCountChanged;

        private void Awake()
        {
            autoAim = GetComponent<AutoAim>();
            BombCount = startBombs;
        }

        public void AddBombs(int count)
        {
            BombCount = Mathf.Min(maxBombs, BombCount + count);
            OnBombCountChanged?.Invoke();
        }

        public void ThrowBomb()
        {
            if (!CanThrow || bombPrefab == null) return;
            nextThrowTime = Time.time + cooldown;
            BombCount--;
            OnBombCountChanged?.Invoke();

            Vector3 targetPos = autoAim != null && autoAim.CurrentTarget != null
                ? autoAim.CurrentTarget.position
                : transform.position + transform.forward * throwDistance;

            Vector3 origin = transform.position + Vector3.up * 1.3f + transform.forward * 0.4f;
            var bomb = ObjectPool.Spawn(bombPrefab, origin, UnityEngine.Random.rotation);

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
