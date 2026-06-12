using System.Collections.Generic;
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

        HashSet<GameObject> processedTargets = new HashSet<GameObject>();

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

            GameObject targetObject = hit.attachedRigidbody != null
                ? hit.attachedRigidbody.gameObject
                : hit.gameObject;

            if (!processedTargets.Add(targetObject))
            {
                continue;
            }

            IDamageReceiver damageReceiver = hit.GetComponentInParent(typeof(IDamageReceiver)) as IDamageReceiver;
            if (damageReceiver == null)
            {
                continue;
            }

            IKnockbackable knockbackable = hit.GetComponentInParent(typeof(IKnockbackable)) as IKnockbackable;
            DamageContext context = CreateDamageContext(targetObject);
            DamageResult result = damageReceiver.ReceiveDamage(context);
            if (result.FinalDamage > 0f
                && knockbackable != null
                && (context.KnockbackForce > 0f || context.KnockbackDuration > 0f))
            {
                knockbackable.TakeKnockback(playerTransform.position, context.KnockbackForce, context.KnockbackDuration);
            }
        }
    }
}
