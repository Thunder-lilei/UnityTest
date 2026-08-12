using UnityEngine;

namespace Wuxia.Player
{
    /// <summary>
    /// 玩家角色控制器：基于 Rigidbody 的俯视角移动。
    /// 输入由外部（如 InputReader）通过公共方法传入，与输入系统解耦。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
        [Header("移动设置")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float runSpeed = 6f;
        [SerializeField] private float rotationSpeed = 10f;

        [Header("跳跃设置")]
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float groundCheckDistance = 0.15f;
        [SerializeField] private LayerMask groundLayer = ~0;

        private Rigidbody _rb;
        private Animator _animator;

        private Vector2 _moveInput;
        private bool _isSprinting;
        private bool _isGrounded;

        // Animator 参数 ID（用 StringToHash 避免每帧字符串查找）
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int JumpTriggerHash = Animator.StringToHash("JumpTrigger");

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _animator = GetComponent<Animator>();

            // Rigidbody 配置：冻结旋转，防止角色被物理力翻倒
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        private void FixedUpdate()
        {
            CheckGround();
            HandleMovement();
            UpdateAnimator();
        }

        #region 公共输入接口（由 InputReader 调用）

        /// <summary>
        /// 设置移动输入。x = 左右，y = 前后。
        /// </summary>
        public void SetMoveInput(Vector2 input)
        {
            _moveInput = input;
        }

        /// <summary>
        /// 设置是否冲刺（跑）。
        /// </summary>
        public void SetSprint(bool sprint)
        {
            _isSprinting = sprint;
        }

        /// <summary>
        /// 触发跳跃。仅在地面时可跳。
        /// </summary>
        public void Jump()
        {
            if (!_isGrounded) return;

            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            _animator.SetTrigger(JumpTriggerHash);
        }

        #endregion

        private void CheckGround()
        {
            // 从角色脚底向下射线检测地面
            var origin = transform.position + Vector3.up * 0.1f;
            _isGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
        }

        private void HandleMovement()
        {
            // 俯视角：输入直接映射到世界坐标（x = 右，y = 前）
            var moveDir = new Vector3(_moveInput.x, 0f, _moveInput.y);
            var speed = _isSprinting ? runSpeed : walkSpeed;

            // 保持 Y 轴速度（重力/跳跃不受影响）
            var velocity = new Vector3(moveDir.x * speed, _rb.velocity.y, moveDir.z * speed);
            _rb.velocity = velocity;

            // 角色朝向移动方向旋转
            if (moveDir.sqrMagnitude > 0.01f)
            {
                var targetRotation = Quaternion.LookRotation(moveDir);
                _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }

        private void UpdateAnimator()
        {
            // Speed: 0 = Idle, 0.5 = Walk, 1 = Run
            var speedParam = _moveInput.magnitude * (_isSprinting ? 1f : 0.5f);
            _animator.SetFloat(SpeedHash, speedParam, 0.1f, Time.fixedDeltaTime);
            _animator.SetBool(IsGroundedHash, _isGrounded);
        }
    }
}
