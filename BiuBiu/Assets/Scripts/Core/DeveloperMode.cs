namespace BiuBiu.Core
{
    /// <summary>
    /// 开发者模式全局开关（开发文档 M0-8；设计文档「开发者模式」章节）。
    /// 灰盒阶段：F3 热键切换（PlayerController 检测），OnGUI 显示状态；
    /// 正式版：移入设置界面（UI 设置页的开关项），本类接口不变。
    /// GodMode=true 时 IDamageable 实现方（玩家）在入口拦截伤害。
    /// </summary>
    public static class DeveloperMode
    {
        /// <summary>无敌模式：开启后玩家不受任何伤害（敌人/陷阱等全部免疫）</summary>
        public static bool GodMode;
    }
}
