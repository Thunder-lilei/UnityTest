namespace BiuBiu.Core
{
    /// <summary>
    /// 全局游戏状态（UI 面板写、玩法系统读——输入锁与互斥的唯一出处，避免玩法反向引用 UI 命名空间）。
    /// 灰盒阶段 OnGUI 面板共三个：死亡战报 / ESC 暂停（+M4 设置）。
    /// timeScale=0 由各面板自理（暂停机制）；本类只承载互斥与输入锁语义。
    /// </summary>
    public static class GameState
    {
        /// <summary>ESC 暂停中（PauseMenu 写）</summary>
        public static bool Paused;

        /// <summary>死亡战报打开中（DeathPanel 写）</summary>
        public static bool DeathReportOpen;

        /// <summary>输入锁：任一面板打开时玩法输入（攻击/移动/翻滚）应冻结</summary>
        public static bool InputLocked => Paused || DeathReportOpen;

        /// <summary>
        /// 开场卡确认后的「攻击抑制直到松开」：玩家用按住左键/任意键跳过开场卡时，原生 Input 的按下会残留，
        /// 进游戏若仍按住会误触发攻击。设 true 后 SlingWeapon 不起手，直到检测到一次鼠标/按键松开才解除。
        /// 由 TitleCard 确认时置 true。
        /// </summary>
        public static bool SuppressFireUntilRelease;
    }
}
