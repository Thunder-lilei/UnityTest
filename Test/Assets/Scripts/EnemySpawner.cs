using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform player;
    public int maxCount = 30;
    public float spawnInterval = 0.5f;
    public float spawnMargin = 2f;
    public GameObject enemyGo;

    private Camera mainCamera;
    private float timer;
    private List<GameObject> enemies = new List<GameObject>();

    void Start()
    {
        mainCamera = Camera.main;
        timer = 0f;
    }

    void Update()
    {
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

        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, enemyGo.transform);

        EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
        if (movement != null)
            movement.player = player;

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
            if (ray.direction.y == 0)
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
