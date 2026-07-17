using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject player;             // 跟随目标
    private Vector3 offset;               // 摄像机与玩家的初始偏移
    
    void Start()
    {
        if (player == null)
            return;
        offset = transform.position - player.transform.position; 
    }

    void LateUpdate()
    {
        if(player != null)
        {
            transform.position = player.transform.position + offset;    
        }
    }
}
