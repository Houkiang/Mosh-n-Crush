using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("子弹属性")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float lifeTime = 5f;

    private DamageContext damageContext;
    private Vector3 direction;
    private float currentLifeTimer;
    private bool isRunning = false;

    public LayerMask whatIsGround;
    public LayerMask whatIsShield;

    void OnEnable()
    {
        currentLifeTimer = lifeTime;
        isRunning = false;
    }

    public void Initialize(DamageContext context, Vector3 moveDirection)
    {
        damageContext = context;
        direction = moveDirection;
        transform.rotation = Quaternion.LookRotation(direction);
        isRunning = true;
    }

    void Update()
    {
        if (!isRunning) return;

        transform.position += direction * moveSpeed * Time.deltaTime;
        currentLifeTimer -= Time.deltaTime;
        if (currentLifeTimer <= 0)
        {
            Despawn();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isRunning) return;

        if (other.CompareTag("Player"))
        {
            IDamageReceiver damageReceiver = other.GetComponentInParent(typeof(IDamageReceiver)) as IDamageReceiver;
            if (damageReceiver != null)
            {
                DamageContext context = damageContext;
                context.Target = other.gameObject;
                damageReceiver.ReceiveDamage(context);
            }
            Despawn();
        }
        else if ((whatIsGround.value & (1 << other.gameObject.layer)) > 0)
        {
            //Debug.Log("敌人子弹击中地面");
            Despawn();
        }
        else if ((whatIsShield.value & (1 << other.gameObject.layer)) > 0)
        {
           // Debug.Log("敌人子弹被盾牌挡下");
            Despawn();
        }
    }

    private void Despawn()
    {
        isRunning = false;
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnObject(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
