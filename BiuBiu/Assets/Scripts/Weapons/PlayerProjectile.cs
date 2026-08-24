using BiuBiu.Core;
using BiuBiu.Enemies;
using BiuBiu.VFX;
using UnityEngine;

namespace BiuBiu.Weapons
{
    /// <summary>
    /// 主角弹弓弹丸（三档蓄力）。
    /// 零级（白）：速射兜底，伤害 1，无击飞，命中消散。
    /// 一级（黄）：伤害+击飞，命中敌人后消散。
    /// 二级（红）：击碎敌人，弹丸穿透不消失，直到撞墙反弹次数用尽。
    /// </summary>
    public class PlayerProjectile : MonoBehaviour
    {
        private Vector2 velocity;
        private int maxBounces;
        private int bounceCount;
        private int chargeLevel;
        private int damage;
        private float knockbackForce;
        private bool shatter;
        private float lifeTimer;
        private SpriteRenderer sr;
        private TrailRenderer trail;

        // 弹丸档位颜色（与数值文档 4.1 三档对应；零级为不透明白，区别于蓄力 orb 的半透白）
        private static readonly Color[] levelColors = {
            new Color(1f, 1f, 1f),        // 零级（白）
            new Color(1f, 0.9f, 0.2f),    // 一级（黄）
            new Color(1f, 0.3f, 0.1f)     // 二级（红）
        };

        private static GameObject template;

        public static GameObject Template
        {
            get
            {
                if (template == null)
                {
                    template = new GameObject("PlayerProjectileTemplate");
                    template.AddComponent<SpriteRenderer>();
                    template.AddComponent<PlayerProjectile>();

                    // 拖尾子物体（AGENTS 已知坑：同物体禁多 LineRenderer/TrailRenderer，用子物体承载）
                    var trailGo = new GameObject("Trail");
                    trailGo.transform.SetParent(template.transform, false);
                    var tr = trailGo.AddComponent<TrailRenderer>();
                    // 内置 Sprites/Default 材质（透明度混合，2D 像素风可用）；无材质则 TrailRenderer 不渲染
                    tr.material = new Material(Shader.Find("Sprites/Default"));
                    tr.sortingLayerName = "Effects"; // 与破碎碎片同层（工程约定特效层），避免落在 Default 层不显示
                    tr.sortingOrder = 18;            // 略低于弹丸本体(20)，拖尾在弹丸下方
                    tr.enabled = false;          // 默认禁用，Launch 时启用并配置
                    template.SetActive(false);
                    Object.DontDestroyOnLoad(template);
                }
                return template;
            }
        }

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            trail = GetComponentInChildren<TrailRenderer>(true);
        }

        private void OnDisable()
        {
            // 回池（SetActive(false)）时停用拖尾：停止记录+渲染，下次 Launch 再开
            if (trail != null)
            {
                trail.emitting = false;
                trail.enabled = false;
            }
        }

        public void Launch(Vector2 dir, int level, int dmg, float knockback, bool shatterEnemy)
        {
            chargeLevel = level;
            damage = dmg;
            knockbackForce = knockback;
            shatter = shatterEnemy;
            maxBounces = GameBalance.ProjectileMaxBounces[level];
            bounceCount = 0;
            lifeTimer = 0f;
            velocity = dir.normalized * GameBalance.ProjectileSpeeds[level];

            if (sr != null)
            {
                sr.sprite = GreyBoxFactory.Circle;
                sr.sortingOrder = 20;
                sr.color = levelColors[level];
            }
            transform.localScale = Vector3.one * GameBalance.ProjectileRadius * 2f;

            // ---- 弹丸拖尾（三档差异：长度/粗细/余晖，颜色复用档位色） ----
            if (trail != null)
            {
                trail.enabled = true;        // 关键：模板默认禁用，必须启用组件才会记录+渲染
                trail.sortingLayerName = "Effects";
                trail.sortingOrder = 18;
                trail.Clear(); // 池复用：清掉上一发残留轨迹
                Color c = levelColors[level];
                float endA = GameBalance.ProjectileTrailEndAlpha[level];
                trail.startColor = new Color(c.r, c.g, c.b, 1f);
                trail.endColor = new Color(c.r, c.g, c.b, endA);
                trail.time = GameBalance.ProjectileTrailTime[level];   // 尾巴长度
                trail.widthMultiplier = GameBalance.ProjectileTrailWidth[level]; // 粗细（匹配弹丸直径）
                trail.minVertexDistance = 0.03f; // 像素风：低采样保持利落
                trail.emitting = true;
            }
        }

