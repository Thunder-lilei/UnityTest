using BiuBiu.Core;
using BiuBiu.Data;
using BiuBiu.UI;
using UnityEngine;

namespace BiuBiu.Weapons
{
    /// <summary>
    /// 弹弓蓄力武器。按住左键蓄力，松开发射弹丸。三档蓄力：
    /// 零级（&lt;0.5s，白色）：速射兜底（伤害1/无击飞/无反弹）
    /// 一级（0.5s，黄色）：伤害+击飞 / 二级（1.0s满，红色）：击碎敌人（无尸体+扇形血迹），弹丸穿透敌人直到撞墙。
    /// 蓄力时弹丸与玩家同步反向拉拽：弹丸从身前拉回身后（弹弓皮筋感），玩家向瞄准反方向缓慢后退（一级起生效）。
    /// </summary>
    public class SlingWeapon : MonoBehaviour
    {
        private Camera mainCam;
        private CameraTrauma trauma;

        // ---- 蓄力状态 ----
        private bool isCharging;
        private float chargeTimer;
        private int chargeLevel; // 0=未蓄力, 1=黄色, 2=红色
        private float overchargeTimer; // 满蓄后继续按住的时间（超时强制脱手）
        private bool overchargeWarned;  // 满蓄超过阈值是否已冒泡提示（只触发一次）

        // ---- 白色速射冷却（防止狂点刷爆 DPS；黄色/红色照常无此限制） ----
        private float lastFireTime = -10f;

        /// <summary>是否正在蓄力（PlayerController 读取以冻结移动）</summary>
        public static bool IsCharging { get; private set; }

        /// <summary>当前蓄力拉拽速度（PlayerController 在蓄力分支统一写入 Rb.velocity，避免两脚本抢写造成漂移空窗）</summary>
        public static Vector2 ChargePullVelocity { get; private set; } = Vector2.zero;

        // ---- 蓄力视觉（弹丸拉拽） ----
        private SpriteRenderer chargeOrb;
        private Vector2 chargeDir;
        private Vector3 playerOriginalPos; // 蓄力前位置（拉拽回弹用）

        // 蓄力等级颜色
        private static readonly Color[] levelColors = {
            new Color(1f, 1f, 1f, 0.5f),   // 未蓄力（白半透）
            new Color(1f, 0.9f, 0.2f),      // 一级（黄）
            new Color(1f, 0.3f, 0.1f)       // 二级满（红）
        };

        private void Awake()
        {
            mainCam = Camera.main;
            trauma = mainCam != null ? mainCam.GetComponent<CameraTrauma>() : null;

            for (int i = transform.childCount - 1; i >= 0; i--)
                if (transform.GetChild(i).name == "ChargeOrb")
                    DestroyImmediate(transform.GetChild(i).gameObject);

            var go = new GameObject("ChargeOrb");
            go.transform.SetParent(transform, false);
            chargeOrb = go.AddComponent<SpriteRenderer>();
            chargeOrb.sprite = GreyBoxFactory.Circle;
            chargeOrb.sortingOrder = 20;
            chargeOrb.enabled = false;
        }

