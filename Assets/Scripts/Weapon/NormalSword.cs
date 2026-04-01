using System.Collections;
using UnityEngine;

public class NormalSword : WeaponBase
{
    [Header("调试")]
    [SerializeField] private bool showGizmosAlways = true;

    protected override void Attack()
    {
        // 1. 获取攻击方向 
        Vector3 attackDirection = GetAttackDirection();

        // 2. 视觉
        if (weaponData.weaponModelPrefab != null)
        {
            StartCoroutine(PerformSlashVisual(attackDirection));
        }

        // 3. 判定
        DetectAndDealDamage(attackDirection);
    }

    private Vector3 GetAttackDirection()
    {
        // 默认朝向玩家前方
        Vector3 direction = playerTransform.forward;
        if (weaponManager != null && weaponManager.NearestEnemy != null)
        {
            // 计算方向向量
            Vector3 dirToEnemy = (weaponManager.NearestEnemy.position - playerTransform.position).normalized;
            
            // 抹平 Y 轴
            direction = new Vector3(dirToEnemy.x, 0, dirToEnemy.z).normalized;
        }

        return direction;
    }

    private void DetectAndDealDamage(Vector3 attackDirection)
    {
        // 检测扇形区域
        Collider[] hits = Physics.OverlapSphere(playerTransform.position, currentRange, enemyLayerMask);
        if (hits.Length == 0) return;
        float finalDamage = GetDamageAfterPlayer();

        foreach (var hit in hits)
        {
            Vector3 dirToEnemy = (hit.transform.position - playerTransform.position).normalized;
            Vector3 flatDir = new Vector3(dirToEnemy.x, 0, dirToEnemy.z).normalized;
            
            float angle = Vector3.Angle(attackDirection, flatDir);

            if (angle <= weaponData.attackAngle / 2f)
            {
                IDamageable target = hit.GetComponent<IDamageable>();
                if (target != null)
                {
                    target.TakeDamage(finalDamage);
                    target.TakeKnockback(playerTransform.position, currentKnockback, weaponData.knockbackDuration);
                }
            }
        }
    }
private IEnumerator PerformSlashVisual(Vector3 attackDirection)
{
    // 1. 创建临时的支点物体
    // 支点位于玩家脚下，负责处理旋转
    GameObject pivot = new GameObject("SwordPivot");
    pivot.transform.SetParent(playerTransform);
    pivot.transform.localPosition = Vector3.zero; // 归零，确保在玩家中心

    // 2. 生成剑的模型
    // 剑作为支点的子物体，负责处理位置偏移 (Offset)
    GameObject swordInstance = Instantiate(weaponData.weaponModelPrefab, pivot.transform);
    swordInstance.transform.localPosition = weaponData.modelOffset;
    

    swordInstance.transform.localRotation = Quaternion.Euler(90, 0, 0); 

    // 3. 准备旋转数据
    Quaternion baseRotation = Quaternion.LookRotation(attackDirection);
    
    // 计算扇形的起止角度 (相对于瞄准方向的左右偏转)
    float halfAngle = weaponData.attackAngle / 2f;
    Quaternion startArc = Quaternion.Euler(0, -halfAngle, 0); // 向左偏
    Quaternion endArc = Quaternion.Euler(0, halfAngle, 0);    // 向右偏

    // 4. 动画循环
    float timer = 0f;
    while (timer < weaponData.swingDuration)
    {
        // 如果玩家中途死亡或被销毁，立即停止协程并清理，防止报错
        if (playerTransform == null) 
        {
            Destroy(pivot);
            yield break;
        }

        timer += Time.deltaTime;
        float progress = timer / weaponData.swingDuration;
        
        // --- 核心旋转逻辑 ---
        // Slerp: 计算当前帧在扇形中的进度 (从左到右)
        Quaternion currentArcRotation = Quaternion.Slerp(startArc, endArc, progress);
        
        // 组合旋转: 基准瞄准方向 * 扇形偏移
        // 直接设置 pivot.rotation (世界旋转)，这样即使 Player 转身，剑也不会跟着歪掉
        pivot.transform.rotation = baseRotation * currentArcRotation;
        
        
        yield return null; 
    }

    // 5. 动画结束，销毁支点
    Destroy(pivot);
}

     private void OnDrawGizmos()
    {
        // 安全检查
        if (!this.enabled || weaponData == null) return;
        
        // 尝试获取持有者 (运行时用 playerTransform，编辑时尝试找父物体)
        Transform owner = playerTransform != null ? playerTransform : transform.parent;
        if (owner == null) return;

        // --- 1. 确定 Gizmos 的朝向 ---
        Vector3 aimDirection = owner.forward; // 默认：正前方

        // 如果在游戏运行中，且 Manager 找到了敌人，则朝向敌人
        if (Application.isPlaying && weaponManager != null && weaponManager.NearestEnemy != null)
        {
            Vector3 dirToEnemy = (weaponManager.NearestEnemy.position - owner.position).normalized;
            // 抹平 Y 轴，保持水平
            aimDirection = new Vector3(dirToEnemy.x, 0, dirToEnemy.z).normalized;
        }

        // --- 2. 绘制扇形 ---
        Gizmos.color = new Color(1, 0.5f, 0, 0.3f); // 橙色半透明

        Vector3 center = owner.position;
        float range = weaponData.attackRange;
        float halfAngle = weaponData.attackAngle / 2f;

        // 计算左右边界的向量
        // 使用 Quaternion.LookRotation 创建一个“看向目标方向”的旋转，然后左右偏转
        Quaternion lookRot = Quaternion.LookRotation(aimDirection);
        Vector3 leftDir = lookRot * Quaternion.Euler(0, -halfAngle, 0) * Vector3.forward;
        Vector3 rightDir = lookRot * Quaternion.Euler(0, halfAngle, 0) * Vector3.forward;

        // 画扇形的两条边
        Gizmos.DrawLine(center, center + leftDir * range);
        Gizmos.DrawLine(center, center + rightDir * range);

        // 画圆弧 (分段绘制)
        int segments = 20;
        Vector3 prevPos = center + leftDir * range;
        
        Gizmos.color = Color.yellow; // 圆弧边缘用黄色，更明显
        for (int i = 1; i <= segments; i++)
        {
            float step = (float)i / segments;
            // 在左右边界之间插值
            Vector3 currentDir = lookRot * Quaternion.Euler(0, Mathf.Lerp(-halfAngle, halfAngle, step), 0) * Vector3.forward;
            Vector3 nextPos = center + currentDir * range;
            
            Gizmos.DrawLine(prevPos, nextPos);
            prevPos = nextPos;
        }
    }
}
