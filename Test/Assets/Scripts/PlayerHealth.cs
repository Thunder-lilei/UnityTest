using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Game.Audio;
using Game.Enemy;
using Game.Combat;
using Game.UI;
using Game.Systems;

namespace Game.Player
{
    
    /// <summary>玩家健康：受击扣血、闪避无敌、死亡判定、游戏结束</summary>
    public class PlayerHealth : MonoBehaviour
    {
        [Header("UI")]
        public GameObject gameOverPanel;
        public TMPro.TextMeshProUGUI resultText;
    
        // 事件：玩家死亡（其他系统订阅以做收尾）
        public event System.Action OnPlayerDied;
    
        private HealthBar healthBar;
        private bool isDashing;
    
        void Start()
        {
            healthBar = GetComponent<HealthBar>();
    
            // 重置游戏状态
            Time.timeScale = 1;
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
    
            // 订阅 PlayerMovement 的闪避状态事件（闪避期间无敌）
            var movement = GetComponent<PlayerMovement>();
            if (movement != null)
                movement.OnDashStateChanged += OnDashStateChanged;
        }
    
        void OnDestroy()
        {
            // 取消订阅，防止内存泄漏
            var movement = GetComponent<PlayerMovement>();
            if (movement != null)
                movement.OnDashStateChanged -= OnDashStateChanged;
        }
    
        /// <summary>闪避状态变化回调：更新无敌状态</summary>
        void OnDashStateChanged(bool dashing)
        {
            isDashing = dashing;
        }
    
        /// <summary>持续碰撞：敌人接触扣血，闪避期间无敌</summary>
        void OnCollisionStay(Collision collision)
        {
            if (isDashing) return;
    
            if (collision.gameObject.CompareTag("Enemy"))
            {
                if (healthBar != null)
                {
                    healthBar.TakeDamage(20f * Time.deltaTime);
                    AudioManager.Instance?.PlayPlayerHurt();
    
                    if (healthBar.IsDead())
                    {
                        AudioManager.Instance?.PlayPlayerDeath();
                        ShowGameOver();
                        OnPlayerDied?.Invoke();
                    }
                }
            }
        }
    
        /// <summary>显示游戏结束面板并暂停游戏</summary>
        void ShowGameOver()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
            AudioManager.Instance?.PlayGameOver();
            Time.timeScale = 0;
        }
    
        /// <summary>重新加载当前场景</summary>
        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    
        /// <summary>退出游戏</summary>
        public void QuitGame()
        {
            Application.Quit();
        }
    }
    
}
