using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyContactDamage : MonoBehaviour
{
    [Header("碰撞伤害设置")]
    [SerializeField] private float damageCooldown = 0.35f;
    [SerializeField] private bool useContinuousDamage = true;

    private Enemy enemyCore;
    private float damageTimer = 0f;

    void Awake()
    {
        enemyCore = GetComponent<Enemy>();
    }

    void OnEnable()
    {
        damageTimer = 0f;
    }

    void Update()
    {
        if (damageTimer > 0)
        {
            damageTimer -= Time.deltaTime;
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (enemyCore.IsDead) return;

        if (damageTimer <= 0f && collision.gameObject.CompareTag("Player"))
        {
            TryDealDamage(collision.gameObject);
        }
    }

    private void TryDealDamage(GameObject target)
    {
        var player = target.GetComponent<Player>();
        if (player != null)
        {
            DamageContext context = enemyCore.CreateDamageContext(player.gameObject);
            player.ReceiveDamage(context);
            damageTimer = useContinuousDamage ? damageCooldown : float.MaxValue;
        }
    }
}
