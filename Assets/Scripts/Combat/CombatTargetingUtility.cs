using UnityEngine;

public static class CombatTargetingUtility
{
    public static Vector3 GetDirection(
        Transform origin,
        Transform target,
        Vector3 fallback,
        bool flattenY = true)
    {
        if (origin == null)
        {
            return fallback.sqrMagnitude > 0f ? fallback.normalized : Vector3.forward;
        }

        Vector3 direction = fallback.sqrMagnitude > 0f ? fallback.normalized : origin.forward;
        if (target != null)
        {
            direction = target.position - origin.position;
        }

        if (flattenY)
        {
            direction.y = 0f;
        }

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = flattenY
                ? new Vector3(origin.forward.x, 0f, origin.forward.z)
                : origin.forward;
        }

        return direction.normalized;
    }

    public static bool IsTargetWithinArc(
        Vector3 origin,
        Vector3 targetPosition,
        Vector3 attackDirection,
        float attackAngle)
    {
        Vector3 toTarget = targetPosition - origin;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            return true;
        }

        float angle = Vector3.Angle(attackDirection.normalized, toTarget.normalized);
        return angle <= attackAngle * 0.5f;
    }
}
