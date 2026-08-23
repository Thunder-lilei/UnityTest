using UnityEngine;

namespace BiuBiu.Core
{
    /// <summary>
    /// 可击退接口（v3.5：改物理冲量语义，第二参数=目标速度 tile/s）。
    /// 敌人实现本接口供武器命中/连锁碰撞调用；玩家暂未实现（精英冲撞击退为待接入项）。
    /// </summary>
    public interface IKnockbackable
    {
        /// <summary>
        /// 沿指定方向施加击退冲量（内部 AddForce(Impulse)，质量参与）。
        /// </summary>
        /// <param name="direction">击退方向（单位向量）</param>
        /// <param name="speed">击退目标速度（tile/s，冲量=质量×速度）</param>
        void Knockback(Vector2 direction, float speed);
    }
}
