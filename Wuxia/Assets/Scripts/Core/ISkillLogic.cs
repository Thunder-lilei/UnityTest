namespace Wuxia.Core
{
    /// <summary>
    /// 技能逻辑接口。
    /// 前期由 C# 实现，后期可由 Lua 控制技能特殊效果。
    /// </summary>
    public interface ISkillLogic
    {
        /// <summary>
        /// 技能触发时的逻辑处理。
        /// </summary>
        /// <param name="skillId">技能标识</param>
        void OnSkillTriggered(string skillId);

        /// <summary>
        /// 技能命中目标后的逻辑处理。
        /// </summary>
        /// <param name="skillId">技能标识</param>
        /// <param name="targetId">目标标识</param>
        void OnSkillHit(string skillId, int targetId);
    }
}
