using UnityEngine;

public class Fireball : MonoBehaviour
{
    public float speed = 20f;
    public float lifetime = 3f;

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
    }

    void Update()
    {
        // 火球不断前移
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Destroy(other.gameObject);
            AudioManager.Instance?.PlayEnemyDeath();
        }
        AudioManager.Instance?.PlayFireballHit();
        Destroy(gameObject);
    }
}