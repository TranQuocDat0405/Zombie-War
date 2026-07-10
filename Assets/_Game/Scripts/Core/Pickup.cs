using UnityEngine;
using ZombieWar.Player;
using ZombieWar.Weapons;

namespace ZombieWar.Core
{
    public enum PickupType { Health, Ammo, Bomb }

    /// <summary>Spinning, bobbing pickup that heals or refills ammo on touch.</summary>
    public class Pickup : MonoBehaviour
    {
        [SerializeField] private PickupType type = PickupType.Health;
        [SerializeField] private float healAmount = 35f;
        [SerializeField] private int ammoMagazines = 2;
        [SerializeField] private int bombCount = 1;
        [SerializeField] private float spinSpeed = 90f;
        [SerializeField] private float bobHeight = 0.2f;
        [SerializeField] private Transform visual;

        private float baseY;

        private void Awake()
        {
            if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
            baseY = visual != null ? visual.localPosition.y : 0f;
        }

        private void Update()
        {
            if (visual == null) return;
            visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            var p = visual.localPosition;
            p.y = baseY + Mathf.Sin(Time.time * 2.5f) * bobHeight;
            visual.localPosition = p;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            if (type == PickupType.Health)
            {
                var hp = other.GetComponent<PlayerHealth>();
                if (hp != null) hp.Heal(healAmount);
            }
            else if (type == PickupType.Ammo)
            {
                var wc = other.GetComponent<WeaponController>();
                if (wc != null) wc.AddAmmoMagazines(ammoMagazines);
            }
            else
            {
                var bt = other.GetComponent<BombThrower>();
                if (bt != null) bt.AddBombs(bombCount);
            }

            ObjectPool.Release(gameObject);
        }
    }
}
