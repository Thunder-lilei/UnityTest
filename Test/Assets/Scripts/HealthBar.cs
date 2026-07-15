using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public RectTransform fillRect;
    public float maxHealth = 100f;
    private float currentHealth;

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

    void UpdateFill()
    {
        if (fillRect != null)
        {
            float ratio = currentHealth / maxHealth;
            fillRect.anchorMax = new Vector2(ratio, fillRect.anchorMax.y);
        }
    }
}
