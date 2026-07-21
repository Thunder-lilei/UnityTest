using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    /// <summary>三轴非均匀旋转，使用 unscaledDeltaTime 确保暂停时仍旋转</summary>
    void Update()
    {
        transform.Rotate (new Vector3 (15, 30, 45) * Time.unscaledDeltaTime);
    }
}
