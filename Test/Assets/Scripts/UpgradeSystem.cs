using UnityEngine;

public class UpgradeSystem : MonoBehaviour
{
    public GameObject upgradePanel;        // 升级面板 UI
    public UpgradeCard[] cards;            // 三张卡片引用
    public Sprite[] icons;                 // 四种升级图标（按 UpgradeType 顺序）

    public enum UpgradeType { MaxHealth, Speed, FireballCount, MagnetRange }  // 升级类型枚举
    private const int OPTION_COUNT = 3;   // 每次展示3个选项

    private static readonly UpgradeType[] allTypes = {           // 所有可选类型
        UpgradeType.MaxHealth,
        UpgradeType.Speed,
        UpgradeType.FireballCount,
        UpgradeType.MagnetRange
    };

    private static readonly string[] titles = {                  // 卡片标题（按类型顺序）
        "+ \u6700\u5927\u8840\u91cf",
        "+ \u79fb\u52a8\u901f\u5ea6",
        "+ \u706b\u7403\u6570\u91cf",
        "+ \u5438\u53d6\u8303\u56f4"
    };

    private static readonly string[] descs = {                   // 卡片描述（按类型顺序）
        "\u6700\u5927\u751f\u547d\u503c +20\uff0c\u540c\u65f6\u56de\u590d20\u70b9\u8840\u91cf",
        "\u79fb\u52a8\u901f\u5ea6 +2",
        "\u706b\u7403\u53d1\u5c04\u6570\u91cf +1",
        "\u5438\u53d6\u534a\u5f84 +1"
    };

    private PlayerController player;       // 缓存 PlayerController 引用
    private HealthBar healthBar;           // 缓存 HealthBar 引用
    private MagnetDetector magnetDetector; // 缓存磁吸检测器引用

    void Start()
    {
        player = GetComponent<PlayerController>();
        healthBar = GetComponent<HealthBar>();
        magnetDetector = GetComponentInChildren<MagnetDetector>();
    }

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

        if (player != null) player.SetPaused(true);
        Time.timeScale = 0;
        upgradePanel.SetActive(true);
    }

    public void SelectUpgrade(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.MaxHealth:
                if (healthBar != null)
                    healthBar.IncreaseMaxHealth(20f);
                break;
            case UpgradeType.Speed:
                if (player != null)
                    player.speed += 2f;
                break;
            case UpgradeType.FireballCount:
                if (player != null)
                    player.fireballCount += 1;
                break;
            case UpgradeType.MagnetRange:
                if (magnetDetector != null)
                    magnetDetector.IncreaseRadius(1f);
                break;
        }

        AudioManager.Instance?.PlayUpgradeConfirm();
        Time.timeScale = 1;
        if (player != null) player.SetPaused(false);
        upgradePanel.SetActive(false);
    }
}