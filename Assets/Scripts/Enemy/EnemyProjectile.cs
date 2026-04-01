using TMPro;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("子弹属性")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float lifeTime = 5f; // 最大飞行时间

    private float damage;
    private Vector3 direction;
    private float currentLifeTimer; // 替换 Invoke，使用计时器
    private bool isRunning = false;
    public LayerMask whatIsGround;
    public LayerMask whatIsShield;

    // 当从对象池取出时，Unity 会调用 OnEnable
    void OnEnable()
    {
        // 重置计时器
        currentLifeTimer = lifeTime;
        // 此时还没有 Initialize，所以先不让它飞，等待 Initialize 被调用
        isRunning = false; 
    }

    public void Initialize(float damageAmount, Vector3 moveDirection)
    {
        this.damage = damageAmount;
        this.direction = moveDirection;
        transform.rotation = Quaternion.LookRotation(direction);
        
        isRunning = true;
    }

    void Update()
    {
        if (!isRunning) return;

        // 1. 移动
        transform.position += direction * moveSpeed * Time.deltaTime;

        // 2. 寿命检测
        currentLifeTimer -= Time.deltaTime;
        if (currentLifeTimer <= 0)
        {
            Despawn();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isRunning) return; // 防止回收瞬间多次触发

        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(damage);
            }
            Despawn();
        }
        else if ((whatIsGround.value & (1 << other.gameObject.layer)) > 0)
        {
            Debug.Log("敌人子弹击中地面");
            Despawn();
        }
        else if ((whatIsShield.value & (1 << other.gameObject.layer)) > 0)
        {
            Debug.Log("敌人子弹被盾牌挡下");
            Despawn();
        }
    }

    private void Despawn()
    {
        isRunning = false;
        // 归还给对象池 
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnObject(this.gameObject);
        }
        else
        {
            // 防止场景关闭时 PoolManager 先销毁报错
            Destroy(gameObject);
        }
    }
}
