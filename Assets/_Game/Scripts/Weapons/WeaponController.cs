using System;
using UnityEngine;
using ZombieWar.Core;
using ZombieWar.Player;

namespace ZombieWar.Weapons
{
    /// <summary>
    /// Auto-fires the equipped gun at the AutoAim target: hitscan rays with
    /// pooled muzzle flash, impact VFX, tracer, recoil and sound.
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private WeaponData[] weapons;
        [SerializeField] private Transform handSocket;
        [SerializeField] private GameObject tracerPrefab;
        [SerializeField] private LayerMask hitMask;      // Zombie + obstacles
        [SerializeField] private float aimHeight = 1.1f; // aim at zombie chest

        private AutoAim autoAim;
        private PlayerAnimation playerAnimation;
        private Recoil recoil;

        private GameObject[] gunInstances;
        private Transform[] muzzles;
        private int currentIndex;
        private float nextFireTime;

        public WeaponData CurrentWeapon =>
            weapons != null && weapons.Length > 0 ? weapons[currentIndex] : null;

        public event Action<WeaponData> OnWeaponChanged;

        private void Awake()
        {
            autoAim = GetComponent<AutoAim>();
            playerAnimation = GetComponent<PlayerAnimation>();
        }

        private void Start()
        {
            BuildGunInstances();
            Equip(0);
        }

        private void BuildGunInstances()
        {
            gunInstances = new GameObject[weapons.Length];
            muzzles = new Transform[weapons.Length];

            if (handSocket == null)
            {
                Debug.LogError("WeaponController: handSocket not assigned");
                return;
            }

            recoil = handSocket.GetComponent<Recoil>();
            if (recoil == null) recoil = handSocket.gameObject.AddComponent<Recoil>();

            for (int i = 0; i < weapons.Length; i++)
            {
                var model = Instantiate(weapons[i].gunModelPrefab, handSocket);
                model.name = weapons[i].displayName;
                gunInstances[i] = model;
                var muzzle = model.transform.Find("Muzzle");
                muzzles[i] = muzzle != null ? muzzle : model.transform;
                model.SetActive(false);
            }
        }

        public void NextWeapon()
        {
            if (weapons.Length == 0) return;
            Equip((currentIndex + 1) % weapons.Length);
        }

        private void Equip(int index)
        {
            currentIndex = index;
            for (int i = 0; i < gunInstances.Length; i++)
            {
                if (gunInstances[i] != null) gunInstances[i].SetActive(i == index);
            }
            if (playerAnimation != null)
            {
                playerAnimation.SetShootSpeed(weapons[index].shootAnimSpeed);
            }
            OnWeaponChanged?.Invoke(weapons[index]);
        }

        private void Update()
        {
            var weapon = CurrentWeapon;
            if (weapon == null || autoAim == null) return;
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;

            var target = autoAim.CurrentTarget;
            if (target == null) return;

            Vector3 aimPoint = target.position + Vector3.up * aimHeight;
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist > weapon.range) return;

            if (Time.time < nextFireTime) return;
            nextFireTime = Time.time + 1f / weapon.fireRate;
            Fire(weapon, aimPoint);
        }

        private void Fire(WeaponData weapon, Vector3 aimPoint)
        {
            Vector3 muzzlePos = muzzles[currentIndex].position;
            Vector3 origin = transform.position + Vector3.up * aimHeight;
            Vector3 baseDir = (aimPoint - origin).normalized;

            for (int i = 0; i < weapon.pelletCount; i++)
            {
                Vector3 dir = baseDir;
                if (weapon.spreadAngle > 0.01f)
                {
                    float yaw = UnityEngine.Random.Range(-weapon.spreadAngle, weapon.spreadAngle);
                    float pitch = UnityEngine.Random.Range(-weapon.spreadAngle, weapon.spreadAngle) * 0.25f;
                    dir = Quaternion.Euler(pitch, yaw, 0f) * dir;
                }

                Vector3 end = origin + dir * weapon.range;
                if (Physics.Raycast(origin, dir, out RaycastHit hit, weapon.range, hitMask, QueryTriggerInteraction.Ignore))
                {
                    end = hit.point;
                    var damageable = hit.collider.GetComponentInParent<IDamageable>();
                    if (damageable != null && !damageable.IsDead)
                    {
                        damageable.TakeDamage(weapon.damage, hit.point, dir);
                    }
                    if (weapon.impactPrefab != null)
                    {
                        ObjectPool.Spawn(weapon.impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    }
                }

                if (tracerPrefab != null)
                {
                    var tracer = ObjectPool.Spawn(tracerPrefab, muzzlePos, Quaternion.identity);
                    tracer.GetComponent<Tracer>().Show(muzzlePos, end);
                }
            }

            if (weapon.muzzleFlashPrefab != null)
            {
                ObjectPool.Spawn(weapon.muzzleFlashPrefab, muzzlePos, muzzles[currentIndex].rotation);
            }
            if (weapon.fireClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfxRandomPitch(weapon.fireClip, muzzlePos, weapon.fireVolume);
            }
            if (recoil != null) recoil.Kick(weapon.recoilKick);
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(weapon.cameraShake);
            if (playerAnimation != null) playerAnimation.NotifyFiring();
        }
    }
}
