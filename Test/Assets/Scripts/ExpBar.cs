using UnityEngine;
using TMPro;

public class ExpBar : MonoBehaviour
{
    public RectTransform fillRect;
    public TextMeshProUGUI levelText;
    public float maxExp = 100f;
    private float currentExp;
    public int level = 1;

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
            currentExp -= maxExp;
            level++;
            maxExp += 20f;
            UpdateLevelText();
            AudioManager.Instance?.PlayLevelUp();
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
