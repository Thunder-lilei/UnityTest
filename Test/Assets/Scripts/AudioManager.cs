using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }  // 单例实例

    public AudioSource fireballLaunch;     // 火球发射音效
    public AudioSource fireballHit;        // 火球命中音效
    public AudioSource enemyDeath;         // 敌人死亡音效
    public AudioSource playerHurt;         // 玩家受伤音效
    public AudioSource playerDeath;        // 玩家死亡音效
    public AudioSource pickupExp;          // 拾取经验音效
    public AudioSource levelUp;            // 升级音效
    public AudioSource gameOver;           // 游戏结束音效
    public AudioSource healthPotionPickup; // 拾取血瓶音效
    public AudioSource upgradeConfirm;     // 升级选择确认音效
    public AudioSource dash;              // 闪避音效

    /// <summary>单例初始化</summary>
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void PlayFireballLaunch() { if (fireballLaunch != null) fireballLaunch.Play(); }
    public void PlayFireballHit() { if (fireballHit != null) fireballHit.Play(); }
    public void PlayEnemyDeath() { if (enemyDeath != null) enemyDeath.Play(); }
    public void PlayPlayerHurt() { if (playerHurt != null) playerHurt.Play(); }
    public void PlayPlayerDeath() { if (playerDeath != null) playerDeath.Play(); }
    public void PlayPickupExp() { if (pickupExp != null) pickupExp.Play(); }
    public void PlayLevelUp() { if (levelUp != null) levelUp.Play(); }
    public void PlayGameOver() { if (gameOver != null) gameOver.Play(); }
    public void PlayHealthPotionPickup() { if (healthPotionPickup != null) healthPotionPickup.Play(); }
    public void PlayUpgradeConfirm() { if (upgradeConfirm != null) upgradeConfirm.Play(); }
    public void PlayDash() { if (dash != null) dash.Play(); }
}
