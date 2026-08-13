using UnityEngine;
using UnityEngine.UI;
using Game.Audio;
using Game.Enemy;
using Game.Combat;
using Game.UI;
using Game.Systems;

namespace Game.Player
{
    
    /// <summary>玩家移动：恒定速度移动、朝向旋转、闪避冲刺、脚印生成</summary>
    public class PlayerMovement : MonoBehaviour
    {
        [Header("移动")]
        public float speed = 10f;
    
        [Header("闪避")]
        public float dashSpeed = 30f;
        public float dashDuration = 0.2f;
        public float dashCooldown = 2f;
        public Image dashIcon;
    
        [Header("脚印")]
        public GameObject footprintPrefab;
        public float footprintSpacing = 1f;
        public GameObject foot;
    
        // 事件：闪避状态变化（PlayerHealth 订阅以实现无敌帧）
        public event System.Action<bool> OnDashStateChanged;
    
        private Rigidbody rb;
        private Animator animator;
        private bool isDashing;
        private float dashTimer;
        private float cooldownTimer;
        private Vector3 lastFootprintPos;
        private bool isLeftFoot = true;
        private ObjectPool footprintPool;
        private bool isPaused;
    
        void Start()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponentInChildren<Animator>();
            lastFootprintPos = transform.position;
    
            // 创建脚印对象池
            if (footprintPrefab != null && foot != null)
            {
                footprintPool = CreatePool(footprintPrefab, foot.transform, 20);
                footprintPrefab.GetComponent<Footprint>()?.SetPool(footprintPool);
            }
        }
    
        void FixedUpdate()
        {
            if (isPaused) return;
    
            float movementX = Input.GetAxis("Horizontal");
            float movementY = Input.GetAxis("Vertical");
            Vector3 movement = new Vector3(movementX, 0.0f, movementY);
    
            if (!isDashing)
            {
                Vector3 vel = rb.velocity;
                vel.x = movementX * speed;
                vel.z = movementY * speed;
                rb.velocity = vel;
            }
    
            if (animator != null)
                animator.SetFloat("Speed", rb.velocity.magnitude);
    
            // 朝向移动方向
            if (movement.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movement);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.1f);
            }
    
            // 脚印生成
            if (rb.velocity.magnitude > 0.1f && footprintPool != null)
            {
                if (Vector3.Distance(transform.position, lastFootprintPos) >= footprintSpacing)
                {
                    Vector3 pos = transform.position;
                    pos.y = 0.01f;
                    pos += transform.right * (isLeftFoot ? -0.2f : 0.2f);
                    Quaternion rot = Quaternion.LookRotation(rb.velocity) * Quaternion.Euler(90, 0, 0);
                    footprintPool.Spawn(pos, rot);
                    lastFootprintPos = transform.position;
                    isLeftFoot = !isLeftFoot;
                }
            }
    
            // 闪避计时
            if (isDashing)
            {
                dashTimer -= Time.fixedDeltaTime;
                if (dashTimer <= 0)
                {
                    isDashing = false;
                    cooldownTimer = dashCooldown;
                    OnDashStateChanged?.Invoke(false);
                }
            }
        }
    
        void Update()
        {
            if (isPaused) return;
    
            // 闪避输入
            if (Input.GetKeyDown(KeyCode.Space) && !isDashing && cooldownTimer <= 0)
            {
                Dash();
            }
    
            // 冷却 UI 更新
            if (cooldownTimer > 0)
            {
                cooldownTimer -= Time.deltaTime;
                if (dashIcon != null)
                {
                    dashIcon.fillAmount = 1f - (cooldownTimer / dashCooldown);
                    dashIcon.color = new Color(0.3f, 0.6f, 1f, 1f);
                }
            }
            else if (dashIcon != null)
            {
                dashIcon.fillAmount = 1f;
                dashIcon.color = new Color(0.3f, 0.6f, 1f, 1f);
            }
        }
    
        /// <summary>闪避冲刺：朝移动方向高速位移</summary>
        void Dash()
        {
            isDashing = true;
            dashTimer = dashDuration;
            OnDashStateChanged?.Invoke(true);
    
            float mx = Input.GetAxis("Horizontal");
            float my = Input.GetAxis("Vertical");
            Vector3 dir = new Vector3(mx, 0, my);
            if (dir.magnitude < 0.1f)
                dir = transform.forward;
    
            dir.y = 0;
            dir.Normalize();
            rb.velocity = dir * dashSpeed;
            AudioManager.Instance?.PlayDash();
        }
    
        /// <summary>设置暂停状态（升级选择时调用）</summary>
        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }
    
        ObjectPool CreatePool(GameObject prefab, Transform parent, int size)
        {
            var poolGo = new GameObject(prefab.name + "_Pool");
            poolGo.transform.SetParent(parent, false);
            var pool = poolGo.AddComponent<ObjectPool>();
            pool.prefab = prefab;
            pool.initialSize = size;
            pool.parent = parent;
            return pool;
        }
    }
    
}
