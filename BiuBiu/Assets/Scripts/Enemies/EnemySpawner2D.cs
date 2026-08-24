using System;
using System.Collections.Generic;
using BiuBiu.Core;
using BiuBiu.Data;
using BiuBiu.UI;
using UnityEngine;

namespace BiuBiu.Enemies
{
    /// <summary>
    /// 刷怪器（波次制：1→2→4→8→16...，击杀完当前波次全部敌人才开下一轮）。
    /// - 生成位置：屏幕外环形区；
    /// - 每轮数量 = 2^(轮次-1)，类型从已解锁敌人中随机；
    /// - 精英：3:00 首只，此后每 180s 一只（独立计时不占波次配额）；
    /// - Boss：5:00 首只，此后每 300s 一只（第 n 只血量 30×n）；
    /// - EnemyData SO 数据驱动；配置为空时 Resources 兜底加载。
    /// </summary>
    public class EnemySpawner2D : MonoBehaviour
    {
        [Header("敌人配置（空=Resources 兜底加载）")]
        [Tooltip("基础敌人配置数组（近战扇形 / 远程直线 / 近战横扫；unlockTime 驱动解锁表）")]
        [SerializeField] private EnemyData[] normalEnemies;

        [Tooltip("精英配置（精英）")]
        [SerializeField] private EnemyData eliteData;

        [Tooltip("Boss 配置（Boss）")]
        [SerializeField] private EnemyData bossData;

        [Header("生成参数")]
        [Tooltip("屏幕外生成外扩边距（tile，相对视口矩形）")]
        [SerializeField] private float spawnMargin = 1.5f;

        /// <summary>消息提示事件（HUD toast：开场/轮次/精英·Boss 登场，文案就地维护）</summary>
        public static event Action<string> OnEnemyIntro;
        /// <summary>开场消息缓冲：Start() 可能早于 GameHud 订阅，故缓存供 HUD 启用时补消费，避免丢失。</summary>
        public static string PendingOpeningMessage { get; private set; }
        /// <summary>读取并清空开场消息（供 GameHud 启用时补消费）。</summary>
        public static string ConsumePendingOpeningMessage()
        {
            var msg = PendingOpeningMessage;
            PendingOpeningMessage = null;
            return msg;
        }

        /// <summary>当前波次（HUD 读取；1 起）</summary>
        public static int CurrentWave { get; private set; } = 1;

        // ---- 运行状态 ----
        private int waveNumber;                          // 当前波次（1 起）
        private int waveSpawnCount;                      // 本轮还需生成的数量
        private float waveSpawnTimer;                     // 本轮生成间隔计时
        private readonly List<GameObject> activeNormals = new List<GameObject>(); // 活跃普通敌人
        private float nextEliteTime;                     // 下一只精英生成时刻
        private float nextBossTime;                      // 下一只 Boss 生成时刻
        private int bossCount;                           // 已生成 Boss 数（第 n 只血量 30×n）
        private float waveDelayTimer;                    // 波次间隔等待计时（>0=等待中）

        // ---- 灰盒模板（EnemyData.prefab 为空时兜底；静态缓存跨实例复用） ----
        private static GameObject greyNormalTemplate;
        private static GameObject greyBossTemplate;

        /// <summary>Resources 下数据资产路径前缀（灰盒兜底加载）</summary>
        private const string ResPath = "Data/Enemies/";

        private void Start()
        {
            // 配置兜底：场景未序列化注入时从 Resources 加载（资产由一次性脚本生成于此）
            if (normalEnemies == null || normalEnemies.Length == 0)
            {
                normalEnemies = new[]
                {
                    Resources.Load<EnemyData>(ResPath + "Enemy_Melee"),
                    Resources.Load<EnemyData>(ResPath + "Enemy_Ranged"),
                    Resources.Load<EnemyData>(ResPath + "Enemy_MeleeSweep")
                };
            }
            if (eliteData == null) eliteData = Resources.Load<EnemyData>(ResPath + "Enemy_Elite");
            if (bossData == null) bossData = Resources.Load<EnemyData>(ResPath + "Enemy_Boss");

            // 独立定时器初值（数值文档 5.2/5.3：精英 3:00 首只、Boss 5:00 首只）
            nextEliteTime = GameBalance.EliteFirstSpawnTime;
            nextBossTime = GameBalance.BossFirstSpawnTime;

            // 初始化第一波（1 只，延迟 2s 等开场消息消失后刷出）
            waveNumber = 1;
            CurrentWave = 1;
            waveSpawnCount = 1;
            waveSpawnTimer = 0f;
            waveDelayTimer = 2f; // 开场消息显示 2s 后才开始刷怪
            // 开场消息：缓存而非直接事件触发（Start 可能早于 GameHud 订阅，直接 Invoke 会丢失）
            PendingOpeningMessage = "记得攻击  记得闪避  好了 上吧！";
        }

