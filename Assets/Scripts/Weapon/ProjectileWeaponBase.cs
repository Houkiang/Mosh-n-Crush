using UnityEngine;

public abstract class ProjectileWeaponBase : WeaponBase
{
    [Header("Projectile Launch")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private bool aimWithVerticalOffset = true;

    protected sealed override void Attack()
    {
        if (weaponData.projectilePrefab == null)
        {
            Debug.LogError($"{nameof(WeaponDataSO)} is missing a projectile prefab for {name}");
            return;
        }

        Vector3 shootDirection = GetShootDirection();
        GameObject projectileObject = PoolManager.Instance.GetObject(
            weaponData.projectilePrefab,
            GetProjectileSpawnPosition(),
            Quaternion.LookRotation(shootDirection));

        if (projectileObject == null)
        {
            return;
        }

        InitializeProjectile(projectileObject, CreateDamageContext(), shootDirection);
    }

    protected virtual Vector3 GetShootDirection()
    {
        return CombatTargetingUtility.GetDirection(
            playerTransform,
            weaponManager != null ? weaponManager.NearestEnemy : null,
            playerTransform != null ? playerTransform.forward : Vector3.forward,
            !aimWithVerticalOffset);
    }

    protected virtual Vector3 GetProjectileSpawnPosition()
    {
        return playerTransform != null
            ? playerTransform.position + spawnOffset
            : transform.position;
    }

    protected abstract void InitializeProjectile(GameObject projectileObject, DamageContext context, Vector3 shootDirection);
}
