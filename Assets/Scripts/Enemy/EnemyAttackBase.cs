using System;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Enemy))]
public abstract class EnemyAttackBase : MonoBehaviour
{
    [Header("Attack Timing")]
    [SerializeField] protected float attackCooldown = 1f;
    [SerializeField] private bool randomizeInitialCooldown = true;

    protected Enemy enemyCore;
    protected float attackTimer;

    public event Action OnAttack;

    protected virtual void Awake()
    {
        enemyCore = GetComponent<Enemy>();
    }

    protected virtual void OnEnable()
    {
        attackTimer = randomizeInitialCooldown
            ? Random.Range(0f, Mathf.Max(attackCooldown, 0.1f))
            : 0f;
    }

    protected virtual void Update()
    {
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }
    }

    protected bool CanAttackTarget()
    {
        return enemyCore != null && !enemyCore.IsDead && enemyCore.PlayerTransform != null;
    }

    protected bool IsAttackReady()
    {
        return CanAttackTarget() && attackTimer <= 0f;
    }

    protected void ConsumeAttackCooldown()
    {
        attackTimer = attackCooldown;
    }

    protected void RaiseAttack()
    {
        OnAttack?.Invoke();
    }
}
