namespace Wuxia.Core
{
    /// <summary>
    /// 伤害计算接口。
    /// 前期由 C# 实现，后期可由 Lua 脚本重写，调用方不变。
    /// </summary>
    public interface IDamageCalculator
    {
        /// <summary>
        /// 计算最终伤害值。
        /// </summary>
        /// <param name="baseDamage">攻击基础伤害</param>
        /// <param name="attackerLevel">攻击者等级</param>
        /// <param name="defenderLevel">防御者等级</param>
        /// <param name="defense">防御者防御力</param>
        /// <returns>最终伤害值</returns>
        float Calculate(float baseDamage, int attackerLevel, int defenderLevel, float defense);
    }
}
