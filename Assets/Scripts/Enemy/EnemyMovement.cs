using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Enemy))]
public class EnemyMovement : MonoBehaviour
{
    [Header("移动设置")]
    [Tooltip("距离玩家此距离时停止移动，近战怪默认0.01，远程怪可适当增大")]
    [SerializeField] private float stopDistance = 0.01f;

    [Header("群组分离优化 (Separation)")]
    [Tooltip("检测周围队友的半径")]
    [SerializeField] private float separationRadius = 1.5f;
    [Tooltip("分离力度权重，越大排斥越强")]
    [SerializeField] private float separationWeight = 20f;
    [Tooltip("敌人的层级，用于物理检测")]
    [SerializeField] private LayerMask enemyLayer;
    [Tooltip("分离力计算间隔(秒)，越大性能越好但反应越慢")]
    [SerializeField] private float separationUpdateInterval = 0.2f;
    private Rigidbody rb;
    private Enemy enemyCore;

    // 击退/眩晕状态
    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;

    // 分离力相关缓存变量
    private Vector3 currentSeparationForce = Vector3.zero;
    private float separationTimer = 0f;
    private Collider[] neighborBuffer = new Collider[10]; // 限制最大检测数量

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        enemyCore = GetComponent<Enemy>();
        // 随机化初始计时器，防止1000个敌人在同一帧同时计算OverlapSphere
        separationTimer = Random.Range(0f, separationUpdateInterval);
        SetupRigidbody();
    }

    private void SetupRigidbody()
    {
        rb.isKinematic = false;
        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation; 
    }

    public void ResetState()
    {
        isKnockedBack = false;
        knockbackTimer = 0f;
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void FixedUpdate()
    {
        if (enemyCore.IsDead || enemyCore.PlayerTransform == null) 
        {
            rb.velocity = Vector3.zero;
            return;
        }

        if (isKnockedBack)
        {
            HandleKnockback();
        }
        else
        {
            // 1. 计算/更新分离力 (低频)
            CalculateSeparationForceLowFreq();
            
            // 2. 执行移动 (高频)
            MoveTowardsPlayer();
        }
    }
    /// <summary>
    /// 低频率更新周围环境检测，极大节省性能
    /// </summary>
    private void CalculateSeparationForceLowFreq()
    {
        separationTimer -= Time.fixedDeltaTime;
        if (separationTimer <= 0)
        {
            separationTimer = separationUpdateInterval;
            
            // 使用 NonAlloc 版本避免 GC
            int count = Physics.OverlapSphereNonAlloc(transform.position, separationRadius, neighborBuffer, enemyLayer);
            
            if (count > 0)
            {
                Vector3 separationSum = Vector3.zero;
                int validNeighbors = 0;

                for (int i = 0; i < count; i++)
                {
                    var col = neighborBuffer[i];
                    // 排除自己
                    if (col.gameObject == gameObject) continue;

                    // 计算排斥向量：从邻居指向自己
                    Vector3 pushDir = transform.position - col.transform.position;
                    pushDir.y = 0; // 忽略高度差

                    float sqrMag = pushDir.sqrMagnitude;
                    if (sqrMag > 0.001f) // 防止除以0
                    {
                        // 距离越近，排斥力越大 (1/距离)
                        // normalized / magnitude = pushDir / sqrMagnitude
                        separationSum += pushDir / sqrMag; 
                        validNeighbors++;
                    }
                }

                if (validNeighbors > 0)
                {
                    // 计算平均值并应用权重
                    currentSeparationForce = (separationSum / validNeighbors) * separationWeight;
                }
                else
                {
                    currentSeparationForce = Vector3.zero;
                }
            }
            else
            {
                currentSeparationForce = Vector3.zero;
            }
        }
    }
    private void HandleKnockback()
    {
        knockbackTimer -= Time.fixedDeltaTime;
        
        // 停止击退
        if (knockbackTimer <= 0.4f)
        {
            rb.velocity = Vector3.zero;

            if(knockbackTimer <= 0f)
            {
                isKnockedBack = false;
            }
        }
    }

    private void MoveTowardsPlayer()
    {
        Vector3 targetPos = enemyCore.PlayerTransform.position;
        targetPos.y = transform.position.y; 

        Vector3 offset = targetPos - transform.position;
        float sqrDist = offset.sqrMagnitude;
        Vector3 desiredDirection = offset.normalized;

        // 旋转朝向 (只看目标，不看分离力，这样看起来更自然)
        if (desiredDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredDirection);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 10f * Time.fixedDeltaTime));
        }
        
        if (sqrDist > stopDistance * stopDistance)
        {
            float speed = enemyCore.CurrentMoveSpeed;
            
            // 核心融合：寻路方向 + 分离方向
            // 保持 Y 轴速度 (重力)
            Vector3 finalVelocity = (desiredDirection * speed) + currentSeparationForce;
            
            rb.velocity = new Vector3(finalVelocity.x, rb.velocity.y, finalVelocity.z);
        }
        else
        {
            // 即使停止移动，如果被挤压，依然允许分离力推动（防止穿模）
            if (currentSeparationForce.sqrMagnitude > 0.1f)
            {
                rb.velocity = new Vector3(currentSeparationForce.x, rb.velocity.y, currentSeparationForce.z);
            }
            else
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }
        }
    }

    public void ApplyKnockback(Vector3 sourcePosition, float force, float stunDuration)
    {
        isKnockedBack = true;
        knockbackTimer = stunDuration;
                currentSeparationForce = Vector3.zero; // 击退时不考虑分离力
        Vector3 knockbackDir = (transform.position - sourcePosition).normalized;
        knockbackDir.y = 0; 

        rb.velocity = Vector3.zero; // 清空当前移动动量
        rb.AddForce(knockbackDir * force, ForceMode.Impulse);
    }
}
