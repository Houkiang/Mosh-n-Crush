using UnityEngine;

public interface IDamageable
{
    // 受到伤害
    void TakeDamage(float amount);
    
    // 受到击退 (来源位置，击退力度，眩晕时间)
    void TakeKnockback(Vector3 sourcePosition, float force, float stunDuration);
}
