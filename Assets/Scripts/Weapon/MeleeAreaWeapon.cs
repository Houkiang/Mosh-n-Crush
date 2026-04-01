using UnityEngine;

public class MeleeAreaWeapon 
{
    // [Header("近战设置")]
    // [SerializeField] private LayerMask enemyLayer;
    // [SerializeField] private GameObject visualEffectPrefab; // 比如一个圆形的剑气特效

    // protected override void Attack()
    // {
    //     // 1. 播放特效
    //     if (visualEffectPrefab != null)
    //     {
    //         var vfx = Instantiate(visualEffectPrefab, playerTransform.position, Quaternion.identity);
    //         Destroy(vfx, 0.5f); // 0.5秒后销毁特效
    //     }

    //     // 2. 检测周围敌人
    //     // 使用 OverlapSphere 获取范围内所有碰撞体
    //     Collider[] hits = Physics.OverlapSphere(playerTransform.position, range, enemyLayer);

    //     if (hits.Length > 0)
    //     {
    //         foreach (var hit in hits)
    //         {
    //             // 尝试获取接口
    //             IDamageable target = hit.GetComponent<IDamageable>();
    //             if (target != null)
    //             {
    //                 // 造成伤害
    //                 target.TakeDamage(damage);
    //                 // 造成击退 (击退源是玩家位置，眩晕0.2秒)
    //                 target.TakeKnockback(playerTransform.position, knockbackForce, 0.2f);
    //             }
    //         }
    //         Debug.Log($"近战攻击！击中了 {hits.Length} 个敌人");
    //     }
    // }

    // // 在编辑器里画出攻击范围，方便调试
    // private void OnDrawGizmosSelected()
    // {
    //     Gizmos.color = Color.yellow;
    //     Gizmos.DrawWireSphere(transform.position, range);
    // }
}
