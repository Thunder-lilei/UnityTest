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

    [Header("连击音效设置")]
    [Tooltip("每次连击音调升高幅度")]
    public float comboPitchStep = 0.05f;
    [Tooltip("连击最大音调倍数")]
    public float maxComboPitch = 1.5f;
    [Tooltip("连击重置时间（秒）")]
    public float comboResetTime = 2f;

    private int comboCount;
    private float lastHitTime;

    /// <summary>单例初始化</summary>
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        // 超过重置时间未命中，连击归零
        if (comboCount > 0 && Time.time - lastHitTime >= comboResetTime)
            comboCount = 0;
    }

    public void PlayFireballLaunch() { if (fireballLaunch != null) fireballLaunch.Play(); }

    /// <summary>火球命中：连击次数越多音调越高，超过重置时间则归零</summary>
    public void PlayFireballHit()
    {
        if (fireballHit == null) return;

        if (Time.time - lastHitTime < comboResetTime)
            comboCount++;
        else
            comboCount = 0;

        lastHitTime = Time.time;
        fireballHit.pitch = Mathf.Min(1f + comboCount * comboPitchStep, maxComboPitch);
        fireballHit.Play();
    }

    /// <summary>敌人死亡时重置连击</summary>
    public void PlayEnemyDeath()
    {
        if (enemyDeath != null) enemyDeath.Play();
        comboCount = 0;
    }

    public void PlayPlayerHurt() { if (playerHurt != null) playerHurt.Play(); }
    public void PlayPlayerDeath() { if (playerDeath != null) playerDeath.Play(); }
    public void PlayPickupExp() { if (pickupExp != null) pickupExp.Play(); }
    public void PlayLevelUp() { if (levelUp != null) levelUp.Play(); }
    public void PlayGameOver() { if (gameOver != null) gameOver.Play(); }
    public void PlayHealthPotionPickup() { if (healthPotionPickup != null) healthPotionPickup.Play(); }
    public void PlayUpgradeConfirm() { if (upgradeConfirm != null) upgradeConfirm.Play(); }
    public void PlayDash() { if (dash != null) dash.Play(); }
}
