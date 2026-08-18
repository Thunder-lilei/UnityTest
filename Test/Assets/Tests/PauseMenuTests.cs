using NUnit.Framework;
using UnityEngine;
using Game.UI;
using Game.Systems;
using Game.Audio;

namespace Game.Tests
{
    /// <summary>暂停菜单与设置系统测试</summary>
    public class PauseMenuTests
    {
        private GameObject canvasGO;
        private GameObject playerGO;
        private GameObject pauseMenuPanel;
        private GameObject settingsPanel;
        private GameObject upgradePanel;
        private GameObject gameOverPanel;
        private PauseMenu pauseMenu;
        private AudioManager audioManager;

        [SetUp]
        public void Setup()
        {
            // 创建 Canvas
            canvasGO = new GameObject("Canvas");

            // 创建面板
            pauseMenuPanel = new GameObject("PauseMenuPanel");
            pauseMenuPanel.transform.SetParent(canvasGO.transform, false);
            pauseMenuPanel.SetActive(false);

            settingsPanel = new GameObject("SettingsPanel");
            settingsPanel.transform.SetParent(canvasGO.transform, false);
            settingsPanel.SetActive(false);

            upgradePanel = new GameObject("UpgradePanel");
            upgradePanel.transform.SetParent(canvasGO.transform, false);
            upgradePanel.SetActive(false);

            gameOverPanel = new GameObject("GameOverPanel");
            gameOverPanel.transform.SetParent(canvasGO.transform, false);
            gameOverPanel.SetActive(false);

            // 创建 Player 及组件
            playerGO = new GameObject("Player");
            var movement = playerGO.AddComponent<Game.Player.PlayerMovement>();
            var combat = playerGO.AddComponent<Game.Player.PlayerCombat>();
            var melee = playerGO.AddComponent<Game.Combat.MeleeCombat>();
            var health = playerGO.AddComponent<Game.Player.PlayerHealth>();

            // 挂载 PauseMenu
            pauseMenu = canvasGO.AddComponent<PauseMenu>();
            pauseMenu.pauseMenuPanel = pauseMenuPanel;
            pauseMenu.settingsPanel = settingsPanel;
            pauseMenu.upgradePanel = upgradePanel;
            pauseMenu.gameOverPanel = gameOverPanel;
            pauseMenu.playerMovement = movement;
            pauseMenu.playerCombat = combat;
            pauseMenu.meleeCombat = melee;
            pauseMenu.playerHealth = health;

            // 创建 AudioManager（单例）
            var amGO = new GameObject("AudioManager");
            audioManager = amGO.AddComponent<AudioManager>();
            // 通过反射设置单例实例（Instance 属性只有 getter）
            var instanceField = typeof(AudioManager).GetField("<Instance>k__BackingField",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (instanceField != null)
                instanceField.SetValue(null, audioManager);

            // 重置状态
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        [TearDown]
        public void Teardown()
        {
            // 清理 PlayerPrefs 测试键
            PlayerPrefs.DeleteKey("Volume_Master");
            PlayerPrefs.DeleteKey("Volume_SFX");
            PlayerPrefs.Save();

            // 恢复状态
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // 清理 AudioManager 单例
            var instanceField = typeof(AudioManager).GetField("<Instance>k__BackingField",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (instanceField != null)
                instanceField.SetValue(null, null);

            Object.DestroyImmediate(canvasGO);
            Object.DestroyImmediate(playerGO);
            Object.DestroyImmediate(audioManager.gameObject);
        }

        [Test]
        public void PauseMenu_Pause_SetsTimeScaleZero()
        {
            pauseMenu.Pause();
            Assert.AreEqual(0f, Time.timeScale, "暂停后 timeScale 应为 0");
        }

        [Test]
        public void PauseMenu_Resume_RestoresTimeScale()
        {
            pauseMenu.Pause();
            pauseMenu.Resume();
            Assert.AreEqual(1f, Time.timeScale, "恢复后 timeScale 应为 1");
        }

        [Test]
        public void PauseMenu_Pause_ShowsCursor()
        {
            pauseMenu.Pause();
            Assert.IsTrue(Cursor.visible, "暂停后鼠标应可见");
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState, "暂停后鼠标应解锁");
        }

        [Test]
        public void PauseMenu_Resume_HidesCursor()
        {
            pauseMenu.Pause();
            pauseMenu.Resume();
            Assert.IsFalse(Cursor.visible, "恢复后鼠标应隐藏");
            Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState, "恢复后鼠标应锁定");
        }

        [Test]
        public void PauseMenu_Pause_ShowsPanel()
        {
            pauseMenu.Pause();
            Assert.IsTrue(pauseMenuPanel.activeSelf, "暂停后面板应激活");
        }

        [Test]
        public void PauseMenu_Resume_HidesPanel()
        {
            pauseMenu.Pause();
            pauseMenu.Resume();
            Assert.IsFalse(pauseMenuPanel.activeSelf, "恢复后面板应隐藏");
        }

        [Test]
        public void PauseMenu_OpenSettings_HidesPauseShowsSettings()
        {
            pauseMenu.Pause();
            pauseMenu.OpenSettings();
            Assert.IsFalse(pauseMenuPanel.activeSelf, "打开设置后暂停面板应隐藏");
            Assert.IsTrue(settingsPanel.activeSelf, "打开设置后设置面板应显示");
        }

        [Test]
        public void PauseMenu_CloseSettings_ShowsPauseHidesSettings()
        {
            pauseMenu.Pause();
            pauseMenu.OpenSettings();
            pauseMenu.CloseSettings();
            Assert.IsTrue(pauseMenuPanel.activeSelf, "关闭设置后暂停面板应显示");
            Assert.IsFalse(settingsPanel.activeSelf, "关闭设置后设置面板应隐藏");
        }

        [Test]
        public void AudioManager_SetMasterVolume_AppliesToListener()
        {
            audioManager.SetMasterVolume(0.5f);
            Assert.AreEqual(0.5f, AudioListener.volume, 0.001f, "主音量应应用到 AudioListener");
        }

        [Test]
        public void AudioManager_SetSFXVolume_AppliesToAllSources()
        {
            // 创建测试 AudioSource 并赋给 AudioManager 的公共字段
            var srcGO = new GameObject("TestSFX");
            srcGO.transform.SetParent(audioManager.transform, false);
            var src = srcGO.AddComponent<AudioSource>();
            src.volume = 1f;

            // 将测试 AudioSource 赋给 fireballLaunch 字段（Awake 已初始化 sfxSources 列表）
            audioManager.fireballLaunch = src;

            // 通过反射重新构建 sfxSources 列表以包含测试 AudioSource
            var field = typeof(AudioManager).GetField("sfxSources",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var existingList = field.GetValue(audioManager) as System.Collections.Generic.List<AudioSource>;
            if (existingList != null)
            {
                existingList.Add(src);
            }
            else
            {
                // Awake 可能未执行，手动创建列表
                var newList = new System.Collections.Generic.List<AudioSource> { src };
                field.SetValue(audioManager, newList);
            }

            audioManager.SetSFXVolume(0.3f);
            Assert.AreEqual(0.3f, src.volume, 0.001f, "SFX 音量应应用到所有 AudioSource");

            Object.DestroyImmediate(srcGO);
        }

        [Test]
        public void AudioManager_SetMasterVolume_PersistsInPlayerPrefs()
        {
            audioManager.SetMasterVolume(0.7f);
            Assert.AreEqual(0.7f, PlayerPrefs.GetFloat("Volume_Master", 0f), 0.001f,
                "主音量应持久化到 PlayerPrefs");
        }

        [Test]
        public void AudioManager_SetSFXVolume_PersistsInPlayerPrefs()
        {
            audioManager.SetSFXVolume(0.4f);
            Assert.AreEqual(0.4f, PlayerPrefs.GetFloat("Volume_SFX", 0f), 0.001f,
                "SFX 音量应持久化到 PlayerPrefs");
        }

        [Test]
        public void AudioManager_GetMasterVolume_ReturnsSetValue()
        {
            audioManager.SetMasterVolume(0.8f);
            Assert.AreEqual(0.8f, audioManager.GetMasterVolume(), 0.001f,
                "GetMasterVolume 应返回设置的值");
        }

        [Test]
        public void AudioManager_GetSFXVolume_ReturnsSetValue()
        {
            audioManager.SetSFXVolume(0.2f);
            Assert.AreEqual(0.2f, audioManager.GetSFXVolume(), 0.001f,
                "GetSFXVolume 应返回设置的值");
        }

        [Test]
        public void AudioManager_SetVolume_ClampsToMinimum()
        {
            audioManager.SetMasterVolume(0f);
            Assert.IsTrue(audioManager.GetMasterVolume() >= 0.0001f,
                "音量设为 0 应被钳制到最小值 0.0001");
        }
    }
}
