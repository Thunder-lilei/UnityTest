using UnityEngine;

namespace BiuBiu.Core
{
    /// <summary>
    /// 数值常量表（数值文档 v3.3 一一对应；开发文档 4.1：全部平衡数值唯一入口，禁止代码内散落魔法数）。
    /// 单位约定（数值文档 1 章）：距离 = tile（1 tile = 1 unit，PPU 32），时间 = 秒，速度 = tile/s。
    /// 调参流程：先改数值文档（版本号 +0.1）→ 再同步本表。
    /// v3.3 清理：棍子/激光/磁吸/经验/升级/血瓶/掉落喷发/枪火照亮等已取消机制的常量已删除。
    /// </summary>
    public static class GameBalance
    {
        // ==================== 第 3 章 玩家数值 ====================

        /// <summary>玩家移速（tile/s，快于全部普通敌人）。手感偏快，下调至 3.4 更稳。</summary>
        public const float PlayerMoveSpeed = 3.4f;

        /// <summary>玩家初始血量（心）</summary>
        public const int PlayerMaxHealth = 2;

        /// <summary>玩家血量上限（常量保留；当前无成长途径，供后续扩展）</summary>
        public const int PlayerHealthCap = 8;

        /// <summary>玩家受击伤害（心/次，全部敌人统一）</summary>
        public const int PlayerDamagePerHit = 1;

        /// <summary>玩家受击无敌时长（秒，与闪白同步）</summary>
        public const float PlayerInvulnDuration = 1.0f;

        // ── 受击红边（Hurt Vignette，GameFeel 危险反馈）──
        // 玩家实际扣血时屏幕边缘浮现红色径向晕影；残血时叠加常驻红晕。
        // 全部走 IMGUI（OnGUI 画运行时生成的径向渐变贴图），无后处理栈、无 shader 后处理。
        public const float HurtVignetteFlashInTime = 0.07f;   // 红边淡入时长（秒）：受击瞬间快速亮起
        public const float HurtVignetteFadeOutTime = 0.5f;    // 红边淡出时长（秒）：受击无敌窗口内的平滑消退
        public const float HurtVignettePeakAlpha = 0.6f;      // 红边峰值 alpha 上限（0~1）：多受击 clamp 不爆表
        public const int HurtVignetteLowHealthThreshold = 1;  // 残血阈值（心）：当前血量 ≤ 此值时红晕常驻
        public const float HurtVignetteLowHealthAlpha = 0.18f;// 残血常驻 alpha（0~1）：轻微持续红晕强度
        public const float HurtVignettePulseSpeed = 0.6f;      // 残血红晕脉动频率（Hz）：边缘红晕呼吸感，越残血越紧迫
        public const float HurtVignettePulseAmount = 0.4f;     // 残血红晕脉动幅度（比例）：常驻 alpha 上下浮动 ±40%

        /// <summary>翻滚无敌时长（秒，动作周期前段）</summary>
        public const float RollInvulnTime = 0.30f;

        /// <summary>翻滚动作周期（秒；翻滚结束后还需等 RollCooldown 才能再次翻滚）</summary>
        public const float RollDuration = 0.40f;

        /// <summary>翻滚冷却（秒）：翻滚动作结束后额外等待时间，期间不可再次翻滚（总周期 = RollDuration + RollCooldown）</summary>
        public const float RollCooldown = 2.0f;

        /// <summary>翻滚位移距离（tile，位移曲线缓入缓出）</summary>
        public const float RollDistance = 2.5f;

        /// <summary>发射后坐力：各蓄力档位的反向位移幅度（tile，沿瞄准反方向 sin 包络冲出再回弹）。三档递增——绷得越满弹得越狠；零级极微保留连续射击节奏感，不干扰走位</summary>
        public static readonly float[] PlayerRecoilDistance = { 0.15f, 0.35f, 0.6f };

        /// <summary>发射后坐力回弹总时长（秒）：冲出→缓回原点的周期，短时不影响走位</summary>
        public const float PlayerRecoilDuration = 0.12f;

        /// <summary>玩家圆形碰撞半径（tile）</summary>
        public const float PlayerCollisionRadius = 0.4f;

        /// <summary>玩家刚体线性阻力（velocity 衰减系数）：让后坐力脉冲冲出后自然减速归零；移动每帧重设 velocity 故不受此影响</summary>
        public const float PlayerLinearDrag = 12f;

