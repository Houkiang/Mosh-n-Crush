
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Player player; 
    [SerializeField] private Slider healthSlider; 
    [SerializeField] private Image fillImage; 

    [Header("设置")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0); // 血条在头顶的高度偏移
    [SerializeField] private bool maintainOriginalRotation = true; // 不随角色旋转

    private Transform targetTransform;
    private Quaternion originalRotation;

    void Awake()
    {

    }

    void Start()
    {
        if (player != null)
        {
            targetTransform = player.transform;
            // 订阅事件
            player.OnHealthChange += UpdateHealthBar;
            // 初始化显示
            UpdateHealthBar(player.CurrentHealth, player.MaxHealth);
        }

        // 记录世界坐标的UI旋转
        originalRotation = transform.rotation;
    }

    void OnDestroy()
    {
        if (player != null)
            player.OnHealthChange -= UpdateHealthBar;
    }

    private void UpdateHealthBar(float current, float max)
    {
        if (healthSlider != null)
        {
            healthSlider.value = current / max;
        }
    }

    void LateUpdate()
    {
        if (targetTransform != null)
        {

            
            // 防止血条随角色旋转而旋转
            if (maintainOriginalRotation)
            {
                transform.rotation = originalRotation;
            }
            else
            {
                // 始终朝向摄像机
                transform.forward = Camera.main.transform.forward;
            }
        }
    }
}
