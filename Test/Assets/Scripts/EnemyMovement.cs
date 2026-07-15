using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public Transform player;
    public GameObject pickUpPrefab;

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
        if (isQuitting || pickUpPrefab == null)
            return;

        Transform parent = GameObject.Find("PickUp")?.transform;
        Instantiate(pickUpPrefab, transform.position, Quaternion.identity, parent);
    }
}
