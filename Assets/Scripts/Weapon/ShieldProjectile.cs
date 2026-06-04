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

        IDamageReceiver damageReceiver = other.GetComponent(typeof(IDamageReceiver)) as IDamageReceiver;
        if (damageReceiver == null) return;

        IKnockbackable knockbackable = other.GetComponent(typeof(IKnockbackable)) as IKnockbackable;

        if (controller == null)
        {
            Debug.LogError("ShieldProjectile 未正确初始化控制器引用！");
            return;
        }

        DamageContext context = controller.CreateShieldDamageContext(other.gameObject);
        DamageResult result = damageReceiver.ReceiveDamage(context);
        if (result.FinalDamage > 0f && knockbackable != null)
        {
            knockbackable.TakeKnockback(controller.GetPlayerPosition(), context.KnockbackForce, context.KnockbackDuration);
        }
    }
}
