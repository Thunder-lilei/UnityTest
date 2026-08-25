using BiuBiu.Player;
using BiuBiu.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BiuBiu.Core
{
    /// <summary>
    /// 全局游戏入口与系统粘合层（设计文档 14 章工程架构：Boot 场景 → GameBootstrap → 加载 Main）。
    /// 职责（M1 范围）：
    /// 1. 单例 + DontDestroyOnLoad（Boot 场景常驻，切 Main 不销毁）；
    /// 2. 场景流：Boot → Main（M1 灰盒无开始页，点开就玩；回到标题=重进 Boot=再进 Main）；
    /// 3. 持有 RunStats（单局统计）并推进局时（难度曲线唯一输入）；
    /// 4. 局生命周期：StartRun（开局登记）/ EndRun（死亡结算，产出 BattleReport 供战报 UI）；
    /// 5. 事件汇总：敌人击杀计数转发（敌人不直接持有统计）。
    /// Main 场景内容（地图/玩家/系统/UI）由 RuntimeSceneBuilder 运行时构建，装配完成回调 OnMainSceneReady 开局。
    /// 注意：所有外部引用走惰性自愈（热重载清空普通 C# 引用——工程纪律）。
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        /// <summary>全局单例（Boot 场景唯一实例）</summary>
        public static GameBootstrap Instance { get; private set; }

        /// <summary>本局统计（局开始重建；热重载后判 null 自愈重建）</summary>
        public RunStats RunStats { get; private set; }

        /// <summary>玩家成长属性聚合（每轮结束自动微增的作用目标；武器/玩家读取）</summary>
        public PlayerStats PlayerStats { get; private set; }

        /// <summary>玩家引用（Main 场景注入或场景查找；热重载后自愈）</summary>
        public PlayerController Player { get; private set; }

        /// <summary>局是否进行中（false=未开始/已死亡结算）</summary>
        public bool IsRunActive { get; private set; }

        /// <summary>防重复导航标记（sceneLoaded 与 Start 双通道触发的去重）</summary>
        private bool mainLoadQueued;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); // 回到标题时 Boot 重进：常驻实例已存在，本副本销毁
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// 场景加载回调：进入 Boot（首次启动或回到标题）→ 进 Main / 播开场卡。
        /// 首次启动时 sceneLoaded 早于 Start 触发（引擎时序），回到标题时常驻实例借此接管。
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            mainLoadQueued = false; // 任意场景加载完成即解除导航锁
            if (scene.name == "Boot") EnterGameOrTitle();
        }

        private void Start()
        {
            // 兜底通道：个别引擎时序下首场景 sceneLoaded 早于 OnEnable 注册 → Start 再试一次（已排队则去重）
            if (SceneManager.GetActiveScene().name == "Boot") EnterGameOrTitle();
        }

        /// <summary>
        /// Boot → 进游戏：开场电影卡（TitleCard）仅首次启动播放一次，确认后由它负责 LoadScene("Main")；
        /// 已播放过则直接进 Main。避免 GameBootstrap 与 TitleCard 重复导航。
        /// </summary>
        private void EnterGameOrTitle()
        {
            if (mainLoadQueued) return;
#if !UNITY_EDITOR
            // 真机/打包：本次启动已播过 → 直接进 Main（不重复播）
            if (TitleCard.HasPlayedThisSession)
            {
                GoToMain();
                return;
            }
#endif
            // 尚未播开场卡：尝试播放（编辑器下忽略"已播"强制重播以便验证）
            TitleCard.TryPlay();
            mainLoadQueued = true; // 锁住，等 TitleCard 确认后再导航（防 Start/sceneLoaded 双通道重入）
        }

        /// <summary>进 Main（单次导航；重复调用去重）</summary>
        private void GoToMain()
        {
            if (mainLoadQueued) return;
            mainLoadQueued = true;
            SceneManager.LoadScene("Main");
        }

        /// <summary>
        /// 获取/创建全局单例（直接 Play Main 调试时由 RuntimeSceneBuilder.Start 兜底创建）。
        /// </summary>
        public static GameBootstrap EnsureInstance()
        {
            if (Instance == null)
            {
                var go = new GameObject("[GameBootstrap]");
                go.AddComponent<GameBootstrap>(); // Awake 内完成单例登记与 DontDestroyOnLoad
            }
            return Instance;
        }

        /// <summary>
        /// Main 场景装配完成（RuntimeSceneBuilder.Start 调用）：登记玩家 + 开局。
        /// 再战/重开/回标题再进都会重新走这里（重载 Main = 全新一局）。
        /// </summary>
        public void OnMainSceneReady()
        {
            Player = FindFirstObjectByType<PlayerController>();
            StartRun();
        }

        private void Update()
        {
            // 局时推进（暂停时 timeScale=0，deltaTime=0，自然停表——ESC 暂停与升级暂停共用该机制）
            if (IsRunActive && RunStats != null)
            {
                RunStats.ElapsedSeconds += Time.deltaTime;
            }
        }

        /// <summary>玩家引用自愈（敌人/掉落物等调用方：先判 null 再取）</summary>
        public PlayerController GetPlayer()
        {
            if (Player == null)
            {
                Player = FindFirstObjectByType<PlayerController>();
            }
            return Player;
        }

        /// <summary>
        /// 开局：重建统计。
        /// 再来一局时也会走到这里（重开零成本，无中途存档）。
        /// </summary>
        public void StartRun()
        {
            RunStats = new RunStats();
            PlayerStats = new PlayerStats(); // 成长属性随每局重建（重开零成本）
            IsRunActive = true;
            ObjectPool.ClearAll(); // 清掉上局回池的闲置实例（活跃实例已随场景卸载销毁）
        }

        /// <summary>
        /// 死亡结算（玩家死亡流程末尾调用）。
        /// 停表 → SettleRun 写存档并产出战报。
        /// </summary>
        /// <returns>战报（两项历史最佳对比，战报 UI 用）</returns>
        public BattleReport EndRun()
        {
            IsRunActive = false;
            BattleReport report = SaveSystem.SettleRun(RunStats);
            Debug.Log($"[GameBootstrap] 力战而竭：打到第 {RunStats.Wave} 轮 / 砍了 {RunStats.Kills} 个"
                + (report.WaveNewRecord || report.KillsNewRecord ? " —— 新纪录！" : ""));
            return report;
        }

        // ==================== 事件汇总（各系统 → 统计） ====================

        /// <summary>
        /// 敌人击杀通知（EnemyBase2D 死亡时调用）：计数。
        /// </summary>
        public void NotifyEnemyKilled(bool isBoss)
        {
            if (RunStats == null) return;
            RunStats.Kills++;
        }
    }
}
