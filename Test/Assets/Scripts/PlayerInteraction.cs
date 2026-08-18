using UnityEngine;
using Game.Audio;
using Game.UI;

namespace Game.Player
{
    
    /// <summary>玩家交互：拾取经验方块和血瓶</summary>
    public class PlayerInteraction : MonoBehaviour
    {
        private ExpBar expBar;
        private HealthBar healthBar;
    
        void Start()
        {
            expBar = GetComponent<ExpBar>();
            healthBar = GetComponent<HealthBar>();
        }
    
        /// <summary>触发器回调：拾取经验方块和血瓶</summary>
        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("PickUp"))
            {
                Destroy(other.gameObject);
                if (expBar != null)
                    expBar.AddExp(10f);
                AudioManager.Instance?.PlayPickupExp();
            }
            else if (other.gameObject.CompareTag("HealthPotion"))
            {
                if (healthBar != null && !healthBar.IsFull())
                {
                    Destroy(other.gameObject);
                    healthBar.Heal(30f);
                    AudioManager.Instance?.PlayHealthPotionPickup();
                }
            }
        }
    }
    
}
