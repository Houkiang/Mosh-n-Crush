using UnityEngine;

public interface ICombatant : IDamageReceiver, IKnockbackable
{
    Transform CombatTransform { get; }
    bool IsAlive { get; }
    StatCollection Stats { get; }
}
