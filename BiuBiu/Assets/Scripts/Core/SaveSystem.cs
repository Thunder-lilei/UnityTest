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
    /// 跨局持久化（JSON 文件实现，无中途存档——设计文档 2.4：死亡即结算，重开零成本）。
    /// 仅记录两项历史最佳（供战报「新纪录！」与 ESC 统计记录查看）：
    /// 1. 历史最高轮次（BestWave）；
    /// 2. 历史单局最多击杀（BestKills）。
    /// 成就系统已取消；累计统计（总开局/累计击杀/存活/Boss/首次死亡）不再持久化。
    /// 存储位置：Application.persistentDataPath + "/save.json"（Windows 即 AppData/LocalLow/DefaultCompany/BiuBiu/save.json）。
    /// </summary>
    public static class SaveSystem
    {
        // JSON 存档路径（进程内缓存，避免频繁读文件）
        private static string SavePath => Application.persistentDataPath + "/save.json";

        [System.Serializable]
        private class SaveData
        {
            public int BestWave;
            public int BestKills;
        }

        private static SaveData _cache;
        private static SaveData Cache
        {
            get
            {
                if (_cache == null) _cache = Load();
                return _cache;
            }
        }

        private static SaveData Load()
        {
            try
            {
                if (System.IO.File.Exists(SavePath))
                {
                    string json = System.IO.File.ReadAllText(SavePath);
                    var data = JsonUtility.FromJson<SaveData>(json);
                    if (data != null) return data;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveSystem] 读取存档失败，重置为默认值：{e.Message}");
            }
            return new SaveData(); // 缺失/损坏 → 全 0
        }

        private static void Persist()
        {
            try
            {
                string json = JsonUtility.ToJson(_cache, true);
                System.IO.File.WriteAllText(SavePath, json);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveSystem] 写入存档失败：{e.Message}");
            }
        }

        // ==================== 历史最佳（单局，仅两项） ====================

        /// <summary>历史最高轮次（ESC 统计记录 / 战报展示）</summary>
        public static int BestWave => Cache.BestWave;

        /// <summary>历史单局最多击杀（ESC 统计记录 / 战报展示）</summary>
        public static int BestKills => Cache.BestKills;

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
            report.WaveNewRecord = run.Wave > Cache.BestWave;
            report.KillsNewRecord = run.Kills > Cache.BestKills;

            // 写入新纪录（内存 + 落盘）
            if (report.WaveNewRecord)
                Cache.BestWave = run.Wave;
            if (report.KillsNewRecord)
                Cache.BestKills = run.Kills;
            Persist();

            return report;
        }

        /// <summary>清空全部存档（设置界面「清空战绩」备用）</summary>
        public static void WipeAll()
        {
            _cache = new SaveData(); // 全 0
            Persist();
        }
    }
}
