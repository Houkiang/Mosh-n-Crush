using UnityEngine;

public class ExperienceOrb : MonoBehaviour
{
    private int xpValue;
    private Transform targetPlayer;
    private bool isMagnetized = false;
    private float moveSpeed = 0f;
    private float acceleration = 15f; // 飞向玩家的加速度

    public void Initialize(int value)
    {
        this.xpValue = value;
        this.isMagnetized = false;
        this.moveSpeed = 5f; // 初始被吸取速度
        this.targetPlayer = null;
    }

    // 当玩家的磁铁触发器碰到球时调用
    public void Magnetize(Transform player)
    {
        if (isMagnetized) return;
        isMagnetized = true;
        targetPlayer = player;
    }

    void Update()
    {
        if (!isMagnetized || targetPlayer == null) return;

        // 加速飞向玩家
        moveSpeed += acceleration * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetPlayer.position, moveSpeed * Time.deltaTime);

        // 检测是否接触到玩家 (距离极近)
        if (Vector3.SqrMagnitude(transform.position - targetPlayer.position) < 0.5f)
        {
            Collect();
        }
    }

    private void Collect()
    {
        // 给玩家加经验
        var playerScript = targetPlayer.GetComponentInParent<Player>();
        if (playerScript != null)
        {
            playerScript.GainExperience(xpValue);
        }

        // 回收自己
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnObject(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
