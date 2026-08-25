using System.Collections;
using BiuBiu.Core;
using BiuBiu.Data;
using BiuBiu.Drops;
using BiuBiu.Player;
using BiuBiu.UI;
using UnityEngine;

namespace BiuBiu.Enemies
{
    /// <summary>
    /// 正式敌人基类（数值文档 5.1/5.2；EnemyData SO 数据驱动，替换 M0 灰盒 GreyBoxZombie）。
    /// 覆盖普通敌人三种攻击方式 + 精英八方向投掷（Boss 行为差异大，见 EnemyBoss2D）：
    /// - 近战单点（近战扇形型）：贴身 → 前摇渐红 → 命中判定 → 冷却；
    /// - 远程直线（远程）：进入射程 → 前摇 → 发射直线弹丸（Projectile2D）→ 冷却；
    /// - 范围横扫（近战横扫型）：进入半径 → 前摇 → 朝玩家方向扇形判定 → 冷却；
    /// - 八方向投掷（精英专用）：进入投掷距离 → 前摇 0.6s → 朝玩家 1 发 + 米字其余 7 发（距离 = 普通远程 ×2）。
    /// 物理方案：Rigidbody2D Dynamic + CircleCollider2D（敌人间物理分离防堆叠）；
    /// 玩家无碰撞体（代码距离判定伤害，可穿敌群靠翻滚无敌帧脱身）。
    /// 通用反馈：受击闪白（HitFlash）+ 击退（IKnockbackable）+ 头顶血条；
    /// 精英/Boss 血量阈值掉血瓶（75%/50%/25% 各 1，数值文档 5.2/5.3）。
    /// </summary>
    public class EnemyBase2D : MonoBehaviour, IDamageable, IKnockbackable
    {
        /// <summary>行为状态机（Seek 追踪 / Windup 前摇 / Cooldown 冷却 / 精英冲撞三段）</summary>
        private enum State
        {
            Seek,         // 追踪玩家
            Windup,       // 攻击前摇（渐红蓄力）
            Cooldown,     // 攻击冷却
            ChargeWindup, // 精英冲撞前摇
            Charging,     // 精英冲撞中（直线冲刺）
            RangedCharge, // 投掷蓄力（弹弓拉拽：投掷物反向缓慢移动）
            RangedFire    // 投掷发射瞬间
        }

        /// <summary>敌人数据（Spawner 注入；血量等实际值在 Initialize 计算）</summary>
        private EnemyData data;

        /// <summary>当前血量</summary>
        private int health;

        /// <summary>最大血量（含难度成长）</summary>
        private int maxHealth;

        /// <summary>当前状态</summary>
        private State state;

        /// <summary>是否已死亡（Die/Shatter 置位，Initialize 复位；供屏幕外箭头过滤尸体）</summary>
        private bool isDead;

        /// <summary>公开只读：是否已死亡（含击碎）。</summary>
        public bool IsDead => isDead;

        /// <summary>状态计时（前摇/冷却/冲撞共用）</summary>
        private float stateTimer;

        // ---- 组件引用（Awake 获取；热重载后 UnityEngine.Object 存活无需自愈） ----
        private SpriteRenderer sr;
        private Rigidbody2D rb;
        private HitFlash hitFlash;
        private CircleCollider2D circle;

        // ---- 视觉基准（前摇渐变/池复用重置） ----
        private Color baseColor;

        /// <summary>
        /// 敌人主色（只读）。击碎破碎粒子着色取自此处（与灰盒/血条同源：GreyColor(enemyType)）。
        /// 池复用 Reset 时随 baseColor 一并复位，取色时机须在 Shatter 回池前。
        /// </summary>
        public Color MainColor => baseColor;

        // ---- 击退（物理冲量驱动，v3.5） ----
        private float knockTimer;
        private bool isKnockFlying;        // 当前击退是否为可连锁的飞行态
        private float chainPower;          // 本次飞行可传递的连锁冲量速度（每跳衰减，< 阈值停止）
        private bool chainProcessedThisFlight; // 本次飞行是否已处理过连锁
        private const float KnockTime = 0.15f; // 击退失控时长（结束后 AI 恢复控制）

        // ---- 尸体留存上限（全局 FIFO，超出最旧渐隐回池） ----
        private static readonly System.Collections.Generic.Queue<EnemyBase2D> corpses =
            new System.Collections.Generic.Queue<EnemyBase2D>();

        // ---- 精英冲撞 ----
        private float chargeCooldownTimer; // 冲撞冷却剩余（<=0 可冲）
        private Vector2 chargeDir;         // 冲刺方向（前摇结束时锁定）
        private float chargeRemaining;     // 剩余冲刺距离（tile）
        private bool chargeHitPlayer;      // 本次冲撞是否已命中（一次冲撞只伤一次）

        // ---- 血瓶阈值（精英/Boss；基础敌人恒 0 不触发） ----
        private int potionThresholdsFired;

