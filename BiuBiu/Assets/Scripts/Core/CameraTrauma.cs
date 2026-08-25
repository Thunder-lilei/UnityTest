using UnityEngine;

namespace BiuBiu.Core
{
    /// <summary>
    /// Trauma 震屏（设计文档 12/17 章，M0-5）。
    /// 纯代码数学模型，无引擎/插件依赖：
    /// - trauma ∈ [0,1]，外部通过 AddTrauma 累加（上限 1）
    /// - 幅度 = trauma²（小抖动不明显、大抖动骤增——平方曲线）
    /// - 偏移/旋转 = Perlin 噪声（平滑连续，比纯随机抖动自然）
    /// - trauma 随时间线性衰减
    /// 分级建议（设计文档 12 章）：微震 0.2 / 强震 0.5 / 超强震 0.8
    ///
    /// 相机写权设计（M0-7 起与 CameraFollow 共存）：
    /// 全部逻辑在 OnPreRender（渲染前最后一步，晚于所有 LateUpdate）：
    /// 先撤销上一帧偏移（还原相机到「逻辑位置」——由 CameraFollow 或初始状态拥有），
    /// 再叠加新偏移。与跟随正交：跟谁写逻辑位置无关，震屏只做「渲染层抖动」，零累积漂移。
    /// </summary>
    public class CameraTrauma : MonoBehaviour
    {
        [Header("震屏参数（Inspector 可调，M0 验证手感）")]
        [Tooltip("最大位移幅度（tile）。trauma=1 时的最大偏移")]
        public float maxOffset = 0.25f;

        [Tooltip("最大旋转幅度（度）。trauma=1 时的最大角度")]
        public float maxAngle = 4f;

        [Tooltip("trauma 每秒衰减量（线性）")]
        public float decayPerSecond = 1.2f;

        [Tooltip("噪声流动速度（越大抖动越快）")]
        public float noiseSpeed = 25f;

        /// <summary>全局访问点（命中微震/受击强震等由各系统调用）</summary>
        public static CameraTrauma Instance { get; private set; }

        /// <summary>当前 trauma 值（0~1），供外部做目标趋近式叠加（如满蓄力过载抖动）</summary>
        public float CurrentTrauma => trauma;

        private float trauma;               // 当前 trauma 值 0~1
        private Quaternion baseLocalRot;    // 相机初始局部旋转（旋转基准恒定，直接复位消浮点误差）
        private Vector3 lastPosOffset;      // 上一帧叠加的位置偏移（本帧先撤销再叠新）
        private float[] seeds = new float[3]; // x/y/angle 三路噪声的独立种子（非 readonly：域重载后需重建）
        private float curOffsetMul = 1f;    // 本帧位移倍率（仅满蓄过载路径放大，不影响其他震屏）
        private float curAngleMul = 1f;     // 本帧旋转倍率（同上）

        /// <summary>初始化：单例、记录初始旋转、随机噪声种子</summary>
        private void Awake()
        {
            Instance = this;
            baseLocalRot = transform.localRotation;
            for (int i = 0; i < 3; i++)
                seeds[i] = Random.value * 100f; // 0~100 的随机种子
        }

        /// <summary>
        /// 叠加震屏（默认倍率 1，兼容既有调用）
        /// </summary>
        /// <param name="amount">trauma 增量（微震 0.2 / 强震 0.5 / 超强震 0.8）</param>
        public void AddTrauma(float amount)
        {
            AddTrauma(amount, 1f, 1f);
        }

        /// <summary>
        /// 叠加震屏（带位移/旋转倍率，供满蓄过载等需要更剧烈抖动但又不改变全局 maxOffset 的场景）
        /// </summary>
        /// <param name="amount">trauma 增量</param>
        /// <param name="offsetMul">位移倍率（>1 放大抖动幅度，仅作用于本帧）</param>
        /// <param name="angleMul">旋转倍率（>1 放大抖动角度，仅作用于本帧）</param>
        public void AddTrauma(float amount, float offsetMul, float angleMul)
        {
            // 设置面板开关：屏幕震动关闭时不叠加（M2 设置面板接入）
            if (BiuBiu.UI.SettingsPanel.ScreenShakeEnabled == false) return;
            trauma = Mathf.Clamp01(trauma + amount);
            // 取较强的一方（同一帧若有多源叠加，取最大倍率，避免相互抵消）
            if (offsetMul > curOffsetMul) curOffsetMul = offsetMul;
            if (angleMul > curAngleMul) curAngleMul = angleMul;
        }

        /// <summary>渲染前：撤销旧偏移 → 衰减 → 叠加新偏移（每帧从逻辑位置算起，不累积）</summary>
        private void OnPreRender()
        {
            // 域重载自愈：Play 中脚本热重载清空普通 C# 引用与静态字段
            // （seeds 数组/Instance 静态引用不存活）→ 惰性重建
            if (Instance == null) Instance = this;
            if (seeds == null || seeds.Length != 3)
            {
                seeds = new float[3];
                for (int i = 0; i < 3; i++) seeds[i] = Random.value * 100f;
            }

            // 0) 屏幕震动开关：关闭时即时停震（清零残留 trauma，避免「关了还在抖」的观感）
            if (BiuBiu.UI.SettingsPanel.ScreenShakeEnabled == false)
            {
                trauma = 0f;
                transform.localPosition -= lastPosOffset;
                transform.localRotation = baseLocalRot;
                lastPosOffset = Vector3.zero;
                return;
            }

            // 1) 撤销上一帧偏移：还原到逻辑位置（CameraFollow 的跟随结果/初始位置）
            transform.localPosition -= lastPosOffset;
            transform.localRotation = baseLocalRot;
            lastPosOffset = Vector3.zero;

            // 2) 线性衰减
            if (trauma > 0f)
            {
                trauma -= decayPerSecond * Time.deltaTime;
                if (trauma < 0f) trauma = 0f;
            }
            if (trauma <= 0f) return; // 无震动：保持还原态（完全复位）

            // 3) 幅度 = trauma²（平方曲线：小抖柔和、大抖骤增）
            float shake = trauma * trauma;
            float t = Time.time * noiseSpeed;

            // 三路独立 Perlin 噪声 → [-1,1]
            float nx = Mathf.PerlinNoise(t, seeds[0]) * 2f - 1f;
            float ny = Mathf.PerlinNoise(t, seeds[1]) * 2f - 1f;
            float na = Mathf.PerlinNoise(t, seeds[2]) * 2f - 1f;

            // 叠加新偏移（记录在案，下一帧撤销）；本帧位移/旋转倍率仅作用于过载路径
            lastPosOffset = new Vector3(nx, ny, 0f) * (maxOffset * shake * curOffsetMul);
            transform.localPosition += lastPosOffset;
            transform.localRotation = baseLocalRot * Quaternion.Euler(0f, 0f, na * maxAngle * shake * curAngleMul);

            // 本帧倍率已消费，重置为 1（下一帧若没有过载源，则恢复默认幅度）
            curOffsetMul = 1f;
            curAngleMul = 1f;
        }
    }
}
