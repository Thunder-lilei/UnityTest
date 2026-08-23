using BiuBiu.Core;
using BiuBiu.Enemies;
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
                    template.SetActive(false);
                    Object.DontDestroyOnLoad(template);
                }
                return template;
            }
        }

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
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
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            Vector2 pos = transform.position;
            Vector2 newPos = pos + velocity * dt;

            // 碰墙反弹
            var hit = Physics2D.Raycast(pos, velocity.normalized, velocity.magnitude * dt + 0.05f);
            if (hit.collider != null && hit.collider is BoxCollider2D)
            {
                if (bounceCount >= maxBounces)
                {
                    ObjectPool.Release(gameObject);
                    return;
                }
                velocity = Vector2.Reflect(velocity, hit.normal).normalized * velocity.magnitude;
                bounceCount++;
                newPos = hit.point + velocity.normalized * 0.05f;
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
    }
}
