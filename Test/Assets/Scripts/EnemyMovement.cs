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
    private Camera mainCamera;            // 主摄像机缓存

    /// <summary>初始化 NavMeshAgent、血量、头顶血条（高度按缩放调整）</summary>
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
        mainCamera = Camera.main;

        if (healthBarPrefab != null)
        {
            float scale = transform.localScale.y;
            float heightOffset = 1.5f * scale;
            healthBarInstance = Instantiate(healthBarPrefab, transform.position + Vector3.up * heightOffset, Quaternion.identity, transform);
            healthBarInstance.SetActive(false);
            healthFill = healthBarInstance.transform.Find("Fill")?.GetComponent<RectTransform>();
        }
    }

    /// <summary>每帧追踪玩家位置，血条朝向摄像机，屏幕外隐藏血条</summary>
    void Update()
    {
        if (player != null)
        {
            navMeshAgent.SetDestination(player.position);
        }

        // 血条朝向摄像机
        if (healthBarInstance != null && healthBarInstance.activeSelf && mainCamera != null)
        {
            healthBarInstance.transform.LookAt(mainCamera.transform);
        }
    }

    /// <summary>OnBecomeInvisible/OnBecomeVisible 回调：控制血条显示</summary>
    void OnBecameVisible()
    {
        if (healthBarInstance != null && currentHealth < maxHealth)
            healthBarInstance.SetActive(true);
    }

    /// <summary>离开屏幕时隐藏血条</summary>
    void OnBecameInvisible()
    {
        if (healthBarInstance != null)
            healthBarInstance.SetActive(false);
    }

    /// <summary>扣血并显示血条（仅受伤后显示），血量归零时死亡</summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        // 只在受伤后显示血条，且需要模型在屏幕内
        if (healthBarInstance != null && !healthBarInstance.activeSelf)
        {
            bool isVisible = GetComponentInChildren<Renderer>() != null && GetComponentInChildren<Renderer>().isVisible;
            if (isVisible || currentHealth < maxHealth)
                healthBarInstance.SetActive(true);
        }

        UpdateHealthBar();

        if (currentHealth <= 0)
            Die();
    }

    /// <summary>根据当前血量比例更新血条 UI</summary>
    void UpdateHealthBar()
    {
        if (healthFill != null)
        {
            float ratio = currentHealth / maxHealth;
            healthFill.anchorMax = new Vector2(ratio, healthFill.anchorMax.y);
        }
    }

    /// <summary>死亡处理：播放音效、掉落经验和血瓶、销毁血条和自身</summary>
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

    /// <summary>应用退出时设置标志，防止 OnDestroy 误触发掉落</summary>
    void OnApplicationQuit()
    {
        isQuitting = true;
    }

    /// <summary>销毁回调：退出应用时跳过掉落逻辑</summary>
    void OnDestroy()
    {
        if (isQuitting)
            return;
    }
}
