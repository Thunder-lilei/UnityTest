using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    public float speed = 20f;
    private int count;
    public TextMeshProUGUI countText;
    public GameObject winTextObject;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        count = 0; 
        SetCountText();
        winTextObject.SetActive(false);
    }

    void FixedUpdate()
    {
        float movementX = Input.GetAxis("Horizontal");
        float movementY = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(movementX, 0.0f, movementY);

        rb.AddForce(movement * speed);
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
            winTextObject.gameObject.SetActive(true);
            winTextObject.GetComponent<TextMeshProUGUI>().text = "失败!";
        }
    }

    void SetCountText() 
    {
       countText.text =  "得分: " + count.ToString();

       if (count >= 4)
       {
           winTextObject.SetActive(true);
           // 移除敌人
           Destroy(GameObject.FindGameObjectWithTag("Enemy"));
       }
    }
}
