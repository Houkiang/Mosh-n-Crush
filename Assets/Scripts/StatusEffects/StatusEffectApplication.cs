using System.Collections.Generic;

public struct StatusEffectApplication
{
    public StatusEffectDataSO Data;
    public int StackCount;

    public static StatusEffectApplication[] FromData(StatusEffectDataSO[] effects, int stackCount = 1)
    {
        if (effects == null || effects.Length == 0)
        {
            return null;
        }

        List<StatusEffectApplication> results = new List<StatusEffectApplication>();
        for (int i = 0; i < effects.Length; i++)
        {
            if (effects[i] == null)
            {
                continue;
            }

            results.Add(new StatusEffectApplication
            {
                Data = effects[i],
                StackCount = stackCount
            });
        }

        return results.Count > 0 ? results.ToArray() : null;
    }
}
