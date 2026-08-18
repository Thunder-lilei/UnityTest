using System.Collections.Generic;
using System.IO;
using UnityEngine;
using XLua;

namespace Game.Systems
{
    
    /// <summary>
    /// Lua 运行时管理器：管理 LuaEnv 生命周期，加载 Lua 配置文件
    /// C# 通过此类与 Lua 交互，不直接操作 LuaEnv
    /// </summary>
    public class LuaManager : MonoBehaviour
    {
        public static LuaManager Instance { get; private set; }

        private LuaEnv luaEnv;
        private LuaTable upgradeModule;           // Lua 模块 M 的引用
        private LuaFunction getRandomUpgradesFunc; // 缓存 Lua 函数

        /// <summary>初始化 Lua 虚拟机，注册自定义 loader，加载升级配置</summary>
        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            luaEnv = new LuaEnv();

            // 注册自定义 loader：让 require 'upgrades' 能从 Assets/Lua/ 加载
            luaEnv.AddLoader((ref string filepath) =>
            {
                filepath = filepath.Replace('.', '/');
                string fullPath = Path.Combine(Application.dataPath, "Lua", filepath + ".lua.txt");
                if (File.Exists(fullPath))
                    return System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(fullPath));
                return null;
            });

            // 加载升级配置模块，require 返回模块表 M
            object[] results = luaEnv.DoString("return require 'upgrades'");
            upgradeModule = results[0] as LuaTable;
            if (upgradeModule == null)
            {
                Debug.LogError("[LuaManager] 无法加载 upgrades 模块");
                return;
            }
            getRandomUpgradesFunc = upgradeModule.Get<LuaFunction>("getRandomUpgrades");
        }

        /// <summary>从 Lua 获取随机升级选项</summary>
        /// <param name="count">选择的数量</param>
        /// <returns>升级数据列表</returns>
        public List<UpgradeData> GetRandomUpgrades(int count)
        {
            List<UpgradeData> result = new List<UpgradeData>();

            if (getRandomUpgradesFunc == null)
            {
                Debug.LogError("[LuaManager] Lua 函数未初始化");
                return result;
            }

            // 调用 Lua 函数，返回一个 table 数组
            object[] returns = getRandomUpgradesFunc.Call(count);
            LuaTable list = returns[0] as LuaTable;

            if (list == null)
            {
                Debug.LogError("[LuaManager] Lua 返回结果不是 table");
                return result;
            }

            // 遍历 Lua table，逐项提取字段（Lua 数组从 1 开始）
            int length = list.Length;
            for (int i = 1; i <= length; i++)
            {
                LuaTable item = list.Get<int, LuaTable>(i);
                if (item == null) continue;

                UpgradeData data = new UpgradeData
                {
                    id = item.Get<string>("id"),
                    title = item.Get<string>("title"),
                    desc = item.Get<string>("desc"),
                    action = item.Get<string>("action"),
                    value = item.Get<float>("value"),
                    iconName = item.Get<string>("icon")
                };
                result.Add(data);
                item.Dispose();
            }

            return result;
        }

        /// <summary>释放 Lua 虚拟机</summary>
        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            getRandomUpgradesFunc?.Dispose();
            upgradeModule?.Dispose();
            luaEnv?.Dispose();
        }
    }

    /// <summary>升级选项数据（C# 侧的表示）</summary>
    public class UpgradeData
    {
        public string id;       // 唯一标识
        public string title;    // 卡片标题
        public string desc;     // 卡片描述
        public string action;   // C# 方法名（用于分发）
        public float value;     // 数值参数
        public string iconName; // 图标文件名（不含扩展名，在 Resources/Icons/ 下查找）
    }
}
