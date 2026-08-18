using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

namespace Game.Enemy
{
    
    /// <summary>敌人持续生成器：数据驱动配置，难度递增，Boss 定时生成</summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("敌人配置")]
        [Tooltip("敌人数据配置数组（普通/快速/坦克）")]
        public EnemyData[] enemyConfigs;
    
        [Tooltip("Boss 数据配置")]
        public EnemyData bossConfig;
    
        [Header("生成参数")]
        public Transform player;
        public int maxCount = 100;              // 最大敌人数（初始值，运行时随难度递增）
        public float spawnInterval = 0.3f;       // 生成间隔（初始值，运行时随难度递增）
        public float spawnMargin = 2f;
        public GameObject enemyGo;
        public TextMeshProUGUI timerText;
        public float bossInterval = 30f;        // Boss 生成间隔
    
        private Camera mainCamera;
        private float timer;
        private List<GameObject> enemies = new List<GameObject>();
        private float gameTimer;
        private float bossTimer;
        private float cleanupTimer;             // 敌人列表清理计时器
    
        void Start()
        {
            mainCamera = Camera.main;
            timer = 0f;
            gameTimer = 0f;
            bossTimer = 0f;
            cleanupTimer = 0f;
        }
    
        void Update()
        {
            gameTimer += Time.deltaTime;
    
            if (timerText != null)
                timerText.text = FormatTime(gameTimer);
    
            int difficultyLevel = Mathf.FloorToInt(gameTimer / 10f);
            spawnInterval = Mathf.Max(0.05f, 0.3f - difficultyLevel * 0.015f);
            maxCount = Mathf.Min(500, 100 + difficultyLevel * 20);
    
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
    
            // 降低 List 清理频率（每 2 秒一次，而非每次生成前）
            cleanupTimer += Time.deltaTime;
            if (cleanupTimer >= 2f)
            {
                cleanupTimer = 0f;
                enemies.RemoveAll(e => e == null);
            }
        }
    
        string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    
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
                movement.Initialize(bossConfig, difficultyLevel * 1f);  // Boss HP 增长放缓
            }
    
            enemies.Add(boss);
        }
    
        void SpawnEnemy()
        {
            if (enemies.Count >= maxCount) return;
    
            Vector3 spawnPos = GetSpawnPositionOutsideViewport();
            if (spawnPos == Vector3.zero) return;
    
            int difficultyLevel = Mathf.FloorToInt(gameTimer / 10f);
    
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
