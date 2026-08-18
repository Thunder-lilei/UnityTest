using UnityEngine;
using Game.Audio;
using Game.Combat;
using Game.Systems;

namespace Game.Player
{
    
    /// <summary>玩家战斗：自动定时发射火球，锁定最近敌人</summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("火球")]
        public GameObject fireballPrefab;
        public GameObject skill;
        public int fireballCount = 1;

        [Header("自动发射")]
        [Tooltip("发射间隔（秒）")]
        public float fireInterval = 1.0f;
        [Tooltip("索敌检测半径")]
        public float detectRadius = 15f;
        [Tooltip("最小发射间隔（升级下限）")]
        public float minFireInterval = 0.1f;

        private ObjectPool fireballPool;
        private bool isPaused;
        private float fireTimer;

        void Start()
        {
            // 创建火球对象池
            if (fireballPrefab != null && skill != null)
            {
                fireballPool = CreatePool(fireballPrefab, skill.transform, 5);
                fireballPrefab.GetComponent<Fireball>()?.SetPool(fireballPool);
            }
        }

        void Update()
        {
            if (isPaused) return;

            fireTimer += Time.deltaTime;
            if (fireTimer >= fireInterval)
            {
                fireTimer = 0f;
                FireFireball();
            }
        }

        /// <summary>自动发射火球：锁定最近敌人，无敌人时朝面朝方向</summary>
        void FireFireball()
        {
            if (fireballPrefab == null || skill == null || fireballPool == null)
                return;

            Vector3 direction = FindFireDirection();

            for (int i = 0; i < fireballCount; i++)
            {
                float angle = 0f;
                if (fireballCount > 1)
                    angle = Mathf.Lerp(-15f, 15f, i / (float)(fireballCount - 1));

                Vector3 dir = Quaternion.Euler(0, angle, 0) * direction;
                Vector3 spawnPos = transform.position + Vector3.up + dir;
                fireballPool.Spawn(spawnPos, Quaternion.LookRotation(dir));
            }
            AudioManager.Instance?.PlayFireballLaunch();
        }

        /// <summary>
        /// 计算发射方向：优先锁定检测范围内最近敌人，无敌人时朝面朝方向
        /// 使用 Physics.OverlapSphere 碰撞体检测，避免全场景遍历
        /// </summary>
        Vector3 FindFireDirection()
        {
            Vector3 myPos = transform.position;
            Collider[] hits = Physics.OverlapSphere(myPos, detectRadius);

            Transform nearest = null;
            float minDistSqr = float.MaxValue;

            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Enemy")) continue;
                if (hit.transform == transform) continue;

                float distSqr = (hit.transform.position - myPos).sqrMagnitude;
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    nearest = hit.transform;
                }
            }

            if (nearest != null)
            {
                Vector3 dir = nearest.position - myPos;
                dir.y = 0;
                dir.Normalize();
                return dir;
            }

            // 无敌人时朝主角面朝方向
            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();
            return forward;
        }

        /// <summary>升级缩短发射间隔（每次 -0.1s，下限 minFireInterval）</summary>
        public void ReduceFireInterval(float amount)
        {
            fireInterval = Mathf.Max(minFireInterval, fireInterval - amount);
        }

        /// <summary>设置暂停状态（升级选择时调用）</summary>
        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }

        ObjectPool CreatePool(GameObject prefab, Transform parent, int size)
        {
            var poolGo = new GameObject(prefab.name + "_Pool");
            poolGo.transform.SetParent(parent, false);
            var pool = poolGo.AddComponent<ObjectPool>();
            pool.prefab = prefab;
            pool.initialSize = size;
            pool.parent = parent;
            return pool;
        }
    }
    
}
