using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("Starting Weapons")]
    [SerializeField] private List<WeaponDataSO> startingWeapons;

    [Header("Weapon Holder")]
    [Tooltip("Weapon runtime objects will be spawned as children of this transform.")]
    [SerializeField] private Transform weaponHolder;

    [Header("Global Targeting")]
    [SerializeField] private float scanRadius = 10f;
    [SerializeField] private LayerMask enemyLayer;

    public Player player;
    public Transform NearestEnemy { get; private set; }

    private float scanTimer;
    private const float ScanInterval = 0.1f;
    private readonly List<WeaponBase> activeWeapons = new List<WeaponBase>();

    private void Start()
    {
        if (weaponHolder == null)
        {
            weaponHolder = transform;
        }

        if (enemyLayer == 0)
        {
            enemyLayer = LayerMask.GetMask("Enemy");
        }

        if (player == null)
        {
            Debug.LogError("WeaponManager could not find Player.");
        }

        for (int i = 0; i < startingWeapons.Count; i++)
        {
            AddWeapon(startingWeapons[i]);
        }
    }

    private void Update()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            FindNearestEnemy();
            scanTimer = ScanInterval;
        }
    }

    public void AddWeapon(WeaponDataSO data)
    {
        GameObject weaponObject = Instantiate(data.weaponPrefab, weaponHolder);
        weaponObject.name = data.weaponName;

        WeaponBase weapon = weaponObject.GetComponent<WeaponBase>();
        if (weapon == null)
        {
            return;
        }

        weapon.Initialize(data, transform, this);
        activeWeapons.Add(weapon);
    }

    public bool HasWeapon(WeaponDataSO data)
    {
        return TryGetWeapon(data, out _);
    }

    public void UpgradeWeaponDamage(WeaponDataSO data, float additionalDamage)
    {
        if (TryGetWeapon(data, out WeaponBase weapon))
        {
            weapon.IncreaseDamage(additionalDamage);
        }
    }

    public void UpgradeWeaponFireRate(WeaponDataSO data, float reductionAmount)
    {
        if (TryGetWeapon(data, out WeaponBase weapon))
        {
            weapon.ReduceCooldown(reductionAmount);
        }
    }

    public void UpgradeWeaponCount(WeaponDataSO data, float additionalCount)
    {
        if (TryGetWeapon(data, out WeaponBase weapon))
        {
            weapon.IncreaseWeaponCount((int)additionalCount);
        }
    }

    public void UpgradeWeaponKnockback(WeaponDataSO data, float increasePercent)
    {
        if (TryGetWeapon(data, out WeaponBase weapon))
        {
            weapon.IncreaseKnockback(increasePercent);
        }
    }

    public void AddWeaponStatusEffect(WeaponDataSO data, StatusEffectDataSO statusEffect, int stackCount = 1)
    {
        if (TryGetWeapon(data, out WeaponBase weapon))
        {
            weapon.AddStatusEffect(statusEffect, stackCount);
        }
    }

    private bool TryGetWeapon(WeaponDataSO data, out WeaponBase weapon)
    {
        weapon = null;
        if (data == null)
        {
            return false;
        }

        for (int i = 0; i < activeWeapons.Count; i++)
        {
            WeaponBase candidate = activeWeapons[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.weaponData == data || candidate.gameObject.name == data.weaponName)
            {
                weapon = candidate;
                return true;
            }
        }

        return false;
    }

    private void FindNearestEnemy()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, scanRadius, enemyLayer);
        Transform bestTarget = null;
        float minDistanceSqr = Mathf.Infinity;
        Vector3 currentPos = transform.position;

        for (int i = 0; i < enemies.Length; i++)
        {
            float distanceSqr = (enemies[i].transform.position - currentPos).sqrMagnitude;
            if (distanceSqr < minDistanceSqr)
            {
                minDistanceSqr = distanceSqr;
                bestTarget = enemies[i].transform;
            }
        }

        NearestEnemy = bestTarget;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, scanRadius);

        if (Application.isPlaying && NearestEnemy != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, NearestEnemy.position);
            Gizmos.DrawWireSphere(NearestEnemy.position, 3f);
        }
    }
}
