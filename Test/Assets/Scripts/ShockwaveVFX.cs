using System.Collections;
using UnityEngine;

namespace Game.Combat
{

    /// <summary>冲击波视觉特效：扩散圆环从 0 扩大到目标半径后自动销毁</summary>
    public class ShockwaveVFX : MonoBehaviour
    {
        [Tooltip("圆环 MeshRenderer")]
        public MeshRenderer ringRenderer;

        [Tooltip("最大缩放倍数（基于半径）")]
        public float maxScaleMultiplier = 1f;

        private Vector3 startScale = Vector3.zero;
        private Vector3 targetScale;

        /// <summary>播放扩散动画：从 0 扩大到目标半径</summary>
        /// <param name="radius">目标半径</param>
        /// <param name="duration">持续时间（秒）</param>
        public void Play(float radius, float duration)
        {
            targetScale = Vector3.one * radius * maxScaleMultiplier;
            StartCoroutine(ExpandRoutine(duration));
        }

        /// <summary>逐帧扩大圆环，完成后销毁</summary>
        IEnumerator ExpandRoutine(float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                // 缓出曲线：先快后慢
                float eased = 1f - (1f - t) * (1f - t);
                transform.localScale = Vector3.Lerp(startScale, targetScale, eased);

                // 逐渐淡化（URP Unlit 使用 _BaseColor）
                if (ringRenderer != null)
                {
                    float alpha = 1f - t;
                    Color c = ringRenderer.material.color;
                    c.a = alpha;
                    ringRenderer.material.color = c;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            transform.localScale = targetScale;
            yield return null;
            Destroy(gameObject);
        }
    }
}
