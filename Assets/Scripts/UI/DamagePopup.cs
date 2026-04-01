using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [Header("组件引用")]
    [SerializeField] private TextMeshPro textMesh;

    [Header("动画设置")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float disappearSpeed = 3f;
    [SerializeField] private float lifeTime = 1f;
    
    [Header("样式")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color criticalColor = Color.yellow;
    [SerializeField] private Vector3 randomOffset = new Vector3(0.5f, 0, 0.5f);

    private Color textColor;
    private float timer;
    private Transform cachedCameraTransform; 

    private void Awake()
    {

        if (textMesh == null) textMesh = GetComponent<TextMeshPro>();
    }

 
    public void Setup(float damageAmount, bool isCritical, Transform cameraTransform)
    {
        this.cachedCameraTransform = cameraTransform; // 保存引用

        textMesh.text = Mathf.RoundToInt(damageAmount).ToString();

        if (isCritical)
        {
            textMesh.fontSize = 6;
            textColor = criticalColor;
        }
        else
        {
            textMesh.fontSize = 4;
            textColor = normalColor;
        }
        textMesh.color = textColor;

        timer = lifeTime;
        
        // 随机偏移
        transform.position += new Vector3(
            Random.Range(-randomOffset.x, randomOffset.x),
            Random.Range(-randomOffset.y, randomOffset.y),
            Random.Range(-randomOffset.z, randomOffset.z)
        );
    }

    private void Update()
    {

        if (cachedCameraTransform != null)
        {
            // 让文字背对摄像机（即面向屏幕）
            transform.rotation = Quaternion.LookRotation(transform.position - cachedCameraTransform.position);
        }

        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            textColor.a -= disappearSpeed * Time.deltaTime;
            textMesh.color = textColor;

            if (textColor.a <= 0)
            {
                PoolManager.Instance.ReturnObject(this.gameObject);
            }
        }
    }
}
