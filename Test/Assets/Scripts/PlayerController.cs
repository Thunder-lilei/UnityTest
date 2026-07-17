using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;                  // 玩家刚体组件
    private Animator animator;             // 玩家动画控制器
    public float speed = 10f;              // 移动速度
    public GameObject gameOverPanel;       // 游戏结束面板
    public TextMeshProUGUI resultText;     // 结束结果文本
    public GameObject footprintPrefab;     // 脚印 Prefab
    public float footprintSpacing = 1f;    // 脚印生成间距
    private Vector3 lastFootprintPos;      // 上一个脚印位置
    private bool isLeftFoot = true;        // 左右脚交替标记
    public GameObject foot;                // 脚印父物体
    public GameObject skill;               // 技能特效父物体
    public GameObject fireballPrefab;      // 火球 Prefab
    public Camera mainCamera;              // 主摄像机
    public int fireballCount = 1;          // 火球发射数量

    void Start()
    {
        Time.timeScale = 1;
        gameOverPanel.SetActive(false);
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        lastFootprintPos = transform.position;
        // 获取场景主相机
        mainCamera = Camera.main;
    }

    void Update()
    {
        // 左键按下
        if (Input.GetMouseButtonDown(0) && Time.timeScale > 0)
        {
            FireFireball();
        }
    }

    void FixedUpdate()
    {
        float movementX = Input.GetAxis("Horizontal");
        float movementY = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(movementX, 0.0f, movementY);

        // 不用AddForce 恒定速度
        // 不改变Y轴 避免穿模
        Vector3 vel = rb.velocity;
        vel.x = movementX * speed;
        vel.z = movementY * speed;
        rb.velocity = vel;
        animator.SetFloat("Speed", rb.velocity.magnitude);

        // 朝向移动方向
        if (movement.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.1f);
        }

        // 移动时留脚印
        if (rb.velocity.magnitude > 0.1f)
        {
            if (Vector3.Distance(transform.position, lastFootprintPos) >= footprintSpacing)
            {
                Vector3 pos = transform.position;
                pos.y = 0.01f;
                
                // 左右脚偏移
                pos += transform.right * (isLeftFoot ? -0.2f : 0.2f);
                
                Quaternion rot = Quaternion.LookRotation(rb.velocity) * Quaternion.Euler(90, 0, 0);
                Instantiate(footprintPrefab, pos, rot, foot.transform);
                lastFootprintPos = transform.position;
                isLeftFoot = !isLeftFoot;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PickUp"))
        {
            Destroy(other.gameObject);
            ExpBar expBar = GetComponent<ExpBar>();
            if (expBar != null)
                expBar.AddExp(10f);
            AudioManager.Instance?.PlayPickupExp();
        }
        else if (other.gameObject.CompareTag("HealthPotion"))
        {
            HealthBar healthBar = GetComponent<HealthBar>();
            if (healthBar != null && !healthBar.IsFull())
            {
                Destroy(other.gameObject);
                healthBar.Heal(30f);
                AudioManager.Instance?.PlayHealthPotionPickup();
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            HealthBar healthBar = GetComponent<HealthBar>();
            if (healthBar != null)
            {
                healthBar.TakeDamage(20f * Time.deltaTime);
                AudioManager.Instance?.PlayPlayerHurt();
                if (healthBar.IsDead())
                {
                    AudioManager.Instance?.PlayPlayerDeath();
                    ShowGameOver();
                    Destroy(gameObject);
                }
            }
        }
    }

    void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
        AudioManager.Instance?.PlayGameOver();
        Time.timeScale = 0;
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    void FireFireball()
    {
        if (fireballPrefab == null || skill == null || mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
            return;

        Vector3 direction = hit.point - transform.position;
        direction.y = 0;
        direction.Normalize();

        for (int i = 0; i < fireballCount; i++)
        {
            float angle = 0f;
            if (fireballCount > 1)
                angle = Mathf.Lerp(-15f, 15f, i / (float)(fireballCount - 1));

            Vector3 dir = Quaternion.Euler(0, angle, 0) * direction;
            Vector3 spawnPos = transform.position + Vector3.up + dir;
            Instantiate(fireballPrefab, spawnPos, Quaternion.LookRotation(dir), skill.transform);
        }
        AudioManager.Instance?.PlayFireballLaunch();
    }
}

