using UnityEngine;

namespace BiuBiu.Core
{
    /// <summary>
    /// 单局统计（死亡战报数据源：坚持了 mm:ss / 打到第 N 轮 / 砍了 X 个；
    /// 成就判定数据源：轮次 5 / 存活 10:00 / 击杀 500 / 击杀大蜘蛛）。
    /// 纯 C# 类，由 GameBootstrap 持有并每局重建（局开始=新实例，天然归零）。
    /// 注意：Play 中脚本热重载会清空普通 C# 对象引用——GameBootstrap 入口需判 null 自愈重建（工程纪律）。
    /// </summary>
    public class RunStats
    {
        /// <summary>本局存活秒数（局时，难度曲线唯一输入）</summary>
        public float ElapsedSeconds;

        /// <summary>本局击杀数（普通+精英+Boss 合计）</summary>
        public int Kills;

        /// <summary>本局达到的最高轮次（HUD「第 N 轮」与战报「打到第 N 轮」共用；EnemySpawner2D 推进）</summary>
        public int Wave = 1;

        /// <summary>本局是否击杀过任一只大蜘蛛（成就「蛛网突围」判定）</summary>
        public bool BossKilled;

        /// <summary>当前难度级 L = floor(局时 / 30)（数值文档 6.1，敌人血量成长共用输入）</summary>
        public int DifficultyLevel => GameBalance.DifficultyLevel(ElapsedSeconds);

        /// <summary>局时格式化为 mm:ss（战报与 HUD 计时共用；不足 10 分钟补零）</summary>
        public string ElapsedTimeString
        {
            get
            {
                int total = Mathf.FloorToInt(ElapsedSeconds);
                int m = total / 60;
                int s = total % 60;
                return $"{m:00}:{s:00}";
            }
        }
    }
}
