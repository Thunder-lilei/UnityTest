using System.Collections.Generic;
using UnityEngine;

namespace BiuBiu.Data
{
    // 说话者类型（与敌人攻击方式/身份对齐，外加 Player）
    public enum SpeakerType
    {
        Player,
        Melee,       // 近战（扇形型）
        Ranged,      // 远程（直线弹）
        MeleeSweep,  // 近战（横扫型）
        Elite,       // 精英
        Boss         // Boss
    }

    // 事件标签
    public enum SpeechEvent
    {
        Spawn,   // 登场
        Hit,     // 受击
        Attack,  // 出手
        Death,   // 死亡
        RoundUp  // 主角每轮变强
    }

    // 单条文案条目：说话者 + 事件 + 多句候选（仅在用 SpeechBank SO 资产时填充）
    [System.Serializable]
    public class SpeechEntry
    {
        public SpeakerType speaker;
        public SpeechEvent speechEvent;
        [TextArea(1, 3)] public string[] lines;
    }

    /// <summary>
    /// 数据驱动文案池：按 (说话者, 事件) 随机抽句。
    /// 文案来源优先级（均从 Resources 加载，零编译依赖）：
    ///   1) 本 SO 的 entries 字段（策划在编辑器用 SpeechBank.asset 维护，可选覆盖）
    ///   2) 纯文本文件 Resources/Data/Speech/speech.txt（默认出处，改文案只改这个 txt 即可，无需动脚本）
    /// 文本格式（# 开头为注释行；key=value，多句用 | 分隔）：
    ///   Player.Hit : 哎呦！我的午饭！ | 谁扔的？！
    ///   Melee.Spawn : 啊…脑子… | 我是来散步的
    /// </summary>
    [CreateAssetMenu(fileName = "SpeechBank", menuName = "BiuBiu/数据/SpeechBank")]
    public class SpeechBank : ScriptableObject
    {
        // 可选：编辑器 SO 覆盖（留空则纯用 txt 文本文件）
        public SpeechEntry[] entries;

        // 纯文本文案文件路径（Resources 下，不含扩展名）
        private const string TxtPath = "Data/Speech/speech";

        private Dictionary<(SpeakerType, SpeechEvent), List<string>> _map;
        private bool _loaded;

        private void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _map = new Dictionary<(SpeakerType, SpeechEvent), List<string>>();

            // 1) 先载入 SO entries（若有）
            if (entries != null)
            {
                foreach (var e in entries)
                {
                    var key = (e.speaker, e.speechEvent);
                    if (!_map.TryGetValue(key, out var list))
                    {
                        list = new List<string>();
                        _map[key] = list;
                    }
                    if (e.lines != null) list.AddRange(e.lines);
                }
            }

            // 2) 再并入 txt 文本文件（若 SO 为空或不存在，txt 即唯一出处）
            LoadFromTxt();
        }

        // 从 speech.txt 解析文案，合并进 _map
        private void LoadFromTxt()
        {
            var asset = Resources.Load<TextAsset>(TxtPath);
            if (asset == null) return;

            var lines = asset.text.Split('\n');
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue; // 忽略空行与注释

                int colon = line.IndexOf(':');
                if (colon <= 0) continue;

                string keyStr = line.Substring(0, colon).Trim();
                string valStr = line.Substring(colon + 1).Trim();
                if (!ParseKey(keyStr, out var speaker, out var speechEvent)) continue;

                var sentences = valStr.Split('|');
                var list = GetOrAdd(speaker, speechEvent);
                foreach (var s in sentences)
                {
                    var t = s.Trim();
                    if (t.Length > 0) list.Add(t);
                }
            }
        }

        // 解析 "Player.Hit" 形式的 key
        private bool ParseKey(string key, out SpeakerType speaker, out SpeechEvent speechEvent)
        {
            speaker = default;
            speechEvent = default;
            int dot = key.IndexOf('.');
            if (dot <= 0) return false;
            string sp = key.Substring(0, dot).Trim();
            string ev = key.Substring(dot + 1).Trim();
            if (!System.Enum.TryParse<SpeakerType>(sp, true, out speaker)) return false;
            if (!System.Enum.TryParse<SpeechEvent>(ev, true, out speechEvent)) return false;
            return true;
        }

        private List<string> GetOrAdd(SpeakerType speaker, SpeechEvent speechEvent)
        {
            var key = (speaker, speechEvent);
            if (!_map.TryGetValue(key, out var list))
            {
                list = new List<string>();
                _map[key] = list;
            }
            return list;
        }

        // 随机抽一句；无匹配返回 null
        public string GetLine(SpeakerType speaker, SpeechEvent speechEvent)
        {
            EnsureLoaded();
            if (_map.TryGetValue((speaker, speechEvent), out var list) && list.Count > 0)
            {
                return list[Random.Range(0, list.Count)];
            }
            return null;
        }
    }
}
