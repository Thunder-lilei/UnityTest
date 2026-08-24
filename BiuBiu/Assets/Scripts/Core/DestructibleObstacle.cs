using BiuBiu.VFX;
using UnityEngine;

namespace BiuBiu.Core
{
    /// <summary>
    /// 可破坏障碍单元。挂在每个 1×1 障碍单元（俄罗斯方块形状的一个格子）的 collider 上。
    /// 满蓄力弹丸击中时触发碎墙（销毁整个障碍父物体 + 同色碎片特效）。
    /// 边界墙（大墙）不挂此组件，保持不可破坏。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DestructibleObstacle : MonoBehaviour
    {
        [Tooltip("所属障碍父物体（一个俄罗斯方块形状的根）")]
        public Transform Root;

        /// <summary>该障碍（同一 Root）是否已被破坏，避免重复触发</summary>
        public bool Destroyed { get; private set; }

        public void Break()
        {
            if (Destroyed) return;
            Destroyed = true;

            // 同色像素碎片爆发（墙主色取灰白）
            BreakBurstManager.SpawnBreakBurst(transform.position, Vector2.up, Color.gray);

            // 轻震屏反馈
            CameraTrauma.Instance?.AddTrauma(0.25f);

            // 销毁整个障碍（Root 下所有单元一并消失）
            if (Root != null) Destroy(Root.gameObject);
            else Destroy(gameObject);
        }
    }
}
