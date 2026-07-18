using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    public GameObject[] enemyPrefabs;       // 敌人 Prefab 数组（普通/快速/坦克）
    public Transform player;                // 玩家 Transform（传递给生成的敌人）
    public int maxCount = 30;               // 最大敌人数
    public float spawnInterval = 0.5f;      // 生成间隔（秒）
    public float spawnMargin = 2f;          // 屏幕外边距
    public GameObject enemyGo;              // 敌人父物体

    private Camera mainCamera;             // 主摄像机
    private float timer;                   // 生成计时器
    private List<GameObject> enemies = new List<GameObject>();  // 已生成敌人列表
    private float gameTimer;               // 游戏计时器

    void Start()
    {
        mainCamera = Camera.main;
        timer = 0f;
        gameTimer = 0f;
    }

    void Update()
    {
        gameTimer += Time.deltaTime;

        int difficultyLevel = Mathf.FloorToInt(gameTimer / 10f);
        spawnInterval = Mathf.Max(0.15f, 0.5f - difficultyLevel * 0.02f);
        maxCount = Mathf.Min(60, 30 + difficultyLevel * 2);

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnEnemy();
        }
    }

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

        return spawnPos;
    }
}
