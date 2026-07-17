using UnityEngine;

public class MagnetDetector : MonoBehaviour
{
    public float radius = 3f;              // 吸取半径
    private SphereCollider col;            // 触发碰撞体
    private HealthBar healthBar;           // 缓存玩家血量组件

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

    void OnTriggerEnter(Collider other)
    {
        var item = other.GetComponent<PickupItem>();
        if (item == null)
            return;

        if (other.CompareTag("HealthPotion") && healthBar != null && healthBar.IsFull())
            return;

        item.StartAttract(transform.parent);
    }

    public void IncreaseRadius(float amount)
    {
        radius += amount;
        if (col != null)
            col.radius = radius;
    }
}