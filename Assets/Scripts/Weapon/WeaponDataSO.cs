using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Game/Weapon Data")]
public class WeaponDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public string weaponName;
    public GameObject weaponPrefab;

    [Header("Melee Visual")]
    public GameObject weaponModelPrefab;
    public Vector3 modelOffset = new Vector3(0f, 1f, 0.5f);
    public float swingDuration = 0.2f;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    public float projectileLifeTime = 5f;

    [Header("Orbit Count")]
    public int weaponCount = 1;

    [Header("Combat")]
    public float damage = 10f;
    public float cooldown = 3f;
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.4f;
    public DamageType damageType = DamageType.Physical;
    [Tooltip("If false, crit values on this weapon are ignored.")]
    public bool canCrit = false;
    [Range(0f, 1f)] public float critRate = 0f;
    public float critMultiplier = 1.5f;
    public StatusEffectDataSO[] onHitStatusEffects;

    [Header("Range")]
    public float attackRange = 3f;
    [Range(0, 360)] public float attackAngle = 180f;
}
