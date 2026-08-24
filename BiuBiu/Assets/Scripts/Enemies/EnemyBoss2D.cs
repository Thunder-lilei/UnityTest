using BiuBiu.Core;
using BiuBiu.Player;
using UnityEngine;

namespace BiuBiu.Enemies
{
    /// <summary>
    /// Boss（数值文档 5.3；行为差异大，继承 EnemyBase2D 复用受击/死亡/血条链路）。
    /// - 血量 24 ×n（第 n 只，一只比一只强；Spawner 传入 bossIndex；第 5 轮=24，第 10 轮=48）；
    /// - 体型 4×4（近战 4 倍）；移速 1.6 慢速逼近；接触伤害 1 心，0.5s 结算一次；
    /// - 技能循环（差异化：近身范围型，与精英远程封锁型区分）：
    ///   八方向扇形横扫（朝 8 个米字方向各一次扇形判定，半径 4.0、角度 120°，总面积 = 普通近战 4 倍）
    ///   → 等待 3s → 直线冲撞（朝玩家方向 8 tile/s 冲 6 tile，路径接触 1 伤 + 击退玩家 1.5 tile）→ 冷却 6s → 循环；
    /// - 横扫为瞬间判定（非弹丸），前摇 0.6s 渐红蓄力；冲撞为直线冲刺。
    /// 登场演出：强震屏（GameBalance.TraumaBossSpawn）。
    /// </summary>
    public class EnemyBoss2D : EnemyBase2D
    {
        /// <summary>Boss 行为状态（覆盖基类普通敌人状态机）</summary>
        private enum BossState
        {
            SweepWindup,     // 八方向扇形横扫前摇（渐红蓄力 0.6s）
            WaitAfterSweep,  // 横扫后等待 3s
            ChargeWindup,    // 直线冲撞前摇 0.6s
            Charging,        // 直线冲撞中
            WaitAfterCharge  // 冲撞后冷却 6s
        }

        /// <summary>当前 Boss 行为状态</summary>
        private BossState bossState;

        /// <summary>状态计时（前摇/等待/冲撞共用）</summary>
        private float bossTimer;

        /// <summary>直线冲撞方向（前摇结束时锁定）</summary>
        private Vector2 chargeDir;

        /// <summary>直线冲撞剩余距离（tile）</summary>
        private float chargeRemaining;

        /// <summary>本次冲撞是否已命中（一次冲撞只伤一次）</summary>
        private bool chargeHitPlayer;

        /// <summary>接触伤害结算计时（0.5s 一跳）</summary>
        private float contactTimer;

        /// <summary>
        /// Boss 专属初始化（血量 = baseHealth × bossIndex，数值文档 5.3「24 ×n」）。
        /// </summary>
        /// <param name="bossData">Boss 配置（Enemy_Boss）</param>
        /// <param name="bossIndex">第几只（1 起：第 5 轮=1，第 10 轮=2）</param>
        public void InitializeBoss(Data.EnemyData bossData, int bossIndex)
        {
            Initialize(bossData, 0); // 基类通用初始化（难度成长不用，Boss 血量独立公式）
            SetHealthInternal(bossData.baseHealth * bossIndex); // 血量 24 ×n
            bossState = BossState.SweepWindup;
            bossTimer = Data.sweepWindup;
            chargeRemaining = 0f;
            chargeHitPlayer = false;
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

            // 接触伤害由基类 ContactDamageTick 处理（Data.contactTickInterval=0.5）

            bossTimer -= Time.deltaTime;

            switch (bossState)
            {
                case BossState.SweepWindup:
                    // 前摇渐红蓄力
                    float progress = 1f - Mathf.Clamp01(bossTimer / Data.sweepWindup);
                    var sr = GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = Color.Lerp(MainColor, Color.red, progress);
                    if (bossTimer <= 0f)
                    {
                        if (sr != null) sr.color = MainColor;
                        FireEightSweep(player);
                        bossState = BossState.WaitAfterSweep;
                        bossTimer = Data.sweepInterval;
                    }
                    break;

                case BossState.WaitAfterSweep:
                    // 慢速逼近（技能间隙保持移动压迫走位）
                    Rb.velocity = dist > 1f ? toPlayer.normalized * Data.moveSpeed : Vector2.zero;
                    if (bossTimer <= 0f)
                    {
                        bossState = BossState.ChargeWindup;
                        bossTimer = Data.bossChargeWindup;
                    }
                    break;

                case BossState.ChargeWindup:
                    // 前摇渐红蓄力 + 锁定方向
                    progress = 1f - Mathf.Clamp01(bossTimer / Data.bossChargeWindup);
                    var sr2 = GetComponent<SpriteRenderer>();
                    if (sr2 != null) sr2.color = Color.Lerp(MainColor, Color.red, progress);
                    if (bossTimer <= 0f)
                    {
                        chargeDir = toPlayer.normalized;
                        chargeRemaining = Data.bossChargeDistance;
                        chargeHitPlayer = false;
                        if (sr2 != null) sr2.color = MainColor;
                        bossState = BossState.Charging;
                    }
                    break;

                case BossState.Charging:
                    Rb.velocity = chargeDir * Data.bossChargeSpeed;
                    chargeRemaining -= Data.bossChargeSpeed * Time.deltaTime;
                    if (!chargeHitPlayer)
                    {
                        float hitDist = Data.bodySize.x * 0.5f + GameBalance.PlayerCollisionRadius;
                        if (toPlayer.sqrMagnitude <= hitDist * hitDist)
                        {
                            chargeHitPlayer = true;
                            player.TakeDamage(Data.damage);
                            var knockable = player.GetComponent<IKnockbackable>();
                            if (knockable != null) knockable.Knockback(chargeDir, Data.bossChargeKnockback);
                        }
                    }
                    if (chargeRemaining <= 0f)
                    {
                        Rb.velocity = Vector2.zero;
                        bossState = BossState.WaitAfterCharge;
                        bossTimer = Data.bossChargeCooldown;
                    }
                    break;

                case BossState.WaitAfterCharge:
                    Rb.velocity = dist > 1f ? toPlayer.normalized * Data.moveSpeed : Vector2.zero;
                    if (bossTimer <= 0f)
                    {
                        bossState = BossState.SweepWindup;
                        bossTimer = Data.sweepWindup;
                    }
                    break;
            }
        }

        /// <summary>
        /// 八方向扇形横扫：朝 8 个米字方向（每 45°）各做一次扇形判定，
        /// 半径 = GameBalance.BossSweepRadius(4.0)、角度 = GameBalance.BossSweepAngle(120°)，
        /// 每个扇形面积 = 普通近战(120°·r2.0) 的 4 倍；玩家落入任一扇形则受 1 伤。
        /// </summary>
        private void FireEightSweep(PlayerController player)
        {
            Vector2 origin = transform.position;
            Vector2 toPlayer = (Vector2)player.transform.position - origin;
            float distToPlayer = toPlayer.magnitude;

            bool hit = false;
            for (int i = 0; i < GameBalance.BossSweepDirections; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float hitRange = GameBalance.BossSweepRadius + GameBalance.PlayerCollisionRadius;
                float half = GameBalance.BossSweepAngle * 0.5f;
                if (distToPlayer <= hitRange && Vector2.Angle(dir, toPlayer) <= half)
                {
                    hit = true;
                    break;
                }
            }
            if (hit) player.TakeDamage(Data.damage);
        }
    }
}
