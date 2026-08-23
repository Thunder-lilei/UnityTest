using UnityEngine;

namespace BiuBiu.Core
{
    /// <summary>
    /// 受击闪白驱动组件（设计文档 18 章；数值文档：闪白约 10Hz，主角闪白时长=受击无敌 1.0s）。
    /// 挂在任何带 Renderer 的对象上（SpriteRenderer 均可），
    /// 配合 BiuBiu/SpriteFlash 材质使用。
    /// 通过 MaterialPropertyBlock 写入 _FlashAmount，不产生材质实例，多敌人可共享同一材质。
    /// </summary>
    public class HitFlash : MonoBehaviour
    {
        [Tooltip("闪白频率（次/秒），设计值 10")]
        public float flashFrequency = 10f;

        [Tooltip("闪白颜色（默认纯白）")]
        public Color flashColor = Color.white;

        private Renderer targetRenderer;   // 目标渲染器（SpriteRenderer/MeshRenderer 共用基类）
        private MaterialPropertyBlock mpb; // 材质属性块：避免实例化材质
        private int flashAmountId;         // shader 属性 _FlashAmount 的缓存 ID（避免每帧字符串查找）
        private int flashColorId;          // shader 属性 _FlashColor 的缓存 ID

        private float flashTimer = -1f;    // 当前闪白剩余时间；<0 表示空闲
        private bool isOn;                 // 方波当前相位（true=显示闪白色）

        /// <summary>组件初始化：缓存渲染器与属性 ID</summary>
        private void Awake()
        {
            targetRenderer = GetComponent<Renderer>();
            mpb = new MaterialPropertyBlock();
            flashAmountId = Shader.PropertyToID("_FlashAmount");
            flashColorId = Shader.PropertyToID("_FlashColor");
        }

        /// <summary>
        /// 触发一次受击闪白
        /// </summary>
        /// <param name="duration">闪白总时长（秒）。主角=受击无敌时长 1.0s；敌人/可破坏物用短闪（如 0.2s）</param>
        public void PlayFlash(float duration)
        {
            flashTimer = duration;
            isOn = true;
            Apply(1f); // 触发瞬间立即亮起，保证受击反馈即时可见
        }

        /// <summary>每帧驱动方波闪白；空闲时零开销</summary>
        private void Update()
        {
            if (flashTimer < 0f) return; // 空闲态：不写任何渲染状态

            flashTimer -= Time.deltaTime;
            if (flashTimer < 0f)
            {
                // 计时结束：确保归零（回到原色）
                isOn = false;
                Apply(0f);
                return;
            }

            // 10Hz 方波：以绝对时间为基准计算相位，天然抗帧率波动
            // phase ∈ [0,1)，前半周期亮、后半周期灭（周期 = 1/flashFrequency 秒）
            float phase = Mathf.Repeat(Time.time * flashFrequency, 1f);
            bool on = phase < 0.5f;
            if (on != isOn)
            {
                isOn = on;
                Apply(on ? 1f : 0f);
            }
        }

        /// <summary>把当前闪白状态写入 MaterialPropertyBlock（只在相位变化时调用）</summary>
        /// <param name="amount">闪白强度：1=闪白色，0=原色</param>
        private void Apply(float amount)
        {
            // 域重载自愈：Play 中脚本热重载（如外部工具导入资产触发）会清空普通 C# 对象引用
            // （MaterialPropertyBlock 不存活），Awake 不会重跑 → 此处惰性重建，杜绝 ArgumentNullException
            if (mpb == null) mpb = new MaterialPropertyBlock();
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();

            targetRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat(flashAmountId, amount);
            mpb.SetColor(flashColorId, flashColor);
            targetRenderer.SetPropertyBlock(mpb);
        }
    }
}
