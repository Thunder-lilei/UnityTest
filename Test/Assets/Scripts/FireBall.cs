using UnityEngine;
using UnityEngine.VFX;
using Game.Audio;
using Game.Enemy;
using Game.Systems;

namespace Game.Combat
{
    
    /// <summary>火球：对象池复用，飞行命中敌人扣血，超时或碰撞后回收</summary>
    public class Fireball : MonoBehaviour, IPooledObject
    {
        public float speed = 20f;              // 飞行速度
        public float lifetime = 3f;            // 存活时间（秒）
    
        private float timer;                   // 存活计时器
        private ObjectPool pool;               // 所属对象池引用
        private VisualEffect vfx;              // VFX 特效组件
    
        void Awake()
        {
            vfx = GetComponent<VisualEffect>();
        }
    
        /// <summary>对象池激活回调：重置存活计时器并重启 VFX 特效</summary>
        public void OnSpawn()
        {
            timer = 0f;
            if (vfx != null)
            {
                vfx.Reinit();
                vfx.Play();
            }
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
    
        /// <summary>碰撞回调：命中敌人扣血并回收，忽略非敌人的触发器（如磁铁检测器）和玩家自身</summary>
        /// <param name="other">碰撞到的对象</param>
        void OnTriggerEnter(Collider other)
        {
            // 忽略非敌人的触发器（如 MagnetDetector）和玩家自身
            if (other.isTrigger && !other.CompareTag("Enemy"))
                return;
            if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
                return;
    
            // 命中敌人：扣血 + 播放命中音效
            if (other.CompareTag("Enemy"))
            {
                EnemyMovement enemy = other.GetComponent<EnemyMovement>();
                if (enemy != null)
                    enemy.TakeDamage(1f);
                AudioManager.Instance?.PlayFireballHit();
            }
    
            // 无论命中什么，火球都回收
            if (pool != null)
                pool.Despawn(gameObject);
            else
                Destroy(gameObject);
        }
    }
}
