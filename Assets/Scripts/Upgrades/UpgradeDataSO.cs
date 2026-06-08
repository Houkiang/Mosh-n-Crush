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

    public virtual bool CanOffer(UpgradeContext context)
    {
        return context.IsValid;
    }

    public abstract void Apply(UpgradeContext context);
}
