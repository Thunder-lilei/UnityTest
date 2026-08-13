using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;
using Game.Audio;
using Game.Player;
using Game.Combat;
using Game.UI;
using Game.Systems;

namespace Game.Enemy
{
    
    /// <summary>敌人持续生成器：数据驱动配置，难度递增，Boss 定时生成</summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("敌人配置")]
        [Tooltip("敌人数据配置数组（普通/快速/坦克）")]
        public EnemyData[] enemyConfigs;       // 敌人配置（ScriptableObject）
    
        [Tooltip("Boss 数据配置")]
        public EnemyData bossConfig;            // Boss 配置
    
        [Header("生成参数")]
        public Transform player;                // 玩家 Transform（传递给生成的敌人）
        public int maxCount = 30;               // 最大敌人数
        public float spawnInterval = 0.5f;      // 生成间隔（秒）
        public float spawnMargin = 2f;          // 屏幕外边距
        public GameObject enemyGo;              // 敌人父物体
        public TextMeshProUGUI timerText;       // 计时器 UI
        public float bossInterval = 10f;       // Boss 生成间隔
    
        private Camera mainCamera;
        private float timer;
        private List<GameObject> enemies = new List<GameObject>();
        private float gameTimer;
        private float bossTimer;
    
        /// <summary>初始化相机和计时器</summary>
        void Start()
        {
            mainCamera = Camera.main;
            timer = 0f;
            gameTimer = 0f;
            bossTimer = 0f;
        }
    
        /// <summary>每帧更新计时器 UI、难度递增参数、普通敌人生成、Boss 生成</summary>
        void Update()
        {
            gameTimer += Time.deltaTime;
    
            if (timerText != null)
                timerText.text = FormatTime(gameTimer);
    
            int difficultyLevel = Mathf.FloorToInt(gameTimer / 10f);
            spawnInterval = Mathf.Max(0.15f, 0.5f - difficultyLevel * 0.02f);
            maxCount = Mathf.Min(60, 30 + difficultyLevel * 2);
    
            timer += Time.deltaTime;
    
            if (timer >= spawnInterval)
            {
                timer = 0f;
                SpawnEnemy();
            }
    
            bossTimer += Time.deltaTime;
            if (bossTimer >= bossInterval)
            {
                bossTimer = 0f;
                SpawnBoss();
            }
        }
    
        /// <summary>格式化游戏时间为 mm:ss</summary>
        string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    
        /// <summary>生成 Boss：从屏幕外刷新，不受 maxCount 限制</summary>
        void SpawnBoss()
        {
            Vector3 spawnPos = GetSpawnPositionOutsideViewport();
            if (spawnPos == Vector3.zero) return;
    
            if (bossConfig == null || bossConfig.prefab == null) return;
    
            GameObject boss = Instantiate(bossConfig.prefab, spawnPos, Quaternion.identity, enemyGo.transform);
    
            EnemyMovement movement = boss.GetComponent<EnemyMovement>();
            if (movement != null)
            {
                movement.player = player;
                int difficultyLevel = Mathf.FloorToInt(gameTimer / 10f);
                movement.Initialize(bossConfig, difficultyLevel * 2f);
            }
    
            enemies.Add(boss);
        }
    
        /// <summary>生成普通敌人：随机类型、难度递增血量、受 maxCount 限制</summary>
        void SpawnEnemy()
        {
            enemies.RemoveAll(e => e == null);
    
            if (enemies.Count >= maxCount) return;
    
            Vector3 spawnPos = GetSpawnPositionOutsideViewport();
            if (spawnPos == Vector3.zero) return;
    
            int difficultyLevel = Mathf.FloorToInt(gameTimer / 10f);
    
            // 随机选敌人类型，早期只有普通，10秒后加入快速，20秒后加入坦克
            int typeCount = Mathf.Min(enemyConfigs.Length, 1 + Mathf.FloorToInt(gameTimer / 10f));
            int typeIndex = Random.Range(0, typeCount);
    
            var config = enemyConfigs[typeIndex];
            if (config == null || config.prefab == null) return;
    
            GameObject enemy = Instantiate(config.prefab, spawnPos, Quaternion.identity, enemyGo.transform);
    
            EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
            if (movement != null)
            {
                movement.player = player;
                movement.Initialize(config, difficultyLevel);
            }
    
            enemies.Add(enemy);
        }
    
        /// <summary>计算视口外边缘的 NavMesh 上的生成位置</summary>
        Vector3 GetSpawnPositionOutsideViewport()
        {
            Vector3[] viewportCorners = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(0, 1, 0),
                new Vector3(1, 1, 0)
            };
    
            Vector3[] worldCorners = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                Ray ray = mainCamera.ViewportPointToRay(viewportCorners[i]);
                if (Mathf.Abs(ray.direction.y) < 0.001f)
                    return Vector3.zero;
                float t = -ray.origin.y / ray.direction.y;
                worldCorners[i] = ray.origin + ray.direction * t;
            }
    
            float minX = Mathf.Min(worldCorners[0].x, worldCorners[1].x, worldCorners[2].x, worldCorners[3].x) - spawnMargin;
            float maxX = Mathf.Max(worldCorners[0].x, worldCorners[1].x, worldCorners[2].x, worldCorners[3].x) + spawnMargin;
            float minZ = Mathf.Min(worldCorners[0].z, worldCorners[1].z, worldCorners[2].z, worldCorners[3].z) - spawnMargin;
            float maxZ = Mathf.Max(worldCorners[0].z, worldCorners[1].z, worldCorners[2].z, worldCorners[3].z) + spawnMargin;
    
            Vector3 spawnPos;
            int edge = Random.Range(0, 4);
            switch (edge)
            {
                case 0:
                    spawnPos = new Vector3(Random.Range(minX, maxX), 0, minZ);
                    break;
                case 1:
                    spawnPos = new Vector3(Random.Range(minX, maxX), 0, maxZ);
                    break;
                case 2:
                    spawnPos = new Vector3(minX, 0, Random.Range(minZ, maxZ));
                    break;
                default:
                    spawnPos = new Vector3(maxX, 0, Random.Range(minZ, maxZ));
                    break;
            }
    
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                return hit.position;
    
            return Vector3.zero;
        }
    }
    
}
