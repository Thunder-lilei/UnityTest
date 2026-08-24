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

        /// <summary>八方向扇形横扫预警：8 个扇区填充（红，半透明）</summary>
        private SpriteRenderer[] sweepFill;

        /// <summary>八方向扇形横扫预警：扇区边框（红色 LineRenderer，loop）</summary>
        private LineRenderer sweepOutline;

        /// <summary>直线冲撞预警：矩形填充（红，半透明）</summary>
        private SpriteRenderer chargeFill;

        /// <summary>直线冲撞预警：矩形边框（红色 LineRenderer，loop）</summary>
        private LineRenderer chargeOutline;

        /// <summary>创建一条预警 LineRenderer（世界空间、loop 可配置、置于角色上层）</summary>
        private static LineRenderer CreateWarningLine(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(null, false); // 世界空间，不随 Boss 移动（每帧重绘）
            var lr = go.AddComponent<LineRenderer>();
            lr.startWidth = 0.06f;
            lr.endWidth = 0.06f;
            lr.sortingOrder = 18;
            lr.useWorldSpace = true;
            lr.loop = loop;
            var shader = Shader.Find("Sprites/Default");
            if (shader != null) lr.sharedMaterial = new Material(shader);
            lr.enabled = false;
            return lr;
        }

        /// <summary>创建一个预警填充 SpriteRenderer（世界空间、置于角色下层、半透明由 color 控制）</summary>
        private static SpriteRenderer CreateFillSprite(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(null, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GreyBoxFactory.Square; // 白色方块，靠 scale/rotation 塑形，靠 color 染色
            sr.sortingOrder = 17;
            sr.enabled = false;
            return sr;
        }

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

            // 预警渲染器（Boss 攻击范围可视化，让玩家预判闪避）
            sweepOutline = CreateWarningLine("BossSweepOutline", true);
            chargeOutline = CreateWarningLine("BossChargeOutline", true);
            chargeFill = CreateFillSprite("BossChargeFill");

            // 八方向扇形填充：每个方向一个 SpriteRenderer（复用基类扇形纹理，pivot=左中，从 Boss 中心延展）
            var sectorTex = EnemyBase2D.BuildProgressiveArcTexture(GameBalance.BossSweepAngle, 1f);
            var sectorSprite = Sprite.Create(sectorTex, new Rect(0, 0, sectorTex.width, sectorTex.height), new Vector2(0, 0.5f), sectorTex.width);
            int dirs = GameBalance.BossSweepDirections;
            sweepFill = new SpriteRenderer[dirs];
            for (int d = 0; d < dirs; d++)
            {
                var sr = CreateFillSprite("BossSweepFill" + d);
                sr.sprite = sectorSprite;
                sr.transform.rotation = Quaternion.Euler(0, 0, d * 45f); // 米字八方向
                sr.transform.position = transform.position;
                sr.transform.localScale = new Vector3(GameBalance.BossSweepRadius, GameBalance.BossSweepRadius, 1f);
                sweepFill[d] = sr;
            }

            // 登场演出：强震屏（数值文档 9 章 TraumaBossSpawn 0.8）
            if (CameraTrauma.Instance != null)
                CameraTrauma.Instance.AddTrauma(GameBalance.TraumaBossSpawn);
        }

        /// <summary>Boss 行为主体（覆盖基类普通敌人状态机）</summary>
        protected override void Think(PlayerController player)
        {
            Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
            float dist = toPlayer.magnitude;
            float progress = 0f; // 前摇进度（SweepWindup/ChargeWindup 复用）

            // 接触伤害由基类 ContactDamageTick 处理（Data.contactTickInterval=0.5）

            bossTimer -= Time.deltaTime;

            switch (bossState)
            {
                case BossState.SweepWindup:
                    // 前摇渐红蓄力 + 八方向扇形预警
                    progress = 1f - Mathf.Clamp01(bossTimer / Data.sweepWindup);
                    var sr = GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = Color.Lerp(MainColor, Color.red, progress);
                    UpdateSweepWarning(progress);
                    if (bossTimer <= 0f)
                    {
                        if (sr != null) sr.color = MainColor;
                        HideWarnings();
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
                    // 前摇渐红蓄力 + 锁定方向 + 冲撞直线预警
                    progress = 1f - Mathf.Clamp01(bossTimer / Data.bossChargeWindup);
                    var sr2 = GetComponent<SpriteRenderer>();
                    if (sr2 != null) sr2.color = Color.Lerp(MainColor, Color.red, progress);
                    UpdateChargeWarning(progress, toPlayer.normalized);
                    if (bossTimer <= 0f)
                    {
                        chargeDir = toPlayer.normalized;
                        chargeRemaining = Data.bossChargeDistance;
                        chargeHitPlayer = false;
                        if (sr2 != null) sr2.color = MainColor;
                        HideWarnings();
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

        /// <summary>Boss 失活（死亡/回池）时清除所有预警渲染器，避免残留屏幕</summary>
        private void OnDisable()
        {
            HideWarnings();
        }

        /// <summary>
        /// 八方向扇形横扫预警：8 个红色半透明扇形填充 + 红色边框轮廓（半径 = BossSweepRadius、角度 = BossSweepAngle），
        /// alpha 随前摇进度加深，让玩家看清横扫覆盖范围并预判走位。
        /// </summary>
        private void UpdateSweepWarning(float progress)
        {
            if (sweepFill == null) return;
            Vector2 origin = transform.position;
            float r = GameBalance.BossSweepRadius;
            float half = GameBalance.BossSweepAngle * 0.5f;
            float fillAlpha = 0.18f + 0.27f * progress; // 0.18→0.45 填充
            float lineAlpha = 0.4f + 0.5f * progress;    // 0.4→0.9 边框
            var fillCol = new Color(1f, 0.1f, 0.1f, fillAlpha);
            var lineCol = new Color(1f, 0.15f, 0.15f, lineAlpha);

            // 扇区填充：旋转/缩放已在 InitializeBoss 设好，这里只更新位置与颜色
            for (int d = 0; d < sweepFill.Length; d++)
            {
                sweepFill[d].transform.position = origin;
                sweepFill[d].color = fillCol;
                sweepFill[d].enabled = true;
            }

            // 扇区边框：8 个扇形闭合轮廓（中心 → 弧 → 中心）
            var pts = new System.Collections.Generic.List<Vector3>();
            int dirs = GameBalance.BossSweepDirections;
            int arcSeg = 6;
            for (int d = 0; d < dirs; d++)
            {
                float centerAng = d * 45f * Mathf.Deg2Rad;
                float a0 = centerAng - half * Mathf.Deg2Rad;
                float a1 = centerAng + half * Mathf.Deg2Rad;
                pts.Add(origin);
                for (int s = 0; s <= arcSeg; s++)
                {
                    float a = Mathf.Lerp(a0, a1, (float)s / arcSeg);
                    pts.Add(origin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                }
                pts.Add(origin);
            }
            sweepOutline.enabled = true;
            sweepOutline.positionCount = pts.Count;
            sweepOutline.SetPositions(pts.ToArray());
            sweepOutline.startColor = lineCol;
            sweepOutline.endColor = lineCol;
        }

        /// <summary>
        /// 直线冲撞预警：沿玩家方向画一个红色半透明矩形（长 = bossChargeDistance、宽 = Boss 体型），
        /// 外加红色矩形边框，让玩家看清冲撞轨迹并提前闪避。
        /// </summary>
        private void UpdateChargeWarning(float progress, Vector2 dir)
        {
            if (chargeFill == null) return;
            Vector2 origin = transform.position;
            float len = Data.bossChargeDistance;
            float width = Data.bodySize.x; // Boss 体型宽
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float fillAlpha = 0.2f + 0.3f * progress; // 0.2→0.5
            float lineAlpha = 0.45f + 0.5f * progress; // 0.45→0.95

            // 矩形填充：以 Boss 为中心向 dir 延伸的方块（GreyBoxFactory.Square pivot=中心）
            chargeFill.transform.position = origin + dir * (len * 0.5f);
            chargeFill.transform.rotation = Quaternion.Euler(0, 0, ang);
            chargeFill.transform.localScale = new Vector3(len, width, 1f);
            chargeFill.color = new Color(1f, 0.1f, 0.1f, fillAlpha);
            chargeFill.enabled = true;

            // 矩形边框：四角世界坐标（loop）
            Vector2 perp = new Vector2(-dir.y, dir.x) * (width * 0.5f);
            Vector2 c0 = origin + perp;                 // 近左
            Vector2 c1 = origin - perp;                 // 近右
            Vector2 c2 = origin + dir * len - perp;     // 远右
            Vector2 c3 = origin + dir * len + perp;     // 远左
            chargeOutline.enabled = true;
            chargeOutline.positionCount = 5;
            chargeOutline.SetPosition(0, c0);
            chargeOutline.SetPosition(1, c1);
            chargeOutline.SetPosition(2, c2);
            chargeOutline.SetPosition(3, c3);
            chargeOutline.SetPosition(4, c0);
            var lineCol = new Color(1f, 0.15f, 0.15f, lineAlpha);
            chargeOutline.startColor = lineCol;
            chargeOutline.endColor = lineCol;
        }

        /// <summary>清除所有预警渲染器</summary>
        private void HideWarnings()
        {
            if (sweepFill != null) foreach (var s in sweepFill) if (s != null) s.enabled = false;
            if (sweepOutline != null) sweepOutline.enabled = false;
            if (chargeFill != null) chargeFill.enabled = false;
            if (chargeOutline != null) chargeOutline.enabled = false;
        }
    }
}
