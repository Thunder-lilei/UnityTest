using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    private Animator animator;
    public float speed = 10f;
    private int count;
    public TextMeshProUGUI countText;
    public GameObject gameOverPanel;
    public TextMeshProUGUI resultText;
    public GameObject footprintPrefab;   // 脚印 Prefab
    public float footprintSpacing = 1f;  // 每隔多远留一个脚印
    private Vector3 lastFootprintPos;     // 上一个脚印位置
    private bool isLeftFoot = true;
    public GameObject foot;

    void Start()
    {
        Time.timeScale = 1;
        gameOverPanel.SetActive(false);
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        count = 0; 
        SetCountText();
        lastFootprintPos = transform.position;
        foot = transform.Find("Foot").gameObject;
    }

    void FixedUpdate()
    {
        float movementX = Input.GetAxis("Horizontal");
        float movementY = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(movementX, 0.0f, movementY);

        // 不用AddForce 恒定速度
        rb.velocity = new Vector3(movementX * speed, rb.velocity.y, movementY * speed);
        animator.SetFloat("Speed", rb.velocity.magnitude);

        // 朝向移动方向
        if (movement != Vector3.zero)
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
                
                Quaternion rot = Quaternion.LookRotation(rb.velocity);
                Instantiate(footprintPrefab, pos, rot, foot.transform);
                lastFootprintPos = transform.position;
                isLeftFoot = !isLeftFoot;
            }
        }
    }

    void OnTriggerEnter(Collider other) 
    {
        // 标签匹配 这个标签被设置给预制体
        if (other.gameObject.CompareTag("PickUp")) 
        {
            // 将另一个对象设置为非活动
            other.gameObject.SetActive(false);
            count++;
            SetCountText();
        }
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 被抓到就失败
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Destroy(gameObject); 
            ShowGameOver();
        }
    }

    void SetCountText() 
    {
       countText.text =  "得分: " + count.ToString();

       if (count >= 4)
       {
           // 移除敌人
           Destroy(GameObject.FindGameObjectWithTag("Enemy"));
           ShowGameOver();
       }
    }

    void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
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
}

