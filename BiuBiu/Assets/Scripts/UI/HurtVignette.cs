using BiuBiu.Core;
using BiuBiu.Player;
using UnityEngine;

namespace BiuBiu.UI
{
    /// <summary>
    /// 玩家受击红边（Hurt Vignette）控制器：GameFeel 危险反馈（设计文档 18 章；数值文档：受击红边分组）。
    /// 玩家“实际扣血”时屏幕边缘浮现一圈红色径向晕影，快速淡入后平滑淡出；
    /// 残血（当前血量 ≤ 阈值）时叠加一个较低的常驻红晕，随受击再叠加峰值，回满后常驻消失。
    ///
    /// 实现方式：运行时生成一张径向渐变贴图（边缘不透明、中心透明），由 GameHud 的 OnGUI
    /// 以 GUI.DrawTexture 铺满全屏绘制（贴图 rgb=红，alpha=径向遮罩；绘制时 GUI.color 的 alpha
    /// 充当强度乘子）。不引入后处理栈 / 无 shader 后处理，保持 Built-in 管线轻量。
    ///
    /// 参考现有范式：状态用 Time.deltaTime 渐变（同 AfterimageFader 风格）；数值全部走 GameBalance，无魔法数。
    /// 组件挂在 HUD 所在的 ui 对象下（RuntimeSceneBuilder 装配），PlayerController.TakeDamage 调用 Flash()。
    /// </summary>
    public class HurtVignette : MonoBehaviour
    {
        [Tooltip("红边贴图尺寸（像素，正方形）。边缘=不透明红，中心=透明")]
        public int textureSize = 256;

        private Texture2D vignetteTexture;  // 运行时生成的径向渐变贴图
        private BiuBiu.Player.PlayerController player;    // 玩家引用（读当前血量判断残血常驻），惰性自愈

        // ── 瞬时受击闪现状态 ──
        private bool flashActive;           // 是否处于一次受击的淡入/淡出过程中
        private float flashElapsed;         // 当前受击已过去的时间（秒）
        private float flashValue;           // 当前受击贡献强度（0~PeakAlpha），淡入淡出在此累加

        /// <summary>运行时生成的径向渐变贴图（供 GameHud 绘制）</summary>
        public Texture2D Texture => vignetteTexture;

        /// <summary>当前红边综合强度（0~PeakAlpha）：残血常驻 + 受击闪现，clamp 上限</summary>
        public float CurrentAlpha
        {
            get
            {
                float baseAlpha = LowHealthBaseAlpha();
                return Mathf.Clamp(baseAlpha + flashValue, 0f, GameBalance.HurtVignettePeakAlpha);
            }
        }

        /// <summary>组件初始化：生成径向渐变贴图，惰性绑定玩家引用</summary>
        private void Awake()
        {
            GenerateVignetteTexture();
        }

        /// <summary>
        /// 触发一次受击红边：瞬间重置淡入计时（覆盖式叠加，不爆表由 CurrentAlpha clamp 保证）
        /// 仅在实际扣血时由 PlayerController 调用（无敌/翻滚/开发者模式期间不调用）
        /// </summary>
        public void Flash()
        {
            flashActive = true;
            flashElapsed = 0f;
            flashValue = GameBalance.HurtVignettePeakAlpha; // 起始即峰值，淡入阶段再按时间曲线回落式给出
        }

        /// <summary>每帧推进受击闪现的淡入淡出</summary>
        private void Update()
        {
            if (!flashActive) return;

            flashElapsed += Time.deltaTime;
            float inT = GameBalance.HurtVignetteFlashInTime;
            float outT = GameBalance.HurtVignetteFadeOutTime;

            if (flashElapsed <= inT)
            {
                // 淡入：从上一帧值快速升到峰值（约 0.07s 内到顶，受击反馈即时可见）
                float t = inT > 0f ? flashElapsed / inT : 1f;
                flashValue = GameBalance.HurtVignettePeakAlpha * t;
            }
            else if (flashElapsed <= inT + outT)
            {
                // 淡出：从峰值线性消退到 0（共 FadeOutTime 秒）
                float k = (flashElapsed - inT) / outT;
                flashValue = GameBalance.HurtVignettePeakAlpha * (1f - k);
            }
            else
            {
                // 本次受击的闪现结束
                flashActive = false;
                flashValue = 0f;
            }
        }

        /// <summary>残血常驻红晕：当前血量 ≤ 阈值且未死亡时返回常驻 alpha（带呼吸脉动），否则 0</summary>
        private float LowHealthBaseAlpha()
        {
            if (player == null) player = FindObjectOfType<BiuBiu.Player.PlayerController>();
            if (player == null) return 0f;

            int hp = player.CurrentHealth;
            if (hp <= 0) return 0f; // 死亡不显示常驻红晕
            if (hp > GameBalance.HurtVignetteLowHealthThreshold) return 0f;

            // 残血脉动：边缘红晕围绕基础值做正弦呼吸（越残血越紧迫），不依赖受击闪现
            float baseA = GameBalance.HurtVignetteLowHealthAlpha;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f * GameBalance.HurtVignettePulseSpeed);
            return baseA * (1f + GameBalance.HurtVignettePulseAmount * (pulse * 2f - 1f)); // 围绕 baseA 上下浮动 ±PulseAmount
        }

        /// <summary>
        /// 运行时生成径向渐变贴图：中心透明、边缘不透明（白 rgb，alpha 充当遮罩；绘制时染红）。
        /// 参考项目 RuntimeSceneBuilder 运行时创建对象的写法，避免引入 Resources 资产或第三方贴图。
        /// </summary>
        private void GenerateVignetteTexture()
        {
            int size = textureSize;
            vignetteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            vignetteTexture.hideFlags = HideFlags.HideAndDontSave; // 跨场景重载存活（受击暗角纹理缓存）
            vignetteTexture.filterMode = FilterMode.Bilinear;

            float center = (size - 1f) * 0.5f;
            float maxDist = center * Mathf.Sqrt(2f); // 角到中心距离（最远）
            // 红边只在屏幕靠外圈出现：用归一化半径做平滑过渡（内圈透明、外圈不透明）
            float inner = 0.55f;  // 内圈半径比例（此范围内完全透明）
            float outer = 1.0f;   // 外圈半径比例（此范围达到完全不透明）

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float r = Mathf.Sqrt(dx * dx + dy * dy); // 0(中心)~1(边中点)~1.414(角)
                    float n = Mathf.Clamp01((r - inner) / (outer - inner));
                    // smoothstep 让边缘更柔和
                    float a = n * n * (3f - 2f * n);
                    // rgb 用白（绘制时由 GUI.color 染红）；alpha = 径向遮罩
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            vignetteTexture.SetPixels(pixels);
            vignetteTexture.Apply();
        }

        /// <summary>热重载/销毁时释放运行时贴图，避免显存泄漏</summary>
        private void OnDestroy()
        {
            if (vignetteTexture != null)
            {
                Destroy(vignetteTexture);
                vignetteTexture = null;
            }
        }
    }
}
