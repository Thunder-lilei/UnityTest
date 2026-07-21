using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public RectTransform fillRect;         // 血量条填充矩形
    public float maxHealth = 100f;         // 最大血量
    private float currentHealth;           // 当前血量

    /// <summary>初始化血量并更新 UI</summary>
    void Start()
    {
        currentHealth = maxHealth;
        UpdateFill();
    }

    /// <summary>扣血，血量不低于 0</summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        UpdateFill();
    }

    /// <summary>是否死亡（血量归零）</summary>
    /// <returns>血量是否 <= 0</returns>
    public bool IsDead()
    {
        return currentHealth <= 0f;
    }

    /// <summary>是否满血</summary>
    /// <returns>血量是否 >= maxHealth</returns>
    public bool IsFull()
    {
        return currentHealth >= maxHealth;
    }

    /// <summary>回血，血量不超过 maxHealth</summary>
    /// <param name="amount">回复量</param>
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateFill();
    }

    /// <summary>增加最大血量上限并回血</summary>
    /// <param name="amount">增加的上限值</param>
    public void IncreaseMaxHealth(float amount)
    {
        maxHealth += amount;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateFill();
    }

    /// <summary>根据当前血量比例更新血条 UI</summary>
    void UpdateFill()
    {
        if (fillRect != null)
        {
            float ratio = currentHealth / maxHealth;
            fillRect.anchorMax = new Vector2(ratio, fillRect.anchorMax.y);
        }
    }
}
