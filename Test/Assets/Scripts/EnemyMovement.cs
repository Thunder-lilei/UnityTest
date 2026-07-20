using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyMovement : MonoBehaviour
{
    public Transform player;               // 追踪目标（玩家）
    public GameObject pickUpPrefab;        // 死亡掉落的经验方块 Prefab
    public GameObject healthPotionPrefab;  // 死亡掉落的血瓶 Prefab
    public float dropChance = 0.3f;        // 血瓶掉落概率
    public float maxHealth = 2f;           // 最大血量（由 Spawner 设置）
    public GameObject healthBarPrefab;      // 敌人头顶血条 Prefab
    public bool isBoss = false;            // 是否为 Boss

    private NavMeshAgent navMeshAgent;     // NavMesh 代理
    private bool isQuitting;               // 是否正在退出应用
    private float currentHealth;           // 当前血量
    private RectTransform healthFill;      // 血条填充矩形
    private GameObject healthBarInstance;  // 血条实例

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;

        if (healthBarPrefab != null)
        {
            healthBarInstance = Instantiate(healthBarPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity, transform);
            healthBarInstance.SetActive(false);
            healthFill = healthBarInstance.transform.Find("Fill")?.GetComponent<RectTransform>();
        }
    }

    void Update()
    {
        if (player != null)
       {    
           navMeshAgent.SetDestination(player.position);
       }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (healthBarInstance != null && !healthBarInstance.activeSelf)
            healthBarInstance.SetActive(true);

        UpdateHealthBar();

        if (currentHealth <= 0)
            Die();
    }

    void UpdateHealthBar()
    {
        if (healthFill != null)
        {
            float ratio = currentHealth / maxHealth;
            healthFill.anchorMax = new Vector2(ratio, healthFill.anchorMax.y);
        }
    }

    void Die()
    {
        AudioManager.Instance?.PlayEnemyDeath();

        Transform parent = GameObject.Find("PickUp")?.transform;

        int expCount = isBoss ? 3 : 1;
        for (int i = 0; i < expCount; i++)
        {
            if (pickUpPrefab != null)
            {
                Vector3 offset = Random.insideUnitSphere * 0.5f;
                offset.y = 0.5f;
                Instantiate(pickUpPrefab, transform.position + offset, Quaternion.identity, parent);
            }
        }

        if (healthPotionPrefab != null && Random.value <= dropChance)
            Instantiate(healthPotionPrefab, transform.position + Vector3.right * 0.5f + Vector3.up * 0.5f, Quaternion.identity, parent);

        if (healthBarInstance != null)
            Destroy(healthBarInstance);

        Destroy(gameObject);
    }

    void OnApplicationQuit()
    {
        isQuitting = true;
    }

    void OnDestroy()
    {
        if (isQuitting)
            return;
    }
}
