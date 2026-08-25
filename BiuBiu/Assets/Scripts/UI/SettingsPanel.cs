using BiuBiu.Core;
using UnityEngine;

namespace BiuBiu.UI
{
    /// <summary>
    /// 设置面板（M2 提前做；文案文档 U07a~U07c）。
    /// 三项功能：屏幕震动开关 / 慢动作演出开关 / 开发者模式无敌开关。
    /// 开关状态持久化（PlayerPrefs）；从暂停菜单「设置」按钮进入，返回时回暂停菜单。
    /// 灰盒 OnGUI 框架内实现，不迁移 UI 框架。
    /// </summary>
    public class SettingsPanel
    {
        private static bool visible;

        // ---- 持久化设置键 ----
        private const string KeyScreenShake = "Setting_ScreenShake";
        private const string KeySlowmo = "Setting_Slowmo";
        // 开发者模式已有 DeveloperMode.GodMode 静态字段，F3 切换；此面板读写同一状态

        /// <summary>显示设置面板</summary>
        public static void Show()
        {
            visible = true;
            // 设置面板打开时不算 Pause/Upgrade/Death，不设 GameState 标志
            // （已在暂停菜单内=timeScale=0，Update/OnGUI 照常走）
        }

        /// <summary>隐藏设置面板</summary>
        public static void Hide()
        {
            visible = false;
        }

        public static bool IsVisible => visible;

        /// <summary>屏幕震动是否启用（CameraTrauma 读取）</summary>
        public static bool ScreenShakeEnabled
        {
            get => PlayerPrefs.GetInt(KeyScreenShake, 1) == 1;
            set => PlayerPrefs.SetInt(KeyScreenShake, value ? 1 : 0);
        }

        /// <summary>慢动作演出是否启用</summary>
        public static bool SlowmoEnabled
        {
            get => PlayerPrefs.GetInt(KeySlowmo, 1) == 1;
            set => PlayerPrefs.SetInt(KeySlowmo, value ? 1 : 0);
        }

        /// <summary>
        /// 绘制设置面板（由 PauseMenu.OnGUI 在主面板之后调用，保证层级在主面板之上）。
        /// 自身为模态层：全屏遮罩 + 控件，吃掉本帧事件避免穿透到暂停主面板。
        /// </summary>
        public static void Draw()
        {
            if (!visible) return;

            // 全屏半透明遮罩
            Color prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;

            // 面板
            float panelW = 480f, panelH = 360f;
            Rect panel = new Rect((Screen.width - panelW) * 0.5f, (Screen.height - panelH) * 0.5f, panelW, panelH);
            GUI.Box(panel, string.Empty);

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(new Rect(panel.x, panel.y + 20f, panel.width, 40f), "设置", titleStyle);

            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 20 };
            var btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };
            float y = panel.y + 80f;
            float leftX = panel.x + 40f;
            float toggleW = 80f;

            // 屏幕震动开关（文案文档 U07a）
            GUI.Label(new Rect(leftX, y, 200f, 32f), "屏幕震动", labelStyle);
            bool shake = ScreenShakeEnabled;
            if (GUI.Button(new Rect(panel.x + panelW - 120f, y, toggleW, 32f), shake ? "开" : "关", btnStyle))
            {
                ScreenShakeEnabled = !shake;
            }
            y += 50f;

            // 慢动作演出开关（文案文档 U07b）
            GUI.Label(new Rect(leftX, y, 200f, 32f), "慢动作演出", labelStyle);
            bool slowmo = SlowmoEnabled;
            if (GUI.Button(new Rect(panel.x + panelW - 120f, y, toggleW, 32f), slowmo ? "开" : "关", btnStyle))
            {
                SlowmoEnabled = !slowmo;
            }
            y += 50f;

            // 开发者模式无敌开关（文案文档 U07c）
            GUI.Label(new Rect(leftX, y, 200f, 32f), "开发者模式（无敌）", labelStyle);
            bool god = DeveloperMode.GodMode;
            if (GUI.Button(new Rect(panel.x + panelW - 120f, y, toggleW, 32f), god ? "开" : "关", btnStyle))
            {
                DeveloperMode.GodMode = !god;
            }
            y += 50f;

            // 返回按钮（文案文档 U07e）
            if (GUI.Button(new Rect(panel.x + panelW * 0.5f - 60f, y, 120f, 40f), "返回", btnStyle))
            {
                Hide();
                // 返回暂停菜单（PauseMenu 仍在暂停态，直接恢复其显示）
            }

            // 吃掉本帧事件，确保点击遮罩/空白区域不会穿透到下层暂停主面板
            if (Event.current.type == EventType.MouseDown) Event.current.Use();
        }
    }
}
