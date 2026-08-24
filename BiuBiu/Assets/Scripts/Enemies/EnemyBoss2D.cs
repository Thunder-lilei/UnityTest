using BiuBiu.Core;
using BiuBiu.Player;
using UnityEngine;

namespace BiuBiu.Enemies
{
    /// <summary>
    /// Boss（数值文档 5.3；行为差异大，继承 EnemyBase2D 复用受击/死亡/血条链路）。
    /// - 血量 30 ×n（第 n 只，一只比一只强；Spawner 传入 bossIndex）；
    /// - 移速 0.8 慢速逼近；接触伤害 1 心，0.5s 结算一次；
    /// - 技能循环：环形 12 发 → 2s → 直线连射 5 发（间隔 0.25s）→ 2s → 循环；
    /// - 环形弹可穿性（数值文档 5.4）：越贴近越危险——贴身禁区 r≈2，安全输出 r≥4。
    /// 登场演出：强震屏（GameBalance.TraumaBossSpawn）。
    /// </summary>
    public class EnemyBoss2D : EnemyBase2D
    {
        /// <summary>Boss 行为状态（覆盖基类普通敌人状态机）</summary>
        private enum BossState
        {
            RingFire,   // 释放环形弹（单帧完成，转等待）
            WaitAfterRing, // 环形后 2s
            LineFire,   // 直线连射进行中（5 发 × 0.25s）
            WaitAfterLine  // 直线后 2s
        }

        /// <summary>当前 Boss 行为状态</summary>
        private BossState bossState;

        /// <summary>状态计时（等待/连射间隔共用）</summary>
        private float bossTimer;

        /// <summary>直线连射已发射数量</summary>
        private int lineFired;

        /// <summary>接触伤害结算计时（0.5s 一跳）</summary>
        private float contactTimer;

        /// <summary>
        /// Boss 专属初始化（血量 = baseHealth × bossIndex，数值文档 5.3「30 ×n」）。
        /// </summary>
        /// <param name="bossData">Boss 配置（Enemy_Boss）</param>
        /// <param name="bossIndex">第几只（1 起：5:00 首只=1，之后每 300s +1）</param>
        public void InitializeBoss(Data.EnemyData bossData, int bossIndex)
        {
            Initialize(bossData, 0); // 基类通用初始化（难度成长不用，Boss 血量独立公式）
            SetHealthInternal(bossData.baseHealth * bossIndex); // 血量 30 ×n
            bossState = BossState.RingFire;
            bossTimer = 0f;
            lineFired = 0;
            contactTimer = 0f;

            // 登场演出：强震屏（数值文档 9 章 TraumaBossSpawn 0.8）
            if (CameraTrauma.Instance != null)
                CameraTrauma.Instance.AddTrauma(GameBalance.TraumaBossSpawn);
        }

        /// <summary>Boss 行为主体（覆盖基类普通敌人状态机）</summary>
        protected override void Think(PlayerController player)
        {
            Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
            float dist = toPlayer.magnitude;

            // ---- 接触伤害：0.5s 结算一次（数值文档 5.3） ----
            contactTimer -= Time.deltaTime;
            if (contactTimer <= 0f)
            {
                float contactDist = Data.bodySize.x * 0.5f + GameBalance.PlayerCollisionRadius;
                if (dist <= contactDist)
                {
                    player.TakeDamage(GameBalance.EnemyDamageToPlayer);
                    contactTimer = Data.contactTickInterval;
                }
            }

            // ---- 慢速逼近（技能释放中也保持移动，压迫走位空间） ----
            Rb.velocity = dist > 1f ? toPlayer.normalized * Data.moveSpeed : Vector2.zero;

            // ---- 技能循环状态机 ----
            bossTimer -= Time.deltaTime;
            switch (bossState)
            {
                case BossState.RingFire:
                    FireRing();
                    bossState = BossState.WaitAfterRing;
                    bossTimer = 2f;
                    break;

                case BossState.WaitAfterRing:
                    if (bossTimer <= 0f)
                    {
                        bossState = BossState.LineFire;
                        lineFired = 0;
                        bossTimer = 0f; // 立即发第一发
                    }
                    break;

                case BossState.LineFire:
                    if (bossTimer <= 0f)
                    {
                        FireLine(toPlayer.normalized);
                        lineFired++;
                        if (lineFired >= Data.lineBulletCount)
                        {
                            bossState = BossState.WaitAfterLine;
                            bossTimer = 2f;
                        }
                        else
                        {
                            bossTimer = Data.lineFireInterval; // 组内间隔 0.25s
                        }
                    }
                    break;

                case BossState.WaitAfterLine:
                    if (bossTimer <= 0f)
                    {
                        bossState = BossState.RingFire;
                    }
                    break;
            }
        }

        /// <summary>环形能量弹：12 发均匀圆周（弹速 1.5；可穿性见数值文档 5.4）</summary>
        private void FireRing()
        {
            int count = Data.ringBulletCount;
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count; // 均匀圆周
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                FireBossProjectile(dir, Data.ringBulletSpeed);
            }
        }

        /// <summary>直线连射单发：朝玩家当前方向（每发独立追踪，走位可甩开）</summary>
        private void FireLine(Vector2 dir)
        {
            FireBossProjectile(dir, Data.lineBulletSpeed);
        }

        /// <summary>发射一枚 Boss 弹丸（ObjectPool 池化，碰墙销毁）</summary>
        private void FireBossProjectile(Vector2 dir, float speed)
        {
            var go = ObjectPool.Get(Projectile2D.GreyTemplate, transform.position, Quaternion.identity);
            go.GetComponent<Projectile2D>().Launch(dir, speed,
                Data.bulletRadius, new Color(0.9f, 0.3f, 0.9f)); // 品红 Boss 弹
        }
    }
}
