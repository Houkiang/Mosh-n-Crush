using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Enemy))]
public class EnemyFlybyMovement : MonoBehaviour
{
    private const float DirectionBlendSpeed = 10f;

    private Rigidbody rb;
    private Enemy enemyCore;
    private Vector3 moveDirection;
    private Vector3 currentMoveDirection;
    private Vector3 targetPoint;
    private float moveSpeed;
    private float despawnDistance;
    private float maxLifeTime;
    private float lifeTimer;
    private bool hasPassedTarget;
    private bool isConfigured;
    private bool isKnockedBack;
    private float knockbackTimer;
    private EnemyRushGroupController rushGroupController;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        enemyCore = GetComponent<Enemy>();
    }

    private void OnDisable()
    {
        ClearRushGroupController();
    }

    public void ResetState()
    {
        ClearRushGroupController();
        moveDirection = Vector3.zero;
        currentMoveDirection = Vector3.zero;
        targetPoint = Vector3.zero;
        moveSpeed = 0f;
        despawnDistance = 0f;
        maxLifeTime = 0f;
        lifeTimer = 0f;
        hasPassedTarget = false;
        isConfigured = false;
        isKnockedBack = false;
        knockbackTimer = 0f;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void SetRushGroupController(EnemyRushGroupController controller)
    {
        if (rushGroupController == controller)
        {
            return;
        }

        if (rushGroupController != null)
        {
            rushGroupController.UnregisterMember(this);
        }

        rushGroupController = controller;
        rushGroupController?.RegisterMember(this);
    }

    public void Configure(Vector3 direction, Vector3 lockedTargetPoint, float speed, float maxDistance, float lifeTime)
    {
        moveDirection = new Vector3(direction.x, 0f, direction.z).normalized;
        if (moveDirection.sqrMagnitude <= 0.0001f)
        {
            moveDirection = transform.forward;
            moveDirection.y = 0f;
            moveDirection.Normalize();
        }

        targetPoint = lockedTargetPoint;
        currentMoveDirection = moveDirection;
        moveSpeed = Mathf.Max(0f, speed);
        despawnDistance = Mathf.Max(0f, maxDistance);
        maxLifeTime = Mathf.Max(0.1f, lifeTime);
        lifeTimer = 0f;
        hasPassedTarget = false;
        isConfigured = true;
        isKnockedBack = false;
        knockbackTimer = 0f;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
    }

    public void ApplyKnockback(Vector3 sourcePosition, float force, float stunDuration)
    {
        if (force <= 0f && stunDuration <= 0f)
        {
            return;
        }

        isKnockedBack = true;
        knockbackTimer = stunDuration;

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

    private void FixedUpdate()
    {
        if (!isConfigured || enemyCore == null || enemyCore.IsDead)
        {
            return;
        }

        lifeTimer += Time.fixedDeltaTime;
        if (lifeTimer >= maxLifeTime)
        {
            enemyCore.Despawn();
            return;
        }

        if (isKnockedBack)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            if (knockbackTimer <= 0f)
            {
                isKnockedBack = false;
            }
            else
            {
                return;
            }
        }

        Vector3 desiredDirection = moveDirection;
        if (rushGroupController != null)
        {
            Vector3 steering = rushGroupController.GetSteeringFor(this);
            desiredDirection += steering;
            desiredDirection.y = 0f;
        }

        if (desiredDirection.sqrMagnitude <= 0.0001f)
        {
            desiredDirection = moveDirection;
        }

        desiredDirection.Normalize();
        currentMoveDirection = Vector3.Slerp(currentMoveDirection, desiredDirection, Time.fixedDeltaTime * DirectionBlendSpeed);
        currentMoveDirection.y = 0f;
        if (currentMoveDirection.sqrMagnitude <= 0.0001f)
        {
            currentMoveDirection = desiredDirection;
        }
        else
        {
            currentMoveDirection.Normalize();
        }

        Vector3 currentPosition = transform.position;
        Vector3 toTargetPoint = currentPosition - targetPoint;
        float forwardProgress = Vector3.Dot(toTargetPoint, moveDirection);
        if (forwardProgress > 0f)
        {
            hasPassedTarget = true;
        }

        if (hasPassedTarget && despawnDistance > 0f)
        {
            Vector3 planarOffset = new Vector3(toTargetPoint.x, 0f, toTargetPoint.z);
            if (planarOffset.sqrMagnitude >= despawnDistance * despawnDistance)
            {
                enemyCore.Despawn();
                return;
            }
        }

        rb.MoveRotation(Quaternion.LookRotation(currentMoveDirection, Vector3.up));
        rb.velocity = new Vector3(currentMoveDirection.x * moveSpeed, rb.velocity.y, currentMoveDirection.z * moveSpeed);
    }

    private void ClearRushGroupController()
    {
        if (rushGroupController == null)
        {
            return;
        }

        rushGroupController.UnregisterMember(this);
        rushGroupController = null;
    }
}
