using UnityEngine;
using System.Collections.Generic;

public class ShieldProjectile : MonoBehaviour
{
    private ShieldWeapon controller; // 引用控制器以获取最新数值


    // 初始化方法
    public void Initialize(ShieldWeapon weaponController)
    {
        controller = weaponController;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 检查是否是敌人层级
        if (other.CompareTag("Enemy"))
        {
            IDamageable target = other.GetComponent<IDamageable>();
            if (target != null)
            {
                if(controller == null) Debug.LogError("ShieldProjectile 未正确初始化控制器引用！");
                // 1. 获取伤害 (通过控制器计算，包含玩家属性加成)
                float damage = controller.GetActualDamage();
                
                // 2. 造成伤害
                target.TakeDamage(damage);

                // 3. 造成击退 (方向：从玩家中心 -> 敌人)
                Vector3 knockbackDir = (other.transform.position - controller.GetPlayerPosition()).normalized;
                target.TakeKnockback(controller.GetPlayerPosition(), controller.GetKnockbackForce(), controller.GetKnockbackDuration());
                

            }
        }
    }
}
