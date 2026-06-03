using UnityEngine;

public class PenetratingBow : WeaponBase
{
    protected override void Attack()
    {
        if (weaponData.projectilePrefab == null)
        {
            Debug.LogError("WeaponDataSO 缺少 Projectile Prefab");
            return;
        }

        Vector3 shootDirection = GetShootDirection();
        DamageContext damageContext = CreateDamageContext();

        GameObject projectileObj = PoolManager.Instance.GetObject(
            weaponData.projectilePrefab,
            playerTransform.position + Vector3.up * 1.5f,
            Quaternion.LookRotation(shootDirection)
        );

        PenetratingProjectile projectileScript = projectileObj.GetComponent<PenetratingProjectile>();
        if (projectileScript != null)
        {
            projectileScript.Initialize(
                damageContext,
                weaponData.projectileSpeed,
                weaponData.projectileLifeTime
            );
        }
    }

    private Vector3 GetShootDirection()
    {
        Vector3 direction = playerTransform.forward;

        if (weaponManager != null && weaponManager.NearestEnemy != null)
        {
            Vector3 dirToEnemy = (weaponManager.NearestEnemy.position - playerTransform.position).normalized;
            direction = new Vector3(dirToEnemy.x, dirToEnemy.y, dirToEnemy.z).normalized;
        }

        return direction;
    }
}
