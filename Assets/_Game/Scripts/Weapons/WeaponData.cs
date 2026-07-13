using UnityEngine;

namespace ZombieWar.Weapons
{
    /// <summary>Static description of one gun type.</summary>
    [CreateAssetMenu(menuName = "ZombieWar/Weapon", fileName = "Weapon")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Rifle";
        public Sprite icon;
        public GameObject gunModelPrefab;

        [Header("Ballistics")]
        public float damage = 12f;
        public float fireRate = 8f;        // shots per second
        public float range = 12f;
        public int pelletCount = 1;        // >1 = shotgun spread
        public float spreadAngle = 1.5f;   // degrees, per pellet

        [Header("Projectile")]
        public GameObject bulletPrefab;
        public float bulletSpeed = 45f;    // slow enough to read, fast enough to rarely miss

        [Header("Ammo")]
        public int magazineSize = 30;
        public int reserveAmmo = 90;       // starting reserve
        public float reloadTime = 1.6f;

        [Header("Feel")]
        public float recoilKick = 0.05f;   // gun model kickback metres
        public float cameraShake = 0.08f;
        public float shootAnimSpeed = 1f;  // upper-body shoot anim speed multiplier

        [Header("FX")]
        public GameObject muzzleFlashPrefab;
        public GameObject impactPrefab;
        public AudioClip fireClip;
        [Range(0f, 1f)] public float fireVolume = 0.6f;
    }
}
