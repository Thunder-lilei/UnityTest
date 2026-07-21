using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class EnemySpawner : MonoBehaviour
{
    public GameObject[] enemyPrefabs;       // 敌人 Prefab 数组（普通/快速/坦克）
    public GameObject bossPrefab;           // Boss Prefab
    public Transform player;                // 玩家 Transform（传递给生成的敌人）
    public int maxCount = 30;               // 最大敌人数
    public float spawnInterval = 0.5f;      // 生成间隔（秒）
    public float spawnMargin = 2f;          // 屏幕外边距
    public GameObject enemyGo;              // 敌人父物体
    public TextMeshProUGUI timerText;       // 计时器 UI

    private Camera mainCamera;             // 主摄像机
    private float timer;                   // 生成计时器
    private List<GameObject> enemies = new List<GameObject>();  // 已生成敌人列表
    private float gameTimer;               // 游戏计时器
    private float bossTimer;               // Boss 计时器
    public float bossInterval = 10f;       // Boss 生成间隔

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
    /// <param name="time">秒数</param>
    /// <returns>格式化时间字符串</returns>
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
        if (spawnPos == Vector3.zero)
            return;

        GameObject boss = Instantiate(bossPrefab, spawnPos, Quaternion.identity, enemyGo.transform);

        EnemyMovement movement = boss.GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.player = player;
            int difficultyLevel = Mathf.FloorToInt(gameTimer / 10f);
            movement.maxHealth = 20f + difficultyLevel * 2f;
        }

        enemies.Add(boss);
    }

    /// <summary>生成普通敌人：随机类型、难度递增血量、受 maxCount 限制</summary>
    void SpawnEnemy()
    {
        enemies.RemoveAll(e => e == null);

        if (enemies.Count >= maxCount)
            return;

        Vector3 spawnPos = GetSpawnPositionOutsideViewport();
        if (spawnPos == Vector3.zero)
            return;

        int difficultyLevel = Mathf.FloorToInt(gameTimer / 10f);

        // 随机选敌人类型，早期只有普通，10秒后加入快速，20秒后加入坦克
        int typeCount = Mathf.Min(3, 1 + Mathf.FloorToInt(gameTimer / 10f));
        int typeIndex = Random.Range(0, typeCount);

        if (typeIndex >= enemyPrefabs.Length)
            typeIndex = 0;

        GameObject prefab = enemyPrefabs[typeIndex];
        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity, enemyGo.transform);

        EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.player = player;
            // 难度递增：在基础血量上叠加
            float healthBonus = difficultyLevel;
            movement.maxHealth = movement.maxHealth + healthBonus;
        }

        enemies.Add(enemy);
    }

    /// <summary>计算视口外边缘的 NavMesh 上的生成位置</summary>
    /// <returns>生成位置，Vector3.zero 表示无效</returns>
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

        // NavMesh 采样失败，返回零向量表示跳过本次生成
        return Vector3.zero;
    }
}
