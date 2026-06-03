using UnityEngine;

public interface ICombatant : IDamageable
{
    Transform CombatTransform { get; }
    bool IsAlive { get; }
    DamageResult ReceiveDamage(DamageContext context);
}
