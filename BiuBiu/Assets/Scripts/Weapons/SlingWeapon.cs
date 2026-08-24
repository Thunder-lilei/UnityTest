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

        /// <summary>是否正在蓄力（PlayerController 读取以冻结移动）</summary>
        public static bool IsCharging { get; private set; }

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

                // 蓄力视觉：弹丸像弹弓皮筋被拉开——随蓄力时长从身前向瞄准反方向拉回（满蓄力封顶保持）
                chargeOrb.enabled = true;
                chargeDir = aimDir;
                float chargeProgress = Mathf.Clamp01(chargeTimer / GameBalance.ChargeLevel2Time);
                float pullDist = Mathf.Lerp(GameBalance.ChargeOrbStartDist, -GameBalance.ChargeOrbMaxPull, chargeProgress);
                float orbSize = 0.3f + chargeLevel * 0.2f;
                chargeOrb.transform.position = origin + aimDir * pullDist;
                chargeOrb.transform.localScale = Vector3.one * orbSize;
                chargeOrb.color = levelColors[chargeLevel];

                // 蓄力拉拽动作：玩家向瞄准反方向缓慢后退，但总位移封顶（基于蓄力起点），
                // 避免长按无限倒退、也避免松手时已有过大偏移显得突兀
                if (chargeLevel > 0)
                {
                    Vector2 target = (Vector2)playerOriginalPos - aimDir * GameBalance.ChargeMaxPullback;
                    // 仅在未超过封顶距离时朝目标推进（每帧小幅逼近，手感平滑）
                    Vector2 cur = transform.position;
                    if (Vector2.Distance(cur, playerOriginalPos) < GameBalance.ChargeMaxPullback - 0.001f)
                    {
                        float pullSpeed = 0.5f + chargeLevel * 0.5f; // 一级1.0/二级1.5 tile/s
                        Vector3 pullBack = (Vector3)((-aimDir * pullSpeed * Time.deltaTime));
                        Vector3 np = transform.position + pullBack;
                        // 不超过封顶目标点
                        if (Vector2.Distance((Vector2)np, playerOriginalPos) <= GameBalance.ChargeMaxPullback)
                            transform.position = np;
                    }
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
            AudioManager.StopLoop(); // 终止蓄力音（发射时由发射音接续，中断时直接停）
        }

        /// <summary>发射弹丸（三档：0=白速射/1=黄/2=红，档位由实际蓄力时长决定）</summary>
        private void Fire(Vector2 aimDir)
        {
            int level = chargeLevel;

            // 每轮攻击力微增作用于弹丸伤害（数值文档第 7 章：浮点 +0.5/轮，向下取整）
            var stats = GameBootstrap.Instance != null ? GameBootstrap.Instance.PlayerStats : null;
            int attackBonus = stats != null ? Mathf.FloorToInt(stats.AttackBonusFloat) : 0;

            int damage = (level >= 2 ? 2 : 1) + attackBonus;   // 白/黄=1，红=2（击碎），加每轮微增
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
        }
    }
}
