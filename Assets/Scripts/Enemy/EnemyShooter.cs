using UnityEngine;

public class EnemyShooter : EnemyAttackBase
{
    [Header("Ranged Attack")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float attackRange = 6f;
    [SerializeField] private float yOffset = 1.5f;

    protected override void Update()
    {
        base.Update();

        if (!IsAttackReady())
        {
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, enemyCore.PlayerTransform.position);
        if (distanceToPlayer > attackRange)
        {
            return;
        }

        Shoot();
        ConsumeAttackCooldown();
    }

    private void Shoot()
    {
        if (projectilePrefab == null)
        {
            return;
        }

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        GameObject projectileObject = PoolManager.Instance.GetObject(projectilePrefab, spawnPos, transform.rotation);
        EnemyProjectile projectile = projectileObject.GetComponent<EnemyProjectile>();

        RaiseAttack();
        if (projectile == null)
        {
            return;
        }

        Vector3 shootDirection = enemyCore.PlayerTransform.position - spawnPos;
        shootDirection.y += yOffset;
        shootDirection.Normalize();
        projectile.Initialize(enemyCore.CreateDamageContext(), shootDirection);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
