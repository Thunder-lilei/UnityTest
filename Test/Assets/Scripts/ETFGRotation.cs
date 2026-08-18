using UnityEngine;

/// <summary>特效旋转：持续绕 Z 轴旋转</summary>
public class ETFGRotation : MonoBehaviour
{
    public float speed = 30f;

    void Update()
    {
        transform.Rotate(0, 0, speed * Time.deltaTime);
    }
}