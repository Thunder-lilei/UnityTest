using System.Collections.Generic;
using UnityEngine;

namespace Game.Audio
{
    
    /// <summary>音频管理器：单例模式，统一管理所有游戏音效播放与火球连击音调递增</summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }  // 单例实例
    
        public AudioSource fireballLaunch;     // 火球发射音效
        public AudioSource fireballHit;        // 火球命中音效
        public AudioSource enemyDeath;         // 敌人死亡音效
        public AudioSource playerHurt;         // 玩家受伤音效
        public AudioSource playerDeath;        // 玩家死亡音效
        public AudioSource pickupExp;          // 拾取经验音效
        public AudioSource levelUp;            // 升级音效
        public AudioSource gameOver;           // 游戏结束音效
        public AudioSource healthPotionPickup; // 拾取血瓶音效
        public AudioSource upgradeConfirm;     // 升级选择确认音效
        public AudioSource dash;              // 闪避音效
        public AudioSource slashAttack;       // 斩击音效
    
        [Header("连击音效设置")]
        [Tooltip("每次连击音调升高幅度")]
        public float comboPitchStep = 0.05f;
        [Tooltip("连击最大音调倍数")]
        public float maxComboPitch = 1.5f;
        [Tooltip("连击重置时间（秒）")]
        public float comboResetTime = 2f;
    
        private int comboCount;
        private float lastHitTime;
    
        // 音量设置
        private const string PREF_MASTER = "Volume_Master";
        private const string PREF_SFX = "Volume_SFX";
        private const float MIN_VOLUME = 0.0001f;
        private float masterVolume = 1f;
        private float sfxVolume = 1f;
        private List<AudioSource> sfxSources;
    
        /// <summary>单例初始化</summary>
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
    
            // 收集所有 SFX AudioSource 引用
            sfxSources = new List<AudioSource>
            {
                fireballLaunch, fireballHit, enemyDeath, playerHurt,
                playerDeath, pickupExp, levelUp, gameOver,
                healthPotionPickup, upgradeConfirm, dash, slashAttack
            };
    
            // 从 PlayerPrefs 加载音量设置
            masterVolume = PlayerPrefs.GetFloat(PREF_MASTER, 1f);
            sfxVolume = PlayerPrefs.GetFloat(PREF_SFX, 1f);
            ApplyVolumes();
        }
    
        /// <summary>将当前音量值应用到所有 AudioSource 和 AudioListener</summary>
        void ApplyVolumes()
        {
            AudioListener.volume = masterVolume;
            if (sfxSources != null)
            {
                foreach (var src in sfxSources)
                {
                    if (src != null)
                        src.volume = sfxVolume;
                }
            }
        }
    
        /// <summary>设置主音量（全局 AudioListener）</summary>
        public void SetMasterVolume(float v)
        {
            masterVolume = Mathf.Max(MIN_VOLUME, v);
            AudioListener.volume = masterVolume;
            PlayerPrefs.SetFloat(PREF_MASTER, masterVolume);
            PlayerPrefs.Save();
        }
    
        /// <summary>设置音效音量（所有 SFX AudioSource）</summary>
        public void SetSFXVolume(float v)
        {
            sfxVolume = Mathf.Max(MIN_VOLUME, v);
            if (sfxSources != null)
            {
                foreach (var src in sfxSources)
                {
                    if (src != null)
                        src.volume = sfxVolume;
                }
            }
            PlayerPrefs.SetFloat(PREF_SFX, sfxVolume);
            PlayerPrefs.Save();
        }
    
        /// <summary>获取当前主音量</summary>
        public float GetMasterVolume() => masterVolume;
    
        /// <summary>获取当前音效音量</summary>
        public float GetSFXVolume() => sfxVolume;
    
        void Update()
        {
            // 超过重置时间未命中，连击归零
            if (comboCount > 0 && Time.time - lastHitTime >= comboResetTime)
                comboCount = 0;
        }
    
        public void PlayFireballLaunch() { if (fireballLaunch != null) fireballLaunch.Play(); }
    
        /// <summary>火球命中：连击次数越多音调越高，超过重置时间则归零</summary>
        public void PlayFireballHit()
        {
            if (fireballHit == null) return;
    
            if (Time.time - lastHitTime < comboResetTime)
                comboCount++;
            else
                comboCount = 0;
    
            lastHitTime = Time.time;
            fireballHit.pitch = Mathf.Min(1f + comboCount * comboPitchStep, maxComboPitch);
            fireballHit.Play();
        }
    
        /// <summary>敌人死亡时重置连击</summary>
        public void PlayEnemyDeath()
        {
            if (enemyDeath != null) enemyDeath.Play();
            comboCount = 0;
        }
    
        public void PlayPlayerHurt() { if (playerHurt != null) playerHurt.Play(); }
        public void PlayPlayerDeath() { if (playerDeath != null) playerDeath.Play(); }
        public void PlayPickupExp() { if (pickupExp != null) pickupExp.Play(); }
        public void PlayLevelUp() { if (levelUp != null) levelUp.Play(); }
        public void PlayGameOver() { if (gameOver != null) gameOver.Play(); }
        public void PlayHealthPotionPickup() { if (healthPotionPickup != null) healthPotionPickup.Play(); }
        public void PlayUpgradeConfirm() { if (upgradeConfirm != null) upgradeConfirm.Play(); }
        public void PlayDash() { if (dash != null) dash.Play(); }
        public void PlaySlashAttack() { if (slashAttack != null) slashAttack.Play(); }
    }
    
}
