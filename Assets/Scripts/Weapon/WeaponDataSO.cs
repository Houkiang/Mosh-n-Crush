using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Game/Weapon Data")]
public class WeaponDataSO : ScriptableObject
{
    [Header("基本信息")]
    public string weaponName;
    public GameObject weaponPrefab;

    [Header("视觉模型 (近战用)")]
    public GameObject weaponModelPrefab;
    public Vector3 modelOffset = new Vector3(0, 1f, 0.5f);
    public float swingDuration = 0.2f;

    [Header("投射物设置 (远程用)")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    public float projectileLifeTime = 5f;

    [Header("数量设置 (环绕武器用)")]
    public int weaponCount = 1;

    [Header("战斗数值")]
    public float damage = 10f;
    public float cooldown = 3f;
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.4f;
    public DamageType damageType = DamageType.Physical;
    public bool canCrit = false;
    [Range(0f, 1f)] public float critRate = 0f;
    public float critMultiplier = 1.5f;

    [Header("范围设置")]
    public float attackRange = 3f;
    [Range(0, 360)]
    public float attackAngle = 180f;
}
