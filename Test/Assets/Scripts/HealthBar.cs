using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public RectTransform fillRect;         // 血量条填充矩形
    public float maxHealth = 100f;         // 最大血量
    private float currentHealth;           // 当前血量

    void Start()
    {
        currentHealth = maxHealth;
        UpdateFill();
    }

    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        UpdateFill();
    }

    public bool IsDead()
    {
        return currentHealth <= 0f;
    }

    public bool IsFull()
    {
        return currentHealth >= maxHealth;
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateFill();
    }

    public void IncreaseMaxHealth(float amount)
    {
        maxHealth += amount;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateFill();
    }

    void UpdateFill()
    {
        if (fillRect != null)
        {
            float ratio = currentHealth / maxHealth;
            fillRect.anchorMax = new Vector2(ratio, fillRect.anchorMax.y);
        }
    }
}
