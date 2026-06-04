using UnityEngine;

public class EnemyContactDamage : EnemyAttackBase
{
    [Header("Contact Attack")]
    [SerializeField] private bool useContinuousDamage = true;

    protected override void Update()
    {
        base.Update();
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!IsAttackReady() || !collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        TryDealDamage(collision.gameObject);
    }

    private void TryDealDamage(GameObject target)
    {
        IDamageReceiver damageReceiver = target.GetComponentInParent(typeof(IDamageReceiver)) as IDamageReceiver;
        if (damageReceiver == null)
        {
            return;
        }

        DamageContext context = enemyCore.CreateDamageContext(target);
        damageReceiver.ReceiveDamage(context);
        RaiseAttack();
        attackTimer = useContinuousDamage ? attackCooldown : float.MaxValue;
    }
}
