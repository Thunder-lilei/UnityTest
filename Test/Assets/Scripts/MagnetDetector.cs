using UnityEngine;

public class MagnetDetector : MonoBehaviour
{
    public float radius = 3f;              // 吸取半径
    private SphereCollider col;            // 触发碰撞体
    private HealthBar healthBar;           // 缓存玩家血量组件

    /// <summary>初始化碰撞体和血量引用</summary>
    void Start()
    {
        col = GetComponent<SphereCollider>();
        if (col != null)
        {
            col.isTrigger = true;
            col.radius = radius;
        }
        healthBar = GetComponentInParent<HealthBar>();
    }

    /// <summary>触发器回调：PickUp/血瓶进入范围时启动吸引，满血不吸血瓶</summary>
    /// <param name="other">碰撞到的对象</param>
    void OnTriggerEnter(Collider other)
    {
        var item = other.GetComponent<PickupItem>();
        if (item == null)
            return;

        if (other.CompareTag("HealthPotion") && healthBar != null && healthBar.IsFull())
            return;

        item.StartAttract(transform.parent);
    }

    /// <summary>增大吸取半径（升级系统调用）</summary>
    /// <param name="amount">增加的半径</param>
    public void IncreaseRadius(float amount)
    {
        radius += amount;
        if (col != null)
            col.radius = radius;
    }
}