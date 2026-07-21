using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject player;             // 跟随目标
    private Vector3 offset;               // 摄像机与玩家的初始偏移
    
    /// <summary>计算摄像机与玩家的初始偏移</summary>
    void Start()
    {
        if (player == null)
            return;
        offset = transform.position - player.transform.position; 
    }

    /// <summary>在所有 Update 完成后跟随玩家位置（避免抖动）</summary>
    void LateUpdate()
    {
        if(player != null)
        {
            transform.position = player.transform.position + offset;    
        }
    }
}
