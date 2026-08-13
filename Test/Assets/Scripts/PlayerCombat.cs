using UnityEngine;
using Game.Audio;
using Game.Enemy;
using Game.Combat;
using Game.UI;
using Game.Systems;

namespace Game.Player
{
    
    /// <summary>玩家战斗：火球发射，支持多发扇形分布</summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("火球")]
        public GameObject fireballPrefab;
        public GameObject skill;
        public Camera mainCamera;
        public int fireballCount = 1;
    
        private ObjectPool fireballPool;
        private bool isPaused;
    
        void Start()
        {
            mainCamera = Camera.main;
    
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
    
            if (Input.GetMouseButtonDown(0))
            {
                FireFireball();
            }
        }
    
        /// <summary>朝鼠标指向方向发射火球，多发时扇形分布</summary>
        void FireFireball()
        {
            if (fireballPrefab == null || skill == null || mainCamera == null)
                return;
    
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
                return;
    
            Vector3 direction = hit.point - transform.position;
            direction.y = 0;
            direction.Normalize();
    
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
