using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    // 旋转速度 (15, 30, 45) 度/秒，使用 unscaledDeltaTime 确保暂停时仍旋转
    void Update()
    {
        transform.Rotate (new Vector3 (15, 30, 45) * Time.unscaledDeltaTime);
    }
}
