using UnityEngine;

public class PenetratingBow : ProjectileWeaponBase
{
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
