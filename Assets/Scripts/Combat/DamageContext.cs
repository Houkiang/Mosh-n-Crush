using UnityEngine;

public struct DamageContext
{
    public GameObject Source;
    public GameObject Target;
    public float BaseDamage;
    public DamageType DamageType;
    public bool CanCrit;
    public float CritRate;
    public float CritMultiplier;
    public float KnockbackForce;
    public float KnockbackDuration;
}
