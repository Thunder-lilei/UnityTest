using BiuBiu.Effects;
using UnityEngine;

namespace BiuBiu.Core
{
    /// <summary>
    /// 可破坏障碍单元。挂在每个 1×1 障碍单元（俄罗斯方块形状的一个格子）的 collider 上。
    /// 满蓄力弹丸击中时：原地留下建筑残骸（无碰撞、变暗、可通行），并销毁被命中的这一格；
    /// 同一形状其余格保留，可被继续击碎（一格一格啃掉障碍）。
    /// 边界墙（大墙）不挂此组件，保持不可破坏。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DestructibleObstacle : MonoBehaviour
    {
        [Tooltip("所属障碍父物体（一个俄罗斯方块形状的根）")]
        public Transform Root;

        /// <summary>该单元是否已被破坏，避免重复触发</summary>
        public bool Destroyed { get; private set; }

        public void Break()
        {
            if (Destroyed) return;
            Destroyed = true;

            Vector3 pos = transform.position;

            // 碎石音效（视野外剔除）
            AudioManager.PlayWorld("stone_break", pos);

            // 同色像素碎片爆发（墙主色取灰白）
            BreakBurstManager.SpawnBreakBurst(pos, Vector2.up, Color.gray);

            // 轻震屏反馈
            CameraTrauma.Instance?.AddTrauma(0.25f);

            // 在原位置补充建筑残骸：无碰撞、变暗、缩小，可通行（纯装饰）
            SpawnRubble(pos);

            // 仅销毁被命中的这一格（其余格保留，可继续碎）
            Destroy(gameObject);
        }

        /// <summary>生成建筑残骸：一簇随机小暗灰碎块（瓦砾堆，噪声感），可通行、不可再碎</summary>
        private void SpawnRubble(Vector3 pos)
        {
            var parent = Root != null ? Root : transform.parent;
            int chunks = Random.Range(3, 6); // 3~5 块碎块拼成残骸
            for (int i = 0; i < chunks; i++)
            {
                // 碎块尺寸 0.22~0.4，随机偏移在单元范围内，随机旋转
                float s = Random.Range(0.22f, 0.4f);
                float ox = Random.Range(-0.32f, 0.32f);
                float oy = Random.Range(-0.32f, 0.32f);

                var chunk = GreyBoxFactory.MakeBox($"Rubble_{i}", false, Color.white, Vector2.one * s);
                chunk.transform.SetParent(parent, false);
                chunk.transform.position = new Vector3(pos.x + ox, pos.y + oy, pos.z);
                chunk.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

                var sr = chunk.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    // 暗灰基调 + 亮度抖动，形成噪声层次
                    float v = Random.Range(0.22f, 0.42f);
                    sr.color = new Color(v, v, v * 0.96f);
                    sr.sortingOrder = 1; // 压地面、低于完整墙(2)
                }
                // 刻意不挂 Collider2D / DestructibleObstacle：残骸可通行、不可再碎
            }
        }
    }
}
