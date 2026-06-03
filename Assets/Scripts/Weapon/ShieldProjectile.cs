using UnityEngine;

public class ShieldProjectile : MonoBehaviour
{
    private ShieldWeapon controller;

    public void Initialize(ShieldWeapon weaponController)
    {
        controller = weaponController;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;

        ICombatant target = other.GetComponent(typeof(ICombatant)) as ICombatant;
        if (target == null) return;

        if (controller == null)
        {
            Debug.LogError("ShieldProjectile 未正确初始化控制器引用！");
            return;
        }

        DamageContext context = controller.CreateShieldDamageContext(other.gameObject);
        DamageResult result = target.ReceiveDamage(context);
        if (result.FinalDamage > 0f)
        {
            target.TakeKnockback(controller.GetPlayerPosition(), context.KnockbackForce, context.KnockbackDuration);
        }
    }
}
