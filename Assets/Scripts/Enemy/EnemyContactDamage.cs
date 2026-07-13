using UnityEngine;

public class EnemyContactDamage : EnemyAttackBase
{
    [Header("Contact Attack")]
    [SerializeField] private bool useContinuousDamage = true;
    [SerializeField] private float attackDistance = 0.5f;
    [SerializeField] private float disCheckCoolDown = 2.5f;
    [SerializeField] private float disCheckTimer = 0f;
    [SerializeField] private float disCheckFrequencyThreshold = 10f;
    private bool isDistanceLessThanThreshold = false;
    private float sqrDisCheckFrequencyThreshold= 0f;
    private float sqrAttackDistance=0f;

    protected  void Start()
    {

        sqrDisCheckFrequencyThreshold = disCheckFrequencyThreshold * disCheckFrequencyThreshold;
        sqrAttackDistance = attackDistance * attackDistance;
    }
    protected override void Update()
    {
        base.Update();
        if(!IsAttackReady())
        {
            return;
        }
        else
        {
            if(enemyCore.PlayerTransform!=null)
            {   
                if(isDistanceLessThanThreshold)
                {
                    float sqrDistance = (enemyCore.PlayerTransform.position - transform.position).sqrMagnitude;
                    if(sqrDistance <= sqrAttackDistance)
                    {
                        TryDealDamage(enemyCore.PlayerTransform.gameObject);
                    }
                    isDistanceLessThanThreshold=sqrDistance<= sqrDisCheckFrequencyThreshold;

                }
                else
                {
                    if(IsDistanceCheckReady())
                    {
                        float sqrDistance = (enemyCore.PlayerTransform.position - transform.position).sqrMagnitude;
                        if(sqrDistance <= sqrAttackDistance)
                        {
                            TryDealDamage(enemyCore.PlayerTransform.gameObject);
                        }
                        isDistanceLessThanThreshold = sqrDistance <= sqrDisCheckFrequencyThreshold;

                    }
                }
            }
        }

    }
    private bool IsDistanceCheckReady()
    {
        if(disCheckTimer>0f)
        {
            disCheckTimer -= Time.deltaTime;
            return false;
        }
        else
        {
            disCheckTimer = disCheckCoolDown;
            return true;
        }
    }


    // private void OnCollisionStay(Collision collision)
    // {
    //     if (!IsAttackReady() || !collision.gameObject.CompareTag("Player"))
    //     {
    //         return;
    //     }

    //     TryDealDamage(collision.gameObject);
    // }

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
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        DrawHorizontalCircle(transform.position, attackDistance);
        Gizmos.color = Color.yellow;
        DrawHorizontalCircle(transform.position, disCheckFrequencyThreshold);
    }

    private static void DrawHorizontalCircle(Vector3 center, float radius)
    {
        const int segments = 64;
        if (radius <= 0f)
        {
            return;
        }

        Vector3 previousPoint = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }
}
