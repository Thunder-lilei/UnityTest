using UnityEngine;
using UnityEngine.AI;
using Game.Audio;
using Game.Combat;

namespace Game.Enemy
{
    
    /// <summary>敌人 AI：NavMesh 追踪 + 受击 + 死亡掉落 + 溶解特效</summary>
    public class EnemyMovement : MonoBehaviour
    {
        [Header("引用")]
        public Transform player;               // 追踪目标（玩家）
        public GameObject pickUpPrefab;        // 死亡掉落的经验方块 Prefab
        public GameObject healthPotionPrefab;  // 死亡掉落的血瓶 Prefab
        public GameObject healthBarPrefab;      // 敌人头顶血条 Prefab
    
        [Header("运行时属性（由 EnemyData 配置，Spawner 初始化）")]
        public float maxHealth = 2f;           // 最大血量
        public float dropChance = 0.3f;        // 血瓶掉落概率
        public bool isBoss = false;            // 是否为 Boss
        public int expDrop = 1;               // 经验掉落数量
        public EnemyData enemyData;            // 数据配置引用（调试用）
    
        private NavMeshAgent navMeshAgent;
        private Animator animator;
        private float currentHealth;
        private RectTransform healthFill;
        private GameObject healthBarInstance;
        private Camera mainCamera;
    
        /// <summary>由 Spawner 调用：从 ScriptableObject 配置初始化敌人属性</summary>
        /// <param name="data">敌人数据配置</param>
        /// <param name="healthBonus">难度递增血量加成</param>
        public void Initialize(EnemyData data, float healthBonus)
        {
            enemyData = data;
            isBoss = data.isBoss;
            maxHealth = data.maxHealth + healthBonus;
            dropChance = data.dropChance;
            expDrop = data.expDrop;
    
            // 应用外观和物理设置
            transform.localScale = Vector3.one * data.scale;
    
            var agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.speed = data.moveSpeed;
        }
    
        /// <summary>初始化 NavMeshAgent、血量、头顶血条（高度按缩放调整）</summary>
        void Start()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            currentHealth = maxHealth;
            mainCamera = Camera.main;
    
            // 修复 Prefab Variant 导致的 rootBone 丢失：确保 SkinnedMeshRenderer 有 rootBone
            var armature = transform.Find("CharacterArmature");
            if (armature != null)
            {
                var root = armature.Find("Root");
                if (root != null)
                {
                    foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        if (smr.rootBone == null)
                            smr.rootBone = root;
                    }
                }
            }
    
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
            if (player != null && navMeshAgent != null && navMeshAgent.isOnNavMesh && navMeshAgent.enabled)
            {
                navMeshAgent.SetDestination(player.position);
            }
    
            if (animator != null)
            {
                animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
            }
    
            if (healthBarInstance != null && healthBarInstance.activeSelf && mainCamera != null)
            {
                healthBarInstance.transform.LookAt(mainCamera.transform);
            }
        }
    
        void OnBecameVisible()
        {
            if (healthBarInstance != null && currentHealth < maxHealth)
                healthBarInstance.SetActive(true);
        }
    
        void OnBecameInvisible()
        {
            if (healthBarInstance != null)
                healthBarInstance.SetActive(false);
        }
    
        /// <summary>扣血并显示血条，血量归零时死亡</summary>
        public void TakeDamage(float damage)
        {
            currentHealth -= damage;
    
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
    
        void UpdateHealthBar()
        {
            if (healthFill != null)
            {
                float ratio = currentHealth / maxHealth;
                healthFill.anchorMax = new Vector2(ratio, healthFill.anchorMax.y);
            }
        }
    
        /// <summary>死亡处理：播放音效、掉落经验和血瓶、溶解特效</summary>
        void Die()
        {
            AudioManager.Instance?.PlayEnemyDeath();
    
            Transform parent = GameObject.Find("PickUp")?.transform;
    
            for (int i = 0; i < expDrop; i++)
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
    
            // 触发溶解特效（替换直接销毁）
            var dissolve = GetComponent<DissolveEffect>();
            if (dissolve != null && dissolve.dissolveMaterial != null)
                dissolve.StartDissolve();
            else
                Destroy(gameObject);
        }
    

    }
    
}
