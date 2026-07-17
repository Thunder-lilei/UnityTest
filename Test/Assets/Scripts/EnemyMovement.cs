using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public Transform player;               // 追踪目标（玩家）
    public GameObject pickUpPrefab;        // 死亡掉落的经验方块 Prefab
    public GameObject healthPotionPrefab;  // 死亡掉落的血瓶 Prefab
    public float dropChance = 0.3f;        // 血瓶掉落概率

    private NavMeshAgent navMeshAgent;     // NavMesh 代理
    private bool isQuitting;               // 是否正在退出应用

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (player != null)
       {    
           navMeshAgent.SetDestination(player.position);
       }
    }

    void OnApplicationQuit()
    {
        isQuitting = true;
    }

    void OnDestroy()
    {
        if (isQuitting)
            return;

        Transform parent = GameObject.Find("PickUp")?.transform;

        if (pickUpPrefab != null)
            Instantiate(pickUpPrefab, transform.position + Vector3.left * 0.5f + Vector3.up * 0.5f, Quaternion.identity, parent);

        if (healthPotionPrefab != null && Random.value <= dropChance)
            Instantiate(healthPotionPrefab, transform.position + Vector3.right * 0.5f + Vector3.up * 0.5f, Quaternion.identity, parent);
    }
}
