using BiuBiu.Core;
using BiuBiu.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BiuBiu.Player
{
    /// <summary>
    /// 主角控制器（数值文档第 3 章；数值接 GameBootstrap.PlayerStats——每轮微增即时生效）。
    /// 移动：Input System（Move=WASD），移速 = 基础 4 tile/s × 每轮微增乘数。
    /// 翻滚：Roll 动作（默认空格，支持改键）；位移 = 基础 2.5 tile / 周期 0.40s / 前 0.30s 无敌；
    /// 无冷却（动作周期自然限频）；位移曲线缓入缓出（SmoothStep）。
    /// 受击：IDamageable——2 心起步 / 1 伤每次 / 受击无敌 1.0s（与闪白同步）+ 强震屏。
    /// 死亡（正式流程，设计文档 15 章）：慢动作 0.2×1.5s（unscaled 计时）→ 镜头聚焦（正交 ×0.6）
    /// → GameBootstrap.EndRun 结算 → DeathPanel 战报（再战/回到标题）。
    /// </summary>
    public class PlayerController : MonoBehaviour, IDamageable
    {
        [Header("引用（场景注入 / RuntimeSceneBuilder 运行时注入）")]
        [Tooltip("输入资产（PlayerControls.inputactions）；M1-9 起由场景装配器注入，公开供运行时赋值")]
        public InputActionAsset actions;

        private SpriteRenderer sr;          // 本体渲染（翻滚半透明闪烁/死亡隐藏）
        private HitFlash hitFlash;          // 受击闪白（M0-3）
        private CameraTrauma trauma;        // 震屏（M0-5）
        private Camera mainCam;             // 主相机（死亡镜头聚焦）
        private InputAction moveAction;     // Move：Vector2
        private InputAction rollAction;     // Roll：Button（支持改键后的绑定）

        private int health;                 // 当前血量
        private float invulnTimer;          // 受击无敌计时（>0 = 无敌中）
        private float rollTimer;            // 翻滚计时（>0 = 翻滚中）
        private Vector2 rollDir;            // 翻滚方向（单位向量）
        private Vector2 lastMoveDir = Vector2.down; // 上一次移动方向（无输入时翻滚的兜底方向）

        // ---- 翻滚残影（M2：位置残影序列替代 alpha 闪烁）----
        private float afterimageTimer;
        private static GameObject afterimageRoot;

        // ---- 死亡演出（数值文档 9 章） ----
        private bool dead;
        private bool deathFinished;         // 结算只跑一次（否则 Update 每帧重复 EndRun，存档累计被刷爆）
        private float deathTimer;           // 慢动作剩余时长（unscaled）
        private float deathZoomLerp;        // 镜头聚焦插值进度
        private float camOriginalSize;      // 死亡前相机正交尺寸（恢复用）

        /// <summary>当前血量（HUD 红心读取）</summary>
        public int CurrentHealth => health;

        /// <summary>是否已死亡</summary>
        public bool IsDead => dead;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            hitFlash = GetComponent<HitFlash>();
            mainCam = Camera.main;
            trauma = mainCam != null ? mainCam.GetComponent<CameraTrauma>() : null;
            health = GameBalance.PlayerMaxHealth;

            moveAction = actions != null ? actions.FindAction("Move") : null;
            rollAction = actions != null ? actions.FindAction("Roll") : null;
        }

        private void OnEnable()
        {
            if (moveAction != null) moveAction.Enable();
            if (rollAction != null) rollAction.Enable();
        }

        /// <summary>触发器碰撞：与敌人接触受伤（翻滚无敌/受击无敌期免疫）</summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (dead) return;
            // 翻滚无敌期
            if (rollTimer > (GameBalance.RollDuration - GameBalance.RollInvulnTime)) return;
            // 受击无敌期
            if (invulnTimer > 0f) return;

            var enemy = other.GetComponentInParent<Enemies.EnemyBase2D>();
            if (enemy != null)
            {
                TakeDamage(GameBalance.EnemyDamageToPlayer);
            }
        }

        private void OnDisable()
        {
            if (moveAction != null) moveAction.Disable();
            if (rollAction != null) rollAction.Disable();
        }

        private void Update()
        {
            // 域重载自愈：Play 中脚本热重载会清空普通 C# 对象引用（InputAction 不存活）
            // → actions 资产引用是 UnityEngine.Object 可存活，动作引用丢失时重新查找并补启用
            // （运行时注入晚于 Awake 的场景同样靠这里补启用——Enable 幂等，重复调用安全）
            if (actions != null && (moveAction == null || rollAction == null))
            {
                if (moveAction == null)
                {
                    moveAction = actions.FindAction("Move");
                    moveAction?.Enable();
                }
                if (rollAction == null)
                {
                    rollAction = actions.FindAction("Roll");
                    rollAction?.Enable();
                }
            }

            // ---- 死亡演出：慢动作 → 镜头聚焦 → 结算+战报 ----
            if (dead)
            {
                deathTimer -= Time.unscaledDeltaTime; // 慢动作下正常计时（timeScale 被压低）
                deathZoomLerp = Mathf.MoveTowards(deathZoomLerp, 1f, Time.unscaledDeltaTime / GameBalance.DeathSlowmoDuration);
                if (mainCam != null)
                {
                    // 镜头聚焦：正交尺寸缩至 0.6×（数值文档 9 章 DeathZoomScale）
                    mainCam.orthographicSize = Mathf.Lerp(camOriginalSize,
                        camOriginalSize * GameBalance.DeathZoomScale, deathZoomLerp);
                }
                if (deathTimer <= 0f && !deathFinished)
                {
                    deathFinished = true;
                    FinishDeath();
                }
                return;
            }

            // ---- 面板打开时锁玩法输入（暂停/升级/战报；timeScale=0 已停位移，这里挡翻滚触发等） ----
            if (GameState.InputLocked) return;

            // ---- 计时器推进 ----
            if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;

            var stats = GameBootstrap.Instance != null ? GameBootstrap.Instance.PlayerStats : null;

            // ---- 翻滚 ----
            if (rollTimer > 0f)
            {
                // 缓入缓出位移：进度 = 1 - 剩余时间比例（rollTimer 递减 → 进度从 0 递增到 1）
                float rollDist = stats != null ? stats.RollDistance : GameBalance.RollDistance;
                float rollDur = GameBalance.RollDuration;
                float remainPrev = Mathf.Clamp01((rollTimer + Time.deltaTime) / rollDur);
                rollTimer -= Time.deltaTime;
                float remainNow = Mathf.Clamp01(rollTimer / rollDur);
                float sPrev = Mathf.SmoothStep(0f, 1f, 1f - remainPrev);
                float sNow = Mathf.SmoothStep(0f, 1f, 1f - remainNow);
                float delta = (sNow - sPrev) * rollDist; // 本帧位移量（恒为正）
                // 翻滚位移也做碰撞检测（分轴推进，防卡进墙）
                Vector3 rollPos = transform.position;
                Vector3 rollDelta = (Vector3)(rollDir * delta);
                rollPos.x += rollDelta.x;
                if (IsBlocked(rollPos)) rollPos.x -= rollDelta.x;
                rollPos.y += rollDelta.y;
                if (IsBlocked(rollPos)) rollPos.y -= rollDelta.y;
                transform.position = rollPos;

                // 翻滚残影：按间隔留位置残影（M2 替代旧 alpha 闪烁）
                afterimageTimer -= Time.deltaTime;
                if (afterimageTimer <= 0f)
                {
                    SpawnAfterimage();
                    afterimageTimer = GameBalance.RollAfterimageInterval;
                }

                if (rollTimer <= 0f) // 翻滚结束
                {
                    // 恢复透明度
                    if (sr != null)
                    {
                        Color end = sr.color;
                        end.a = 1f;
                        sr.color = end;
                    }
                }
                return; // 翻滚期间锁移动输入
            }

            // ---- 蓄力时冻结常规移动（由 SlingWeapon 拉拽控制位置） ----
            if (Weapons.SlingWeapon.IsCharging) return;

            // ---- 常规移动（移速 = 基础 × 每轮微增乘数） ----
            float moveSpeed = stats != null ? stats.CurrentMoveSpeed : GameBalance.PlayerMoveSpeed;
            Vector2 move = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            if (move.sqrMagnitude > 0.01f)
            {
                lastMoveDir = move.normalized;
                Vector3 delta = (Vector3)(move.normalized * (moveSpeed * Time.deltaTime));
                // 碰撞检测：分轴推进，遇障碍墙/边界墙阻挡
                Vector3 pos = transform.position;
                // X 轴
                pos.x += delta.x;
                if (IsBlocked(pos)) pos.x -= delta.x;
                // Y 轴
                pos.y += delta.y;
                if (IsBlocked(pos)) pos.y -= delta.y;
                transform.position = pos;
            }

            // ---- 翻滚触发（周期结束才可再次触发=自然限频） ----
            if (rollAction != null && rollAction.WasPerformedThisFrame())
            {
                rollTimer = GameBalance.RollDuration;
                rollDir = move.sqrMagnitude > 0.01f ? move.normalized : lastMoveDir;
            }

            // ---- 开发者模式无敌开关（M0-8；正式版移入设置界面） ----
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
            {
                DeveloperMode.GodMode = !DeveloperMode.GodMode;
                Debug.Log($"[DevMode] 开发者模式无敌：{(DeveloperMode.GodMode ? "开" : "关")}");
            }
        }

        /// <summary>碰撞检测：玩家圆形与障碍墙/边界墙的 BoxCollider2D 重叠判定</summary>
        private bool IsBlocked(Vector3 pos)
        {
            float r = GameBalance.PlayerCollisionRadius;
            var hit = Physics2D.OverlapCircle(pos, r);
            return hit != null && hit is BoxCollider2D;
        }

        /// <summary>回复治疗（每轮结束回满血调用；EnemySpawner2D.ApplyRoundBonus）</summary>
        public void Heal(int amount)
        {
            if (dead || amount <= 0) return;
            var stats = GameBootstrap.Instance != null ? GameBootstrap.Instance.PlayerStats : null;
            int maxHp = stats != null ? stats.MaxHealth : GameBalance.PlayerMaxHealth;
            health = Mathf.Min(health + amount, maxHp);
        }

        /// <summary>IDamageable：受击=掉血+闪白+无敌+强震；开发者无敌/翻滚无敌期/受击无敌期免疫</summary>
        public void TakeDamage(int amount)
        {
            if (DeveloperMode.GodMode) return; // 开发者模式：入口拦截
            if (dead || amount <= 0) return;
            if (rollTimer > (GameBalance.RollDuration - GameBalance.RollInvulnTime)) return; // 翻滚前 0.30s 无敌帧
            if (invulnTimer > 0f) return;                                                     // 受击无敌期

            var stats = GameBootstrap.Instance != null ? GameBootstrap.Instance.PlayerStats : null;
            float invuln = stats != null ? stats.InvulnDuration : GameBalance.PlayerInvulnDuration;

            health -= amount;
            hitFlash.PlayFlash(invuln); // 受击闪白与无敌同步
            invulnTimer = invuln;
            if (trauma != null) trauma.AddTrauma(GameBalance.TraumaPlayerHit); // 受击强震

            if (health <= 0) BeginDeath();
        }

        // ==================== 死亡流程（设计文档 15 章；数值文档 9 章） ====================

        /// <summary>死亡开始：本体隐藏+慢动作 0.2×（1.5s）+镜头聚焦</summary>
        private void BeginDeath()
        {
            dead = true;
            // 隐藏本体与所有子 Renderer
            var renderers = GetComponents<Renderer>();
            foreach (var r in renderers) r.enabled = false;
            var childRenderers = GetComponentsInChildren<Renderer>();
            foreach (var r in childRenderers) r.enabled = false;
            Time.timeScale = GameBalance.DeathSlowmoScale; // 慢动作 0.2
            deathTimer = GameBalance.DeathSlowmoDuration;
            deathZoomLerp = 0f;
            camOriginalSize = mainCam != null ? mainCam.orthographicSize : 9f;
        }

        /// <summary>死亡演出结束：停表结算+弹战报（GameBootstrap.EndRun → BattleReport → DeathPanel）</summary>
        private void FinishDeath()
        {
            Time.timeScale = 1f; // 恢复正常流速（战报面板自身不需要慢动作）
            BattleReport report = GameBootstrap.Instance != null
                ? GameBootstrap.Instance.EndRun()
                : default;
            DeathPanel.Show(report);
        }

        // ==================== 翻滚残影（M2：位置残影序列） ====================

        /// <summary>生成一个位置残影：复制当前精灵位置+颜色，独立渐隐后自毁</summary>
        private void SpawnAfterimage()
        {
            if (sr == null || sr.sprite == null) return;

            if (afterimageRoot == null)
            {
                afterimageRoot = new GameObject("[RollAfterimages]");
            }

            var go = new GameObject("Afterimage");
            go.transform.SetParent(afterimageRoot.transform, false);
            go.transform.position = transform.position;
            go.transform.rotation = transform.rotation;
            go.transform.localScale = transform.localScale;

            var ghostSr = go.AddComponent<SpriteRenderer>();
            ghostSr.sprite = sr.sprite;
            ghostSr.color = new Color(sr.color.r, sr.color.g, sr.color.b, GameBalance.RollAfterimageStartAlpha);
            ghostSr.flipX = sr.flipX;
            ghostSr.sortingOrder = sr.sortingOrder - 1; // 在本体之下
            ghostSr.sortingLayerName = sr.sortingLayerName;

            // 协程渐隐+自毁（用独立组件承载协程，避免依赖 PlayerController 生命周期）
            var fader = go.AddComponent<AfterimageFader>();
            fader.Initialize(ghostSr, GameBalance.RollAfterimageLifetime);
        }
    }

    /// <summary>
    /// 残影渐隐组件（独立于 PlayerController 生命周期，自毁式）。
    /// 从起始透明度线性渐隐至 0 后 Destroy 自身 GameObject。
    /// </summary>
    public class AfterimageFader : MonoBehaviour
    {
        private SpriteRenderer sr;
        private float lifetime;
        private float timer;
        private float startAlpha;

        public void Initialize(SpriteRenderer target, float life)
        {
            sr = target;
            lifetime = life;
            timer = life;
            startAlpha = target.color.a;
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (sr != null)
            {
                float t = Mathf.Clamp01(timer / lifetime);
                var c = sr.color;
                c.a = startAlpha * t;
                sr.color = c;
            }
            if (timer <= 0f) Destroy(gameObject);
        }
    }
}
