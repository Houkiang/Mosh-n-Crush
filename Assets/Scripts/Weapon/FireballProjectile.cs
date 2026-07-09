using System.Collections.Generic;
using UnityEngine;

public class FireballProjectile : MonoBehaviour
{
    private DamageContext damageContext;
    private float speed;
    private float lifeTimer;
    private float explosionTimer;
    private float armedTimer;
    private Vector3 horizontalDirection;
    private float verticalVelocity;
    private Collider projectileCollider;
    private SphereCollider sphereCollider;
    private int enemyLayerMask;
    private bool hasExploded;
    private Vector3 explosionOrigin;
    private float defaultColliderRadius;
    private float defaultColliderCenterY;
    private Transform travelVfxRoot;
    private Transform explosionVfxRoot;
    private ParticleSystem[] explosionParticles;
    private bool[] explosionParticleLoopStates;
    

    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float explosionRadius = 2.5f;

    [SerializeField] private ParticleSystem travelVfx;
    [SerializeField] private Transform explosionVfx;

    [SerializeField] private float heightInitialSpeed = 6f;

    [SerializeField] private float heightGravity = 12f;
    [SerializeField] private float rotationSmoothSpeed = 10f;
    [SerializeField] private float groundCollisionArmDelay = 0.15f;

    private void Awake()
    {
        projectileCollider = GetComponent<Collider>();
        if (projectileCollider != null && !projectileCollider.isTrigger)
        {
            projectileCollider.isTrigger = true;
        }

        sphereCollider = projectileCollider as SphereCollider;
        if (sphereCollider != null)
        {
            defaultColliderRadius = sphereCollider.radius;
            defaultColliderCenterY = sphereCollider.center.y;
        }

        if (travelVfx == null)
        {
            travelVfx = GetComponentInChildren<ParticleSystem>(true);
        }

        if (travelVfx != null)
        {
            travelVfxRoot = GetTopLevelChild(travelVfx.transform);
        }

        if (explosionVfx == null)
        {
            explosionVfxRoot = FindExplosionVfxRoot();
        }
        else
        {
            explosionVfxRoot = explosionVfx;
        }

        if (explosionVfxRoot != null)
        {
            explosionParticles = explosionVfxRoot.GetComponentsInChildren<ParticleSystem>(true);
            explosionParticleLoopStates = new bool[explosionParticles.Length];

            for (int i = 0; i < explosionParticles.Length; i++)
            {
                if (explosionParticles[i] == null)
                {
                    continue;
                }

                ParticleSystem.MainModule main = explosionParticles[i].main;
                explosionParticleLoopStates[i] = main.loop;
                main.loop = false;
            }
        }

        enemyLayerMask = LayerMask.GetMask("Enemy");
    }

    public void Initialize(DamageContext context, float spd, float lifeTime, Vector3 shootDirection)
    {
        damageContext = context;
        speed = spd;
        lifeTimer = lifeTime;
        explosionTimer = 0f;
        armedTimer = 0f;
        hasExploded = false;
        explosionOrigin = transform.position;

        horizontalDirection = new Vector3(shootDirection.x, 0f, shootDirection.z);
        if (horizontalDirection.sqrMagnitude <= 0.0001f)
        {
            horizontalDirection = new Vector3(transform.forward.x, 0f, transform.forward.z);
        }

        horizontalDirection.Normalize();
        verticalVelocity = heightInitialSpeed;
        transform.rotation = Quaternion.LookRotation(horizontalDirection);
        SetTravelState(true);
        SetExplosionState(false);

        if (projectileCollider != null)
        {
            projectileCollider.enabled = true;
        }

        if (sphereCollider != null)
        {
            sphereCollider.radius = defaultColliderRadius;
            Vector3 center = sphereCollider.center;
            center.y = defaultColliderCenterY;
            sphereCollider.center = center;
        }

        if (travelVfx != null)
        {
            travelVfx.Clear(true);
            travelVfx.Play(true);
        }
    }

    void Update()
    {
        if (hasExploded)
        {
            explosionTimer -= Time.deltaTime;
            if (explosionTimer <= 0f)
            {
                PoolManager.Instance.ReturnObject(gameObject);
            }

            return;
        }

        Vector3 currentVelocity = GetCurrentVelocity();
        Quaternion targetRotation = Quaternion.LookRotation(currentVelocity.normalized);
        float smoothFactor = Mathf.Max(0f, rotationSmoothSpeed) * Time.deltaTime;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothFactor);

