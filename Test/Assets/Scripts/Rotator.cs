using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Audio;
using Game.Player;
using Game.Enemy;
using Game.Combat;
using Game.UI;

namespace Game.Systems
{
    
    public class Rotator : MonoBehaviour
    {
        /// <summary>三轴非均匀旋转，使用 unscaledDeltaTime 确保暂停时仍旋转</summary>
        void Update()
        {
            transform.Rotate (new Vector3 (15, 30, 45) * Time.unscaledDeltaTime);
        }
    }
    
}
