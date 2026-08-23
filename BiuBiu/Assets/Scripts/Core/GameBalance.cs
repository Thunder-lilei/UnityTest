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

        /// <summary>玩家移速（tile/s，快于全部普通敌人）</summary>
        public const float PlayerMoveSpeed = 4.0f;

        /// <summary>玩家初始血量（心）</summary>
        public const int PlayerMaxHealth = 2;

        /// <summary>玩家血量上限（常量保留；当前无成长途径，供后续扩展）</summary>
        public const int PlayerHealthCap = 8;

        /// <summary>玩家受击伤害（心/次，全部敌人统一）</summary>
        public const int PlayerDamagePerHit = 1;

        /// <summary>玩家受击无敌时长（秒，与闪白同步）</summary>
        public const float PlayerInvulnDuration = 1.0f;

        /// <summary>翻滚无敌时长（秒，动作周期前段）</summary>
        public const float RollInvulnTime = 0.30f;

        /// <summary>翻滚动作周期（秒；无冷却：本次动作完毕才可再次翻滚，周期自然限频）</summary>
        public const float RollDuration = 0.40f;

        /// <summary>翻滚位移距离（tile，位移曲线缓入缓出）</summary>
        public const float RollDistance = 2.5f;

        /// <summary>玩家圆形碰撞半径（tile）</summary>
        public const float PlayerCollisionRadius = 0.4f;

        // ==================== 第 5 章 敌人数值（基础值；实际值经第 6 章成长公式） ====================
        // 基础值落在 EnemyData ScriptableObject（数据驱动），此处只放公式与通用参数。

        /// <summary>敌人对玩家伤害恒定（心/次，次数制原则不做成长）</summary>
        public const int EnemyDamageToPlayer = 1;

        /// <summary>精英登场间隔（秒；3:00 首只，此后每 180s 一只，独立计时不占普通配额）</summary>
        public const float EliteSpawnInterval = 180f;

        /// <summary>精英首次登场时间（秒）</summary>
        public const float EliteFirstSpawnTime = 180f;

        /// <summary>大蜘蛛登场间隔（秒；5:00 首只，此后每 300s 一只，一只比一只强）</summary>
        public const float BossSpawnInterval = 300f;

        /// <summary>大蜘蛛首次登场时间（秒）</summary>
        public const float BossFirstSpawnTime = 300f;

        // ==================== 第 6 章 时间难度曲线（仅驱动敌人血量成长） ====================

        /// <summary>难度级时间步长（秒）：难度级 L = floor(存活秒数 / 该值)，每 30s +1 级</summary>
        public const float DifficultyStepSeconds = 30f;

        /// <summary>普通敌人血量成长：基础值 + floor(L / 该值)，约每 2 分钟全体 +1 血</summary>
        public const int EnemyHealthGrowthDivisor = 4;

        /// <summary>精英血量成长：10 + floor(L / 该值)，比普通敌人更陡</summary>
        public const int EliteHealthGrowthDivisor = 2;

        // ==================== 第 6.3 章 性能保护上限 ====================

        /// <summary>弹丸同屏上限（超限时优先回收离主角最远的）</summary>
        public const int MaxProjectilesOnScreen = 80;

        /// <summary>痕迹（血迹）池上限</summary>
        public const int MaxGroundStains = 500;

        /// <summary>尸体留存上限（超出时最旧尸体渐隐回池，防长局堆积）</summary>
        public const int MaxCorpses = 200;

        // ==================== 第 9 章 交互与演出数值 ====================

        /// <summary>成就 toast 显示时长（秒）</summary>
        public const float AchievementToastDuration = 2.0f;

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

        /// <summary>蓄力弹丸视觉：起始距离（角色前方，tile，见数值文档第 9 章）</summary>
        public const float ChargeOrbStartDist = 0.5f;

        /// <summary>蓄力弹丸视觉：满蓄力时拉回到角色身后的距离（tile，弹弓拉皮筋感）</summary>
        public const float ChargeOrbMaxPull = 0.8f;

        /// <summary>弹弓弹丸速度（零级=白/一级=黄/二级=红，见数值文档 4.1）</summary>
        public static readonly float[] ProjectileSpeeds = { 7f, 9f, 14f };

        /// <summary>弹弓弹丸最大反弹次数（零级=0/一级=1/二级=3，见数值文档 4.1）</summary>
        public static readonly int[] ProjectileMaxBounces = { 0, 1, 3 };

        /// <summary>弹弓弹丸碰撞半径</summary>
        public const float ProjectileRadius = 0.3f;

        /// <summary>弹弓弹丸存活上限（秒）</summary>
        public const float ProjectileLifetime = 5f;

        /// <summary>hitstop 时长（秒，仅精英/大蜘蛛终结一击）</summary>
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

        /// <summary>尸体留存：变色（暗绿，区分活体）</summary>
        public static readonly Color CorpseTint = new Color(0.3f, 0.35f, 0.2f, 1f);

        /// <summary>相机前瞻偏移距离（tile，朝玩家移动方向前探）</summary>
        public const float CameraLookAheadDistance = 1.5f;

        /// <summary>相机前瞻偏移平滑时间（秒）</summary>
        public const float CameraLookAheadSmoothTime = 0.15f;

        // ==================== 大地图（设计文档 3.1 章） ====================
        public const int MapSizeTiles = 108;

        // ==================== 第 12 章 地图障碍参数（数值文档 v2.4） ====================

        /// <summary>障碍撒点数量（整图）</summary>
        public const int ObstacleCount = 48;

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

        /// <summary>精英当前血量 = 10 + floor(L / 2)（数值文档 6.1；基础 10 落在 EnemyData）</summary>
        public static int EliteHealth(int baseHealth, int level)
        {
            return baseHealth + Mathf.FloorToInt((float)level / EliteHealthGrowthDivisor);
        }
    }
}
