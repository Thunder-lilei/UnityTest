namespace BiuBiu.Core
{
    /// <summary>
    /// 统一伤害接口（设计文档 17 章：伤害入口统一走 IDamageable，
    /// 开发者模式一键无敌=开关直接跳过对主角的结算）。
    /// 主角、敌人、可破坏物（装饰/资源点/掩体墙）均实现此接口。
    /// </summary>
    public interface IDamageable
    {
        /// <summary>承受一次伤害</summary>
        /// <param name="amount">伤害数值（心/血量点数，整数制）</param>
        void TakeDamage(int amount);
    }
}
