using System.Collections.Generic;
using UnityEngine;

namespace BiuBiu.Core
{
    /// <summary>
    /// 轻量音频管理器（无第三方音频中间件，Built-in 管线）。
    /// 职责：持有一个 AudioSource，按名懒加载 Resources/Audio/&lt;name&gt; 下的 AudioClip 并播放。
    /// 命名约定：音频资产放 Assets/Resources/Audio/&lt;name&gt;.wav，调用 AudioManager.Play("&lt;name&gt;")。
    /// 单例 + 惰性自愈（热重载清空引用时自动重建），符合项目工程纪律。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null) EnsureInstance();
                return _instance;
            }
        }

        private AudioSource _source;
        // 持续/可中断音轨（如蓄力音）：独立 source，支持随时停止
        private AudioSource _loopSource;
        private readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;

            _loopSource = gameObject.AddComponent<AudioSource>();
            _loopSource.playOnAwake = false;
            _loopSource.loop = false; // 持续音轨迹不循环：只播一次，满蓄力后自然停止；松手/中断由 StopLoop 截断
        }

        /// <summary>确保全局单例存在（直接 Play 调试或热重载后自愈用）</summary>
        public static void EnsureInstance()
        {
            if (_instance == null)
            {
                var go = new GameObject("[AudioManager]");
                go.AddComponent<AudioManager>(); // Awake 内完成单例登记与 DontDestroyOnLoad
            }
        }

        /// <summary>播放一次音效（按 Resources/Audio/&lt;name&gt; 加载，结果缓存）</summary>
        public static void Play(string clipName)
        {
            var mgr = Instance;
            if (mgr == null || mgr._source == null) return;

            AudioClip clip = mgr.LoadClip(clipName);
            if (clip == null) return;
            mgr._source.PlayOneShot(clip);
        }

        private AudioClip LoadClip(string clipName)
        {
            if (_cache.TryGetValue(clipName, out var cached)) return cached;
            var clip = Resources.Load<AudioClip>("Audio/" + clipName);
            if (clip != null) _cache[clipName] = clip;
            else Debug.LogWarning($"[AudioManager] 未找到音频资产：Resources/Audio/{clipName}");
            return clip;
        }

        /// <summary>播放一段可中断的持续音（如蓄力音）。同一时刻仅一条持续音轨，重复调用会替换。
        /// 播放期间可随时用 StopLoop 终止（无需播完整段）。</summary>
        public static void PlayLoop(string clipName)
        {
            var mgr = Instance;
            if (mgr == null || mgr._loopSource == null) return;

            AudioClip clip = mgr.LoadClip(clipName);
            if (clip == null) return;
            // 蓄力音音量单独下调至默认的 2/3（仅作用于持续音轨，不影响其他一次性音效）
            mgr._loopSource.volume = 0.666f;
            mgr._loopSource.clip = clip;
            mgr._loopSource.loop = false; // 不循环：一次性渐强到峰值，满蓄力后不再重复播放
            mgr._loopSource.Play();
        }

        /// <summary>停止当前持续音轨（蓄力中断/发射时调用，立即终止播放）。</summary>
        public static void StopLoop()
        {
            var mgr = Instance;
            if (mgr == null || mgr._loopSource == null) return;
            if (mgr._loopSource.isPlaying)
                mgr._loopSource.Stop();
        }
    }
}
