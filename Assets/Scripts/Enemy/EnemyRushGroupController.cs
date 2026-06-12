using System.Collections.Generic;
using UnityEngine;

public class EnemyRushGroupController : MonoBehaviour
{
    private readonly List<EnemyFlybyMovement> members = new List<EnemyFlybyMovement>();
    private float cohesionWeight;
    private float separationWeight;
    private float cohesionRadius;
    private float separationRadius;

    public void Initialize(
        float groupCohesionWeight,
        float groupSeparationWeight,
        float groupCohesionRadius,
        float groupSeparationRadius)
    {
        cohesionWeight = Mathf.Max(0f, groupCohesionWeight);
        separationWeight = Mathf.Max(0f, groupSeparationWeight);
        cohesionRadius = Mathf.Max(0.1f, groupCohesionRadius);
        separationRadius = Mathf.Max(0.1f, groupSeparationRadius);
    }

    public void RegisterMember(EnemyFlybyMovement member)
    {
        if (member == null || members.Contains(member))
        {
            return;
        }

        members.Add(member);
    }

    public void UnregisterMember(EnemyFlybyMovement member)
    {
        if (member == null)
        {
            return;
        }

        members.Remove(member);
    }

    public Vector3 GetSteeringFor(EnemyFlybyMovement member)
    {
        if (member == null)
        {
            return Vector3.zero;
        }

        CleanupMembers();
        if (members.Count <= 1)
        {
            return Vector3.zero;
        }

        Vector3 selfPosition = member.transform.position;
        selfPosition.y = 0f;

        Vector3 groupCenter = Vector3.zero;
        int neighborCount = 0;
        Vector3 separation = Vector3.zero;
        float separationRadiusSqr = separationRadius * separationRadius;

        for (int i = 0; i < members.Count; i++)
        {
            EnemyFlybyMovement other = members[i];
            if (other == null || other == member || !other.isActiveAndEnabled)
            {
                continue;
            }

            Vector3 otherPosition = other.transform.position;
            otherPosition.y = 0f;

            groupCenter += otherPosition;
            neighborCount++;

            Vector3 offset = selfPosition - otherPosition;
            float sqrDistance = offset.sqrMagnitude;
            if (sqrDistance <= 0.0001f || sqrDistance > separationRadiusSqr)
            {
                continue;
            }

            float distance = Mathf.Sqrt(sqrDistance);
            float strength = 1f - distance / separationRadius;
            separation += offset / distance * strength;
        }

        if (neighborCount <= 0)
        {
            return Vector3.zero;
        }

        groupCenter /= neighborCount;

        Vector3 cohesion = groupCenter - selfPosition;
        float cohesionDistance = cohesion.magnitude;
        if (cohesionDistance > 0.001f)
        {
            float strength = Mathf.Clamp01(cohesionDistance / cohesionRadius);
            cohesion = cohesion / cohesionDistance * strength;
        }
        else
        {
            cohesion = Vector3.zero;
        }

        return cohesion * cohesionWeight + separation * separationWeight;
    }

    private void LateUpdate()
    {
        CleanupMembers();
        if (members.Count == 0)
        {
            Destroy(gameObject);
        }
    }

    private void CleanupMembers()
    {
        for (int i = members.Count - 1; i >= 0; i--)
        {
            if (members[i] == null || !members[i].gameObject.activeInHierarchy)
            {
                members.RemoveAt(i);
            }
        }
    }
}
