
using System.ComponentModel.Design;
using UnityEngine;

public class PenetratingProjectile : MonoBehaviour
{
    private float damage;
    private float knockbackForce;
    private float knockbackDuration;
    private float speed;
    private float lifeTimer;
    [SerializeField] private LayerMask whatIsGround;    
    
    // 防止同一支箭对同一个敌人在短时间内造成多次伤害
    private System.Collections.Generic.List<GameObject> hitHistory = new System.Collections.Generic.List<GameObject>();

    // 初始化方法：由武器发射时调用
    public void Initialize(float dmg, float spd, float kbForce, float kbDuration, float lifeTime)
    {
        damage = dmg;
        speed = spd;
        knockbackForce = kbForce;
        knockbackDuration = kbDuration;
        lifeTimer = lifeTime;
        
        hitHistory.Clear(); // 清空历史击中记录
    }

    void Update()
    {
        // 1. 向前飞行
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // 2. 计时销毁
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0)
        {
            PoolManager.Instance.ReturnObject(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 处理敌人：穿透（造成伤害但不销毁自己）
        if (other.CompareTag("Enemy")) 
        {
            // 防止同一帧或极短时间内多次触发
            if (hitHistory.Contains(other.gameObject)) return;
            hitHistory.Add(other.gameObject);

            IDamageable target = other.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(damage);
                // 击退方向：箭矢飞行的方向
                target.TakeKnockback(transform.position, knockbackForce, knockbackDuration);
            }
        }
        else if ((whatIsGround.value & (1 << other.gameObject.layer)) > 0)
        {
            // 撞到障碍物，回收进对象池
            //Debug.Log("PenetratingProjectile: 撞到障碍物，回收进对象池");
            PoolManager.Instance.ReturnObject(gameObject);
        }

    }

}
