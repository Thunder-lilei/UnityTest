using UnityEngine;

public class PickupItem : MonoBehaviour
{
    private Transform target;              // 吸引目标（玩家）
    private bool isAttracting = false;    // 是否正在被吸引
    public float attractSpeed = 8f;       // 吸引飞行速度

    public void StartAttract(Transform target)
    {
        this.target = target;
        isAttracting = true;
    }

    void Update()
    {
        if (isAttracting && target != null)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, target.position, attractSpeed * Time.deltaTime);
        }
    }
}