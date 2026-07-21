using UnityEngine;

public class Fireball : MonoBehaviour, IPooledObject
{
    public float speed = 20f;              // 飞行速度
    public float lifetime = 3f;            // 存活时间（秒）

    private float timer;                   // 存活计时器
    private ObjectPool pool;               // 所属对象池引用

    /// <summary>对象池激活回调：重置存活计时器</summary>
    public void OnSpawn()
    {
        timer = 0f;
    }

    /// <summary>设置所属对象池引用</summary>
    /// <param name="pool">对象池实例</param>
    public void SetPool(ObjectPool pool)
    {
        this.pool = pool;
    }

    /// <summary>每帧前移并检查存活时间，超时回收</summary>
    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;

        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            if (pool != null)
                pool.Despawn(gameObject);
            else
                Destroy(gameObject);
        }
    }

    /// <summary>碰撞回调：命中敌人扣血，命中后回收火球</summary>
    /// <param name="other">碰撞到的对象</param>
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
        else
            Destroy(gameObject);
    }
}