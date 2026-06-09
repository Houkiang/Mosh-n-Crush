using UnityEngine;

public abstract class UpgradeDataSO : ScriptableObject
{
    [Header("UI")]
    public string upgradeName;
    public Sprite icon;
    [TextArea] public string description;

    [Header("Weight")]
    [Tooltip("Higher weight means a higher chance of appearing.")]
    [Range(1, 1000)]
    public int weight = 100;

    [Header("Offer Rules")]
    [Tooltip("How many times this upgrade can be picked. Set to 0 for unlimited.")]
    [Min(0)]
    public int maxPickCount;

    public virtual bool CanOffer(UpgradeContext context)
    {
        return context.IsValid && !context.HasReachedPickLimit(this, maxPickCount);
    }

    public abstract void Apply(UpgradeContext context);
}
