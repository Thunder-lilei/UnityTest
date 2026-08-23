using BiuBiu.Core;

namespace BiuBiu.Player
{
    /// <summary>
    /// 玩家成长属性聚合（数值文档第 7 章：每轮结束自动属性微增的作用目标）。
    /// EnemySpawner2D 每轮全灭时累加移速/攻击加成；玩家控制器与弹弓武器每帧读取
    /// ——数值与逻辑分离，无魔法数。基础值来自 GameBalance；每局重建（重开零成本）。
    /// 纯 C# 类由 GameBootstrap 持有（热重载自愈：判 null 重建）。
    /// </summary>
    public class PlayerStats
    {
        /// <summary>最大生命（心）：基础 2（上限常量 8 保留在 GameBalance，当前无成长途径）</summary>
        public int MaxHealth = GameBalance.PlayerMaxHealth;

        /// <summary>移速乘数：基础 1.0；每轮结束移速 +0.5 tile/s（换算为乘数增量累加）</summary>
        public float MoveSpeedMult = 1f;

        /// <summary>受击无敌时长（秒）：当前恒为基础值（预留每轮微增扩展点）</summary>
        public float InvulnDuration = GameBalance.PlayerInvulnDuration;

        /// <summary>翻滚位移（tile）：当前恒为基础值（预留每轮微增扩展点）</summary>
        public float RollDistance = GameBalance.RollDistance;

        /// <summary>攻击力浮点加成：每轮结束 +0.5，作用于弹丸伤害（向下取整，见 SlingWeapon.Fire）</summary>
        public float AttackBonusFloat = 0f;

        /// <summary>当前实际移速（tile/s）= 基础 × 乘数</summary>
        public float CurrentMoveSpeed => GameBalance.PlayerMoveSpeed * MoveSpeedMult;
    }
}
