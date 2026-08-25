using UnityEngine;

namespace BiuBiu.Effects
{
    /// <summary>
    /// 击碎破碎碎片：单枚像素小方块。
    /// 由 BreakBurstManager 经对象池取出后 Initialize 放飞；
    /// 生命周期内沿给定速度匀速直线飞散（无重力、无碰撞），同时缩小+渐隐，timer 归零回池自毁。
    /// 写法参考 PlayerController.AfterimageFader（自毁式渐隐组件）。
    /// </summary>
    public class BreakShard : MonoBehaviour
    {
        private SpriteRenderer sr;          // 自身渲染器（缓存，懒初始化）
        private float lifeTimer;            // 剩余生命周期（秒）
        private float lifeTotal;            // 总生命周期（用于按比值渐隐/缩小）
        private Vector2 velocity;           // 匀速飞散速度（tile/s，世界坐标）
        private float startScale;           // 初始缩放（像素尺寸经 PPU 换算）

        /// <summary>
        /// 初始化一枚碎片并放飞。
        /// </summary>
        /// <param name="color">碎片着色（取被命中敌人主色）</param>
        /// <param name="life">生命周期（秒，走 GameBalance.BreakShardLife）</param>
        /// <param name="vel">飞散速度向量（tile/s，已含放射角度与初速）</param>
        /// <param name="pixelSize">碎片像素边长（走 GameBalance.BreakShardSize*，用于换算 scale）</param>
        public void Initialize(Color color, float life, Vector2 vel, float pixelSize)
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();

            lifeTotal = life;
            lifeTimer = life;
            velocity = vel;
            startScale = pixelSize / 32f; // PPU=32：像素尺寸 → 世界 scale

            // 排序层固定“特效(5)”（工程约定：特效叠加层），避免继承模板默认层
            sr.sortingLayerName = "Effects";
            sr.sortingOrder = 5;
            sr.color = color;
            sr.enabled = true;

            transform.localScale = new Vector3(startScale, startScale, 1f);
            gameObject.SetActive(true);
        }

        private void Update()
        {
            // 生命周期计时（与渲染解耦，按真实时间推进）
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
            {
                // 归零直接回对象池，不依赖任何落地/碰撞判定（俯视角无地面高度概念）
                ReturnToPool();
                return;
            }

            // 匀速直线飞散
            transform.position += (Vector3)(velocity * Time.deltaTime);

            // 按剩余比例同步缩小（scale → 0）+ 渐隐（alpha → 0）
            float t = lifeTimer / lifeTotal;
            float s = startScale * t;
            transform.localScale = new Vector3(s, s, 1f);
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            Color c = sr.color;
            c.a = t;
            sr.color = c;
        }

        /// <summary>
        /// 回池（由 BreakBurstManager 的池负责回收）。
        /// </summary>
        private void ReturnToPool()
        {
            // 复用 Core.ObjectPool：以模板为键，实例回池后 SetActive(false) 挂根下
            Core.ObjectPool.Release(gameObject);
        }

        /// <summary>
        /// 出池瞬间复位渲染器（防止回池期间被其它逻辑读到旧 alpha）。
        /// </summary>
        private void OnEnable()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;
        }
    }
}
