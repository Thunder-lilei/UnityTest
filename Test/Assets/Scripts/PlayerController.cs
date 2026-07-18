using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;                  // 玩家刚体组件
    private Animator animator;             // 玩家动画控制器
    private HealthBar healthBar;           // 缓存血量组件
    private ExpBar expBar;                 // 缓存经验组件
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
    private bool isPaused = false;         // 是否暂停（升级选择时）
    private ObjectPool fireballPool;       // 火球对象池
    private ObjectPool footprintPool;      // 脚印对象池
    public float dashSpeed = 30f;          // 闪避速度
    public float dashDuration = 0.2f;      // 闪避持续时间
    public float dashCooldown = 2f;       // 冷却时间
    private bool isDashing = false;        // 是否正在闪避
    private float dashTimer = 0f;         // 闪避计时器
    private float cooldownTimer = 0f;      // 冷却计时器
    public Image dashIcon;                 // 冷却图标引用

    void Start()
    {
        Time.timeScale = 1;
        gameOverPanel.SetActive(false);
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        healthBar = GetComponent<HealthBar>();
        expBar = GetComponent<ExpBar>();
        lastFootprintPos = transform.position;
        // 获取场景主相机
        mainCamera = Camera.main;

        // 初始化对象池
        fireballPool = CreatePool(fireballPrefab, skill.transform, 5);
        fireballPrefab.GetComponent<Fireball>()?.SetPool(fireballPool);
        footprintPool = CreatePool(footprintPrefab, foot.transform, 20);
        footprintPrefab.GetComponent<Footprint>()?.SetPool(footprintPool);
    }

    ObjectPool CreatePool(GameObject prefab, Transform parent, int size)
    {
        var poolGo = new GameObject(prefab.name + "_Pool");
        poolGo.transform.SetParent(parent, false);
        var pool = poolGo.AddComponent<ObjectPool>();
        pool.prefab = prefab;
        pool.initialSize = size;
        pool.parent = parent;
        return pool;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isPaused)
        {
            FireFireball();
        }

        if (Input.GetKeyDown(KeyCode.Space) && !isPaused && !isDashing && cooldownTimer <= 0)
        {
            Dash();
        }

        // 冷却计时 + UI 更新
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            if (dashIcon != null)
            {
                // 蓝色覆盖层从 0→1 恢复
                dashIcon.fillAmount = 1f - (cooldownTimer / dashCooldown);
                dashIcon.color = new Color(0.3f, 0.6f, 1f, 1f);
            }
        }
        else
        {
            if (dashIcon != null)
            {
                dashIcon.fillAmount = 1f;
                dashIcon.color = new Color(0.3f, 0.6f, 1f, 1f);
            }
        }
    }

    void FixedUpdate()
    {
        float movementX = Input.GetAxis("Horizontal");
        float movementY = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(movementX, 0.0f, movementY);

        if (!isDashing)
        {
            Vector3 vel = rb.velocity;
            vel.x = movementX * speed;
            vel.z = movementY * speed;
            rb.velocity = vel;
        }
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
                footprintPool.Spawn(pos, rot);
                lastFootprintPos = transform.position;
                isLeftFoot = !isLeftFoot;
            }
        }

        // 闪避计时
        if (isDashing)
        {
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0)
            {
                isDashing = false;
                cooldownTimer = dashCooldown;
            }
        }
    }

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

    private void OnCollisionStay(Collision collision)
    {
        if (isDashing)
            return;

        if (collision.gameObject.CompareTag("Enemy"))
        {
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

    public void SetPaused(bool paused)
    {
        isPaused = paused;
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
            fireballPool.Spawn(spawnPos, Quaternion.LookRotation(dir));
        }
        AudioManager.Instance?.PlayFireballLaunch();
    }

    void Dash()
    {
        isDashing = true;
        dashTimer = dashDuration;

        float mx = Input.GetAxis("Horizontal");
        float my = Input.GetAxis("Vertical");
        Vector3 dir = new Vector3(mx, 0, my);
        if (dir.magnitude < 0.1f)
            dir = transform.forward;

        dir.y = 0;
        dir.Normalize();

        rb.velocity = dir * dashSpeed;
        AudioManager.Instance?.PlayDash();
    }
}

