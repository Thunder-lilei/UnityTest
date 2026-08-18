using UnityEngine;
using UnityEngine.UI;
using Game.Player;
using Game.Combat;

namespace Game.UI
{
    /// <summary>暂停菜单：ESC 开关暂停，与升级/游戏结束面板互斥，管理鼠标光标显隐</summary>
    public class PauseMenu : MonoBehaviour
    {
        [Header("面板引用")]
        public GameObject pauseMenuPanel;
        public GameObject settingsPanel;
        public GameObject upgradePanel;
        public GameObject gameOverPanel;

        [Header("玩家组件引用（用于 SetPaused）")]
        public PlayerMovement playerMovement;
        public PlayerCombat playerCombat;
        public MeleeCombat meleeCombat;

        [Header("玩家健康引用（重启/退出）")]
        public PlayerHealth playerHealth;

        private bool isPaused;

        void Start()
        {
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // 按钮事件绑定（代码绑定，不依赖 Inspector 序列化）
            BindButton(pauseMenuPanel, "ResumeButton", Resume);
            BindButton(pauseMenuPanel, "SettingsButton", OpenSettings);
            BindButton(pauseMenuPanel, "RestartButton", RestartGame);
            BindButton(pauseMenuPanel, "QuitButton", QuitGame);
            BindButton(settingsPanel, "BackButton", CloseSettings);

            // 游戏开始时隐藏并锁定鼠标
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            isPaused = false;
        }

        /// <summary>在指定面板下按名称查找 Button 并绑定回调</summary>
        void BindButton(GameObject panel, string buttonName, System.Action callback)
        {
            if (panel == null) return;
            var t = panel.transform.Find(buttonName);
            if (t == null) return;
            var btn = t.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => callback());
        }

        void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            // 互斥：升级面板激活时忽略
            if (upgradePanel != null && upgradePanel.activeSelf) return;
            // 互斥：游戏结束面板激活时忽略
            if (gameOverPanel != null && gameOverPanel.activeSelf) return;

            if (isPaused)
            {
                // 设置面板打开时 ESC 返回暂停菜单
                if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    CloseSettings();
                }
                else
                {
                    Resume();
                }
            }
            else
            {
                Pause();
            }
        }

        /// <summary>暂停游戏：timeScale=0 + 暂停子系统 + 显示菜单 + 显示鼠标</summary>
        public void Pause()
        {
            isPaused = true;
            Time.timeScale = 0;

            if (playerMovement != null) playerMovement.SetPaused(true);
            if (playerCombat != null) playerCombat.SetPaused(true);
            if (meleeCombat != null) meleeCombat.SetPaused(true);

            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        /// <summary>恢复游戏：timeScale=1 + 恢复子系统 + 隐藏菜单 + 隐藏鼠标</summary>
        public void Resume()
        {
            isPaused = false;
            Time.timeScale = 1;

            if (playerMovement != null) playerMovement.SetPaused(false);
            if (playerCombat != null) playerCombat.SetPaused(false);
            if (meleeCombat != null) meleeCombat.SetPaused(false);

            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        /// <summary>打开设置面板（隐藏暂停菜单）</summary>
        public void OpenSettings()
        {
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        /// <summary>关闭设置面板（返回暂停菜单）</summary>
        public void CloseSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        }

        /// <summary>重新开始游戏</summary>
        public void RestartGame()
        {
            Time.timeScale = 1;
            if (playerHealth != null)
                playerHealth.RestartGame();
        }

        /// <summary>退出游戏</summary>
        public void QuitGame()
        {
            if (playerHealth != null)
                playerHealth.QuitGame();
        }
    }
}
