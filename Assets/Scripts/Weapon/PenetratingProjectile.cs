using System.Collections.Generic;
using UnityEngine;

public class PenetratingProjectile : MonoBehaviour
{
    private DamageContext damageContext;
    private float speed;
    private float lifeTimer;

    [SerializeField] private LayerMask whatIsGround;

    private readonly List<GameObject> hitHistory = new List<GameObject>();

    public void Initialize(DamageContext context, float spd, float lifeTime)
    {
        damageContext = context;
        speed = spd;
        lifeTimer = lifeTime;
        hitHistory.Clear();
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0)
        {
            PoolManager.Instance.ReturnObject(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (hitHistory.Contains(other.gameObject)) return;
            hitHistory.Add(other.gameObject);

            ICombatant target = other.GetComponent(typeof(ICombatant)) as ICombatant;
            if (target != null)
            {
                DamageContext context = damageContext;
                context.Target = other.gameObject;
                DamageResult result = target.ReceiveDamage(context);
                if (result.FinalDamage > 0f)
                {
                    target.TakeKnockback(transform.position, context.KnockbackForce, context.KnockbackDuration);
                }
            }
        }
        else if ((whatIsGround.value & (1 << other.gameObject.layer)) > 0)
        {
            PoolManager.Instance.ReturnObject(gameObject);
        }
    }
}
