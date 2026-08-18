using UnityEngine;

namespace Game.Systems
{
    
    /// <summary>旋转装饰物：三轴非均匀旋转，暂停时仍旋转（unscaledDeltaTime）</summary>
    public class Rotator : MonoBehaviour
    {
        /// <summary>三轴非均匀旋转，使用 unscaledDeltaTime 确保暂停时仍旋转</summary>
        void Update()
        {
            transform.Rotate (new Vector3 (15, 30, 45) * Time.unscaledDeltaTime);
        }
    }
    
}
