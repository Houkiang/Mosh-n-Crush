using System.Collections.Generic;
using UnityEngine;

public class ShieldWeapon : WeaponBase
{
    [Header("盾牌特有属性")]
    [SerializeField] private float rotationSpeed = 90f;

    private int currentShieldCount;
    private float currentRotationAngle = 0f;
    private readonly List<GameObject> spawnedShields = new List<GameObject>();

    public override void Initialize(WeaponDataSO data, Transform owner, WeaponManager manager)
    {
        base.Initialize(data, owner, manager);
        currentShieldCount = currentWeaponCount;
        SpawnShields();
    }

    protected override void Update()
    {
        if (playerTransform == null) return;

        currentRotationAngle += rotationSpeed * Time.deltaTime;
        if (currentRotationAngle >= 360f) currentRotationAngle -= 360f;

        UpdateShieldPositions();
    }

    protected override void Attack()
    {
    }

    protected override bool UsesCountBurst()
    {
        return false;
    }

    private void SpawnShields()
    {
        foreach (var shield in spawnedShields)
        {
            if (shield != null) Destroy(shield);
        }
        spawnedShields.Clear();

        if (weaponData.weaponModelPrefab == null)
        {
            Debug.LogError("ShieldWeapon: 缺少 WeaponModelPrefab!");
            return;
        }

        for (int i = 0; i < currentShieldCount; i++)
        {
            GameObject obj = Instantiate(weaponData.weaponModelPrefab);
            obj.transform.SetParent(playerTransform);

            ShieldProjectile projectile = obj.GetComponent<ShieldProjectile>();
            if (projectile == null) projectile = obj.AddComponent<ShieldProjectile>();
            projectile.Initialize(this);

            spawnedShields.Add(obj);
        }

        UpdateShieldPositions();
    }

    private void UpdateShieldPositions()
    {
        if (spawnedShields.Count == 0) return;

        float angleStep = 360f / spawnedShields.Count;
        for (int i = 0; i < spawnedShields.Count; i++)
        {
            if (spawnedShields[i] == null) continue;

            float angle = currentRotationAngle + angleStep * i;
            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * currentRange;
            Vector3 targetPos = playerTransform.position + offset + Vector3.up * 1f;

            spawnedShields[i].transform.position = targetPos;
            spawnedShields[i].transform.rotation = Quaternion.LookRotation(offset);
        }
    }

    public override void IncreaseWeaponCount(int amount)
    {
        currentShieldCount = Mathf.Max(1, currentShieldCount + amount);
        currentWeaponCount = currentShieldCount;
        SpawnShields();
    }

    public DamageContext CreateShieldDamageContext(GameObject target = null)
    {
        return CreateDamageContext(target);
    }

    public float GetActualDamage() => GetDamageAfterPlayer();
    public float GetKnockbackForce() => currentKnockback;
    public float GetKnockbackDuration() => currentKnockbackDuration;
    public Vector3 GetPlayerPosition() => playerTransform.position;
    public int GetEnemyLayerMask() => enemyLayerMask;

    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerTransform.position, currentRange);
        }
    }
}
