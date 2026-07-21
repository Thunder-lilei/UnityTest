using UnityEngine;
using TMPro;

public class ExpBar : MonoBehaviour
{
    public RectTransform fillRect;         // 经验条填充矩形
    public TextMeshProUGUI levelText;      // 等级文本
    public float maxExp = 100f;            // 当前等级所需经验
    private float currentExp;              // 当前累计经验
    public int level = 1;                  // 当前等级

    /// <summary>初始化经验值和 UI</summary>
    void Start()
    {
        currentExp = 0f;
        UpdateFill();
        UpdateLevelText();
    }

    /// <summary>增加经验，满经验升级（while 循环支持跨多级），触发升级面板</summary>
    /// <param name="amount">经验值</param>
    public void AddExp(float amount)
    {
        currentExp += amount;
        if (currentExp >= maxExp)
        {
            while (currentExp >= maxExp)
            {
                currentExp -= maxExp;
                level++;
                maxExp += 20f;
            }
            UpdateLevelText();
            AudioManager.Instance?.PlayLevelUp();
            UpgradeSystem upgradeSystem = GetComponent<UpgradeSystem>();
            if (upgradeSystem != null)
                upgradeSystem.ShowUpgrades();
        }
        UpdateFill();
    }

    /// <summary>根据当前经验比例更新经验条 UI</summary>
    void UpdateFill()
    {
        if (fillRect != null)
        {
            float ratio = currentExp / maxExp;
            fillRect.anchorMax = new Vector2(ratio, fillRect.anchorMax.y);
        }
    }

    /// <summary>更新等级文本为 "Lv. N"</summary>
    void UpdateLevelText()
    {
        if (levelText != null)
            levelText.text = "Lv. " + level.ToString();
    }
}
