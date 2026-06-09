using UnityEngine;

public class PenetratingBow : ProjectileWeaponBase
{
    [Header("Fan Shot")]
    [SerializeField] private float spreadAngle = 30f;

    protected override bool UsesCountBurst()
    {
        return false;
    }

    protected override int GetProjectileCountPerAttack()
    {
        return Mathf.Max(1, currentWeaponCount);
    }

    protected override Vector3 GetProjectileDirection(Vector3 baseDirection, int projectileIndex, int projectileCount)
    {
        if (projectileCount <= 1)
        {
            return baseDirection;
        }

        float totalSpread = Mathf.Max(0f, spreadAngle);
        float angleStep = projectileCount > 1 ? totalSpread / (projectileCount - 1) : 0f;
        float startAngle = -totalSpread * 0.5f;
        float currentAngle = startAngle + angleStep * projectileIndex;

        Vector3 rotatedDirection = Quaternion.AngleAxis(currentAngle, Vector3.up) * baseDirection;
        return rotatedDirection.normalized;
    }

    protected override void InitializeProjectile(GameObject projectileObject, DamageContext context, Vector3 shootDirection)
    {
        PenetratingProjectile projectile = projectileObject.GetComponent<PenetratingProjectile>();
        if (projectile == null)
        {
            return;
        }

        projectile.Initialize(
            context,
            weaponData.projectileSpeed,
            weaponData.projectileLifeTime);
    }
}
