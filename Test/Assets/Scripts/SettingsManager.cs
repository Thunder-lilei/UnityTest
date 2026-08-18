using UnityEngine;
using UnityEngine.UI;
using Game.Audio;

namespace Game.Systems
{
    /// <summary>设置管理器：管理主音量/音效音量 Slider，读写 PlayerPrefs，实时应用到 AudioManager</summary>
    public class SettingsManager : MonoBehaviour
    {
        [Header("音量 Slider")]
        public Slider masterVolumeSlider;
        public Slider sfxVolumeSlider;

        void Start()
        {
            // 从 AudioManager 读取当前音量（Awake 中已从 PlayerPrefs 加载）
            float master = AudioManager.Instance != null ? AudioManager.Instance.GetMasterVolume() : 1f;
            float sfx = AudioManager.Instance != null ? AudioManager.Instance.GetSFXVolume() : 1f;

            // 设置 Slider 初始值（不触发 onValueChanged）
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(master);
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(sfx);
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
        }

        void OnMasterVolumeChanged(float value)
        {
            AudioManager.Instance?.SetMasterVolume(value);
        }

        void OnSFXVolumeChanged(float value)
        {
            AudioManager.Instance?.SetSFXVolume(value);
        }

        void OnDestroy()
        {
            PlayerPrefs.Save();
        }
    }
}
