using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Game/Weapon Data")]
public class WeaponDataSO : ScriptableObject
{
    [Header("基本信息")]
    public string weaponName;
    public GameObject weaponPrefab; // 逻辑预制体

    [Header("视觉模型 (近战用)")]
    public GameObject weaponModelPrefab; 
    public Vector3 modelOffset = new Vector3(0, 1f, 0.5f);
    public float swingDuration = 0.2f;

    [Header("投射物设置 (远程用)")]
    // 新增：箭矢的预制体
    public GameObject projectilePrefab; 
    // 新增：飞行速度
    public float projectileSpeed = 10f;
    // 新增：最大生存时间（防止飞出地图无限存在）
    public float projectileLifeTime = 5f;
    [Header("可增加数量设置 (盾牌用)")]
    public int weaponCount = 1;

    [Header("战斗数值")]
    public float damage = 10f;
    public float cooldown = 3f;
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.4f;
    
    [Header("范围设置")]
    public float attackRange = 3f; 
    [Range(0, 360)]
    public float attackAngle = 180f; 
}