        // ==================== 物理层（碰撞矩阵默认全开，仅在代码里按需 Ignore） ====================
        /// <summary>玩家实体碰撞体层（用于被墙/障碍阻挡；与敌层 Ignore 以保留"穿敌群"手感）</summary>
        public const int LayerPlayerWall = 8;
        /// <summary>玩家受击触发器层（与敌接触触发伤害；不参与物理阻挡）</summary>
        public const int LayerPlayerHurt = 10;
        /// <summary>敌人层（与玩家墙层 Ignore 碰撞，使玩家可穿过敌群）</summary>
        public const int LayerEnemy = 9;

        // ==================== 第 5 章 敌人数值（基础值；实际值经第 6 章成长公式） ====================
        // 基础值落在 EnemyData ScriptableObject（数据驱动），此处只放公式与通用参数。

        /// <summary>敌人对玩家伤害恒定（心/次，次数制原则不做成长）</summary>
        public const int EnemyDamageToPlayer = 1;

        // ==================== 第 5.x 章 按轮次登场规则（v3.7 重构：登场绑定波次而非真实时间） ====================

        /// <summary>精英起始登场轮次：第 3 轮起，每轮额外 +1 只精英（独立生成，不占普通配额）</summary>
        public const int EliteStartWave = 3;

        /// <summary>精英每轮递增数量（第 EliteStartWave 轮起每轮 +1）</summary>
        public const int ElitePerWaveIncrement = 1;

        /// <summary>第 8 轮起普通数量封顶（不再翻倍，转由血量/精英占比制造难点）</summary>
        public const int WaveCapWave = 8;

        /// <summary>普通敌人数量封顶值（第 WaveCapWave 轮起恒定；= 第 7 轮波次量 2^6 = 64）</summary>
        public const int NormalCountCap = 64;

        /// <summary>第 8 轮起精英数量封顶（不再无限 +1，转由波次内"精英化"提升占比）</summary>
        public const int EliteCountCap = 5;

        /// <summary>第 8 轮起波次内"精英化"数量：每轮 min(波次-7, 上限) 个普通槽位改为生成精英（总量不增、质量换）</summary>
        public const int EliteInWaveMax = 32;

        /// <summary>Boss 登场轮次：第 5、10 轮各 1 只（bossIndex 从 1 起，血量 ×bossIndex）</summary>
        public static readonly int[] BossWaves = { 5, 10 };

        /// <summary>Boss 轮普通数量减半（让 Boss 成为焦点）</summary>
        public const float BossWaveNormalScale = 0.5f;

        /// <summary>体型倍率（相对普通近战 1×1）：精英 = 2 倍，Boss = 4 倍</summary>
        public const float EliteBodyScale = 2.0f;
        public const float BossBodyScale = 4.0f;

        /// <summary>血量倍率（相对普通近战基础血 3）：精英 = 4 倍，Boss = 8 倍（首只 24，第 10 轮 48）</summary>
        public const int EliteHealthMultiplier = 4;
        public const int BossHealthMultiplier = 8;

        /// <summary>精英投掷距离倍率（相对普通远程 attackRange 6.0）：2 倍 = 12.0</summary>
        public const float EliteThrowRangeScale = 2.0f;

        /// <summary>Boss 扇形横扫倍率（相对普通近战 120°·r2.0）：面积 4 倍 → 半径 4.0（×2）+ 角度保持 120°</summary>
        public const float BossSweepRadius = 4.0f;
        public const float BossSweepAngle = 120f;

        /// <summary>Boss 直线冲撞参数（tile/s、tile；冲撞距离 = 普通近战距离的 3 倍+）</summary>
        public const float BossChargeSpeed = 8.0f;
        public const float BossChargeDistance = 18.0f;

        /// <summary>Boss 八方向扇形横扫：每次朝 8 个米字方向各发一个扇形</summary>
        public const int BossSweepDirections = 8;

        /// <summary>Boss 冲撞连击：单次冲撞后追加冲撞的最大次数（普通阶段 rng→0..2，二阶段强制 1..2）</summary>
        public const int BossChargeComboMax = 2;

        /// <summary>Boss 二阶段狂暴血量阈值（占当前最大血量比例）：低于该值进入狂暴（连冲↑/移速↑/冷却↓）</summary>
        public const float BossEnrageThreshold = 0.5f;

