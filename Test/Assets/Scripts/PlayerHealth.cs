using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Audio;
using Game.UI;

namespace Game.Player
{
    
    /// <summary>玩家健康：受击扣血（DPS 上限）、闪避无敌、死亡判定、游戏结束</summary>
    public class PlayerHealth : MonoBehaviour
    {
        [Header("UI")]
        public GameObject gameOverPanel;
        public TMPro.TextMeshProUGUI resultText;

        [Header("受击")]
        [Tooltip("全局受击 DPS 上限（无论多少敌人围攻）")]
        public float maxDPS = 40f;
        [Tooltip("受击音效播放冷却（秒）")]
        public float hurtSoundCooldown = 0.5f;

        // 事件：玩家死亡（其他系统订阅以做收尾）
        public event System.Action OnPlayerDied;

        private HealthBar healthBar;
        private bool isDashing;
        private float damageAccumulator;
        private float lastHurtSoundTime;

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
            var movement = GetComponent<PlayerMovement>();
            if (movement != null)
                movement.OnDashStateChanged -= OnDashStateChanged;
        }

        void OnDashStateChanged(bool dashing)
        {
            isDashing = dashing;
        }

        /// <summary>持续碰撞：敌人接触累计伤害，全局 DPS 上限防秒杀</summary>
        void OnCollisionStay(Collision collision)
        {
            if (isDashing) return;

            if (collision.gameObject.CompareTag("Enemy"))
            {
                // 累计伤害，不直接扣血
                damageAccumulator += 20f * Time.deltaTime;
            }
        }

        void FixedUpdate()
        {
            if (healthBar == null || isDashing) return;

            // 统一结算伤害：累计值超过 DPS 上限的部分丢弃
            if (damageAccumulator > 0f)
            {
                float maxDamage = maxDPS * Time.fixedDeltaTime;
                float actualDamage = Mathf.Min(damageAccumulator, maxDamage);
                damageAccumulator = 0f;

                if (actualDamage > 0f)
                {
                    healthBar.TakeDamage(actualDamage);

                    // 受击音效冷却
                    if (Time.time - lastHurtSoundTime > hurtSoundCooldown)
                    {
                        AudioManager.Instance?.PlayPlayerHurt();
                        lastHurtSoundTime = Time.time;
                    }

                    if (healthBar.IsDead())
                    {
                        AudioManager.Instance?.PlayPlayerDeath();
                        ShowGameOver();
                        OnPlayerDied?.Invoke();
                    }
                }
            }
        }

        void ShowGameOver()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
            AudioManager.Instance?.PlayGameOver();
            Time.timeScale = 0;

            // 游戏结束需要鼠标点击按钮
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
    
}
