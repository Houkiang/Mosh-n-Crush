using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Enemy))]
public class EnemyMovement : MonoBehaviour
{
    [Header("移动")]
    [Tooltip("敌人与玩家距离小于该值时停止主动前进。")]
    [SerializeField] private float stopDistance = 0.01f;
    private float sqtrStopDistance;

    [Header("分离")]
    [Tooltip("用于检测附近敌人的分离半径。")]
    [SerializeField] private float separationRadius = 1.5f;
    [Tooltip("敌人之间相互推开的力度。")]
    [SerializeField] private float separationWeight = 20f;
    [Tooltip("分离检测使用的敌人层。")]
    [SerializeField] private LayerMask enemyLayer;
    [Tooltip("分离力的重算间隔。")]
    [SerializeField] private float separationUpdateInterval = 0.2f;

    private Rigidbody rb;
    private Enemy enemyCore;
    private bool isKnockedBack;
    private float knockbackTimer;
    private Vector3 currentSeparationForce = Vector3.zero;
    private float separationTimer;

    private readonly Collider[] neighborBuffer = new Collider[10];

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        enemyCore = GetComponent<Enemy>();
        separationTimer = Random.Range(0f, separationUpdateInterval);
        SetupRigidbody();
        sqtrStopDistance = stopDistance * stopDistance;
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
        currentSeparationForce = Vector3.zero;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void Update()
    {
        if (enemyCore.IsDead || enemyCore.PlayerTransform == null)
        {
            rb.velocity = Vector3.zero;
            return;
        }

        if (isKnockedBack)
        {
            HandleKnockback();
            return;
        }

        //CalculateSeparationForceLowFreq();
        MoveTowardsPlayer();
    }

    private void CalculateSeparationForceLowFreq()
    {
        separationTimer -= Time.deltaTime;
        if (separationTimer > 0f)
        {
            return;
        }

        separationTimer = separationUpdateInterval;
        int count = Physics.OverlapSphereNonAlloc(transform.position, separationRadius, neighborBuffer, enemyLayer);

        if (count <= 0)
        {
            currentSeparationForce = Vector3.zero;
            return;
        }

        Vector3 separationSum = Vector3.zero;
        int validNeighbors = 0;

        for (int i = 0; i < count; i++)
        {
            Collider col = neighborBuffer[i];
            if (col == null || col.gameObject == gameObject)
            {
                continue;
            }

            EnemyMovement otherMovement = col.GetComponentInParent<EnemyMovement>();
            if (otherMovement == null || !otherMovement.enabled || otherMovement == this)
            {
                continue;
            }

            Vector3 pushDir = transform.position - col.transform.position;
            pushDir.y = 0f;

            float sqrMag = pushDir.sqrMagnitude;
            if (sqrMag <= 0.001f)
            {
                continue;
            }

            separationSum += pushDir / sqrMag;
            validNeighbors++;
        }

        currentSeparationForce = validNeighbors > 0
            ? (separationSum / validNeighbors) * separationWeight
            : Vector3.zero;
    }

    private void HandleKnockback()
    {
        knockbackTimer -= Time.deltaTime;

        if (knockbackTimer <= 0.4f)
        {
            rb.velocity = Vector3.zero;

            if (knockbackTimer <= 0f)
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

        if (desiredDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredDirection);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 10f * Time.deltaTime));
        }

        if (sqrDist > sqtrStopDistance)
        {
            float speed = enemyCore.CurrentMoveSpeed;
            Vector3 finalVelocity = (desiredDirection * speed) + currentSeparationForce;
            rb.velocity = new Vector3(finalVelocity.x, rb.velocity.y, finalVelocity.z);
            return;
        }

        if (currentSeparationForce.sqrMagnitude > 0.1f)
        {
            rb.velocity = new Vector3(currentSeparationForce.x, rb.velocity.y, currentSeparationForce.z);
        }
        else
        {
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        }
    }

    public void ApplyKnockback(Vector3 sourcePosition, float force, float stunDuration)
    {
        if (force <= 0f && stunDuration <= 0f)
        {
            return;
        }

        isKnockedBack = true;
        knockbackTimer = stunDuration;
        currentSeparationForce = Vector3.zero;

        Vector3 knockbackDir = transform.position - sourcePosition;
        knockbackDir.y = 0f;

        if (knockbackDir.sqrMagnitude <= 0.0001f)
        {
            knockbackDir = transform.forward;
            knockbackDir.y = 0f;
        }

        knockbackDir.Normalize();

        rb.velocity = Vector3.zero;

        if (force > 0f)
        {
            rb.AddForce(knockbackDir * force, ForceMode.Impulse);
        }
    }
}

