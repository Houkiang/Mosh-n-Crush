using UnityEngine;

public interface ICombatant : IDamageable
{
    Transform CombatTransform { get; }
    bool IsAlive { get; }
    StatCollection Stats { get; }
    DamageResult ReceiveDamage(DamageContext context);
}