        private void Update()
        {
            if (GameState.InputLocked)
            {
                if (isCharging) CancelCharge();
                return;
            }

            Vector2 origin = transform.position;
            Vector2 aimDir = ((Vector2)mainCam.ScreenToWorldPoint(Input.mousePosition) - origin).normalized;

            // ---- 按住左键：蓄力 ----
            if (Input.GetMouseButton(0))
            {
                if (!isCharging)
                {
                    isCharging = true;
                    IsCharging = true;
                    chargeTimer = 0f;
                    chargeLevel = 0;
                    playerOriginalPos = transform.position;
                    AudioManager.PlayLoop("laser_charge"); // 蓄力起手音效（持续音，松手/中断即停）
                }

                chargeTimer += Time.deltaTime;

                // 计算蓄力等级（两级）
                int newLevel = 0;
                if (chargeTimer >= GameBalance.ChargeLevel2Time) newLevel = 2;
                else if (chargeTimer >= GameBalance.ChargeLevel1Time) newLevel = 1;

                if (newLevel != chargeLevel)
                {
                    chargeLevel = newLevel;
                    if (trauma != null && chargeLevel > 0)
                        trauma.AddTrauma(0.05f * chargeLevel);
                }

                // 满蓄力持续抖动 + 超时脱手：按住越久越不稳，超过阈值强制发射（防止挂机蓄满）
                if (chargeLevel >= 2)
                {
                    overchargeTimer += Time.deltaTime;
                    if (trauma != null)
                    {
                        // 目标 trauma 随过载时间从 0.45 爬升到 0.95（越久越抖），每帧向目标逼近，自然衰减也补偿
                        float target = Mathf.Clamp(0.45f + overchargeTimer * GameBalance.OverchargeTraumaPerSecond, 0.45f, 0.95f);
                        float cur = trauma.CurrentTrauma;
                        if (target > cur)
                            // 过载路径用放大倍率（OverchargeShakeMul），仅增强满蓄抖动，不影响受击/击杀等其他震屏
                            trauma.AddTrauma((target - cur) * 3f * Time.deltaTime, GameBalance.OverchargeShakeMul, GameBalance.OverchargeShakeMul);
                    }

                    // 满蓄超过 1s 冒泡提示“要憋不住了！”（仅触发一次）
                    if (!overchargeWarned && overchargeTimer >= GameBalance.OverchargeWarnTime)
                    {
                        overchargeWarned = true;
                        SpeechBubbleManager.Say(transform, SpeakerType.Player, SpeechEvent.Overcharge);
                    }


                    if (overchargeTimer >= GameBalance.OverchargeMaxHoldTime)
                    {
                        // 过载脱手 = 手滑：给瞄准方向加一个大幅度随机偏移，红弹明显飞偏以表现“瞄不准”
                        float spread = Random.Range(-GameBalance.OverchargeSpreadAngle, GameBalance.OverchargeSpreadAngle) * Mathf.Deg2Rad;
                        Vector2 firedDir = new Vector2(
                            aimDir.x * Mathf.Cos(spread) - aimDir.y * Mathf.Sin(spread),
                            aimDir.x * Mathf.Sin(spread) + aimDir.y * Mathf.Cos(spread));
                        Fire(firedDir);
                        CancelCharge();
                    }
                }

                // 蓄力视觉：弹丸像弹弓皮筋被拉开——随蓄力时长从身前向瞄准反方向拉回（满蓄力封顶保持）
                chargeOrb.enabled = true;
                chargeDir = aimDir;
                float chargeProgress = Mathf.Clamp01(chargeTimer / GameBalance.ChargeLevel2Time);
                float pullDist = Mathf.Lerp(GameBalance.ChargeOrbStartDist, -GameBalance.ChargeOrbMaxPull, chargeProgress);
                float orbSize = 0.3f + chargeLevel * 0.2f;
                chargeOrb.transform.position = origin + aimDir * pullDist;
                chargeOrb.transform.localScale = Vector3.one * orbSize;
                chargeOrb.color = levelColors[chargeLevel];

                // 蓄力拉拽动作：玩家向瞄准反方向缓慢后退（velocity 驱动，物理引擎挡墙），总位移封顶避免长按倒退过远
                if (chargeLevel > 0)
                {
                    Vector2 cur = transform.position;
                    if (Vector2.Distance(cur, playerOriginalPos) < GameBalance.ChargeMaxPullback - 0.001f)
                    {
                        float pullSpeed = 0.5f + chargeLevel * 0.5f; // 一级1.0/二级1.5 tile/s
                        ChargePullVelocity = -aimDir * pullSpeed;
                    }
                    else
                    {
                        // 已达封顶距离：停住不再后退
                        ChargePullVelocity = Vector2.zero;
                    }
                    // 通过玩家刚体 velocity 推进反向位移（物理引擎自动与墙碰撞阻挡）
                    var pc = GameBootstrap.Instance != null ? GameBootstrap.Instance.GetPlayer() : null;
                    if (pc != null && pc.Rb != null) pc.Rb.velocity = ChargePullVelocity;
                }
            }

            // ---- 松开左键：发射 ----
            if (Input.GetMouseButtonUp(0) && isCharging)
            {
                Fire(aimDir);
                CancelCharge();
            }
        }

