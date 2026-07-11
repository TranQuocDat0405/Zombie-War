using System;
using UnityEngine;
using ZombieWar.Core;
using ZombieWar.Player;

namespace ZombieWar.Weapons
{
    /// <summary>
    /// Auto-fires the equipped gun at the AutoAim target: hitscan rays with
    /// pooled muzzle flash, impact VFX, tracer, recoil and sound. Tracks a
    /// magazine + reserve ammo per weapon with timed reloads.
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private WeaponData[] weapons;
        [SerializeField] private Transform handSocket;
        [SerializeField] private Recoil bodyRecoil;     // soldier mesh root, kicks with the gun
        [SerializeField] private GameObject tracerPrefab;
        [SerializeField] private LayerMask hitMask;      // Zombie + obstacles
        [SerializeField] private float aimHeight = 1.1f; // aim at zombie chest

        private AutoAim autoAim;
        private PlayerAnimation playerAnimation;
        private Recoil recoil;

        private GameObject[] gunInstances;
        private Transform[] muzzles;
        private ParticleSystem[] muzzleFlashes; // persistent, childed to each muzzle
        private int[] ammo;      // rounds left in magazine, per weapon
        private int[] reserve;   // reserve rounds, per weapon
        private int currentIndex;
        private float nextFireTime;
        private float reloadEndTime;
        private bool isReloading;

        public WeaponData CurrentWeapon =>
            weapons != null && weapons.Length > 0 ? weapons[currentIndex] : null;

        public int CurrentAmmo => ammo != null ? ammo[currentIndex] : 0;
        public int CurrentReserve => reserve != null ? reserve[currentIndex] : 0;
        public bool IsReloading => isReloading;
        public float ReloadProgress => !isReloading || CurrentWeapon == null
            ? 1f
            : Mathf.Clamp01(1f - (reloadEndTime - Time.time) / CurrentWeapon.reloadTime);

        public event Action<WeaponData> OnWeaponChanged;
        public event Action OnAmmoChanged;

        private void Awake()
        {
            autoAim = GetComponent<AutoAim>();
            playerAnimation = GetComponent<PlayerAnimation>();
        }

        private void Start()
        {
            BuildGunInstances();
            ammo = new int[weapons.Length];
            reserve = new int[weapons.Length];
            for (int i = 0; i < weapons.Length; i++)
            {
                ammo[i] = weapons[i].magazineSize;
                reserve[i] = weapons[i].reserveAmmo;
            }
            Equip(0);
        }

        private void BuildGunInstances()
        {
            gunInstances = new GameObject[weapons.Length];
            muzzles = new Transform[weapons.Length];
            muzzleFlashes = new ParticleSystem[weapons.Length];

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

                // The muzzle flash lives permanently on the muzzle and is replayed
                // per shot: always in sync with the tracer, never drifts or leaks.
                if (weapons[i].muzzleFlashPrefab != null)
                {
                    var fx = Instantiate(weapons[i].muzzleFlashPrefab, muzzles[i]);
                    fx.transform.localPosition = Vector3.zero;
                    fx.transform.localRotation = Quaternion.identity;
                    var pooled = fx.GetComponent<PooledAutoRelease>();
                    if (pooled != null) Destroy(pooled); // not pooled in this mode
                    foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        var main = ps.main;
                        main.playOnAwake = false;
                        main.loop = false; // one burst per shot — never keeps flashing on its own
                    }
                    muzzleFlashes[i] = fx.GetComponentInChildren<ParticleSystem>(true);
                }

                model.SetActive(false);
            }
        }

        public void NextWeapon()
        {
            if (weapons.Length == 0) return;
            if (isReloading)
            {
                isReloading = false; // switching cancels the reload in progress
                if (playerAnimation != null) playerAnimation.CancelReload();
            }
            Equip((currentIndex + 1) % weapons.Length);
        }

        /// <summary>Ammo pickup: adds the given number of magazines to every weapon's reserve.</summary>
        public void AddAmmoMagazines(int magazines)
        {
            for (int i = 0; i < weapons.Length; i++)
            {
                reserve[i] += weapons[i].magazineSize * magazines;
            }
            OnAmmoChanged?.Invoke();
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
            OnAmmoChanged?.Invoke();

            if (ammo != null && ammo[currentIndex] == 0) TryStartReload();
        }

        private void TryStartReload()
        {
            if (isReloading) return;
            var weapon = CurrentWeapon;
            if (weapon == null || reserve[currentIndex] <= 0) return;
            if (ammo[currentIndex] >= weapon.magazineSize) return;

            isReloading = true;
            reloadEndTime = Time.time + weapon.reloadTime;
            if (playerAnimation != null) playerAnimation.PlayReload(weapon.reloadTime);
        }

        private void FinishReload()
        {
            isReloading = false;
            var weapon = CurrentWeapon;
            int need = weapon.magazineSize - ammo[currentIndex];
            int take = Mathf.Min(need, reserve[currentIndex]);
            ammo[currentIndex] += take;
            reserve[currentIndex] -= take;
            OnAmmoChanged?.Invoke();
        }

        private void Update()
        {
            var weapon = CurrentWeapon;
            if (weapon == null || autoAim == null) return;
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;

            if (isReloading)
            {
                if (Time.time >= reloadEndTime) FinishReload();
                else return;
            }

            var target = autoAim.CurrentTarget;
            if (target == null) return;

            Vector3 aimPoint = target.position + Vector3.up * aimHeight;
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist > weapon.range) return;

            if (Time.time < nextFireTime) return;

            if (ammo[currentIndex] <= 0)
            {
                TryStartReload();
                return;
            }

            nextFireTime = Time.time + 1f / weapon.fireRate;
            Fire(weapon, aimPoint);
        }

        private void Fire(WeaponData weapon, Vector3 aimPoint)
        {
            ammo[currentIndex]--;

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

            var flash = muzzleFlashes[currentIndex];
            if (flash != null)
            {
                flash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                flash.Play(true); // same frame as the tracer/raycast
            }
            if (weapon.fireClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfxRandomPitch(weapon.fireClip, muzzlePos, weapon.fireVolume);
            }
            if (recoil != null) recoil.Kick(weapon.recoilKick);
            if (bodyRecoil != null) bodyRecoil.Kick(weapon.recoilKick * 4f); // whole-body jerk, clearly visible
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(weapon.cameraShake);
            if (playerAnimation != null) playerAnimation.NotifyFiring();

            OnAmmoChanged?.Invoke();
            if (ammo[currentIndex] <= 0) TryStartReload();
        }
    }
}