        /// <summary>Boss 二阶段狂暴移速倍率（Data.moveSpeed 乘子）</summary>
        public const float BossEnrageMoveScale = 1.5f;

        /// <summary>Boss 二阶段狂暴冲撞冷却倍率（Data.bossChargeCooldown 乘子）</summary>
        public const float BossEnrageCooldownScale = 0.6f;

        // ==================== 第 6 章 时间难度曲线（仅驱动敌人血量成长） ====================

        /// <summary>难度级时间步长（秒）：难度级 L = floor(存活秒数 / 该值)，每 30s +1 级</summary>
        public const float DifficultyStepSeconds = 30f;

        /// <summary>普通敌人血量成长：基础值 + floor(L / 该值)，约每 2 分钟全体 +1 血</summary>
        public const int EnemyHealthGrowthDivisor = 4;

        /// <summary>第 8 轮起普通血量线性偏陡加成：额外血 = floor((wave - WaveCapWave) * baseHealth * 0.5)，约每轮 +50% 基础血</summary>
        public static int NormalHealthBonusFromWave(int wave, int baseHealth)
        {
            if (wave <= WaveCapWave) return 0;
            return Mathf.FloorToInt((wave - WaveCapWave) * baseHealth * 0.5f);
        }

        // ==================== 第 6.3 章 性能保护上限 ====================

        /// <summary>弹丸同屏上限（超限时优先回收离主角最远的）</summary>
        public const int MaxProjectilesOnScreen = 80;

        /// <summary>痕迹（血迹）池上限</summary>
        public const int MaxGroundStains = 500;

        /// <summary>尸体留存上限（超出时最旧尸体渐隐回池，防长局堆积）</summary>
        public const int MaxCorpses = 200;

        // ==================== 第 9 章 交互与演出数值 ====================

        /// <summary>开场提示 toast 显示时长（秒）；命名沿用早期成就 toast 时长，现为开场白提示用</summary>
        public const float AchievementToastDuration = 2.0f;

        // ---- 角色头顶气泡（数值文档第 9 章，v3.6 新增）----

        /// <summary>气泡总存活时长（秒）= 显示期 + 淡出期</summary>
        public const float BubbleLifetime = 2.0f;

        /// <summary>气泡完全显示期（秒，之后进入淡出）</summary>
        public const float BubbleShowDuration = 1.5f;

        /// <summary>气泡淡出期（秒，透明度 1→0）</summary>
        public const float BubbleFadeDuration = 0.5f;

        /// <summary>同目标同类事件最小触发间隔（秒，防受击帧每帧冒泡）</summary>
        public const float BubbleMinInterval = 0.8f;

        // ==================== 第 4.2 章 击飞连锁（v3.4；v3.5 改物理冲量驱动） ====================

        /// <summary>敌人质量（kg，Rigidbody2D.mass；冲量=质量×速度，统一质量便于平衡）</summary>
        public const float EnemyMass = 1f;

        /// <summary>黄色档击飞目标速度（tile/s）——冲量 = 质量 × 该速度</summary>
        public const float KnockbackYellowSpeed = 10f;

        /// <summary>白色档微后仰目标速度（tile/s）</summary>
        public const float HitRecoilSpeed = 2f;

        /// <summary>碰撞连锁传递的目标速度（tile/s，区别于弹丸直击）</summary>
        public const float CollisionKnockbackSpeed = 5f;

        /// <summary>碰撞连锁伤害（与弹丸直击同，保证连锁也能杀敌）</summary>
        public const int CollisionKnockbackDamage = 1;

        /// <summary>连锁衰减系数（每跳传递速度 ×该值，&lt; 阈值停止）</summary>
        public const float ChainDecayFactor = 0.6f;

        /// <summary>连锁停止阈值（传递速度 &lt; 该值时不再连锁）</summary>
        public const float ChainStopThreshold = 1f;

        // ==================== 弹弓蓄力参数 ====================

        /// <summary>弹弓蓄力：一级蓄力时长（秒，黄色）</summary>
        public const float ChargeLevel1Time = 0.5f;

        /// <summary>弹弓蓄力：二级（满）蓄力时长（秒，红色）</summary>
        public const float ChargeLevel2Time = 1.0f;

        /// <summary>满蓄后继续按住的最长允许时间（秒），超时强制脱手发射；防止玩家长时间挂机蓄满</summary>
        public const float OverchargeMaxHoldTime = 2.0f;

