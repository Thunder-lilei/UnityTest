using BiuBiu.Player;
using UnityEngine;

namespace BiuBiu.Core
{
    /// <summary>
    /// 相机平滑跟随（M0-7 灰盒用；M1-9 补地图边界锁定）。
    /// LateUpdate 平滑阻尼跟随目标；z 保持初始深度（2D 标准 -10）。
    /// 镜头边界锁定（设计文档 12 章已定）：相机视口四边 clamp 在 80×80 地图内不越界。
    /// 与 CameraTrauma 正交共存：震屏在 OnPreRender（渲染前）叠加偏移并自行撤销，
    /// 本脚本用独立 logicPos 缓存（不读 transform——避免读到含震屏偏移的显示位置污染跟随速度）。
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Tooltip("跟随目标（玩家；运行时装配晚于相机时自动查找）")]
        [SerializeField] private Transform target;

        [Tooltip("跟随平滑时间（秒）——越小跟得越紧")]
        [SerializeField] private float smoothTime = 0.08f;

        private Vector3 logicPos;   // 相机逻辑位置缓存（不含震屏偏移）
        private Vector3 velocity;   // SmoothDamp 内部速度缓存
        private Vector3 lookAheadVel; // 前瞻偏移 SmoothDamp 速度
        private Vector2 playerLastPos; // 玩家上一帧位置（算速度方向用）
        private Camera ownCamera;   // 本机 Camera（视口半宽计算用；正交）

        private void Awake()
        {
            logicPos = transform.position; // 初始即逻辑位置
            ownCamera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            // 目标自愈：场景装配（RuntimeSceneBuilder 构建玩家）晚于相机激活时自动补引用
            if (target == null)
            {
                var player = FindFirstObjectByType<PlayerController>();
                if (player == null) return;
                target = player.transform;
                playerLastPos = target.position;
            }

            // ---- 前瞻偏移：朝玩家移动方向前探（数值文档 9.2 章 CameraLookAheadDistance） ----
            Vector2 playerDelta = (Vector2)target.position - playerLastPos;
            Vector2 moveDir = playerDelta.magnitude > 0.001f ? playerDelta.normalized : Vector2.zero;
            playerLastPos = target.position;
            Vector3 lookAhead = Vector3.SmoothDamp(Vector3.zero,
                (Vector3)(moveDir * GameBalance.CameraLookAheadDistance),
                ref lookAheadVel, GameBalance.CameraLookAheadSmoothTime);

            // 目标位：xy 跟随 + 前瞻偏移，z 保持初始深度
            Vector3 desired = new Vector3(target.position.x + lookAhead.x,
                                          target.position.y + lookAhead.y,
                                          logicPos.z);
            logicPos = Vector3.SmoothDamp(logicPos, desired, ref velocity, smoothTime);

            // ---- 地图四边 clamp（设计文档 12 章镜头边界锁定）：视口不出 80×80 地图 ----
            if (ownCamera != null && ownCamera.orthographic)
            {
                float halfH = ownCamera.orthographicSize;
                float halfW = halfH * ownCamera.aspect; // 半宽随宽高比动态算
                float s = GameBalance.MapSizeTiles;
                logicPos.x = Mathf.Clamp(logicPos.x, halfW, s - halfW);
                logicPos.y = Mathf.Clamp(logicPos.y, halfH, s - halfH);
            }

            transform.position = logicPos; // 全量写入逻辑位置（震屏偏移由 CameraTrauma 在渲染前另行叠加）
        }
    }
}
