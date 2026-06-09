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

        Vector3 baseDirection = GetShootDirection();
        Vector3 spawnPosition = GetProjectileSpawnPosition();
        int projectileCount = Mathf.Max(1, GetProjectileCountPerAttack());

        for (int i = 0; i < projectileCount; i++)
        {
            Vector3 shootDirection = GetProjectileDirection(baseDirection, i, projectileCount);
            GameObject projectileObject = PoolManager.Instance.GetObject(
                weaponData.projectilePrefab,
                spawnPosition,
                Quaternion.LookRotation(shootDirection));

            if (projectileObject == null)
            {
                continue;
            }

            InitializeProjectile(projectileObject, CreateDamageContext(), shootDirection);
        }
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

    protected virtual int GetProjectileCountPerAttack()
    {
        return 1;
    }

    protected virtual Vector3 GetProjectileDirection(Vector3 baseDirection, int projectileIndex, int projectileCount)
    {
        return baseDirection;
    }

    protected abstract void InitializeProjectile(GameObject projectileObject, DamageContext context, Vector3 shootDirection);
}
