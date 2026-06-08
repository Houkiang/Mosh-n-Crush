using UnityEngine;

public class FireballWand : ProjectileWeaponBase
{
    protected override void InitializeProjectile(GameObject projectileObject, DamageContext context, Vector3 shootDirection)
    {
        FireballProjectile projectile = projectileObject.GetComponent<FireballProjectile>();
        if (projectile == null)
        {
            return;
        }

        projectile.Initialize(
            context,
            weaponData.projectileSpeed,
            weaponData.projectileLifeTime,
            shootDirection);
    }
}