        private void Update()
        {
            var stats = GameBootstrap.Instance != null ? GameBootstrap.Instance.RunStats : null;
            if (stats == null || !GameBootstrap.Instance.IsRunActive) return;

            float elapsed = stats.ElapsedSeconds;
            int level = stats.DifficultyLevel;
            float dt = Time.deltaTime;

            // ---- 波次刷怪 ----
            CleanInactive();

            if (waveDelayTimer > 0f)
            {
                // 波次间隔等待
                waveDelayTimer -= dt;
            }
            else if (waveSpawnCount > 0)
            {
                // 一次性全部刷出本轮所有敌人
                while (waveSpawnCount > 0)
                {
                    SpawnNormal(elapsed, level);
                    waveSpawnCount--;
                }
            }
            else if (activeNormals.Count == 0)
            {
                // 本轮全部生成且全部被击杀 → 属性微增 → 进入下一轮（翻倍）
                waveNumber++;
                CurrentWave = waveNumber;
                if (stats != null) stats.Wave = waveNumber; // 战报「打到第 N 轮」数据源
                waveSpawnCount = Mathf.RoundToInt(Mathf.Pow(2f, waveNumber - 1)); // 1→2→4→8→16...
                waveDelayTimer = 1f; // 短暂间隔
                // 每轮结束属性微增+回满血+屏幕提示
                ApplyRoundBonus();
            }

            // ---- 精英独立定时（不占波次配额） ----
            if (eliteData != null && elapsed >= nextEliteTime)
            {
                nextEliteTime += GameBalance.EliteSpawnInterval;
                SpawnElite(level);
                OnEnemyIntro?.Invoke("精英来了！");
            }

            // ---- Boss 独立定时（不占波次配额；每只都提示——一只比一只强） ----
            if (bossData != null && elapsed >= nextBossTime)
            {
                nextBossTime += GameBalance.BossSpawnInterval;
                SpawnBoss();
                OnEnemyIntro?.Invoke("Boss 登场！！");
            }
        }

        /// <summary>每轮结束属性微增：移速+0.1，攻击力+0.1，屏幕右侧提示</summary>
        private void ApplyRoundBonus()
        {
            var stats = GameBootstrap.Instance != null ? GameBootstrap.Instance.PlayerStats : null;
            if (stats != null)
            {
                stats.MoveSpeedMult += 0.5f / GameBalance.PlayerMoveSpeed; // 移速绝对值+0.5
                stats.AttackBonusFloat += 0.5f; // 攻击力浮点加成+0.5
            }
            // 每轮结束回满血
            var player = GameBootstrap.Instance != null ? GameBootstrap.Instance.GetPlayer() : null;
            if (player != null)
            {
                int maxHp = stats != null ? stats.MaxHealth : GameBalance.PlayerMaxHealth;
                player.Heal(maxHp); // 回满
            }
            OnEnemyIntro?.Invoke($"第 {waveNumber} 轮！  我感觉好像强了一点点！");

            // 主角每轮变强头顶气泡（设计文档 14.x）
            if (player != null)
                SpeechBubbleManager.Say(player.transform, SpeakerType.Player, SpeechEvent.RoundUp);
        }

        /// <summary>生成一只普通敌人（已解锁类型中随机）</summary>
        private void SpawnNormal(float elapsed, int level)
        {
            if (normalEnemies == null || normalEnemies.Length == 0) return;

            // 解锁表过滤（数值文档 5.1：0:00 / 1:00 / 2:00）
            EnemyData picked = null;
            int unlocked = 0;
            foreach (var e in normalEnemies)
            {
                if (e == null || elapsed < e.unlockTime) continue;
                unlocked++;
                // 等权随机：蓄水池抽样（单遍扫描）
                if (UnityEngine.Random.value < 1f / unlocked) picked = e;
            }
            if (picked == null) return;

            // 三类基础敌人开局混合出现（数值文档 v2.4：解锁表归零），新敌登场提示不再触发

            var go = SpawnOne(picked, GreyNormalTemplate);
            go.GetComponent<EnemyBase2D>().Initialize(picked, level);
            activeNormals.Add(go);
        }

        /// <summary>生成精英（独立通道；血量成长公式更陡）</summary>
        private void SpawnElite(int level)
        {
            var go = SpawnOne(eliteData, GreyNormalTemplate);
            go.GetComponent<EnemyBase2D>().Initialize(eliteData, level);
            // 精英不计入普通上限（数值文档 6.1）
        }

        /// <summary>生成 Boss（第 n 只血量 30×n）</summary>
        private void SpawnBoss()
        {
            bossCount++;
            var go = SpawnOne(bossData, GreyBossTemplate);
            go.GetComponent<EnemyBoss2D>().InitializeBoss(bossData, bossCount);
            // Boss 不计入普通上限
        }

