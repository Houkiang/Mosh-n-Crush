using System.Collections.Generic;
using UnityEngine;

public class StatusController : MonoBehaviour
{
    private readonly List<StatusEffectInstance> activeEffects = new List<StatusEffectInstance>();

    private ICombatant owner;

    public void Initialize(ICombatant combatant)
    {
        owner = combatant;
    }

    private void Update()
    {
        if (owner == null || !owner.IsAlive || activeEffects.Count == 0)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            StatusEffectInstance effect = activeEffects[i];
            effect.Update(deltaTime, this);
            if (effect.IsExpired)
            {
                RemoveEffectAt(i);
            }
        }
    }

    public void ApplyFromDamageContext(DamageContext context)
    {
        if (context.StatusEffects == null || context.StatusEffects.Length == 0)
        {
            return;
        }

        for (int i = 0; i < context.StatusEffects.Length; i++)
        {
            StatusEffectApplication application = context.StatusEffects[i];
            if (application.Data == null)
            {
                continue;
            }

            ApplyStatus(application.Data, context.Source, application.StackCount);
        }
    }

    public void ApplyStatus(StatusEffectDataSO data, GameObject source, int stackCount = 1)
    {
        if (owner == null || owner.Stats == null || data == null)
        {
            return;
        }

        StatusEffectInstance existing = FindEffect(data);
        if (existing != null)
        {
            existing.RemoveStatModifiers(owner.Stats);
            existing.Reapply(stackCount);
            existing.ApplyStatModifiers(owner.Stats);
            return;
        }

        StatusEffectInstance instance = new StatusEffectInstance(data, source, stackCount);
        instance.ApplyStatModifiers(owner.Stats);
        activeEffects.Add(instance);
    }

    public float AbsorbIncomingDamage(float damage)
    {
        float remainingDamage = damage;

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            StatusEffectInstance effect = activeEffects[i];
            remainingDamage = effect.AbsorbDamage(remainingDamage);
            if (effect.IsExpired)
            {
                RemoveEffectAt(i);
            }

            if (remainingDamage <= 0f)
            {
                break;
            }
        }

        return remainingDamage;
    }

    public void ApplyTickDamage(StatusEffectInstance effect)
    {
        if (owner == null || effect == null || effect.Data == null || effect.Data.tickDamage <= 0f)
        {
            return;
        }

        owner.ReceiveDamage(new DamageContext
        {
            Source = effect.Source,
            Target = owner.CombatTransform != null ? owner.CombatTransform.gameObject : gameObject,
            BaseDamage = effect.Data.tickDamage * effect.Stacks,
            DamageType = effect.Data.tickDamageType,
            CanCrit = false,
            CritRate = 0f,
            CritMultiplier = 1f,
            KnockbackForce = 0f,
            KnockbackDuration = 0f,
            StatusEffects = null
        });
    }

    public void ClearAllEffects()
    {
        if (owner != null && owner.Stats != null)
        {
            for (int i = 0; i < activeEffects.Count; i++)
            {
                activeEffects[i].RemoveStatModifiers(owner.Stats);
            }
        }

        activeEffects.Clear();
    }

    private StatusEffectInstance FindEffect(StatusEffectDataSO data)
    {
        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (activeEffects[i].Data == data)
            {
                return activeEffects[i];
            }
        }

        return null;
    }

    private void RemoveEffectAt(int index)
    {
        if (index < 0 || index >= activeEffects.Count)
        {
            return;
        }

        StatusEffectInstance effect = activeEffects[index];
        if (owner != null && owner.Stats != null)
        {
            effect.RemoveStatModifiers(owner.Stats);
        }

        activeEffects.RemoveAt(index);
    }
}
