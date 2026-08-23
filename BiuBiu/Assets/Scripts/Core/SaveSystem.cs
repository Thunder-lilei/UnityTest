using UnityEngine;

namespace BiuBiu.Core
{
    /// <summary>一局结束的战报结算结果（破纪录项后缀「新纪录！」）</summary>
    public struct BattleReport
    {
        /// <summary>本局存活秒数</summary>
        public float SurvivalSeconds;

        /// <summary>本局达到的最高轮次</summary>
        public int Wave;

        /// <summary>本局击杀数</summary>
        public int Kills;

        /// <summary>存活时间是否破历史纪录</summary>
        public bool TimeNewRecord;

        /// <summary>轮次是否破历史纪录</summary>
        public bool WaveNewRecord;

        /// <summary>击杀数是否破历史纪录</summary>
        public bool KillsNewRecord;
    }

    /// <summary>
    /// 跨局持久化（PlayerPrefs 轻量实现，无中途存档——设计文档 2.4：死亡即结算，重开零成本）。
    /// 承载两类数据：
    /// 1. 历史最佳（单局）：死亡战报「新纪录！」对比依据；
    /// 2. 累计统计：成就·统计界面（总开局/累计击杀/累计存活/大蜘蛛击杀数）
    ///    与成就五项判定（轮次 5 / 存活 600s / 击杀 500 / 击杀大蜘蛛 / 首次死亡）。
    /// M1 只做数据存取；成就 UI 与统计界面展示在 M4 元游戏里程碑接入。
    /// v3.3：等级口径已改轮次（旧 BestLevel 键弃用，旧纪录不延续）。
    /// </summary>
    public static class SaveSystem
    {
        // PlayerPrefs 键前缀（避免与其他项目/系统冲突）
        private const string KeyPrefix = "BiuBiu.";

        // ==================== 历史最佳（单局） ====================

        /// <summary>历史最长单局存活（秒）</summary>
        public static float BestSurvivalTime => PlayerPrefs.GetFloat(KeyPrefix + "BestSurvivalTime", 0f);

        /// <summary>历史最高轮次</summary>
        public static int BestWave => PlayerPrefs.GetInt(KeyPrefix + "BestWave", 0);

        /// <summary>历史单局最多击杀</summary>
        public static int BestKills => PlayerPrefs.GetInt(KeyPrefix + "BestKills", 0);

        // ==================== 累计统计 ====================

        /// <summary>总开局次数（成就·统计界面「总开局 X 次」）</summary>
        public static int TotalRuns => PlayerPrefs.GetInt(KeyPrefix + "TotalRuns", 0);

        /// <summary>累计击杀（「累计击杀 X 个」）</summary>
        public static int TotalKills => PlayerPrefs.GetInt(KeyPrefix + "TotalKills", 0);

        /// <summary>累计存活总时长（秒，「累计存活 mm:ss」）</summary>
        public static float TotalSurvivalSeconds => PlayerPrefs.GetFloat(KeyPrefix + "TotalSurvivalSeconds", 0f);

        /// <summary>累计击杀大蜘蛛只数（成就「蛛网突围」备用数据）</summary>
        public static int TotalBossKills => PlayerPrefs.GetInt(KeyPrefix + "TotalBossKills", 0);

        /// <summary>是否死亡过至少一次（成就「摔了个跟头」：首次死亡）</summary>
        public static bool HasDied => PlayerPrefs.GetInt(KeyPrefix + "HasDied", 0) == 1;

        // ==================== 结算 ====================

        /// <summary>
        /// 局开始登记（总开局 +1；GameBootstrap 开局调用）
        /// </summary>
        public static void RegisterRunStart()
        {
            PlayerPrefs.SetInt(KeyPrefix + "TotalRuns", TotalRuns + 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 死亡结算：写入本局统计并对比历史最佳（GameBootstrap 死亡流程调用）。
        /// 同时更新累计统计（击杀/存活/大蜘蛛/首次死亡标记）。
        /// </summary>
        /// <param name="run">本局统计</param>
        /// <returns>战报（含三项是否破纪录，供战报 UI 加「新纪录！」后缀）</returns>
        public static BattleReport SettleRun(RunStats run)
        {
            var report = new BattleReport
            {
                SurvivalSeconds = run.ElapsedSeconds,
                Wave = run.Wave,
                Kills = run.Kills
            };

            // 历史最佳对比（严格大于才算破纪录）
            report.TimeNewRecord = run.ElapsedSeconds > BestSurvivalTime;
            report.WaveNewRecord = run.Wave > BestWave;
            report.KillsNewRecord = run.Kills > BestKills;

            // 写入新纪录
            if (report.TimeNewRecord)
                PlayerPrefs.SetFloat(KeyPrefix + "BestSurvivalTime", run.ElapsedSeconds);
            if (report.WaveNewRecord)
                PlayerPrefs.SetInt(KeyPrefix + "BestWave", run.Wave);
            if (report.KillsNewRecord)
                PlayerPrefs.SetInt(KeyPrefix + "BestKills", run.Kills);

            // 累计统计
            PlayerPrefs.SetInt(KeyPrefix + "TotalKills", TotalKills + run.Kills);
            PlayerPrefs.SetFloat(KeyPrefix + "TotalSurvivalSeconds", TotalSurvivalSeconds + run.ElapsedSeconds);
            if (run.BossKilled)
                PlayerPrefs.SetInt(KeyPrefix + "TotalBossKills", TotalBossKills + 1);
            PlayerPrefs.SetInt(KeyPrefix + "HasDied", 1); // 结算即本局已死亡（成就「摔了个跟头」）

            PlayerPrefs.Save();
            return report;
        }

        /// <summary>清空全部存档（设置界面「清空战绩」备用；M4 接入 UI）</summary>
        public static void WipeAll()
        {
            PlayerPrefs.DeleteAll(); // 本项目无其他 PlayerPrefs 键，全删等价于清本项目存档
            PlayerPrefs.Save();
        }
    }
}
