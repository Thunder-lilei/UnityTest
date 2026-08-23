using BiuBiu.Core;
using BiuBiu.Enemies;
using BiuBiu.Player;
using UnityEngine;

namespace BiuBiu.UI
{
    /// <summary>
    /// 战斗 HUD（灰盒 OnGUI）：局时「mm:ss」+ 轮次「第 N 轮」、血量红心、消息框 toast
    /// （开场/轮次/精英·大蜘蛛登场提示——订阅 EnemySpawner2D.OnEnemyIntro，文案就地维护于 Spawner，2s 淡出）。
    /// 暂停/战报打开时本 HUD 照常显示（底层）。
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        /// <summary>消息提示剩余显示时长（秒）</summary>
        private float introTimer;

        /// <summary>当前消息提示文本</summary>
        private string introText;

        private void OnEnable()
        {
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

        private void OnGUI()
        {
            var stats = GameBootstrap.Instance != null ? GameBootstrap.Instance.RunStats : null;
            var player = GameBootstrap.Instance != null ? GameBootstrap.Instance.GetPlayer() : null;
            if (stats == null) return;

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
                GUI.DrawTexture(new Rect(12f + i * 30f, 42f, 26f, 26f), Texture2D.whiteTexture);
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
            GUI.Label(new Rect(12f, 120f, 360f, 20f), "WASD 移动", hintStyle);
            GUI.Label(new Rect(12f, 142f, 360f, 20f), "左键 蓄力射击    空格 闪避", hintStyle);

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
