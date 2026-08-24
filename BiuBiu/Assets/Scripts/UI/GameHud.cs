using BiuBiu.Core;
using BiuBiu.Enemies;
using BiuBiu.Player;
using System.Collections.Generic;
using UnityEngine;

namespace BiuBiu.UI
{
    /// <summary>
    /// 战斗 HUD（灰盒 OnGUI）：局时「mm:ss」+ 轮次「第 N 轮」、血量红心、消息框 toast
    /// （开场/轮次/精英·Boss 登场提示——订阅 EnemySpawner2D.OnEnemyIntro，文案就地维护于 Spawner，2s 淡出）。
    /// 暂停/战报打开时本 HUD 照常显示（底层）。
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        /// <summary>受击红边控制器引用（惰性获取，热重载自愈）</summary>
        private HurtVignette vignette;

        /// <summary>消息提示剩余显示时长（秒）</summary>
        private float introTimer;

        /// <summary>当前消息提示文本</summary>
        private string introText;

        private void OnEnable()
        {
            // 确保头顶气泡管理器存在（设计文档 14.x；懒挂到本常驻 HUD 对象）
            if (GetComponent<SpeechBubbleManager>() == null)
                gameObject.AddComponent<SpeechBubbleManager>();

            EnemySpawner2D.OnEnemyIntro += OnEnemyIntro;
            // 补消费开场消息（Start 可能早于本订阅，直接事件会丢失）
            var opening = EnemySpawner2D.ConsumePendingOpeningMessage();
            if (!string.IsNullOrEmpty(opening))
                OnEnemyIntro(opening);
        }

        private void OnDisable()
        {
            EnemySpawner2D.OnEnemyIntro -= OnEnemyIntro;
        }

        /// <summary>消息提示入队（文案由 Spawner 直接给成品句）</summary>
        private void OnEnemyIntro(string text)
        {
            introText = text;
            introTimer = GameBalance.AchievementToastDuration; // 复用 toast 时长 2s
        }

        private void Update()
        {
            if (introTimer > 0f) introTimer -= Time.unscaledDeltaTime; // 暂停中也要淡出计时
            // 每帧兜底消费开场消息：Spawner.Start 与 HUD.OnEnable 时序不确定，确保不丢失
            var opening = EnemySpawner2D.ConsumePendingOpeningMessage();
            if (!string.IsNullOrEmpty(opening))
                OnEnemyIntro(opening);
        }

        // 运行时生成的心形纹理（白色心形 alpha 遮罩，颜色由 GUI.color 控制）
        private static Texture2D _heartTex;
        private static Texture2D HeartTexture
        {
            get
            {
                if (_heartTex == null) _heartTex = CreateHeartTexture();
                return _heartTex;
            }
        }
        private static Texture2D CreateHeartTexture()
        {
            int s = 32;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var px = new Color32[s * s];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);