        // ---- 投掷蓄力（弹弓拉拽效果）----
        private GameObject pendingProjectile; // 蓄力中的投掷物（尚未发射）
        private Transform carryBall;           // 远程常驻投掷物圆球（0 蓄力外观，蓄力时隐藏避免与飞行弹丸重复）

        // ---- 血条（头顶，灰盒：黑底+红条） ----
        private Transform healthBarFill;

        /// <summary>是否 Boss（击杀标记/演出强度用；Boss 行为本类不处理，仅复用受击/死亡链路）</summary>
        public bool IsBoss => data != null && data.enemyType == EnemyType.Boss;

        /// <summary>是否精英或 Boss（满蓄力不应秒杀的硬核单位）</summary>
        public bool IsEliteOrBoss => data != null && data.enemyType != EnemyType.Normal;

        /// <summary>敌人配置（子类 Boss 专属行为读取参数用）</summary>
        protected EnemyData Data => data;

        /// <summary>刚体（子类控制移动用）</summary>
        protected Rigidbody2D Rb => rb;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            circle = GetComponent<CircleCollider2D>();
            hitFlash = GetComponent<HitFlash>();
            if (hitFlash == null) hitFlash = gameObject.AddComponent<HitFlash>(); // 灰盒模板可能未挂
        }

        /// <summary>
        /// 生成初始化（ObjectPool.Get 后由 Spawner 调用；池复用时重置全部状态）。
        /// </summary>
        /// <param name="enemyData">敌人配置</param>
        /// <param name="difficultyLevel">当前难度级 L（血量成长输入）</param>
        public void Initialize(EnemyData enemyData, int difficultyLevel)
        {
            data = enemyData;
            isDead = false; // 池复用/重新生成时复位死亡标志（屏幕外箭头据此过滤尸体）

            // 血量：普通 = 基础+floor(L/4)；精英 = 基础+floor(L/2)（数值文档 6.1）
            maxHealth = data.enemyType == EnemyType.Elite
                ? GameBalance.EliteHealth(data.baseHealth, difficultyLevel)
                : GameBalance.EnemyHealth(data.baseHealth, difficultyLevel);
            health = maxHealth;

            // 灰盒视觉：按类型染色+体型缩放（素材版 prefab 自带视觉，颜色仅灰盒路径生效）
            baseColor = GreyColor(data.enemyType);
            if (sr != null)
            {
                if (sr.sprite == null) sr.sprite = GreyBoxFactory.Square; // 无 prefab 灰盒兜底
                sr.color = baseColor;
                sr.sortingOrder = 10;
            }
            transform.localScale = new Vector3(data.bodySize.x, data.bodySize.y, 1f);
            if (circle != null) circle.radius = 0.4f; // 本地半径（乘 localScale≈体型 40% 碰撞）

            // 状态重置
            state = State.Seek;
            stateTimer = 0f;
            knockTimer = 0f;
            chargeCooldownTimer = data.bossChargeCooldown * 0.5f; // 开局半冷却后再考虑冲撞
            chargeRemaining = 0f;
            chargeHitPlayer = false;
            potionThresholdsFired = 0;
            if (rb != null) rb.velocity = Vector2.zero;

            EnsureHealthBar();
            UpdateHealthBar();

            // 登场气泡（设计文档 14.x / 数值文档 9 章）
            SpeechBubbleManager.Say(transform, ToSpeaker(data), SpeechEvent.Spawn);

            // 远程敌人常驻 0 蓄力投掷物圆球（与近战方块区分；外观/尺寸与蓄力发射球完全一致：橙红圆、半径 bulletRadius）
            // 球为独立世界物体（不挂父级，避免非等比缩放压扁/偏移），每帧由 Think 平滑环绕敌人朝玩家方向
            // 池复用时先清旧球再按类型重建
            if (carryBall != null) { Destroy(carryBall.gameObject); carryBall = null; }
            if (data.attackType == EnemyAttackType.RangedLine)
            {
                float r = data.bulletRadius;
                carryBall = GreyBoxFactory.MakeBox("CarryBall", true, new Color(1f, 0.6f, 0.15f), new Vector2(r * 2f, r * 2f)).transform;
                var cbsr = carryBall.GetComponent<SpriteRenderer>();
                if (cbsr != null) cbsr.sortingOrder = 15; // 与 Projectile2D.Launch 同排序层
                // 初始位置：身前 0.55 tile（朝玩家）；Think 每帧平滑跟随，进蓄力时隐藏避免与发射弹丸重复
                Vector2 aim = ((Vector2)(GameBootstrap.Instance?.GetPlayer()?.transform.position) - (Vector2)transform.position);
                if (aim == Vector2.zero) aim = Vector2.right;
                carryBall.position = (Vector2)transform.position + aim.normalized * 0.55f;
            }
        }

