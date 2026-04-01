using System.Collections.Generic;
using UnityEngine;

public class ShieldWeapon : WeaponBase
{
    [Header("盾牌特有属性")]

    [SerializeField] private float rotationSpeed = 90f; // 旋转速度 (度/秒)
    
    // 运行时特有属性
    private int currentShieldCount;
    private float currentRotationAngle = 0f;
    private List<GameObject> spawnedShields = new List<GameObject>();

    // 重写初始化
    public override void Initialize(WeaponDataSO data, Transform owner, WeaponManager manager)
    {
        base.Initialize(data, owner, manager);
        
        currentShieldCount = weaponCount;
        
        // 生成盾牌
        SpawnShields();
    }

    // 不需要 WeaponBase 的 CooldownTimer 逻辑
    protected override void Update()
    {

        if (playerTransform == null) return;

        // 1. 处理旋转角度增加
        currentRotationAngle += rotationSpeed * Time.deltaTime;
        if (currentRotationAngle >= 360f) currentRotationAngle -= 360f;

        // 2. 更新所有盾牌的位置
        UpdateShieldPositions();
    }


    protected override void Attack()
    {
        // 不需要定时触发 Attack
    }

    // --- 核心逻辑 ---

    private void SpawnShields()
    {
        // 清理旧的盾牌
        foreach (var shield in spawnedShields)
        {
            if (shield != null) Destroy(shield);
        }
        spawnedShields.Clear();

        if (weaponData.weaponModelPrefab == null)
        {
            Debug.LogError("ShieldWeapon: 缺少 WeaponModelPrefab!");
            return;
        }

        // 生成新的盾牌
        for (int i = 0; i < currentShieldCount; i++)
        {
            GameObject obj = Instantiate(weaponData.weaponModelPrefab);

            obj.transform.SetParent(playerTransform); 
            
            // 初始化碰撞脚本
            ShieldProjectile projectile = obj.GetComponent<ShieldProjectile>();
            if (projectile == null) projectile = obj.AddComponent<ShieldProjectile>();
            projectile.Initialize(this);

            spawnedShields.Add(obj);
        }

        UpdateShieldPositions();
    }

    private void UpdateShieldPositions()
    {
        if (spawnedShields.Count == 0) return;

        // 计算每个盾牌的间隔角度
        float angleStep = 360f / spawnedShields.Count;

        for (int i = 0; i < spawnedShields.Count; i++)
        {
            if (spawnedShields[i] == null) continue;

            // 当前盾牌的角度 = 总旋转角 + 偏移角
            float angle = currentRotationAngle + (angleStep * i);

            // 极坐标转笛卡尔坐标
            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * currentRange;

            // 设置位置：玩家位置 + 偏移
            Vector3 targetPos = playerTransform.position + offset + Vector3.up * 1.0f; 

            spawnedShields[i].transform.position = targetPos;
            
            spawnedShields[i].transform.rotation = Quaternion.LookRotation(offset);
        }
    }

    // --- 升级接口 ---

    public override void IncreaseWeaponCount(int amount)
    {
        currentShieldCount += amount;
        SpawnShields(); // 数量改变需要重新生成
    }

    // 提供给 Projectile 获取数据的公共方法
    public float GetActualDamage() => GetDamageAfterPlayer();
    public float GetKnockbackForce() => currentKnockback;
    public float GetKnockbackDuration() => currentKnockbackDuration;
    public Vector3 GetPlayerPosition() => playerTransform.position;
    public int GetEnemyLayerMask() => enemyLayerMask;

    // --- 调试 ---
    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerTransform.position, currentRange);
        }
    }
}
