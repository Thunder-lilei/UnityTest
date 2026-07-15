using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioSource fireballLaunch;
    public AudioSource fireballHit;
    public AudioSource enemyDeath;
    public AudioSource playerHurt;
    public AudioSource playerDeath;
    public AudioSource pickupExp;
    public AudioSource levelUp;
    public AudioSource gameOver;

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
}
