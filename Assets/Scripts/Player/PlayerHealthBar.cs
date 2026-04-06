
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
    private bool subscribed;

    void Awake()
    {

    }

    void Start()
    {
        BindPlayer(player);

        // 记录世界坐标的UI旋转
        originalRotation = transform.rotation;
    }

    void OnDestroy()
    {
        UnsubscribePlayer(player);
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

    public void BindPlayer(Player newPlayer)
    {
        if (player == newPlayer && (newPlayer == null || subscribed)) return;

        UnsubscribePlayer(player);
        player = newPlayer;
        targetTransform = player != null ? player.transform : null;

        if (player == null)
        {
            if (healthSlider != null) healthSlider.value = 0f;
            return;
        }

        player.OnHealthChange += UpdateHealthBar;
        subscribed = true;
        UpdateHealthBar(player.CurrentHealth, player.MaxHealth);
    }

    private void UnsubscribePlayer(Player target)
    {
        if (!subscribed || target == null) return;
        target.OnHealthChange -= UpdateHealthBar;
        subscribed = false;
    }
}
