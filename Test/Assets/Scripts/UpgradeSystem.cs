using UnityEngine;
using Game.Audio;
using Game.Player;
using Game.Enemy;
using Game.Combat;
using Game.Systems;

namespace Game.UI
{
    
    /// <summary>升级选择系统：升级时暂停游戏，四选三随机，应用升级效果</summary>
    public class UpgradeSystem : MonoBehaviour
    {
        public GameObject upgradePanel;
        public UpgradeCard[] cards;
        public Sprite[] icons;
    
        public enum UpgradeType { MaxHealth, Speed, FireballCount, MagnetRange }
        private const int OPTION_COUNT = 3;
    
        private static readonly UpgradeType[] allTypes = {
            UpgradeType.MaxHealth,
            UpgradeType.Speed,
            UpgradeType.FireballCount,
            UpgradeType.MagnetRange
        };
    
        private static readonly string[] titles = {
            "+ \u6700\u5927\u8840\u91cf",
            "+ \u79fb\u52a8\u901f\u5ea6",
            "+ \u706b\u7403\u6570\u91cf",
            "+ \u5438\u53d6\u8303\u56f4"
        };
    
        private static readonly string[] descs = {
            "\u6700\u5927\u751f\u547d\u503c +20\uff0c\u540c\u65f6\u56de\u590d20\u70b9\u8840\u91cf",
            "\u79fb\u52a8\u901f\u5ea6 +2",
            "\u706b\u7403\u53d1\u5c04\u6570\u91cf +1",
            "\u5438\u53d6\u534a\u5f84 +1"
        };
    
        // 拆分后引用各子系统组件
        private PlayerMovement playerMovement;
        private PlayerCombat playerCombat;
        private HealthBar healthBar;
        private MagnetDetector magnetDetector;
    
        /// <summary>缓存各组件引用</summary>
        void Start()
        {
            playerMovement = GetComponent<PlayerMovement>();
            playerCombat = GetComponent<PlayerCombat>();
            healthBar = GetComponent<HealthBar>();
            magnetDetector = GetComponentInChildren<MagnetDetector>();
        }
    
        /// <summary>显示升级面板：随机打乱四种类型取前三个，暂停游戏</summary>
        public void ShowUpgrades()
        {
            UpgradeType[] shuffled = (UpgradeType[])allTypes.Clone();
            for (int i = shuffled.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                UpgradeType temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }
    
            for (int i = 0; i < cards.Length; i++)
            {
                UpgradeType type = shuffled[i];
                cards[i].SetData(type, titles[(int)type], descs[(int)type]);
                if (icons != null && icons.Length > (int)type && cards[i].icon != null)
                    cards[i].icon.sprite = icons[(int)type];
                cards[i].SetupCallback(this);
            }
    
            // 暂停所有子系统
            if (playerMovement != null) playerMovement.SetPaused(true);
            if (playerCombat != null) playerCombat.SetPaused(true);
            Time.timeScale = 0;
            upgradePanel.SetActive(true);
        }
    
        /// <summary>应用选中的升级效果并恢复游戏</summary>
        public void SelectUpgrade(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.MaxHealth:
                    if (healthBar != null)
                        healthBar.IncreaseMaxHealth(20f);
                    break;
                case UpgradeType.Speed:
                    if (playerMovement != null)
                        playerMovement.speed += 2f;
                    break;
                case UpgradeType.FireballCount:
                    if (playerCombat != null)
                        playerCombat.fireballCount += 1;
                    break;
                case UpgradeType.MagnetRange:
                    if (magnetDetector != null)
                        magnetDetector.IncreaseRadius(1f);
                    break;
            }
    
            AudioManager.Instance?.PlayUpgradeConfirm();
            Time.timeScale = 1;
            if (playerMovement != null) playerMovement.SetPaused(false);
            if (playerCombat != null) playerCombat.SetPaused(false);
            upgradePanel.SetActive(false);
        }
    }
    
}
