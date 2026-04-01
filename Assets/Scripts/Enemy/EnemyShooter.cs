using UnityEngine;
using System;
using Random=UnityEngine.Random;
[RequireComponent(typeof(Enemy))]
public class EnemyShooter : MonoBehaviour
{
    [Header("远程攻击设置")]
    [SerializeField] private GameObject projectilePrefab; 
    [SerializeField] private Transform firePoint;         
    [SerializeField] private float attackRange = 6f;      
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float yOffset = 1.5f;
    
    private Enemy enemyCore;
    private float attackTimer = 0f;
    public event Action OnAttack;

    void Awake()
    {
        enemyCore = GetComponent<Enemy>();
    }

    void OnEnable()
    {
        // 随机初始延迟，错开怪物的攻击节奏
        attackTimer = Random.Range(0f, 1f);
    }

    void Update()
    {
        if (enemyCore.IsDead || enemyCore.PlayerTransform == null) return;

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            float distToPlayer = Vector3.Distance(transform.position, enemyCore.PlayerTransform.position);
            
            if (distToPlayer <= attackRange)
            {
                Shoot();
                attackTimer = attackCooldown;
            }
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        
        // 使用对象池获取子弹 
        GameObject projObj = PoolManager.Instance.GetObject(projectilePrefab, spawnPos, transform.rotation);
        
        // 获取组件并初始化
        EnemyProjectile projectile = projObj.GetComponent<EnemyProjectile>();
        
        OnAttack?.Invoke();
        if (projectile != null)
        {

            Vector3 shootDir = (enemyCore.PlayerTransform.position - spawnPos);
            shootDir.y += yOffset; // 考虑高度偏移
            shootDir.Normalize();
            projectile.Initialize(enemyCore.CurrentDamage, shootDir);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
