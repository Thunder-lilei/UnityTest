using BiuBiu.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BiuBiu.UI
{
    /// <summary>
    /// 死亡战报：标题「力战而竭」；字段：打到第 N 轮 / 砍了 X 个；
    /// 破纪录项后缀「新纪录！」；按钮：再战 / 回到标题。
    /// 触发链（设计文档死亡流程）：玩家血尽 → 慢动作+镜头聚焦（PlayerController）
    /// → GameBootstrap.EndRun 结算 → 本面板展示 → 再战=重载 Main（重开零成本）/ 回到标题=Boot。
    /// 灰盒 OnGUI。
    /// </summary>
    public class DeathPanel : MonoBehaviour
    {
        /// <summary>宿主单例（惰性创建）</summary>
        private static DeathPanel instance;

        /// <summary>战报数据（EndRun 产出）</summary>
        private static BattleReport report;

        /// <summary>是否展示中（PauseMenu 互斥判定用）</summary>
        public static bool IsVisible => visible;

        /// <summary>是否展示中</summary>
        private static bool visible;

        /// <summary>M2 补间计时</summary>
        private float animTime;
        private const float AnimDuration = 0.3f;

        /// <summary>展示战报（PlayerController 死亡流程末尾调用）</summary>
        public static void Show(BattleReport r)
        {
            EnsureInstance();
            report = r;
            visible = true;
            instance.animTime = 0f; // M2 补间重置
            GameState.DeathReportOpen = true; // 输入锁登记（玩法输入冻结）
        }

        /// <summary>惰性创建宿主</summary>
        private static void EnsureInstance()
        {
            if (instance != null) return;
            var go = new GameObject("[DeathPanel]");
            instance = go.AddComponent<DeathPanel>();
            DontDestroyOnLoad(go);
        }

        private void OnGUI()
        {
            if (!visible) return;

            // M2 补间推进（timeScale 可能被恢复=1，用 unscaledDeltaTime 稳妥）
            animTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(animTime / AnimDuration);
            float ease = 1f - Mathf.Pow(1f - t, 3f); // EaseOutCubic
            float panelAlpha = Mathf.Clamp01(t * 2f);

            // ---- 全屏变暗（带补间） ----
            Color prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.75f * panelAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;

            // ---- 面板（带从下方滑入+缩放补间） ----
            float panelW = 560f, panelH = 400f;
            float offsetY = (1f - ease) * 60f; // 从下方 60px 滑入
            Rect panel = new Rect((Screen.width - panelW) * 0.5f,
                (Screen.height - panelH) * 0.5f + offsetY, panelW, panelH);
            GUI.Box(panel, string.Empty);

            // 标题透明度补间
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 42,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 1f, 1f, panelAlpha) }
            };
            GUI.Label(new Rect(panel.x, panel.y + 24f, panel.width, 56f), "力战而竭", titleStyle);

            // 两行字段透明度补间
            float fieldsAlpha = Mathf.Clamp01((t - 0.2f) * 2f); // 延后 20% 出现
            {
                var lineStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 1f, 1f, fieldsAlpha) }
                };
                var recordStyle = new GUIStyle(lineStyle)
                {
                    normal = { textColor = new Color(1f, 0.8f, 0.15f, fieldsAlpha) },
                    fontStyle = FontStyle.Bold
                };

                float y = panel.y + 120f;
                GUI.Label(new Rect(panel.x, y, panel.width, 34f),
                    $"打到第 {report.Wave} 轮" + (report.WaveNewRecord ? "  新纪录！" : string.Empty),
                    report.WaveNewRecord ? recordStyle : lineStyle);
                y += 44f;
                GUI.Label(new Rect(panel.x, y, panel.width, 34f),
                    $"砍了 {report.Kills} 个" + (report.KillsNewRecord ? "  新纪录！" : string.Empty),
                    report.KillsNewRecord ? recordStyle : lineStyle);
            }

            // 按钮补间（最后出现）
            float btnAlpha = Mathf.Clamp01((t - 0.4f) * 2.5f);
            var btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 22 };
            if (btnAlpha > 0.3f)
            {
                if (GUI.Button(new Rect(panel.x + 80f, panel.y + 300f, 180f, 52f), "再战", btnStyle))
                {
                    Restart();
                }
                if (GUI.Button(new Rect(panel.x + panel.width - 260f, panel.y + 300f, 180f, 52f), "回到标题", btnStyle))
                {
                    BackToTitle();
                }
            }
        }

        /// <summary>再战：重载 Main 场景（RuntimeSceneBuilder 重建一切=全新一局，重开零成本）</summary>
        private static void Restart()
        {
            visible = false;
            GameState.DeathReportOpen = false;
            Time.timeScale = 1f; // 防慢动作残留
            SceneManager.LoadScene("Main");
        }

        /// <summary>回到标题：Boot 场景（GameBootstrap 接管再进 Main——灰盒无开始页，M4 补）</summary>
        private static void BackToTitle()
        {
            visible = false;
            GameState.DeathReportOpen = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(0); // Build 首场景 = Boot
        }
    }
}