        /// <summary>生成通用：prefab 优先（素材版），空则灰盒模板；屏幕外取点 + 池化</summary>
        private GameObject SpawnOne(EnemyData data, GameObject greyTemplate)
        {
            GameObject prefab = data.prefab != null ? data.prefab : greyTemplate;
            Vector2 pos = GetSpawnPosOutsideScreen();
            var go = ObjectPool.Get(prefab, pos, Quaternion.identity);
            go.name = data.displayName; // Hierarchy 可读（池复用每次刷新）
            return go;
        }

        /// <summary>清理列表中已回池/已死亡的实例（尸体停留至全局上限后渐隐回池，不计入活跃计数）</summary>
        private void CleanInactive()
        {
            activeNormals.RemoveAll(e =>
                e == null || !e.activeInHierarchy || !e.GetComponent<EnemyBase2D>().enabled);
        }

        /// <summary>
        /// 屏幕外环形区取生成点（视口四角 → 世界矩形 → 外扩边距 → 随机选边），
        /// 再钳制到地图有效区（80×80 减边界墙；玩家贴墙时可能屏内边缘生成，可接受）。
        /// 障碍避让：生成点落在障碍内时重新取点（最多 5 次，失败放弃本次生成）。
        /// </summary>
        private Vector2 GetSpawnPosOutsideScreen()
        {
            Camera cam = Camera.main;
            if (cam == null) return Vector2.zero;

            float cMin = GameBalance.BorderWallThickness + 1f;
            float cMax = GameBalance.MapSizeTiles - GameBalance.BorderWallThickness - 1f;

            for (int attempt = 0; attempt < GameBalance.SpawnRelocateMaxAttempts; attempt++)
            {
                Vector3 min = cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
                Vector3 max = cam.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));
                float minX = min.x - spawnMargin, maxX = max.x + spawnMargin;
                float minY = min.y - spawnMargin, maxY = max.y + spawnMargin;

                Vector2 pos;
                int edge = UnityEngine.Random.Range(0, 4);
                switch (edge)
                {
                    case 0: pos = new Vector2(UnityEngine.Random.Range(minX, maxX), minY); break;
                    case 1: pos = new Vector2(UnityEngine.Random.Range(minX, maxX), maxY); break;
                    case 2: pos = new Vector2(minX, UnityEngine.Random.Range(minY, maxY)); break;
                    default: pos = new Vector2(maxX, UnityEngine.Random.Range(minY, maxY)); break;
                }

                pos.x = Mathf.Clamp(pos.x, cMin, cMax);
                pos.y = Mathf.Clamp(pos.y, cMin, cMax);

                // 障碍避让：检查是否落在任何障碍碰撞体内
                if (!IsInsideObstacle(pos)) return pos;
            }

            // 全部尝试失败：返回最后一个位置（可能落在障碍边缘，敌人物理滑出）
            Vector3 fmin = cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
            Vector3 fmax = cam.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));
            Vector2 fallback = new Vector2(
                Mathf.Clamp(UnityEngine.Random.Range(fmin.x, fmax.x), cMin, cMax),
                Mathf.Clamp(UnityEngine.Random.Range(fmin.y, fmax.y), cMin, cMax));
            return fallback;
        }

        /// <summary>检查位置是否落在任何障碍墙碰撞体内（用 Physics2D 点检测）</summary>
        private static bool IsInsideObstacle(Vector2 pos)
        {
            // 用 OverlapBox 查障碍层（障碍墙带 BoxCollider2D，与边界墙同一默认层）
            var hit = Physics2D.OverlapPoint(pos);
            return hit != null && hit is BoxCollider2D;
        }

        // ==================== 灰盒模板 ====================

        /// <summary>普通敌人灰盒模板（Sprite+Rigidbody+Collider+EnemyBase2D；视觉在 Initialize 染色）</summary>
        private static GameObject GreyNormalTemplate
        {
            get
            {
                if (greyNormalTemplate == null)
                {
                    greyNormalTemplate = BuildGreyEnemy<EnemyBase2D>("GreyEnemyTemplate");
                }
                return greyNormalTemplate;
            }
        }

        /// <summary>Boss 灰盒模板（挂 EnemyBoss2D）</summary>
        private static GameObject GreyBossTemplate
        {
            get
            {
                if (greyBossTemplate == null)
                {
                    greyBossTemplate = BuildGreyEnemy<EnemyBoss2D>("GreyBossTemplate");
                }
                return greyBossTemplate;
            }
        }

        /// <summary>构建灰盒敌人模板（物理分离所需组件齐备；静态缓存仅一份）</summary>
        private static GameObject BuildGreyEnemy<T>(string name) where T : EnemyBase2D
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GreyBoxFactory.Square; // 白方（Initialize 时按类型染色+体型缩放）
            go.AddComponent<T>();
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.4f; // 本地半径（乘 localScale≈体型的 40%）
            // 物理材质：零摩擦+全弹跳，防卡墙（撞墙自然沿切向滑动）
            var pm = new UnityEngine.PhysicsMaterial2D("EnemyNoFriction") { friction = 0f, bounciness = 0f };
            col.sharedMaterial = pm;
            go.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(go);
            return go;
        }
    }
}