        /// <summary>满蓄后每秒叠加的 trauma 强度（随按住时间递增，体现“过载不稳”）</summary>
        public const float OverchargeTraumaPerSecond = 0.35f;

        /// <summary>满蓄过载脱手时的飞行方向随机偏移幅度（度）：脱手=手滑，红弹大幅飞偏以表现“瞄不准”</summary>
        public const float OverchargeSpreadAngle = 40f;

        /// <summary>满蓄过载抖动的位移/旋转放大倍率（仅作用于过载路径，不改全局 maxOffset，避免影响受击/击杀等其他震屏）</summary>
        public const float OverchargeShakeMul = 2.0f;

        /// <summary>满蓄后多久冒泡提示“要憋不住了！”（秒），仅触发一次</summary>
        public const float OverchargeWarnTime = 1.0f;

        /// <summary>白色速射最小发射间隔（秒）：防止狂点刷爆，把白色 DPS 封顶到约 5~6，仍保留速射手感</summary>
        public const float WhiteFireCooldown = 0.3f;

        /// <summary>蓄力弹丸视觉：起始距离（角色前方，tile，见数值文档第 9 章）</summary>
        public const float ChargeOrbStartDist = 0.5f;

        /// <summary>蓄力弹丸视觉：满蓄力时拉回到角色身后的距离（tile，弹弓拉皮筋感）</summary>
        public const float ChargeOrbMaxPull = 0.8f;

        /// <summary>蓄力时玩家反向拉拽的最大总位移（tile，基于蓄力起点封顶）。仅作张力暗示，避免长按无限倒退或松手时偏移过大显得突兀</summary>
        public const float ChargeMaxPullback = 0.3f;

        /// <summary>弹弓弹丸速度（零级=白/一级=黄/二级=红，见数值文档 4.1）</summary>
        public static readonly float[] ProjectileSpeeds = { 7f, 9f, 14f };

        /// <summary>弹弓弹丸最大反弹次数（零级=0/一级=1/二级=3，见数值文档 4.1）</summary>
        public static readonly int[] ProjectileMaxBounces = { 0, 1, 3 };

        /// <summary>弹弓弹丸碰撞半径</summary>
        public const float ProjectileRadius = 0.3f;

        /// <summary>弹弓弹丸存活上限（秒）</summary>
        public const float ProjectileLifetime = 5f;

        /// <summary>弹丸拖尾时长（秒，TrailRenderer.time；数值越大尾巴越长）。三档递增——红档参考满蓄力击碎大块碎片(寿命0.8s/飞出约5tile)的夸张度，拉到 0.35s（红档弹速14→尾长≈4.9tile）</summary>
        public static readonly float[] ProjectileTrailTime = { 0.12f, 0.22f, 0.35f };

        /// <summary>弹丸拖尾宽度（世界单位，TrailRenderer 宽度；参考满蓄力击碎大块碎片尺寸 10~20px=0.31~0.625tile）。三档 0.3/0.45/0.7：白≈小碎片利落、黄中等、红略超大块上限成霸气粗光带</summary>
        public static readonly float[] ProjectileTrailWidth = { 0.3f, 0.45f, 0.7f };

        /// <summary>弹丸拖尾末端透明度（0~1，余晖；数值越小尾巴消散越快）。三档 0/0.15/0.35——红档余晖明显，接近击碎爆发的"糊"冲击感</summary>
        public static readonly float[] ProjectileTrailEndAlpha = { 0.0f, 0.15f, 0.35f };

        /// <summary>hitstop 时长（秒，仅精英/Boss 终结一击）</summary>
        public const float HitstopDuration = 0.12f;

        /// <summary>玩家死亡慢动作倍率</summary>
        public const float DeathSlowmoScale = 0.2f;

        /// <summary>玩家死亡慢动作时长（秒）</summary>
        public const float DeathSlowmoDuration = 1.5f;

        /// <summary>玩家死亡镜头聚焦：正交尺寸缩放倍率</summary>
        public const float DeathZoomScale = 0.6f;

        /// <summary>击杀演出震屏 trauma（精英/Boss 终结：hitstop + 该值）</summary>
        public const float KillCeremonyTrauma = 0.6f;

        // ==================== 第 9 章 震屏 trauma 值表 ====================

        /// <summary>玩家受击震屏 trauma（强震）</summary>
        public const float TraumaPlayerHit = 0.5f;

