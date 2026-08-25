using BiuBiu.Core;
using BiuBiu.Player;
using UnityEngine;

namespace BiuBiu.Enemies
{
    /// <summary>
    /// 敌方弹丸（数值文档 5.1 远程直线弹 / 5.3 Boss 环形与直线弹）。
    /// 直线飞行 → 命中玩家（距离判定）掉心 / 碰墙（出有效地图区）或超时回收。
    /// 判定用距离而非 Physics2D（性能可控且与玩家无敌帧逻辑解耦）。
    /// 池化：本组件由生成方经 ObjectPool 管理，回收=ObjectPool.Release(gameObject)。
    /// </summary>
    public class Projectile2D : MonoBehaviour
    {
        /// <summary>弹丸存活上限（秒，防泄漏兜底；正常应早就碰墙/命中回收）</summary>
        private const float MaxLifetime = 10f;

        /// <summary>飞行速度向量（tile/s）</summary>
        private Vector2 velocity;

        /// <summary>碰撞判定半径（tile，含玩家半径合并到判定距离）</summary>
        private float radius;

        /// <summary>存活计时</summary>
        private float lifeTimer;

        /// <summary>是否已发射（蓄力期间 false=不移动不碰撞）</summary>
        private bool launched;

        /// <summary>渲染组件（灰盒着色）</summary>
        private SpriteRenderer sr;

        /// <summary>是否已被打飞（被棍子击中后变友军弹，不再伤玩家）</summary>
        private bool knocked;

        /// <summary>灰盒弹丸模板（ObjectPool 分池键；素材版由调用方传 prefab 替代）</summary>
        private static GameObject greyTemplate;

        /// <summary>获取灰盒弹丸模板（惰性创建：空物体+本组件，视觉在 Launch 时补）</summary>
        public static GameObject GreyTemplate
        {
            get
            {
                if (greyTemplate == null)
                {
                    greyTemplate = new GameObject("ProjectileGreyTemplate");
                    greyTemplate.AddComponent<SpriteRenderer>(); // 弹丸视觉组件（Launch 时补 sprite+颜色）
                    greyTemplate.AddComponent<Projectile2D>();
                    greyTemplate.SetActive(false);
                    // 模板留在活动场景（不 DDOL）：clone 随 LoadScene 卸载自动清理，杜绝跨局残留（static 引用自愈重建）
                }
                return greyTemplate;
            }
        }

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// 发射初始化（ObjectPool.Get 后调用）。
        /// </summary>
        /// <param name="direction">飞行方向（单位向量）</param>
        /// <param name="speed">弹速（tile/s）</param>
        /// <param name="radius">弹碰撞半径（tile）</param>
        /// <param name="color">灰盒颜色（素材版由 prefab 自带，忽略本参）</param>
        public void Launch(Vector2 direction, float speed, float radius, Color color)
        {
            velocity = direction.normalized * speed;
            this.radius = radius;
            lifeTimer = 0f;
            launched = speed > 0f; // 蓄力阶段 speed=0 → 未发射，不碰撞不移动
            knocked = false;
            if (sr != null && sr.sprite == null)
            {
                // 灰盒路径：无 prefab 时生成方挂本组件，此处补占位视觉
                sr.sprite = GreyBoxFactory.Circle;
                sr.color = color;
                sr.sortingOrder = 15;
            }
            if (sr != null && sr.sprite == GreyBoxFactory.Circle)
            {
                sr.color = color; // 每次发射刷新颜色（池复用）
            }
            transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f); // 直径=2×半径
        }

        /// <summary>被棍子打飞：改变飞行方向+标记为友军弹（不再伤玩家，可伤敌人）</summary>
        public void Deflect(Vector2 direction, float speed)
        {
            velocity = direction.normalized * speed;
            launched = true;
            knocked = true;
            if (sr != null) sr.color = new Color(0.4f, 0.8f, 1f); // 打飞后变蓝色（友军弹标识）
        }

        private void Update()
        {
            // 蓄力阶段（未发射）：不飞行、不碰撞、不计时
            if (!launched) return;

            // ---- 飞行 ----
            float dt = Time.deltaTime;
            transform.position += (Vector3)(velocity * dt);

            // ---- 超时回收 ----
            lifeTimer += dt;
            if (lifeTimer >= MaxLifetime)
            {
                Core.ObjectPool.Release(gameObject);
                return;
            }

            // ---- 碰墙回收：超出有效地图区 或 射线命中 BoxCollider2D ----
            Vector3 p = transform.position;
            float min = Core.GameBalance.BorderWallThickness;
            float max = Core.GameBalance.MapSizeTiles - Core.GameBalance.BorderWallThickness;
            if (p.x < min || p.x > max || p.y < min || p.y > max)
            {
                Core.ObjectPool.Release(gameObject);
                return;
            }
            // 射线检测障碍墙/边界墙（防止穿透）
            var wallHit = Physics2D.Raycast(p, velocity.normalized, 0.3f);
            if (wallHit.collider != null && wallHit.collider is BoxCollider2D)
            {
                Core.ObjectPool.Release(gameObject);
                return;
            }

            // ---- 命中判定 ----
            if (knocked)
            {
                // 打飞后变友军弹：命中敌人造成伤害
                foreach (var col in Physics2D.OverlapCircleAll(p, radius))
                {
                    var damageable = col.GetComponentInParent<IDamageable>();
                    if (damageable == null) continue;
                    if (damageable is Player.PlayerController) continue; // 不伤玩家
                    damageable.TakeDamage(1);
                    Core.ObjectPool.Release(gameObject);
                    return;
                }
            }
            else
            {
                // 正常敌弹：命中玩家掉心
                var player = Core.GameBootstrap.Instance != null ? Core.GameBootstrap.Instance.GetPlayer() : null;
                if (player != null)
                {
                    float hitDist = radius + Core.GameBalance.PlayerCollisionRadius;
                    if (((Vector2)p - (Vector2)player.transform.position).sqrMagnitude <= hitDist * hitDist)
                    {
                        player.TakeDamage(Core.GameBalance.EnemyDamageToPlayer);
                        Core.ObjectPool.Release(gameObject);
                    }
                }
            }
        }
    }
}
