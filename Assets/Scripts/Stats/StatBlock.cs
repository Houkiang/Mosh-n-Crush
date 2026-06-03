using System.Collections.Generic;

public sealed class StatBlock
{
    private readonly Dictionary<StatType, float> values = new Dictionary<StatType, float>();

    public void SetValue(StatType statType, float value)
    {
        values[statType] = value;
    }

    public void AddValue(StatType statType, float value)
    {
        values[statType] = GetValue(statType) + value;
    }

    public float GetValue(StatType statType, float fallback = 0f)
    {
        return values.TryGetValue(statType, out float value) ? value : fallback;
    }

    public void Clear()
    {
        values.Clear();
    }
}
