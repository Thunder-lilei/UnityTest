using UnityEngine;
using TMPro;

public class ExpBar : MonoBehaviour
{
    public RectTransform fillRect;         // 经验条填充矩形
    public TextMeshProUGUI levelText;      // 等级文本
    public float maxExp = 100f;            // 当前等级所需经验
    private float currentExp;              // 当前累计经验
    public int level = 1;                  // 当前等级

    void Start()
    {
        currentExp = 0f;
        UpdateFill();
        UpdateLevelText();
    }

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

    void UpdateFill()
    {
        if (fillRect != null)
        {
            float ratio = currentExp / maxExp;
            fillRect.anchorMax = new Vector2(ratio, fillRect.anchorMax.y);
        }
    }

    void UpdateLevelText()
    {
        if (levelText != null)
            levelText.text = "Lv. " + level.ToString();
    }
}
