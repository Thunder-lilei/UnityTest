using UnityEngine;

namespace Game.Player
{
    
    /// <summary>摄像机跟随：保持与玩家的初始偏移，LateUpdate 中跟随避免抖动</summary>
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
    
}
