using System.Collections;
using UnityEngine;

public class NormalSword : WeaponBase
{
    [Header("调试")]
    [SerializeField] private bool showGizmosAlways = true;

    protected override void Attack()
    {
        Vector3 attackDirection = GetAttackDirection();

        if (weaponData.weaponModelPrefab != null)
        {
            StartCoroutine(PerformSlashVisual(attackDirection));
        }

        DetectAndDealDamage(attackDirection);
    }

    private Vector3 GetAttackDirection()
    {
        Vector3 direction = playerTransform.forward;
        if (weaponManager != null && weaponManager.NearestEnemy != null)
        {
            Vector3 dirToEnemy = (weaponManager.NearestEnemy.position - playerTransform.position).normalized;
            direction = new Vector3(dirToEnemy.x, 0, dirToEnemy.z).normalized;
        }

        return direction;
    }

    private void DetectAndDealDamage(Vector3 attackDirection)
    {
        Collider[] hits = Physics.OverlapSphere(playerTransform.position, currentRange, enemyLayerMask);
        if (hits.Length == 0) return;

        foreach (var hit in hits)
        {
            Vector3 dirToEnemy = (hit.transform.position - playerTransform.position).normalized;
            Vector3 flatDir = new Vector3(dirToEnemy.x, 0, dirToEnemy.z).normalized;
            float angle = Vector3.Angle(attackDirection, flatDir);

            if (angle <= weaponData.attackAngle / 2f)
            {
                IDamageReceiver damageReceiver = hit.GetComponent(typeof(IDamageReceiver)) as IDamageReceiver;
                if (damageReceiver == null) continue;

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

    private IEnumerator PerformSlashVisual(Vector3 attackDirection)
    {
        GameObject pivot = new GameObject("SwordPivot");
        pivot.transform.SetParent(playerTransform);
        pivot.transform.localPosition = Vector3.zero;

        GameObject swordInstance = Instantiate(weaponData.weaponModelPrefab, pivot.transform);
        swordInstance.transform.localPosition = weaponData.modelOffset;
        swordInstance.transform.localRotation = Quaternion.Euler(90, 0, 0);

        Quaternion baseRotation = Quaternion.LookRotation(attackDirection);
        float halfAngle = weaponData.attackAngle / 2f;
        Quaternion startArc = Quaternion.Euler(0, -halfAngle, 0);
        Quaternion endArc = Quaternion.Euler(0, halfAngle, 0);

        float timer = 0f;
        while (timer < weaponData.swingDuration)
        {
            if (playerTransform == null)
            {
                Destroy(pivot);
                yield break;
            }

            timer += Time.deltaTime;
            float progress = timer / weaponData.swingDuration;
            Quaternion currentArcRotation = Quaternion.Slerp(startArc, endArc, progress);
            pivot.transform.rotation = baseRotation * currentArcRotation;
            yield return null;
        }

        Destroy(pivot);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmosAlways || !enabled || weaponData == null) return;

        Transform owner = playerTransform != null ? playerTransform : transform.parent;
        if (owner == null) return;

        Vector3 aimDirection = owner.forward;
        if (Application.isPlaying && weaponManager != null && weaponManager.NearestEnemy != null)
        {
            Vector3 dirToEnemy = (weaponManager.NearestEnemy.position - owner.position).normalized;
            aimDirection = new Vector3(dirToEnemy.x, 0, dirToEnemy.z).normalized;
        }

        Gizmos.color = new Color(1, 0.5f, 0, 0.3f);

        Vector3 center = owner.position;
        float range = weaponData.attackRange;
        float halfAngle = weaponData.attackAngle / 2f;
        Quaternion lookRot = Quaternion.LookRotation(aimDirection);
        Vector3 leftDir = lookRot * Quaternion.Euler(0, -halfAngle, 0) * Vector3.forward;
        Vector3 rightDir = lookRot * Quaternion.Euler(0, halfAngle, 0) * Vector3.forward;

        Gizmos.DrawLine(center, center + leftDir * range);
        Gizmos.DrawLine(center, center + rightDir * range);

        int segments = 20;
        Vector3 prevPos = center + leftDir * range;

        Gizmos.color = Color.yellow;
        for (int i = 1; i <= segments; i++)
        {
            float step = (float)i / segments;
            Vector3 currentDir = lookRot * Quaternion.Euler(0, Mathf.Lerp(-halfAngle, halfAngle, step), 0) * Vector3.forward;
            Vector3 nextPos = center + currentDir * range;
            Gizmos.DrawLine(prevPos, nextPos);
            prevPos = nextPos;
        }
    }
}