        /// <summary>取消蓄力（松开发射/面板打开/切场景等）。停止蓄力持续音，保证无论蓄力时长多少都立即终止。</summary>
        private void CancelCharge()
        {
            isCharging = false;
            IsCharging = false;
            chargeOrb.enabled = false;
            ChargePullVelocity = Vector2.zero; // 复位拉拽速度，避免 PlayerController 读到残留值
            overchargeTimer = 0f; // 蓄力中断/发射后重置过载计时
            overchargeWarned = false; // 重置冒泡提示标志
            AudioManager.StopLoop(); // 终止蓄力音（发射时由发射音接续，中断时直接停）

            // 清零玩家刚体速度：避免蓄力拉拽的残留 velocity 在取消/发射后的过渡帧继续驱动物理位移（漂移 bug 修复）
            var pc = GameBootstrap.Instance != null ? GameBootstrap.Instance.GetPlayer() : null;
            if (pc != null && pc.Rb != null) pc.Rb.velocity = Vector2.zero;
        }

        /// <summary>发射弹丸（三档：0=白速射/1=黄/2=红，档位由实际蓄力时长决定）</summary>
        private void Fire(Vector2 aimDir)
        {
            int level = chargeLevel;

            // 白色速射冷却：连点过快时吞掉本次发射，把 DPS 封顶（黄色/红色无此限制）
            if (level == 0 && Time.time - lastFireTime < GameBalance.WhiteFireCooldown)
                return;
            lastFireTime = Time.time;

            // 每轮攻击力微增作用于弹丸伤害（数值文档第 7 章：浮点 +0.5/轮，向下取整）
            var stats = GameBootstrap.Instance != null ? GameBootstrap.Instance.PlayerStats : null;
            int attackBonus = stats != null ? Mathf.FloorToInt(stats.AttackBonusFloat) : 0;

            int damage = (level >= 2 ? 4 : (level == 1 ? 2 : 1)) + attackBonus;   // 白=1，黄=2(击飞控场)，红=4(对硬核单位高额伤害；对普通敌人走秒杀)，加每轮微增
            float knockback = level == 1 ? 1.5f : 0f;           // 击飞仅黄色档（数值文档 4.1）
            bool shatter = level >= 2;

            var go = ObjectPool.Get(PlayerProjectile.Template, transform.position, Quaternion.identity);
            var proj = go.GetComponent<PlayerProjectile>();
            proj.Launch(aimDir, level, damage, knockback, shatter);

            // 发射吐槽气泡（设计文档 14.x，可选）
            SpeechBubbleManager.Say(transform, SpeakerType.Player, SpeechEvent.Attack);

            if (trauma != null) trauma.AddTrauma(0.1f * level);

            // 发射后坐力：沿瞄准反方向短促回弹，幅度随蓄力档位递增（PlayerController 接管位移）
            var player = GameBootstrap.Instance != null ? GameBootstrap.Instance.GetPlayer() : null;
            if (player != null) player.ApplyRecoil(aimDir, level);

            // 三色发射音：按蓄力档位播放（蓄力持续音已由 CancelCharge 的 StopLoop 终止，此处接续）
            // level: 0=白速射 / 1=黄 / 2=红满蓄
            string fireClip = level >= 2 ? "laser_fire_red" : (level == 1 ? "laser_fire_yellow" : "laser_fire_white");
            AudioManager.Play(fireClip);
        }
    }
}
