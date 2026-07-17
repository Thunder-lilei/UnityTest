using UnityEngine;

public class Fireball : MonoBehaviour
{
    public float speed = 20f;              // 飞行速度
    public float lifetime = 3f;            // 存活时间（秒）

    void Start()
    {
        Destroy(gameObject, lifetime);

        // 忽略发射者（Player）的碰撞
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            Collider playerCol = player.GetComponent<Collider>();
            Collider myCol = GetComponent<Collider>();
            if (playerCol != null && myCol != null)
            {
                Physics.IgnoreCollision(playerCol, myCol);
            }
        }

        // 忽略其他火球的碰撞
        foreach (var other in FindObjectsOfType<Fireball>())
        {
            if (other == this)
                continue;
            Collider otherCol = other.GetComponent<Collider>();
            Collider myCol2 = GetComponent<Collider>();
            if (otherCol != null && myCol2 != null)
                Physics.IgnoreCollision(otherCol, myCol2);
        }
    }

    void Update()
    {
        // 火球不断前移
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PickUp") || other.CompareTag("HealthPotion"))
            return;

        if (other.CompareTag("Enemy"))
        {
            Destroy(other.gameObject);
            AudioManager.Instance?.PlayEnemyDeath();
        }
        AudioManager.Instance?.PlayFireballHit();
        Destroy(gameObject);
    }
}