        /// <summary>EnemyType → SpeakerType（气泡文案池对齐用；按敌人类型区分精英/Boss/各敌人）</summary>
        private static SpeakerType ToSpeaker(EnemyData enemyData)
        {
            switch (enemyData.enemyType)
            {
                case EnemyType.Elite: return SpeakerType.Elite;
                case EnemyType.Boss: return SpeakerType.Boss;
            }
            switch (enemyData.attackType)
            {
                case EnemyAttackType.RangedLine: return SpeakerType.Ranged;
                case EnemyAttackType.ArcSweep: return SpeakerType.MeleeSweep;
                default: return SpeakerType.Ranged;
            }
        }

        /// <summary>覆盖血量（子类 Boss 独立成长公式 30×n 用；同步刷血条）</summary>
        protected void SetHealthInternal(int hp)
        {
            maxHealth = hp;
            health = hp;
            UpdateHealthBar();
        }

        /// <summary>增加最大血量并同步当前血量（第 8 轮起普通敌人血量线性偏陡加成用）</summary>
        public void AddMaxHealth(int n)
        {
            if (n <= 0) return;
            maxHealth += n;
            health += n;
            UpdateHealthBar();
        }

        /// <summary>当前血量（只读；Boss 二阶段判定用）</summary>
        public int GetCurrentHealth()
        {
            return health;
        }

        /// <summary>最大血量（只读；Boss 二阶段判定用）</summary>
        public int GetMaxHealth()
        {
            return maxHealth;
        }

