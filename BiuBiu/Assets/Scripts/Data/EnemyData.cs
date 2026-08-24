using UnityEngine;

namespace BiuBiu.Data
{
    /// <summary>敌人类型（数值文档 5 章：普通走配额随机刷，精英/Boss 按轮次登场）</summary>
    public enum EnemyType
    {
        Normal, // 基础敌人（近战扇形 / 远程直线 / 近战横扫混合，配额内刷）
        Elite,  // 精英（第 3 轮起每轮 +1 只；八方向投掷封锁）
        Boss    // Boss（第 5、10 轮各 1 只；八方向扇形横扫 + 直线冲撞）
    }

    /// <summary>普通敌人攻击方式（数值文档 5.1「攻击」列）</summary>
    public enum EnemyAttackType
    {
        RangedLine,  // 远程直线（远程：直线弹丸）
        ArcSweep,    // 范围横扫（近战横扫型 120°/半径 2.0）
        OctaThrow    // 八方向投掷（精英专用：朝玩家 1 发 + 米字其余 7 发，投掷距离 = 普通远程 ×2）
    }

    /// <summary>
    /// 敌人数值定义（数值文档 5 章一一对应；设计文档 14 章：EnemyData SO 数据驱动，
    /// 新增敌人=新增配置不改代码）。
    /// 基础值经 GameBalance 第 6 章成长公式得到实际值；伤害恒定不成长（次数制原则）。
    /// Boss 血量特殊：实际 = baseHealth × 第 n 只（数值文档 5.3，由刷怪器注入 n）。
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy_New", menuName = "BiuBiu/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("标识")]
        [Tooltip("配置标识（ranged / melee_sweep / elite / boss），代码与文档引用键")]
        public string enemyId;

        [Tooltip("显示名（Hierarchy 可读性与提示文案用）")]
        public string displayName;

        [Tooltip("敌人类型：基础（配额内）/ 精英（独立定时）/ Boss（独立定时）")]
        public EnemyType enemyType;

        [Tooltip("敌人预制体（刷怪器生成用；灰盒阶段可为空，由刷怪器代码兜底生成灰盒）")]
        public GameObject prefab;

        [Header("基础数值（数值文档 5.1/5.2/5.3；血量实际值 = 基础值 + 第 6 章成长公式）")]
        [Tooltip("基础血量（普通实际 = GameBalance.EnemyHealth(基础值, L)；精英 = EliteHealth；Boss = 基础值 × 第 n 只）")]
        public int baseHealth;

        [Tooltip("移速（tile/s，v3.0 移速×2）：近战 3.6 / 远程 3.0 / 近战横扫 2.4 / 精英 3.0 / Boss 1.6")]
        public float moveSpeed;

        [Tooltip("对玩家伤害（心/次，恒定不成长）")]
        public int damage;

        [Tooltip("体型（tile，碰撞体与渲染缩放基准）：普通 1×1 / 精英 2×2（近战 2 倍）/ Boss 4×4（近战 4 倍）；实际由刷怪器按倍率覆盖")]
        public Vector2 bodySize = Vector2.one;

        [Header("普通敌人行为（数值文档 5.1；精英复用横扫字段，Boss 忽略本组）")]
        [Tooltip("攻击方式：近战单点 / 远程直线 / 范围横扫")]
        public EnemyAttackType attackType;

        [Tooltip("攻击前摇（秒）：贴身/进入射程后蓄力，期间脱离则取消")]
        public float windupTime;

        [Tooltip("攻击间隔（秒）：命中/出手后进入冷却")]
        public float attackInterval;

        [Tooltip("射程（tile）：近战=贴身判定半径 0.6；投掷=开火距离 6.0；横扫=扇形半径 2.0（精英 2.5）")]
        public float attackRange;

        [Tooltip("弹速（tile/s，仅 RangedLine 用；远程 6.0，其余=0）")]
        public float projectileSpeed;

        [Tooltip("类型解锁时间（秒，仅基础敌人用；开局近战/远程/横扫混合=全 0）：精英/Boss=0（走独立定时器）")]
        public float unlockTime;

        [Header("精英八方向投掷（数值文档 5.2；仅精英使用，普通/Boss 忽略）")]
        [Tooltip("投掷前摇（秒）：0.6（原地渐红抖动，与横扫同演出）")]
        public float throwWindup;

        [Tooltip("投掷间隔（秒）：1.8（出手后进入冷却）")]
        public float throwInterval;

        [Tooltip("投掷距离（tile）：= 普通远程 attackRange × GameBalance.EliteThrowRangeScale（默认 12.0）")]
        public float throwRange;

        [Tooltip("投掷弹速（tile/s）：5.0")]
        public float throwSpeed;

        [Header("Boss 技能循环（数值文档 5.3；仅 Boss 使用：环形 → 2s → 直线连射 → 2s → 循环）")]
        [Tooltip("接触伤害结算间隔（秒）：0.5（接触伤害 1 心/次按此频率结算）")]
        public float contactTickInterval;

        [Tooltip("环形能量弹每圈数量（发）：12，均匀圆周")]
        public int ringBulletCount;

        [Tooltip("环形弹弹速（tile/s）：1.5")]
        public float ringBulletSpeed;

        [Tooltip("环形弹冷却（秒）：6.0")]
        public float ringCooldown;

        [Tooltip("直线连射每组数量（发）：5，间隔 0.25s，朝玩家方向")]
        public int lineBulletCount;

        [Tooltip("直线连射组内间隔（秒）：0.25")]
        public float lineFireInterval;

        [Tooltip("直线弹弹速（tile/s）：3.0")]
        public float lineBulletSpeed;

        [Tooltip("直线连射冷却（秒）：8.0")]
        public float lineCooldown;

        [Tooltip("弹丸碰撞半径（tile）：0.3（环形/直线通用，碰撞墙销毁）")]
        public float bulletRadius = 0.3f;

        [Header("Boss 八方向扇形横扫（数值文档 5.3 重构；仅 Boss 使用）")]
        [Tooltip("扇形横扫前摇（秒）：0.6")]
        public float sweepWindup;

        [Tooltip("扇形横扫间隔（秒）：3.0")]
        public float sweepInterval;

        [Tooltip("扇形横扫半径（tile）：= GameBalance.BossSweepRadius（默认 4.0，近战 2 倍；面积 4 倍）")]
        public float sweepRadius;

        [Tooltip("扇形横扫角度（度）：= GameBalance.BossSweepAngle（默认 120）")]
        public float sweepAngle;

        [Tooltip("每次横扫发射的扇形方向数（米字）：= GameBalance.BossSweepDirections（默认 8）")]
        public int sweepDirections;

        [Tooltip("扇形横扫弹速（tile/s）：3.0")]
        public float sweepBulletSpeed;

        [Header("Boss 直线冲撞（数值文档 5.3 重构；仅 Boss 使用）")]
        [Tooltip("冲撞前摇（秒）：0.6")]
        public float bossChargeWindup;

        [Tooltip("冲撞速度（tile/s）：= GameBalance.BossChargeSpeed（默认 8.0）")]
        public float bossChargeSpeed;

        [Tooltip("冲撞距离（tile）：= GameBalance.BossChargeDistance（默认 6.0）")]
        public float bossChargeDistance;

        [Tooltip("冲撞冷却（秒）：6.0")]
        public float bossChargeCooldown;

        [Tooltip("冲撞命中击退玩家的距离（tile）：1.5")]
        public float bossChargeKnockback;
    }
}