        transform.position += currentVelocity * Time.deltaTime;
        verticalVelocity -= heightGravity * Time.deltaTime;
        armedTimer += Time.deltaTime;

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0)
        {
            Explode();
        }
    }

    private void OnDisable()
    {
        hasExploded = false;
        explosionTimer = 0f;
        armedTimer = 0f;

        if (travelVfx != null)
        {
            travelVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (explosionParticles != null)
        {
            for (int i = 0; i < explosionParticles.Length; i++)
            {
                if (explosionParticles[i] != null)
                {
                    ParticleSystem.MainModule main = explosionParticles[i].main;
                    if (explosionParticleLoopStates != null && i < explosionParticleLoopStates.Length)
                    {
                        main.loop = false;
                    }

                    explosionParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        SetTravelState(true);
        SetExplosionState(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasExploded)
        {
            return;
        }

        if ((whatIsGround.value & (1 << other.gameObject.layer)) > 0 && IsGroundCollisionArmed())
        {
            explosionOrigin = GetGroundImpactPoint(other);
            Explode();
        }
    }

    private void Explode()
    {
        if (hasExploded)
        {
            return;
        }

        hasExploded = true;
        transform.position = explosionOrigin;
        ApplyExplosionDamage();
        AlignExplosionToGroundPlane();

        if (projectileCollider != null)
        {
            projectileCollider.enabled = false;
        }

        SetTravelState(false);
        explosionTimer = PlayExplosionVfx();
        if (explosionTimer <= 0f)
        {
            PoolManager.Instance.ReturnObject(gameObject);
        }
    }

    private void AlignExplosionToGroundPlane()
    {
        Vector3 flatForward = horizontalDirection.sqrMagnitude > 0.0001f
            ? horizontalDirection
            : new Vector3(transform.forward.x, 0f, transform.forward.z);

        if (flatForward.sqrMagnitude <= 0.0001f)
        {
            flatForward = Vector3.forward;
        }

        transform.rotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
    }

    private void ApplyExplosionDamage()
    {
        float radius = GetExplosionRadius();
        Collider[] hits = Physics.OverlapSphere(explosionOrigin, radius, enemyLayerMask);
        if (hits == null || hits.Length == 0)
        {
            return;
        }

        HashSet<GameObject> damagedTargets = new HashSet<GameObject>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            IDamageReceiver damageReceiver = hit.GetComponentInParent(typeof(IDamageReceiver)) as IDamageReceiver;
            if (damageReceiver == null)
            {
                continue;
            }

            GameObject targetObject = hit.attachedRigidbody != null
                ? hit.attachedRigidbody.gameObject
                : hit.gameObject;

            if (!damagedTargets.Add(targetObject))
            {
                continue;
            }

            DamageContext context = damageContext;
            context.Target = targetObject;
            DamageResult result = damageReceiver.ReceiveDamage(context);

            if (result.FinalDamage <= 0f)
            {
                continue;
            }

            IKnockbackable knockbackable = hit.GetComponentInParent(typeof(IKnockbackable)) as IKnockbackable;
            if (knockbackable != null)
            {
                knockbackable.TakeKnockback(explosionOrigin, context.KnockbackForce, context.KnockbackDuration);
            }
        }
    }

    private float GetExplosionRadius()
    {
        if (sphereCollider == null)
        {
            return explosionRadius;
        }

        float scaledRadius = sphereCollider.radius * Mathf.Max(
            transform.lossyScale.x,
            transform.lossyScale.y,
            transform.lossyScale.z);

        return scaledRadius > 0f ? scaledRadius : explosionRadius;
    }

    private bool IsGroundCollisionArmed()
    {
        if (armedTimer < groundCollisionArmDelay)
        {
            return false;
        }

        return verticalVelocity <= 0f;
    }

    private Vector3 GetGroundImpactPoint(Collider groundCollider)
    {

        float castDistance = Mathf.Max(GetExplosionRadius() * 2f, 5f);
        Vector3 rayOrigin = transform.position + Vector3.up * castDistance * 0.5f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, castDistance, whatIsGround))
        {
            return hit.point;
        }
        else if (groundCollider != null&& 
                (                 
                groundCollider is BoxCollider ||
                groundCollider is SphereCollider ||
                groundCollider is CapsuleCollider ||
                (groundCollider is MeshCollider meshCollider && meshCollider.convex)))
        {
            Vector3 closestPoint = groundCollider.ClosestPoint(transform.position);
            if ((closestPoint - transform.position).sqrMagnitude > 0.0001f)
            {
                return closestPoint;
            }
        }

        return transform.position;
    }

    private Vector3 GetCurrentVelocity()
    {
        Vector3 velocity = horizontalDirection * speed + Vector3.up * verticalVelocity;
        if (velocity.sqrMagnitude <= 0.0001f)
        {
            velocity = horizontalDirection.sqrMagnitude > 0.0001f
                ? horizontalDirection
                : transform.forward;
        }

        return velocity;
    }

    private float PlayExplosionVfx()
    {
        SetExplosionState(true);

        if (explosionParticles == null || explosionParticles.Length == 0)
        {
            return 0f;
        }

        float longestDuration = 0f;
        for (int i = 0; i < explosionParticles.Length; i++)
        {
            ParticleSystem particle = explosionParticles[i];
            if (particle == null)
            {
                continue;
            }

            particle.Clear(true);
            particle.Play(true);

            ParticleSystem.MainModule main = particle.main;
            float lifetime = main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
                ? Mathf.Max(main.startLifetime.constantMin, main.startLifetime.constantMax)
                : main.startLifetime.constant;

            float duration = main.duration;
            if (main.loop)
            {
                duration = Mathf.Max(duration, 0.5f);
            }

            longestDuration = Mathf.Max(longestDuration, duration + lifetime);
        }

        return longestDuration;
    }

    private void SetTravelState(bool active)
    {
        if (travelVfxRoot != null)
        {
            travelVfxRoot.gameObject.SetActive(active);
        }
        else if (travelVfx != null)
        {
            travelVfx.gameObject.SetActive(active);
        }
    }

    private void SetExplosionState(bool active)
    {
        if (explosionVfxRoot != null)
        {
            explosionVfxRoot.gameObject.SetActive(active);
        }
    }

    private Transform FindExplosionVfxRoot()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null || child == travelVfxRoot)
            {
                continue;
            }

            return child;
        }

        return null;
    }

    private Transform GetTopLevelChild(Transform target)
    {
        if (target == null)
        {
            return null;
        }

        Transform current = target;
        while (current.parent != null && current.parent != transform)
        {
            current = current.parent;
        }

        return current.parent == transform ? current : target;
    }
}
