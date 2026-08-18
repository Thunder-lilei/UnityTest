using UnityEngine;

namespace Game.Combat
{
    
    /// <summary>刀光特效：播放后定时自动销毁</summary>
    public class SlashEffect : MonoBehaviour
    {
        public float lifetime = 0.5f;            // 特效存活时间（秒）

        private float timer;                     // 存活计时器
        private ParticleSystem[] particleSystems; // 所有粒子系统组件

        void Awake()
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        void Start()
        {
            foreach (var ps in particleSystems)
                ps.Play();
        }

        /// <summary>每帧计时，超时后销毁</summary>
        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= lifetime)
                Destroy(gameObject);
        }
    }
}
