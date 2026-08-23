using BiuBiu.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BiuBiu.UI
{
    /// <summary>
    /// ESC 暂停菜单：标题「喘口气」；按钮：继续 / 重新开始 / 回到标题 / 设置。
    /// timeScale=0 全局暂停；互斥：死亡战报打开时 ESC 无效。
    /// 设置面板（键位/音量/震屏/慢动作/开发者模式）已接入（SettingsPanel）。
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        /// <summary>场景单例引用（RuntimeSceneBuilder 挂载）</summary>
        private static PauseMenu instance;

        /// <summary>暂停态</summary>
        private static bool paused;

        /// <summary>M2 补间计时</summary>
        private float animTime;
        private const float AnimDuration = 0.2f;

        /// <summary>是否暂停（外部输入过滤用：武器/翻滚在暂停中不响应）</summary>
        public static bool IsPaused => paused;

        private void Update()
        {
            // ESC 开关（战报打开时忽略——互斥，设计文档 15 章演出优先级）
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (DeathPanelVisible) return;

            if (paused) Resume();
            else Pause();
        }

        /// <summary>死亡战报可见性（DeathPanel 私有，反射取值过重——改用其公开只读属性见下）</summary>
        private static bool DeathPanelVisible => DeathPanel.IsVisible;

        /// <summary>暂停：timeScale=0（Update 输入与 OnGUI 不受影响）</summary>
        private static void Pause()
        {
            paused = true;
            GameState.Paused = true; // 输入锁登记（玩法系统读 GameState.InputLocked）
            Time.timeScale = 0f;
            if (instance != null) instance.animTime = 0f; // M2 补间重置
        }

        private void Awake()
        {
            instance = this;
        }

        /// <summary>继续：恢复 timeScale</summary>
        private static void Resume()
        {
            paused = false;
            GameState.Paused = false;
            Time.timeScale = 1f;
        }

        private void OnGUI()
        {
            if (!paused) return;

            // M2 补间推进
            animTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(animTime / AnimDuration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            float alpha = Mathf.Clamp01(t * 2f);

            // ---- 全屏变暗（带补间） ----
            Color prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f * alpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;

            // ---- 面板（从下方滑入） ----
            float panelW = 420f, panelH = 460f;
            float offsetY = (1f - ease) * 40f;
            Rect panel = new Rect((Screen.width - panelW) * 0.5f,
                (Screen.height - panelH) * 0.5f + offsetY, panelW, panelH);
            GUI.Box(panel, string.Empty);

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 36,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 1f, 1f, alpha) }
            };
            GUI.Label(new Rect(panel.x, panel.y + 28f, panel.width, 48f), "喘口气", titleStyle);

            var btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 22 };
            float bw = 260f, bh = 52f, bx = panel.x + (panel.width - bw) * 0.5f;
            float btnAlpha = Mathf.Clamp01((t - 0.2f) * 2f);

            if (btnAlpha > 0.3f)
            {
                if (GUI.Button(new Rect(bx, panel.y + 110f, bw, bh), "继续", btnStyle)) Resume();
                if (GUI.Button(new Rect(bx, panel.y + 180f, bw, bh), "重新开始", btnStyle)) Restart();
                if (GUI.Button(new Rect(bx, panel.y + 250f, bw, bh), "回到标题", btnStyle)) BackToTitle();

                // 设置按钮（接入设置面板 SettingsPanel）
                GUI.enabled = !SettingsPanel.IsVisible;
                if (GUI.Button(new Rect(bx, panel.y + 320f, bw, bh), "设置", btnStyle))
                {
                    SettingsPanel.Show();
                }
                GUI.enabled = true;
            }
        }

        /// <summary>重新开始：重载 Main（与战报「再战」同通道）</summary>
        private static void Restart()
        {
            Resume();
            SceneManager.LoadScene("Main");
        }

        /// <summary>回到标题：Build 首场景</summary>
        private static void BackToTitle()
        {
            Resume();
            SceneManager.LoadScene(0);
        }
    }
}
