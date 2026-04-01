
using UnityEngine;

public class PenetratingBow : WeaponBase
{
    protected override void Attack()
    {
        // 1. 检查是否有配置箭矢预制体
        if (weaponData.projectilePrefab == null)
        {
            Debug.LogError("WeaponDataSO 中缺少 Projectile Prefab！");
            return;
        }

        // 2. 确定攻击方向
        Vector3 shootDirection = GetShootDirection();

        // 3. 计算最终伤害 (调用父类方法)
        float finalDamage = GetDamageAfterPlayer();

        // 4. 从对象池生成箭矢
        // 注意：生成位置稍微抬高一点 (Vector3.up)，防止生成在地板里
        GameObject projectileObj = PoolManager.Instance.GetObject(
            weaponData.projectilePrefab, 
            playerTransform.position + Vector3.up * 1.5f, 
            Quaternion.LookRotation(shootDirection)
        );

        // 5. 初始化箭矢参数
        PenetratingProjectile projectileScript = projectileObj.GetComponent<PenetratingProjectile>();
        if (projectileScript != null)
        {
            projectileScript.Initialize(
                finalDamage,
                weaponData.projectileSpeed,
                currentKnockback,
                currentKnockbackDuration,
                weaponData.projectileLifeTime
            );
        }
    }

    private Vector3 GetShootDirection()
    {
        // 默认朝向玩家前方
        Vector3 direction = playerTransform.forward;

        // 如果 WeaponManager 找到了最近的敌人，就朝向敌人
        if (weaponManager != null && weaponManager.NearestEnemy != null)
        {
            Vector3 dirToEnemy = (weaponManager.NearestEnemy.position - playerTransform.position).normalized;
            
            direction = new Vector3(dirToEnemy.x, dirToEnemy.y, dirToEnemy.z).normalized;
        }

        return direction;
    }
}