        /// <summary>灰盒配色（按敌人类型；素材版被 prefab 视觉替代）</summary>
        private static Color GreyColor(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Elite: return new Color(0.6f, 0.2f, 0.8f);   // 紫
                case EnemyType.Boss: return new Color(1.0f, 0.85f, 0.2f);  // 金
                default: return new Color(0.45f, 0.72f, 0.85f);            // 浅蓝（雾冰蓝：远处屏幕外箭头与绿地板区分；与击飞亮蓝 0.4,0.8,1.0 拉开）
            }
        }

        // ==================== 行为状态机 ====================

        private void Update()
        {
            if (data == null) return;

            // ---- 击退中：物理冲量驱动位移，暂停 AI（结束后恢复） ----
            if (knockTimer > 0f)
            {
                knockTimer -= Time.deltaTime;
                if (knockTimer <= 0f)
                {
                    isKnockFlying = false;
                    chainProcessedThisFlight = false;
                    if (rb != null) rb.velocity = Vector2.zero;
                }
                ClampToMap();
                return;
            }

            // 玩家引用惰性自愈（热重载/生成顺序兜底）
            var player = GameBootstrap.Instance != null ? GameBootstrap.Instance.GetPlayer() : null;
            if (player == null)
            {
                if (rb != null) rb.velocity = Vector2.zero;
                return;
            }

            Think(player);

            ClampToMap(); // 物理分离可能把敌人挤向边界，钳回有效区
        }

        /// <summary>
        /// 行为主体（每帧；击退/钳制等通用逻辑在基类 Update 处理）。
        /// 子类（EnemyBoss2D 技能循环）覆盖本方法实现专属行为。
        /// </summary>
        /// <param name="player">当前玩家（已判空）</param>
        protected virtual void Think(PlayerController player)
        {
            Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
            float dist = toPlayer.magnitude;

            // 冲撞冷却推进（任何状态都计时）
            if (chargeCooldownTimer > 0f) chargeCooldownTimer -= Time.deltaTime;

            // 远程常驻球：平滑环绕敌人朝玩家方向（仅可见时更新；隐藏/蓄力期间不动）
            if (carryBall != null && carryBall.gameObject.activeSelf)
            {
                Vector2 toP = toPlayer; // 已算好的朝玩家向量
                if (toP == Vector2.zero) toP = Vector2.right;
                Vector3 target = (Vector2)transform.position + toP.normalized * 0.55f;
                // 帧率无关平滑（约 12/s 趋近），避免瞬移跳变
                float k = Mathf.Min(1f, 12f * Time.deltaTime);
                carryBall.position = Vector3.Lerp(carryBall.position, target, k);
            }

            switch (state)
            {
                case State.Seek:
                    SeekUpdate(player, toPlayer, dist);
                    break;
                case State.Windup:
                    WindupUpdate(player, dist);
                    break;
                case State.Cooldown:
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f) state = State.Seek;
                    break;
                case State.ChargeWindup:
                    ChargeWindupUpdate(player);
                    break;
                case State.Charging:
                    ChargingUpdate(player);
                    break;
                case State.RangedCharge:
                    RangedChargeUpdate(player);
                    break;
                case State.RangedFire:
                    // 发射瞬间状态（0 帧过渡，实际在 RangedChargeUpdate 末尾完成发射）
                    break;
            }
        }

        /// <summary>追踪：朝玩家移动；进入攻击条件转前摇（精英走八方向投掷分支）</summary>
        private void SeekUpdate(PlayerController player, Vector2 toPlayer, float dist)
        {
            // ---- 进入射程 → 攻击前摇 ----
            // 触发距离比实际攻击范围大，让敌人走到近身才出手
            if (dist <= data.attackRange)
            {
                state = State.Windup;
                stateTimer = data.windupTime;
                rb.velocity = Vector2.zero;
                return;
            }

            // ---- 追踪移动（物理速度：与敌人间分离力协同） ----
            rb.velocity = toPlayer.normalized * data.moveSpeed;
        }

        // ---- 近战扇形蓄力指示器 ----
        private LineRenderer windupArcOutline;  // 扇形空心框
        private SpriteRenderer windupArcFill;   // 扇形填充

        /// <summary>前摇：扇形空心框逐渐变红蓄力；填满后出手</summary>
        private void WindupUpdate(PlayerController player, float dist)
        {
            float effectiveWindup = data.windupTime;
            stateTimer -= Time.deltaTime;
            float progress = 1f - Mathf.Clamp01(stateTimer / effectiveWindup);

            // 近战类型：显示扇形蓄力指示器（空心框逐渐变红）
            if (data.attackType == EnemyAttackType.ArcSweep)
            {
                ShowWindupArc(player, progress);
            }

            if (stateTimer > 0f) return;

            // 前摇结束：隐藏指示器
            HideWindupArc();

            // 前摇结束：出手
            if (data.attackType == EnemyAttackType.RangedLine)
            {
                // 远程直线：弹弓蓄力子状态
                state = State.RangedCharge;
                stateTimer = 0.6f;
                chargeDir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
                pendingProjectile = null;
                if (carryBall != null) carryBall.gameObject.SetActive(false);
            }
            else
            {
                // 近战横扫 / 精英八方向投掷：直接出手
                ExecuteAttack(player);
                state = State.Cooldown;
                stateTimer = data.attackInterval;
            }
            sr.color = baseColor;
            HideWindupArc();
        }

        /// <summary>出手（按攻击方式分支）</summary>
        private void ExecuteAttack(PlayerController player)
        {
            // 出手气泡（设计文档 14.x）
            SpeechBubbleManager.Say(transform, ToSpeaker(data), SpeechEvent.Attack);

            Vector2 origin = transform.position;
            Vector2 toPlayer = (Vector2)player.transform.position - origin;
            Vector2 aimDir = toPlayer.normalized;
            // 实际命中判定范围 = attackRange + 玩家碰撞半径（敌人前摇期间走位拉近距离）
            float hitRange = data.attackRange + GameBalance.PlayerCollisionRadius;
            float hitArc = data.sweepAngle * 0.5f;
            float distToPlayer = toPlayer.magnitude;

            switch (data.attackType)
            {
                case EnemyAttackType.RangedLine:
                    FireProjectile(toPlayer.normalized);
                    break;

                case EnemyAttackType.OctaThrow:
                    FireOctaThrow(player);
                    break;

                case EnemyAttackType.ArcSweep:
                    if (distToPlayer <= hitRange
                        && Vector2.Angle(aimDir, toPlayer) <= hitArc)
                    {
                        player.TakeDamage(data.damage);
                    }
                    break;
            }
        }

        /// <summary>八方向投掷（精英专用）：朝玩家 1 发 + 米字(0/45/.../315)其余 7 发，形成封锁网</summary>
        private void FireOctaThrow(PlayerController player)
        {
            Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
            float playerAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

            // 米字 8 方向（每 45°）
            var dirs = new System.Collections.Generic.List<float>(8);
            for (int i = 0; i < 8; i++) dirs.Add(i * 45f);

            // 朝玩家那发（独立 1 发）
            FireThrow(toPlayer.normalized);

            // 米字其余 7 发（若与朝玩家方向最近的那个重合则去重，避免浪费）
            float nearest = Mathf.Round(playerAngle / 45f) * 45f;
            foreach (float a in dirs)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(a, nearest)) < 1f) continue; // 去重朝玩家方向
                float rad = a * Mathf.Deg2Rad;
                FireThrow(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)));
            }
        }

        /// <summary>发射一枚八方向投掷弹丸（精英；朝某方向，速度/颜色同远程敌弹）</summary>
        private void FireThrow(Vector2 dir)
        {
            var go = ObjectPool.Get(Projectile2D.GreyTemplate,
                transform.position, Quaternion.identity);
            go.GetComponent<Projectile2D>().Launch(dir, data.throwSpeed,
                data.bulletRadius, new Color(1f, 0.6f, 0.15f)); // 橙红敌弹
        }

        /// <summary>发射一枚直线弹丸（远程；ObjectPool 池化）</summary>
        private void FireProjectile(Vector2 dir)
        {
            var go = ObjectPool.Get(Projectile2D.GreyTemplate,
                transform.position, Quaternion.identity);
            go.GetComponent<Projectile2D>().Launch(dir, data.projectileSpeed,
                data.bulletRadius, new Color(1f, 0.6f, 0.15f)); // 橙红敌弹
        }

        /// <summary>精英冲撞前摇：原地渐红抖动；结束锁定方向冲刺</summary>
        private void ChargeWindupUpdate(PlayerController player)
        {
            stateTimer -= Time.deltaTime;
            float progress = 1f - Mathf.Clamp01(stateTimer / data.bossChargeWindup);
            sr.color = Color.Lerp(baseColor, Color.red, progress);

            if (stateTimer > 0f) return;

            // 锁定冲刺方向（前摇期间走位可骗方向）
            chargeDir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
            chargeRemaining = data.bossChargeDistance;
            chargeHitPlayer = false;
            state = State.Charging;
            sr.color = baseColor;
        }

        /// <summary>精英冲撞中：直线冲刺，路径接触玩家 1 伤+击退；冲完进冷却</summary>
        private void ChargingUpdate(PlayerController player)
        {
            float step = data.bossChargeSpeed * Time.deltaTime;
            rb.velocity = chargeDir * data.bossChargeSpeed;
            chargeRemaining -= step;

            // 冲撞路径命中判定（一次冲撞只伤一次）
            if (!chargeHitPlayer)
            {
                float hitDist = data.bodySize.x * 0.5f + GameBalance.PlayerCollisionRadius;
                if (((Vector2)player.transform.position - (Vector2)transform.position).sqrMagnitude
                    <= hitDist * hitDist)
                {
                    chargeHitPlayer = true;
                    player.TakeDamage(data.damage);
                    // 击退玩家 1 tile（数值文档 5.2 冲撞；M1-8 玩家改造接入受击击退后生效）
                    var knockable = player.GetComponent<IKnockbackable>();
                    if (knockable != null) knockable.Knockback(chargeDir, data.bossChargeKnockback);
                }
            }

            if (chargeRemaining <= 0f)
            {
                rb.velocity = Vector2.zero;
                chargeCooldownTimer = data.bossChargeCooldown;
                state = State.Cooldown;
                stateTimer = 0.5f; // 冲撞后短暂僵直
            }
        }

        /// <summary>投掷蓄力（弹弓拉拽）：生成投掷物，反向缓慢移动，蓄力结束发射</summary>
        private void RangedChargeUpdate(PlayerController player)
        {
            stateTimer -= Time.deltaTime;

            // 蓄力开始：生成投掷物（可见但不可碰撞）
            if (pendingProjectile == null)
            {
                Vector2 spawnPos = (Vector2)transform.position + chargeDir * 0.5f; // 略前方生成
                pendingProjectile = ObjectPool.Get(Projectile2D.GreyTemplate, spawnPos, Quaternion.identity);
                var proj = pendingProjectile.GetComponent<Projectile2D>();
                // 蓄力阶段：投掷物不移动、不碰撞（Launch 传入零速+标记蓄力）
                proj.Launch(Vector2.zero, 0f, data.bulletRadius, new Color(1f, 0.6f, 0.15f));
            }

            // 弹弓拉拽：投掷物朝反方向缓慢移动（蓄力感）
            if (pendingProjectile != null)
            {
                Vector2 pullDir = -chargeDir; // 反方向
                float pullSpeed = 1.5f; // 缓慢拉拽速度
                pendingProjectile.transform.position += (Vector3)(pullDir * pullSpeed * Time.deltaTime);
            }

            // 蓄力结束：发射！
            if (stateTimer <= 0f)
            {
                if (pendingProjectile != null)
                {
                    // 回收蓄力占位弹丸
                    ObjectPool.Release(pendingProjectile);
                    pendingProjectile = null;
                }
                // 真正发射：朝玩家方向高速弹丸
                FireProjectile(chargeDir);
                if (carryBall != null) carryBall.gameObject.SetActive(true); // 攻击结束恢复常驻球
                state = State.Cooldown;
                stateTimer = data.attackInterval;
            }
        }

        /// <summary>显示扇形蓄力指示器：空心框+按进度填充红色</summary>
        private void ShowWindupArc(PlayerController player, float progress)
        {
            Vector2 origin = transform.position;
            Vector2 aimDir = ((Vector2)player.transform.position - origin).normalized;
            float aimAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            float r = data.attackRange;
            float half = data.sweepAngle * 0.5f;
            int segments = 16;

            // ---- 空心框（LineRenderer 描边） ----
            if (windupArcOutline == null)
            {
                var go = new GameObject("WindupArcOutline");
                go.transform.SetParent(transform, false);
                windupArcOutline = go.AddComponent<LineRenderer>();
                windupArcOutline.startWidth = 0.05f;
                windupArcOutline.endWidth = 0.05f;
                windupArcOutline.sortingOrder = 18;
                windupArcOutline.useWorldSpace = true;
                windupArcOutline.loop = true;
                var shader = Shader.Find("Sprites/Default");
                if (shader != null) windupArcOutline.sharedMaterial = new Material(shader);
            }
            windupArcOutline.enabled = true;
            windupArcOutline.positionCount = segments + 2;
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float ang = (aimAngle - half + t * half * 2f) * Mathf.Deg2Rad;
                windupArcOutline.SetPosition(i, origin + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
            }
            windupArcOutline.SetPosition(segments + 1, origin);
            float outlineAlpha = 0.3f + progress * 0.5f;
            windupArcOutline.startColor = new Color(1f, 0.2f, 0.1f, outlineAlpha);
            windupArcOutline.endColor = new Color(1f, 0.2f, 0.1f, outlineAlpha);

            // ---- 填充：按进度从扇形一侧逐渐填满 ----
            if (windupArcFill == null)
            {
                var go2 = new GameObject("WindupArcFill");
                go2.transform.SetParent(transform, false);
                windupArcFill = go2.AddComponent<SpriteRenderer>();
                windupArcFill.sortingOrder = 17;
                windupArcFill.enabled = false;
            }
            // 每帧按进度生成扇形贴图（进度=0→空心，进度=1→满扇形）
            var fillTex = BuildProgressiveArcTexture(data.sweepAngle, progress);
            windupArcFill.sprite = Sprite.Create(fillTex,
                new Rect(0, 0, 64, 64), new Vector2(0f, 0.5f), 64f);
            windupArcFill.enabled = true;
            windupArcFill.transform.position = origin;
            windupArcFill.transform.rotation = Quaternion.Euler(0, 0, aimAngle);
            windupArcFill.transform.localScale = new Vector3(r, r, 1f);
            windupArcFill.color = new Color(1f, 0.15f, 0.1f, 0.6f);
        }

        /// <summary>生成按进度填充的扇形贴图（进度从一侧逐渐填满到完整扇形）；Boss 八方向预警复用</summary>
        protected static Texture2D BuildProgressiveArcTexture(float sweepAngle, float progress)
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave; // 跨场景重载存活（Boss 扇形预警纹理 static 缓存）
            tex.filterMode = FilterMode.Point;
            float half = sweepAngle * 0.5f;
            // 当前已填充的角度范围（从 -half 逐步扩展到 +half）
            float filledHalf = half * progress;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float vx = x + 0.5f;
                    float vy = y + 0.5f - size * 0.5f;
                    float dist = Mathf.Sqrt(vx * vx + vy * vy);
                    float ang = Mathf.Atan2(vy, vx) * Mathf.Rad2Deg;
                    // 在完整扇形内 且 在已填充角度范围内
                    bool inArc = dist <= size && ang >= -half && ang <= half;
                    bool isFilled = ang >= -filledHalf && ang <= filledHalf;
                    px[y * size + x] = (inArc && isFilled)
                        ? new Color32(255, 40, 25, 255)
                        : new Color32(0, 0, 0, 0);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        /// <summary>隐藏扇形蓄力指示器</summary>
        private void HideWindupArc()
        {
            if (windupArcOutline != null) windupArcOutline.enabled = false;
            if (windupArcFill != null) windupArcFill.enabled = false;
        }

        /// <summary>钳回有效地图区（80×80 减边界墙；防物理分离挤出界）</summary>
        private void ClampToMap()
        {
            Vector3 p = transform.position;
            float min = GameBalance.BorderWallThickness + data.bodySize.x * 0.5f;
            float max = GameBalance.MapSizeTiles - GameBalance.BorderWallThickness - data.bodySize.x * 0.5f;
            p.x = Mathf.Clamp(p.x, min, max);
            p.y = Mathf.Clamp(p.y, min, max);
            transform.position = p;
        }

        // ==================== 受击/死亡 ====================

        /// <summary>IDamageable：扣血；血尽走终结演出（Die/Shatter），未击杀才触发受击闪白</summary>
        public void TakeDamage(int amount)
        {
            if (data == null || amount <= 0) return;
            health -= amount;
            UpdateHealthBar();

            if (health <= 0f)
            {
                Die();
                return;
            }

            // 仅未击杀才闪白（击杀走 Die/Shatter 的终结演出，无需闪白）
            if (hitFlash != null) hitFlash.PlayFlash(0.15f);

            // 受击音效：仅敌人受伤（未死亡）时播放（死亡走 Die/Shatter，不播此音）
            AudioManager.PlayWorld("enemy_hit", transform.position);

            // 受击气泡（设计文档 14.x；血未尽才冒，避免与死亡气泡重复）
            SpeechBubbleManager.Say(transform, ToSpeaker(data), SpeechEvent.Hit);
        }

        /// <summary>击碎（三级弹丸满蓄力命中）：直接死亡，无尸体，留扇形血迹</summary>
        public void Shatter()
        {
            if (data == null) return;
            isDead = true; // 满蓄力击碎：标记为死亡，屏幕外箭头不再指示

            // 死前隐藏蓄力扇形框（避免蓄力中被击杀时残留）
            HideWindupArc();

            // 击杀计数
            GameBootstrap.Instance?.NotifyEnemyKilled(IsBoss);

            // 扇形血迹（击碎效果：大量大范围血迹喷溅）
            for (int i = 0; i < 8; i++)
            {
                DropManager.Instance?.SpawnStain(
                    transform.position + (Vector3)(Random.insideUnitCircle * 1.5f),
                    data.bodySize.x * (0.5f + Random.value * 0.8f));
            }

            // 击碎震屏
            if (CameraTrauma.Instance != null)
                CameraTrauma.Instance.AddTrauma(GameBalance.TraumaHitEnemy * 2f);

            // 击碎音效（红档满蓄力专属爆裂音，与普通死亡音 enemy_death 区分）
            AudioManager.PlayWorld("enemy_shatter", transform.position);

            // 直接回池（无尸体）
            ObjectPool.Release(gameObject);
        }

        /// <summary>击退（IKnockbackable 接口实现：物理冲量驱动）</summary>
        public void Knockback(Vector2 direction, float speed)
        {
            Knockback(direction, speed, triggerChain: false);
        }

        /// <summary>击退（扩展重载：可指定是否连锁）</summary>
        /// <param name="triggerChain">是否为可连锁的飞行态（黄色档=true，连锁传递=false 靠衰减停止）</param>
        public void Knockback(Vector2 direction, float speed, bool triggerChain = false)
        {
            knockTimer = KnockTime;
            isKnockFlying = triggerChain;
            chainProcessedThisFlight = false;
            // 连锁可传递的冲量速度（本次飞行，后续 OnCollisionEnter 用衰减值传递）
            chainPower = speed * GameBalance.ChainDecayFactor;

            // 物理冲量：AddForce(Impulse) 瞬间给目标速度——质量参与，撞到其他敌人时物理引擎自然传递
            if (rb != null)
            {
                Vector2 impulse = direction.normalized * (GameBalance.EnemyMass * speed);
                rb.AddForce(impulse, ForceMode2D.Impulse);
            }

            // 击退打断前摇（不打断冲撞中——冲撞质量大）
            if (state == State.Windup)
            {
                state = State.Seek;
                sr.color = baseColor;
                HideWindupArc(); // 打断蓄力时同步隐藏扇形框，避免线框残留（bug 修复）
            }
        }

        /// <summary>物理碰撞回调：被击飞中的敌人撞到其他敌人时传递连锁冲量+伤害</summary>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            // 仅飞行态且未处理过连锁时触发
            if (!isKnockFlying || chainProcessedThisFlight) return;
            if (chainPower < GameBalance.ChainStopThreshold) return;

            var other = collision.collider.GetComponent<EnemyBase2D>();
            if (other == null || other == this) return;
            if (other.knockTimer > 0f) return; // 已在飞行态的不重复击飞

            // 碰撞方向=从本敌人指向被撞敌人
            Vector2 dir = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
            if (dir == Vector2.zero) dir = collision.relativeVelocity.normalized;

            // 碰撞伤害（不触发受击闪白，避免连锁满屏白闪）
            other.TakeDamage(GameBalance.CollisionKnockbackDamage);
            // 连锁击飞：传递衰减冲量，triggerChain=false（靠衰减自然停止，不再连锁传递连锁）
            other.Knockback(dir, chainPower, triggerChain: false);
            chainProcessedThisFlight = true; // 本次飞行只处理一次连锁
        }

        /// <summary>死亡：计数+血迹+ -尸体留存（灰盒压扁渐隐）；精英/Boss 触发 hitstop+击杀演出</summary>
        private void Die()
        {
            isDead = true; // 普通击杀：标记为死亡，屏幕外箭头不再指示
            // 同步停用 AI/Think（尸体协程仍独立运行；避免尸体活跃期间占波次计数导致不进下一轮）
            enabled = false;

            // 死前隐藏蓄力扇形框（避免蓄力中被击杀时残留）
            HideWindupArc();

            // 销毁独立常驻球（不随父级回收，必须手动清，避免泄漏/尸体残留）
            if (carryBall != null) { Destroy(carryBall.gameObject); carryBall = null; }

            // 死亡气泡（设计文档 14.x；尸体留存协程期间 transform 仍有效， 气泡可显示）
            if (data != null) SpeechBubbleManager.Say(transform, ToSpeaker(data), SpeechEvent.Death);

            // 死亡音效（仅普通/精英/Boss 正常死亡；击碎走 Shatter 专属音，不播此音）
            AudioManager.PlayWorld("enemy_death", transform.position);

            // 击杀计数（累计击杀数，战报/历史最佳数据源）
            GameBootstrap.Instance?.NotifyEnemyKilled(IsBoss);

            // 地面血迹×3（增加血迹量）
            DropManager.Instance?.SpawnStain(transform.position, data.bodySize.x);
            DropManager.Instance?.SpawnStain(transform.position + (Vector3)(Random.insideUnitCircle * 0.4f), data.bodySize.x * 0.7f);
            DropManager.Instance?.SpawnStain(transform.position + (Vector3)(Random.insideUnitCircle * 0.6f), data.bodySize.x * 0.5f);

            // 精英/Boss 击杀演出：hitstop 0.12s + 强震屏（数值文档 9 章）
            if (data.enemyType != EnemyType.Normal)
            {
                if (CameraTrauma.Instance != null)
                    CameraTrauma.Instance.AddTrauma(GameBalance.KillCeremonyTrauma);
                StartCoroutine(HitstopRoutine());
            }

            // 尸体留存：压扁变色渐隐后回池（普通敌人也留尸体）
            StartCoroutine(CorpseRoutine());
        }

        /// <summary>hitstop 协程：timeScale 归零短暂定格后恢复（仅精英/Boss 终结一击）</summary>
        private static IEnumerator HitstopRoutine()
        {
            float prevScale = Time.timeScale;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(GameBalance.HitstopDuration);
            Time.timeScale = prevScale;
        }

        /// <summary>尸体留存：压扁+变色，停留至全局上限后渐隐回池</summary>
        private IEnumerator CorpseRoutine()
        {
            // 禁用碰撞+AI+血条（尸体不参与游戏逻辑）
            if (circle != null) circle.enabled = false;
            if (rb != null) { rb.velocity = Vector2.zero; rb.isKinematic = true; }
            if (healthBarFill != null) healthBarFill.parent.gameObject.SetActive(false);
            enabled = false; // 停止 Think

            // 压扁+变色
            float flattenY = GameBalance.CorpseFlattenScaleY;
            Vector3 origScale = transform.localScale;
            transform.localScale = new Vector3(origScale.x, origScale.y * flattenY, origScale.z);
            // 尸体染色：精英/Boss 保留各自主色（紫/金）压暗；普通沿用统一绿尸色调
            if (sr != null)
                sr.color = (data != null && data.enemyType != EnemyType.Normal)
                    ? MainColor * 0.6f
                    : GameBalance.CorpseTint;
            if (sr != null) sr.sortingOrder = 5; // 尸体层在角色之下、地面之上

            // 入队 + 超限渐隐回池（防长局堆积）
            corpses.Enqueue(this);
            while (corpses.Count > GameBalance.MaxCorpses)
            {
                var oldest = corpses.Dequeue();
                if (oldest != null && oldest.sr != null)
                    oldest.StartCoroutine(oldest.FadeOutCorpse());
            }
            yield break;
        }

        /// <summary>超龄尸体渐隐后回池（独立协程，不阻塞主流程）</summary>
        private IEnumerator FadeOutCorpse()
        {
            if (sr == null) { ObjectPool.Release(gameObject); yield break; }
            Color c = sr.color;
            float t = GameBalance.CorpseFadeDuration;
            while (t > 0f)
            {
                t -= Time.deltaTime;
                c.a = Mathf.Clamp01(t / GameBalance.CorpseFadeDuration);
                if (sr != null) sr.color = c;
                yield return null;
            }
            ObjectPool.Release(gameObject);
        }

        // ==================== 头顶血条（灰盒） ====================

        /// <summary>确保血条子物体存在（池复用只建一次）</summary>
        private void EnsureHealthBar()
        {
            if (healthBarFill == null)
            {
                var bg = new GameObject("HealthBar_BG");
                bg.transform.SetParent(transform, false);
                var bgSr = bg.AddComponent<SpriteRenderer>();
                bgSr.sprite = GreyBoxFactory.Square;
                bgSr.color = new Color(0f, 0f, 0f, 0.8f);
                bgSr.sortingOrder = 11;
                bg.transform.localPosition = new Vector3(0f, 0.65f, 0f);
                bg.transform.localScale = new Vector3(0.9f, 0.12f, 1f);

                var fill = new GameObject("HealthBar_Fill");
                fill.transform.SetParent(bg.transform, false);
                var fillSr = fill.AddComponent<SpriteRenderer>();
                fillSr.sprite = GreyBoxFactory.Square;
                fillSr.color = new Color(0.9f, 0.2f, 0.2f);
                fillSr.sortingOrder = 12;
                fill.transform.localPosition = new Vector3(0f, 0f, 0f);
                fill.transform.localScale = Vector3.one;
                healthBarFill = fill.transform;
            }
            healthBarFill.parent.gameObject.SetActive(true);
        }

        /// <summary>血条刷新（scaleX = 血量比例；空血隐藏）</summary>
        private void UpdateHealthBar()
        {
            if (healthBarFill == null || maxHealth <= 0) return;
            float ratio = Mathf.Clamp01((float)health / maxHealth);
            healthBarFill.localScale = new Vector3(ratio, 1f, 1f);
            // 锚点居中缩放会双向收缩，视觉近似即可（灰盒不追求精确左锚）
            healthBarFill.parent.gameObject.SetActive(health < maxHealth); // 满血不显血条
        }
    }
}