            // 心形参数方程采样轮廓点（数学坐标，y 向上）：x=16sin³t, y=13cos t−5cos2t−2cos3t−cos4t
            var contour = new List<Vector2>();
            int steps = 400;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps * Mathf.PI * 2f;
                float hx = 16f * Mathf.Pow(Mathf.Sin(t), 3f);
                float hy = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t);
                contour.Add(new Vector2(hx, hy));
            }

            // 参数范围约 x∈[-16,16]、y∈[-17,12]，映射到 32 网格并居中
            float half = 18f; // 覆盖 [-18,18] 留边
            for (int y = 0; y < s; y++)
            {
                // 屏幕 y 向下，数学 y 向上：屏幕顶部(y=0)对应数学底部(my=-half)
                float my = (y / (float)(s - 1)) * 2f * half - half;
                var xs = new List<float>();
                for (int i = 0; i < contour.Count - 1; i++)
                {
                    float y0 = contour[i].y, y1 = contour[i + 1].y;
                    if ((y0 <= my && y1 > my) || (y1 <= my && y0 > my))
                    {
                        float f = (my - y0) / (y1 - y0);
                        xs.Add(contour[i].x + f * (contour[i + 1].x - contour[i].x));
                    }
                }
                xs.Sort();
                for (int k = 0; k + 1 < xs.Count; k += 2)
                {
                    int xStart = Mathf.RoundToInt((xs[k] / half + 1f) * 0.5f * (s - 1));
                    int xEnd = Mathf.RoundToInt((xs[k + 1] / half + 1f) * 0.5f * (s - 1));
                    for (int x = Mathf.Max(0, xStart); x <= Mathf.Min(s - 1, xEnd); x++)
                        px[y * s + x] = new Color32(255, 255, 255, 255);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 绘制受击红边（Hurt Vignette）：取 HurtVignette 组件生成的径向贴图，铺满全屏，
        /// 以 GUI.color 的 alpha 充当强度乘子（贴图 rgb 已由红染色）。组件缺失/热重载自愈时静默跳过。
        /// </summary>
        private void DrawHurtVignette()
        {
            if (vignette == null) vignette = FindObjectOfType<HurtVignette>();
            if (vignette == null || vignette.Texture == null) return;

            float intensity = vignette.CurrentAlpha;
            if (intensity <= 0.001f) return;

            Color prev = GUI.color;
            // 贴图 rgb=白（径向遮罩在 alpha），此处染成红色并按强度调制透明度
            GUI.color = new Color(0.85f, 0.08f, 0.08f, intensity);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height),
                vignette.Texture, ScaleMode.StretchToFill, true);
            GUI.color = prev;
        }

        private void OnGUI()
        {
            var stats = GameBootstrap.Instance != null ? GameBootstrap.Instance.RunStats : null;
            var player = GameBootstrap.Instance != null ? GameBootstrap.Instance.GetPlayer() : null;
            if (stats == null) return;

            // ---- 受击红边（Hurt Vignette）：全屏径向红色晕影，铺在最底层不与文字争抢 ----
            DrawHurtVignette();

            // ---- 顶部左侧：计时 / 轮次（等级口径已退役，统一轮次） ----
            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            int waveNum = BiuBiu.Enemies.EnemySpawner2D.CurrentWave;
            GUI.Label(new Rect(12f, 8f, 400f, 28f),
                $"{stats.ElapsedTimeString}    第 {waveNum} 轮", labelStyle);

            // ---- 血量红心（GUI.DrawTexture 实心绘制替代 GUI.Box——后者默认皮肤填充过淡呈「空心」观感） ----
            var ps = GameBootstrap.Instance.PlayerStats;
            int maxHp = ps != null ? ps.MaxHealth : GameBalance.PlayerMaxHealth;
            int curHp = player != null ? player.CurrentHealth : 0;
            for (int i = 0; i < maxHp; i++)
            {
                Color prev = GUI.color;
                GUI.color = i < curHp ? new Color(0.9f, 0.2f, 0.25f) : new Color(0.25f, 0.25f, 0.25f);
                GUI.DrawTexture(new Rect(12f + i * 30f, 42f, 26f, 26f), HeartTexture);
                GUI.color = prev;
            }

            // ---- 开发者模式无敌常显（M0 有此提示，M1 迁移 HUD 时丢失；F3 误开后无感知=误判不掉血根因之一） ----
            if (BiuBiu.Core.DeveloperMode.GodMode)
            {
                var dmStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                dmStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
                GUI.Label(new Rect(12f, 74f, 400f, 24f), "开发者模式：无敌 开（F3 切换）", dmStyle);
            }

            // ---- 左上角操作提示 ----
            var hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            hintStyle.normal.textColor = new Color(0.7f, 0.75f, 0.7f);
            GUI.Label(new Rect(12f, 80f, 360f, 22f), "WASD 移动", hintStyle);
            GUI.Label(new Rect(12f, 104f, 360f, 22f), "左键 蓄力射击", hintStyle);
            GUI.Label(new Rect(12f, 126f, 360f, 22f), "空格 闪避", hintStyle);

            // ---- 轮次结束提示（屏幕右侧消息框） ----
            if (introTimer > 0f && !string.IsNullOrEmpty(introText))
            {
                float alpha = Mathf.Clamp01(introTimer / 0.4f); // 末段 0.4s 渐隐
                var toastStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 28,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                toastStyle.normal.textColor = new Color(1f, 0.85f, 0.3f, alpha);
                GUI.Label(new Rect(0f, Screen.height * 0.4f, Screen.width, 44f), introText, toastStyle);
            }
        }
    }
}
