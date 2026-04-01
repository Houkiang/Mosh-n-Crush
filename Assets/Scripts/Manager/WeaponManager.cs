
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("初始武器")]
    [SerializeField] private List<WeaponDataSO> startingWeapons;

    [Header("武器挂载点")]
    [Tooltip("武器逻辑物体将作为此物体的子物体生成")]
    [SerializeField] private Transform weaponHolder; 

    [Header("全局索敌设置")]
    [SerializeField] private float scanRadius = 10f; // 索敌半径
    [SerializeField] private LayerMask enemyLayer;   // 敌人层级
    
    public Player player; // 玩家引用，供武器使用

    // 公开属性，供所有武器访问
    public Transform NearestEnemy { get; private set; }
    // 每0.1秒检测一次
    private float scanTimer;
    private float scanInterval = 0.1f;

    // 存储当前所有活跃的武器实例
    private List<WeaponBase> activeWeapons = new List<WeaponBase>();

    void Start()
    {
        // 如果没指定挂载点，就挂在自己下面
        if (weaponHolder == null) weaponHolder = transform;
        if (enemyLayer == 0) enemyLayer = LayerMask.GetMask("Enemy");

        if(player == null)
        {
            Debug.LogError("WeaponManager 未找到 Player 组件！");
        }
        // 初始化初始武器
        foreach (var data in startingWeapons)
        {
            AddWeapon(data);
        }
    }

        void Update()
    {
        // --- 核心：集中索敌逻辑 ---
        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0)
        {
            FindNearestEnemy();
            scanTimer = scanInterval;
        }
    }

    private void FindNearestEnemy()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, scanRadius, enemyLayer);
        
        Transform bestTarget = null;
        float minDistanceSqr = Mathf.Infinity;
        Vector3 currentPos = transform.position;

        foreach (var enemy in enemies)
        {
            // 简单的距离判断
            float dSqr = (enemy.transform.position - currentPos).sqrMagnitude;
            if (dSqr < minDistanceSqr)
            {
                minDistanceSqr = dSqr;
                bestTarget = enemy.transform;
            }
        }
        
        NearestEnemy = bestTarget;
    }

    public void AddWeapon(WeaponDataSO data)
    {
        //Debug.Log("添加武器: " + data.weaponName);
        GameObject weaponObj = Instantiate(data.weaponPrefab, weaponHolder);
        weaponObj.name = data.weaponName;

        WeaponBase weaponScript = weaponObj.GetComponent<WeaponBase>();
        if (weaponScript != null)
        {
            //Debug.Log("初始化武器: " + data.weaponName);
            weaponScript.Initialize(data, transform, this); 
            activeWeapons.Add(weaponScript);
        }
    }
        // 检查玩家是否已经拥有该武器 (通过 WeaponDataSO 判断)
    public bool HasWeapon(WeaponDataSO data)
    {
        foreach (var weapon in activeWeapons)
        {

            if (weapon.gameObject.name == data.weaponName) 
                return true;
        }
        return false;
    }
    public void UpgradeWeaponDamage(WeaponDataSO data,float additionalDamage)
    {
        foreach (var weapon in activeWeapons)
        {
            if (weapon.gameObject.name == data.weaponName)
            {
                weapon.IncreaseDamage(additionalDamage);
                break;
            }
        }
    }
    public void UpgradeWeaponFireRate(WeaponDataSO data,float reductionAmount)
    {
        foreach (var weapon in activeWeapons)
        {
            if (weapon.gameObject.name == data.weaponName)
            {
                weapon.ReduceCooldown(reductionAmount);
                break;
            }
        }
    }
    public void UpgradeWeaponCount(WeaponDataSO data,float additionalCount)
    {
        foreach (var weapon in activeWeapons)
        {
            if (weapon.gameObject.name == data.weaponName)
            {
                weapon.IncreaseWeaponCount((int)additionalCount);
                break;
            }
        }
    }
    public void UpgradeWeaponKnockback(WeaponDataSO data,float increasePercent)
    {
        foreach (var weapon in activeWeapons)
        {
            if (weapon.gameObject.name == data.weaponName)
            {
                weapon.IncreaseKnockback(increasePercent);
                break;
            }
        }
    }
    private void OnDrawGizmos()
    {
        // 1. 画出索敌范围 (青色线框)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, scanRadius);

        // 2. 画出锁定连线 (如果当前有锁定的敌人的话)
        if (Application.isPlaying && NearestEnemy != null)
        {
            Gizmos.color = Color.red;
            
            // 画一条线连接玩家和敌人
            Gizmos.DrawLine(transform.position, NearestEnemy.position);
            
            // 在敌人身上画个小球标记
            Gizmos.DrawWireSphere(NearestEnemy.position, 3f);
        }
    }
    

}
