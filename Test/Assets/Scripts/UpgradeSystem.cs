using UnityEngine;

public class UpgradeSystem : MonoBehaviour
{
    public GameObject upgradePanel;        // 升级面板 UI
    public UpgradeCard[] cards;            // 三张卡片引用
    public Sprite[] icons;                 // 三种升级图标（按 UpgradeType 顺序）

    public enum UpgradeType { MaxHealth, Speed, FireballCount }  // 升级类型枚举

    private static readonly UpgradeType[] allTypes = {           // 所有可选类型
        UpgradeType.MaxHealth,
        UpgradeType.Speed,
        UpgradeType.FireballCount
    };

    private static readonly string[] titles = {                  // 卡片标题（按类型顺序）
        "+ \u6700\u5927\u8840\u91cf",
        "+ \u79fb\u52a8\u901f\u5ea6",
        "+ \u706b\u7403\u6570\u91cf"
    };

    private static readonly string[] descs = {                   // 卡片描述（按类型顺序）
        "\u6700\u5927\u751f\u547d\u503c +20\uff0c\u540c\u65f6\u56de\u590d20\u70b9\u8840\u91cf",
        "\u79fb\u52a8\u901f\u5ea6 +2",
        "\u706b\u7403\u53d1\u5c04\u6570\u91cf +1"
    };

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

        Time.timeScale = 0;
        upgradePanel.SetActive(true);
    }

    public void SelectUpgrade(UpgradeType type)
    {
        HealthBar healthBar = GetComponent<HealthBar>();
        PlayerController player = GetComponent<PlayerController>();

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
        }

        AudioManager.Instance?.PlayUpgradeConfirm();
        Time.timeScale = 1;
        upgradePanel.SetActive(false);
    }
}