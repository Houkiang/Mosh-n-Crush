using UnityEngine;

public abstract class MeleeArcWeaponBase : WeaponBase
{
    protected sealed override void Attack()
    {
        Vector3 attackDirection = GetAttackDirection();
        OnAttackStarted(attackDirection);
        DetectAndDealDamage(attackDirection);
    }

    protected virtual Vector3 GetAttackDirection()
    {
        return CombatTargetingUtility.GetDirection(
            playerTransform,
            weaponManager != null ? weaponManager.NearestEnemy : null,
            playerTransform != null ? playerTransform.forward : Vector3.forward);
    }

    protected virtual void OnAttackStarted(Vector3 attackDirection)
    {
    }

    protected virtual void DetectAndDealDamage(Vector3 attackDirection)
    {
        Collider[] hits = Physics.OverlapSphere(playerTransform.position, currentRange, enemyLayerMask);
        if (hits.Length == 0)
        {
            return;
        }

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (!CombatTargetingUtility.IsTargetWithinArc(
                    playerTransform.position,
                    hit.transform.position,
                    attackDirection,
                    weaponData.attackAngle))
            {
                continue;
            }

            IDamageReceiver damageReceiver = hit.GetComponent(typeof(IDamageReceiver)) as IDamageReceiver;
            if (damageReceiver == null)
            {
                continue;
            }

            IKnockbackable knockbackable = hit.GetComponent(typeof(IKnockbackable)) as IKnockbackable;
            DamageContext context = CreateDamageContext(hit.gameObject);
            DamageResult result = damageReceiver.ReceiveDamage(context);
            if (result.FinalDamage > 0f && knockbackable != null)
            {
                knockbackable.TakeKnockback(playerTransform.position, context.KnockbackForce, context.KnockbackDuration);
            }
        }
    }
}
