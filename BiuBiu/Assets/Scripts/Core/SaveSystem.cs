using UnityEngine;

namespace BiuBiu.Core
{
    /// <summary>一局结束的战报结算结果（破纪录项后缀「新纪录！」）</summary>
    public struct BattleReport
    {
        /// <summary>本局达到的最高轮次</summary>
        public int Wave;

        /// <summary>本局击杀数</summary>
        public int Kills;

        /// <summary>轮次是否破历史纪录</summary>
        public bool WaveNewRecord;

        /// <summary>击杀数是否破历史纪录</summary>
        public bool KillsNewRecord;
    }

    /// <summary>
    /// 跨局持久化（PlayerPrefs 轻量实现，无中途存档——设计文档 2.4：死亡即结算，重开零成本）。
    /// 仅记录两项历史最佳（供战报「新纪录！」与 ESC 统计记录查看）：
    /// 1. 历史最高轮次（BestWave）；
    /// 2. 历史单局最多击杀（BestKills）。
    /// 成就系统已取消；累计统计（总开局/累计击杀/存活/Boss/首次死亡）不再持久化。
    /// v3.3：等级口径已改轮次（旧 BestLevel 键弃用，旧纪录不延续）。
    /// </summary>
    public static class SaveSystem
    {
        // PlayerPrefs 键前缀（避免与其他项目/系统冲突）
        private const string KeyPrefix = "BiuBiu.";

        // ==================== 历史最佳（单局，仅两项） ====================

        /// <summary>历史最高轮次（ESC 统计记录 / 战报展示）</summary>
        public static int BestWave => PlayerPrefs.GetInt(KeyPrefix + "BestWave", 0);

        /// <summary>历史单局最多击杀（ESC 统计记录 / 战报展示）</summary>
        public static int BestKills => PlayerPrefs.GetInt(KeyPrefix + "BestKills", 0);

        // ==================== 结算 ====================

        /// <summary>
        /// 死亡结算：对比并写入历史最佳（GameBootstrap 死亡流程调用）。
        /// 仅轮次与击杀两项参与破纪录判定。
        /// </summary>
        /// <param name="run">本局统计</param>
        /// <returns>战报（含两项是否破纪录，供战报 UI 加「新纪录！」后缀）</returns>
        public static BattleReport SettleRun(RunStats run)
        {
            var report = new BattleReport
            {
                Wave = run.Wave,
                Kills = run.Kills
            };

            // 历史最佳对比（严格大于才算破纪录）
            report.WaveNewRecord = run.Wave > BestWave;
            report.KillsNewRecord = run.Kills > BestKills;

            // 写入新纪录
            if (report.WaveNewRecord)
                PlayerPrefs.SetInt(KeyPrefix + "BestWave", run.Wave);
            if (report.KillsNewRecord)
                PlayerPrefs.SetInt(KeyPrefix + "BestKills", run.Kills);

            PlayerPrefs.Save();
            return report;
        }

        /// <summary>清空全部存档（设置界面「清空战绩」备用）</summary>
        public static void WipeAll()
        {
            PlayerPrefs.DeleteAll(); // 本项目无其他 PlayerPrefs 键，全删等价于清本项目存档
            PlayerPrefs.Save();
        }
    }
}
