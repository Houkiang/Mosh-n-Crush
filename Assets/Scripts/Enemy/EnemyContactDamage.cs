
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyContactDamage : MonoBehaviour
{
    [Header("碰撞伤害设置")]
    [SerializeField] private float damageCooldown = 0.35f; // 稍微增加一点冷却，避免判定过快
    [SerializeField] private bool useContinuousDamage = true;
    
    private Enemy enemyCore;
    private float damageTimer = 0f;

    void Awake()
    {
        enemyCore = GetComponent<Enemy>();
    }

    void OnEnable()
    {
        damageTimer = 0f;
    }

    void Update()
    {
        if (damageTimer > 0)
        {
            damageTimer -= Time.deltaTime;
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (enemyCore.IsDead) return;

        // 只有计时器归零才检测
        if (damageTimer <= 0f)
        {
            // 检查是否是玩家
            if (collision.gameObject.CompareTag("Player"))
            {
                TryDealDamage(collision.gameObject);
            }
        }
    }

    private void TryDealDamage(GameObject target)
    {
        var player = target.GetComponent<Player>(); 
        if (player != null)
        {
            // 从 Core 获取经过成长计算后的伤害
            player.TakeDamage(enemyCore.CurrentDamage);
            
            // 重置计时器
            damageTimer = useContinuousDamage ? damageCooldown : float.MaxValue;
        }
    }
}
