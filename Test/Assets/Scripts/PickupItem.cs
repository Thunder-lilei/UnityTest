using UnityEngine;

public class PickupItem : MonoBehaviour
{
    private Transform target;              // 吸引目标（玩家）
    private bool isAttracting = false;    // 是否正在被吸引
    public float attractSpeed = 8f;       // 吸引飞行速度

    /// <summary>启动吸引，设置目标 Transform</summary>
    /// <param name="target">吸引目标（玩家 Transform）</param>
    public void StartAttract(Transform target)
    {
        this.target = target;
        isAttracting = true;
    }

    /// <summary>每帧朝目标匀速移动（MoveTowards）</summary>
    void Update()
    {
        if (isAttracting && target != null)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, target.position, attractSpeed * Time.deltaTime);
        }
    }
}