using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public Transform player;
    public GameObject pickUpPrefab;
    public GameObject healthPotionPrefab;
    public float dropChance = 0.3f;

    private NavMeshAgent navMeshAgent;
    private bool isQuitting;

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
