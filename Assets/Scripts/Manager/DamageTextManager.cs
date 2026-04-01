using UnityEngine;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    [Header("资源引用")]
    [SerializeField] private GameObject damagePopupPrefab;

    private Transform mainCameraTransform;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            // 尝试找任意一个 Camera
            var cam = FindFirstObjectByType<Camera>();
            if (cam != null) mainCameraTransform = cam.transform;
            else Debug.LogError("严重错误：场景中找不到任何摄像机！伤害数字将无法朝向屏幕。");
        }
    }

    public void ShowDamage(Vector3 position, float damageAmount, bool isCritical = false)
    {
        GameObject popupObj = PoolManager.Instance.GetObject(damagePopupPrefab, position, Quaternion.identity);
        
        DamagePopup popupScript = popupObj.GetComponent<DamagePopup>();
        if (popupScript != null)
        {
            popupScript.Setup(damageAmount, isCritical, mainCameraTransform);
        }
    }
}