        /// <summary>命中敌人震屏 trauma（微震）</summary>
        public const float TraumaHitEnemy = 0.15f;

        /// <summary>弹丸撞墙（非满蓄力 / 边界墙）极轻震屏强度</summary>
        public const float TraumaHitWall = 0.08f;

        /// <summary>Boss 登场震屏 trauma</summary>
        public const float TraumaBossSpawn = 0.8f;

        /// <summary>Boss 击杀震屏 trauma</summary>
        public const float TraumaBossKill = 0.8f;

        // ==================== 第 9.2 章 M2 手感优化参数 ====================

        /// <summary>翻滚残影：生成间隔（秒，每多久留一个残影）</summary>
        public const float RollAfterimageInterval = 0.04f;

        /// <summary>翻滚残影：单影存活时长（秒）</summary>
        public const float RollAfterimageLifetime = 0.25f;

        /// <summary>翻滚残影：起始透明度（渐隐至 0）</summary>
        public const float RollAfterimageStartAlpha = 0.5f;

        /// <summary>尸体留存：压扁缩放 Y 轴倍率</summary>
        public const float CorpseFlattenScaleY = 0.4f;

        /// <summary>尸体留存：渐隐时长（秒，渐隐完毕后回池）</summary>
        public const float CorpseFadeDuration = 3.0f;

        /// <summary>尸体留存：变色（暗蓝，区分活体；与浅蓝普通敌人同源）</summary>
        public static readonly Color CorpseTint = new Color(0.28f, 0.4f, 0.5f, 1f);

        /// <summary>相机前瞻偏移距离（tile，朝玩家移动方向前探）</summary>
        public const float CameraLookAheadDistance = 1.5f;

        /// <summary>相机前瞻偏移平滑时间（秒）</summary>
        public const float CameraLookAheadSmoothTime = 0.15f;

        // ==================== 击碎破碎粒子（数值文档 9 章：仅红档击碎档触发） ====================

        /// <summary>每发击碎爆发的普通碎片数（细碎迸溅）</summary>
        public const int BreakShardCount = 22;

        /// <summary>普通碎片放射初速下限（tile/s，匀速直线无重力）</summary>
        public const float BreakShardSpeedMin = 4f;

        /// <summary>普通碎片放射初速上限（tile/s，匀速直线无重力）</summary>
        public const float BreakShardSpeedMax = 14f;

        /// <summary>普通碎片生命周期（秒）：计时归零回池自毁，不依赖落地判定（俯视角无地面高度）</summary>
        public const float BreakShardLife = 0.55f;

        /// <summary>普通碎片像素尺寸下限（px，经 PPU=32 换算为 scale）</summary>
        public const float BreakShardSizeMin = 3f;

        /// <summary>普通碎片像素尺寸上限（px，经 PPU=32 换算为 scale）</summary>
        public const float BreakShardSizeMax = 12f;

        /// <summary>碎片放射张角（弧度，半角）：以弹丸飞行方向为基准向四周散开</summary>
        public const float BreakShardSpread = Mathf.PI * 0.5f;

        // ---- 大块碎片（满蓄力击碎“更夸张”的核心：少量大而慢的碎块，强化崩解冲击） ----
        /// <summary>大块碎片数（少量，突出崩解主视觉）</summary>
        public const int BreakChunkCount = 6;

        /// <summary>大块碎片放射初速下限（tile/s，比普通碎片慢，飞得更近更“沉”）</summary>
        public const float BreakChunkSpeedMin = 2f;

        /// <summary>大块碎片放射初速上限（tile/s）</summary>
        public const float BreakChunkSpeedMax = 7f;

        /// <summary>大块碎片生命周期（秒，比普通碎片久，留得更明显）</summary>
        public const float BreakChunkLife = 0.8f;

        /// <summary>大块碎片像素尺寸下限（px，明显大于普通碎片）</summary>
        public const float BreakChunkSizeMin = 10f;

        /// <summary>大块碎片像素尺寸上限（px）</summary>
        public const float BreakChunkSizeMax = 20f;

        // ==================== 大地图（设计文档 3.1 章） ====================
        public const int MapSizeTiles = 80;

        // ==================== 第 12 章 地图障碍参数（数值文档 v2.4） ====================

