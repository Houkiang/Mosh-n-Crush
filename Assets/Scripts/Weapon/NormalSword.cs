using System.Collections;
using UnityEngine;

public class NormalSword : MeleeArcWeaponBase
{
    [Header("Debug")]
    [SerializeField] private bool showGizmosAlways = true;

    protected override void OnAttackStarted(Vector3 attackDirection)
    {
        if (weaponData.weaponModelPrefab != null)
        {
            StartCoroutine(PerformSlashVisual(attackDirection));
        }
    }

    private IEnumerator PerformSlashVisual(Vector3 attackDirection)
    {
        GameObject pivot = new GameObject("SwordPivot");
        pivot.transform.SetParent(playerTransform);
        pivot.transform.localPosition = Vector3.zero;

        GameObject swordInstance = Instantiate(weaponData.weaponModelPrefab, pivot.transform);
        swordInstance.transform.localPosition = weaponData.modelOffset;
        swordInstance.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        Quaternion baseRotation = Quaternion.LookRotation(attackDirection);
        float halfAngle = weaponData.attackAngle * 0.5f;
        Quaternion startArc = Quaternion.Euler(0f, -halfAngle, 0f);
        Quaternion endArc = Quaternion.Euler(0f, halfAngle, 0f);

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
        if (!showGizmosAlways || !enabled || weaponData == null)
        {
            return;
        }

        Transform owner = playerTransform != null ? playerTransform : transform.parent;
        if (owner == null)
        {
            return;
        }

        Vector3 aimDirection = CombatTargetingUtility.GetDirection(
            owner,
            Application.isPlaying && weaponManager != null ? weaponManager.NearestEnemy : null,
            owner.forward);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);

        Vector3 center = owner.position;
        float range = weaponData.attackRange;
        float halfAngle = weaponData.attackAngle * 0.5f;
        Quaternion lookRot = Quaternion.LookRotation(aimDirection);
        Vector3 leftDir = lookRot * Quaternion.Euler(0f, -halfAngle, 0f) * Vector3.forward;
        Vector3 rightDir = lookRot * Quaternion.Euler(0f, halfAngle, 0f) * Vector3.forward;

        Gizmos.DrawLine(center, center + leftDir * range);
        Gizmos.DrawLine(center, center + rightDir * range);

        int segments = 20;
        Vector3 prevPos = center + leftDir * range;

        Gizmos.color = Color.yellow;
        for (int i = 1; i <= segments; i++)
        {
            float step = (float)i / segments;
            Vector3 currentDir = lookRot * Quaternion.Euler(0f, Mathf.Lerp(-halfAngle, halfAngle, step), 0f) * Vector3.forward;
            Vector3 nextPos = center + currentDir * range;
            Gizmos.DrawLine(prevPos, nextPos);
            prevPos = nextPos;
        }
    }
}
