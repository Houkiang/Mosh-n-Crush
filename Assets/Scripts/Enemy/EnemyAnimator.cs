using UnityEngine;
using System.Collections;
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(Rigidbody))]
public class EnemyAnimator : MonoBehaviour
{
    [Header("组件引用")]
    private Animator animator;
    private Enemy enemyCore;
    private EnemyShooter enemyShooter; // 可选，如果有远程攻击
    private Rigidbody rb;

    [Header("动画参数名称")]
    [SerializeField] private string speedParamName = "Speed";
    [SerializeField] private string attackParamName = "Attack";
    [SerializeField] private string hitParamName = "Hit";
    [SerializeField] private string dieParamName = "Die";
    [SerializeField] private float deathDelay = 1.0f;

    // 哈希缓存
    private int speedHash;
    private int attackHash;
    private int hitHash;
    private int dieHash;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        enemyCore = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody>();
        enemyShooter = GetComponent<EnemyShooter>();

        // 将字符串转换为Hash ID
        speedHash = Animator.StringToHash(speedParamName);
        attackHash = Animator.StringToHash(attackParamName);
        hitHash = Animator.StringToHash(hitParamName);
        dieHash = Animator.StringToHash(dieParamName);
    }

    private void OnEnable()
    {
        // 订阅事件
        if (enemyCore != null)
        {

            Enemy.OnEnemyKilled += HandleDeath; 
        }

        if (enemyShooter != null)
        {
            enemyShooter.OnAttack += PlayAttack;
        }
    }

    private void OnDisable()
    {

        
        Enemy.OnEnemyKilled -= HandleDeath;

        if (enemyShooter != null)
        {
            enemyShooter.OnAttack -= PlayAttack;
        }
    }

    private void Update()
    {
        if (enemyCore.IsDead) return;

        UpdateMovementAnimation();
    }

    private void UpdateMovementAnimation()
    {
        // 使用 Rigidbody 的水平速度大小来控制 Idle/Move 切换
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        float speed = horizontalVelocity.magnitude;

        animator.SetFloat(speedHash, speed);
    }

    private void PlayAttack()
    {
        animator.SetTrigger(attackHash);
    }

    private void PlayHit()
    {
        // 只有没死的时候才播放受击，避免覆盖死亡动画
        if (!enemyCore.IsDead)
        {
            animator.SetTrigger(hitHash);
        }
    }

    private void HandleDeath(Enemy enemy)
    {
        if (enemy == enemyCore)
        {
            // 1. 播放动画
            animator.SetBool(dieHash, true);

            // 2. 开启协程等待回收
            StartCoroutine(WaitAndDespawn());
        }
    }

    private IEnumerator WaitAndDespawn()
    {
        // 等待固定时间
        yield return new WaitForSeconds(deathDelay);
                enemyCore.Despawn();
    }
}