        private void Update()
        {
            if (trail != null && !trail.emitting) return; // 已回池/未发射：跳过运动（防御性）
            float dt = Time.deltaTime;

            Vector2 pos = transform.position;
            Vector2 newPos = pos + velocity * dt;

            // 碰墙处理
            var hit = Physics2D.Raycast(pos, velocity.normalized, velocity.magnitude * dt + 0.05f);
            if (hit.collider != null && hit.collider is BoxCollider2D)
            {
                var destructible = hit.collider.GetComponent<DestructibleObstacle>();

                if (destructible != null)
                {
                    // 命中内部可破坏障碍
                    if (chargeLevel >= 2)
                    {
                        // 满蓄力：击碎障碍（不反弹）
                        destructible.Break();
                        ObjectPool.Release(gameObject);
                        return;
                    }
                    else
                    {
                        // 非满蓄力：撞墙迸发同色像素碎片 + 轻脆音，弹丸消失
                        SpawnWallSpark(hit.point);
                        ObjectPool.Release(gameObject);
                        return;
                    }
                }
                else
                {
                    // 边界墙（不可破坏）：满蓄力反弹，非满蓄力迸火花销毁
                    if (chargeLevel >= 2 && bounceCount < maxBounces)
                    {
                        velocity = Vector2.Reflect(velocity, hit.normal).normalized * velocity.magnitude;
                        bounceCount++;
                        newPos = hit.point + velocity.normalized * 0.05f;
                    }
                    else
                    {
                        SpawnWallSpark(hit.point);
                        ObjectPool.Release(gameObject);
                        return;
                    }
                }
            }

            transform.position = newPos;

            lifeTimer += dt;
            if (lifeTimer >= GameBalance.ProjectileLifetime)
            {
                ObjectPool.Release(gameObject);
                return;
            }

            // 命中敌人
            HitEnemies(newPos);
        }

        private void HitEnemies(Vector2 pos)
        {
            // 命中检测半径=弹丸实际半径（严格匹配视觉球体，不加额外缓冲）
            float checkRadius = GameBalance.ProjectileRadius;
            foreach (var col in Physics2D.OverlapCircleAll(pos, checkRadius))
            {
                var enemy = col.GetComponentInParent<EnemyBase2D>();
                if (enemy == null) continue;

                if (shatter)
                {
                    // 二级：击碎敌人，弹丸穿透不消失
                    // 破碎粒子爆发：先于 Shatter 取敌人主色与命中点，避免回池后引用失效
                    BreakBurstManager.SpawnBreakBurst(pos, velocity, enemy.MainColor);
                    enemy.Shatter();
                    if (CameraTrauma.Instance != null)
                        CameraTrauma.Instance.AddTrauma(GameBalance.TraumaHitEnemy * 2f);
                }
                else
                {
                    // 一级/零级：伤害+位移反馈，弹丸消散
                    enemy.TakeDamage(damage);
                    // 位移反馈：黄色档=击飞（触发连锁）；白色档=微小后仰（不连锁）
                    float speed = knockbackForce > 0f ? GameBalance.KnockbackYellowSpeed : GameBalance.HitRecoilSpeed;
                    enemy.Knockback(velocity.normalized, speed, triggerChain: knockbackForce > 0f);
                    ObjectPool.Release(gameObject);
                    return;
                }
            }
        }

        /// <summary>撞墙（非满蓄力 / 边界墙）迸发同色像素碎片 + 轻脆音</summary>
        private void SpawnWallSpark(Vector2 point)
        {
            // 小型白/灰碎片爆发（复用击碎特效，取墙主色灰白）
            BreakBurstManager.SpawnBreakBurst(point, velocity, Color.gray);
            // 轻脆撞墙音（音频资产缺失时静默）
            AudioManager.Play("wall_hit");
            // 极轻震屏
            if (CameraTrauma.Instance != null)
                CameraTrauma.Instance.AddTrauma(GameBalance.TraumaHitWall);
        }
    }
}