        /// <summary>障碍（俄罗斯方块 Tetromino 形状）数量——每个形状占 4 个 1×1 单元</summary>
        public const int ObstacleCount = 26;

        /// <summary>出生点禁区半径（tile，该范围内不撒障碍）</summary>
        public const float ObstacleSpawnClearRadius = 5f;

        /// <summary>障碍距边界墙最小距离（tile）</summary>
        public const float ObstacleBorderMargin = 3f;

        /// <summary>两障碍中心最小间距（tile，防连片成墙阵卡死走位）</summary>
        public const float ObstacleMinSpacing = 3f;

        /// <summary>刷怪点落在障碍内时重新取点最大次数</summary>
        public const int SpawnRelocateMaxAttempts = 5;

        /// <summary>边界墙厚度（tile）——激光反弹墙的碰撞体载体</summary>
        public const int BorderWallThickness = 2;

        /// <summary>玩家出生点：地图正中央（世界坐标 = 地图尺寸 / 2）</summary>
        public static readonly Vector2 PlayerSpawnPoint =
            new Vector2(MapSizeTiles / 2f, MapSizeTiles / 2f);

        /// <summary>地面变体 tile 撒点比例（格 1 点缀 ~15%，格 0 基础 ~85%）</summary>
        public const float GroundVariantRatio = 0.15f;

        // ==================== 通用工具公式 ====================

        /// <summary>难度级 L = floor(存活秒数 / 30)（数值文档 6.1 核心公式）</summary>
        public static int DifficultyLevel(float elapsedSeconds)
        {
            return Mathf.FloorToInt(elapsedSeconds / DifficultyStepSeconds);
        }

        /// <summary>普通敌人当前血量 = 基础值 + floor(L / 4)（数值文档 6.1）</summary>
        public static int EnemyHealth(int baseHealth, int level)
        {
            return baseHealth + Mathf.FloorToInt((float)level / EnemyHealthGrowthDivisor);
        }

        /// <summary>精英当前血量 = 近战 4 倍（固定 12，不随难度级成长；4 倍基准已落 EnemyData.baseHealth）</summary>
        public static int EliteHealth(int baseHealth, int level)
        {
            return baseHealth;
        }

        // ==================== 开场电影卡（电影风格开场：每次启动游戏仅播一次） ====================
        // 纯黑底 + 居中两行台词（第一问句大字、第二署名半字号），按任意键开始；
        // 5s 内未按则在右下角淡入「按任意键开始」提醒。进程内静态标记控制：本次启动仅播一次，
        // 回标题重进不重播，完全退出游戏后下次启动（新进程）重新播放。编辑器下强制重播以便验证。

        /// <summary>开场卡：第一行台词（问句，居中大字）</summary>
        public const string TitleCardLine1 = "怎么才算好玩？";

        /// <summary>开场卡：第二行台词（署名，半字号、稍暗）</summary>
        public const string TitleCardLine2 = "——李雷";

        /// <summary>开场卡：右下角超时提醒文案（5s 未按任意键时淡入）</summary>
        public const string TitleCardHint = "按任意键开始";

        /// <summary>开场卡：第一行字号（px）</summary>
        public const int TitleCardLine1FontSize = 52;

        /// <summary>开场卡：第二行字号（px，= 第一行一半）</summary>
        public const int TitleCardLine2FontSize = 26;

        /// <summary>开场卡：提醒字号（px，小字）</summary>
        public const int TitleCardHintFontSize = 18;

        /// <summary>开场卡：台词淡入时长（秒，黑底上文字从透明到显现）</summary>
        public const float TitleCardFadeInTime = 0.6f;

        /// <summary>开场卡：确认后台词淡出时长（秒，黑底保持不透明、仅台词渐隐）</summary>
        public const float TitleCardFadeOutTime = 0.3f;

        /// <summary>开场卡：Done 阶段黑底保持时长（秒，盖住 Boot→Main 切换瞬间，相机已在 Main 首帧 snap 就位，切换丝滑）</summary>
        public const float TitleCardRevealHold = 0.15f;

        /// <summary>开场卡：超时提醒延迟（秒，开局后多久未按任意键才在右下角淡入提醒）</summary>
        public const float TitleCardHintDelay = 3f;

        /// <summary>开场卡：提醒呼吸闪烁周期（秒，透明度在 0.4~0.9 间往复）</summary>
        public const float TitleCardHintPulsePeriod = 1.6f;
    }
}
