using UnityEngine;

public class Fireball : MonoBehaviour, IPooledObject
{
    public float speed = 20f;              // 飞行速度
    public float lifetime = 3f;            // 存活时间（秒）

    private float timer;                   // 存活计时器
    private ObjectPool pool;               // 所属对象池引用

    public void OnSpawn()
    {
        timer = 0f;
    }

    public void SetPool(ObjectPool pool)
    {
        this.pool = pool;
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;

        timer += Time.deltaTime;
        if (timer >= lifetime && pool != null)
            pool.Despawn(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyMovement enemy = other.GetComponent<EnemyMovement>();
            if (enemy != null)
                enemy.TakeDamage(1f);
        }
        AudioManager.Instance?.PlayFireballHit();
        if (pool != null)
            pool.Despawn(gameObject);
    }
}