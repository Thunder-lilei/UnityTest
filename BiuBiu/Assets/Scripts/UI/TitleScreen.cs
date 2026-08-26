using BiuBiu.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BiuBiu.UI
{
    /// <summary>
    /// 简洁标题界面（死亡回标题 / 暂停回标题 使用，不播电影开场卡）。
    /// 黑底 + 居中游戏标题 + 「按任意键 / 点击开始」呼吸提示；按下任意键或点击即重开 Main（新一局）。
    /// IMGUI（OnGUI）实现，与 TitleCard / DeathPanel / PauseMenu 同风格。
    /// 停留在当前有相机的场景显示，避免切到无相机的 Boot 场景导致「No Cameras rendering」。
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        private const float HintDelay = 3f;          // 标题页开始提示延迟（秒）
        private const float HintPulsePeriod = 1.6f;  // 呼吸周期

        private static TitleScreen instance;

        private float elapsed;
        private bool confirmed;

        /// <summary>本局战报（死亡结尾卡时注入；用于在署名下方展示「本次 vs 历史」统计行；非死亡路径调用为 null 则不显示）</summary>
        private static BattleReport? report;

        /// <summary>显示标题界面（单例；已存在则复用）</summary>
        public static void Show()
        {
            if (instance != null) return;
            var go = new GameObject("TitleScreen");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<TitleScreen>();
        }

        /// <summary>显示标题界面并附带本局战报（死亡流程末尾调用，结尾卡在署名下展示「本次 vs 历史」统计行）</summary>
        public static void Show(BattleReport r)
        {
            report = r;
            Show();
        }

        private void Update()
        {
            if (confirmed) return;
            elapsed += Time.unscaledDeltaTime;
            // 任意键 / 点击 → 开始（忽略鼠标移动等非按下事件）
            if (Input.anyKeyDown || (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
            {
                Confirm();
            }
        }

        private void Confirm()
        {
            if (confirmed) return;
            confirmed = true;
            Time.timeScale = 1f;
            GameState.DeathReportOpen = false;
            report = null; // 清战报缓存，避免下次标题页残留
            // 重开 Main（新一局）；RuntimeSceneBuilder 重建并触发 GameBootstrap.OnMainSceneReady
            SceneManager.LoadScene("Main");
            Destroy(gameObject);
        }

        private void OnGUI()
        {
            if (confirmed) return;
            // 黑底（填满屏幕，盖住背后的战场）
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.blackTexture);

            // 标题（居中大字）
            GUI.color = new Color(1f, 1f, 1f, 1f);
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 72,
                fontStyle = FontStyle.Bold,
            };
            GUI.Label(new Rect(0, Screen.height * 0.32f, Screen.width, 100), "在哪跌倒就在哪躺会儿", titleStyle);

            // 署名
            var subStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
            };
            GUI.Label(new Rect(0, Screen.height * 0.32f + 110, Screen.width, 40),
                "——李雷", subStyle);

            // 本局 vs 历史最佳统计行（死亡结尾卡专属：署名下空一行展示，小字半透明）
            if (report.HasValue)
            {
                var br = report.Value;
                var statsStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 20,
                    normal = { textColor = new Color(0.85f, 0.85f, 0.85f, 0.8f) }
                };
                // 空一行（署名基线 40 高 + 空行间距 36）
                GUI.Label(new Rect(0, Screen.height * 0.32f + 110 + 40 + 36, Screen.width, 30),
                    string.Format(GameBalance.EndCardStatsLine, br.Wave, br.Kills, SaveSystem.BestWave, SaveSystem.BestKills),
                    statsStyle);
            }

            // 开始提示（延迟淡入 + 呼吸）
            if (elapsed >= HintDelay)
            {
                float pulse = 0.55f + 0.35f * (0.5f + 0.5f * Mathf.Sin(elapsed * (2f * Mathf.PI / HintPulsePeriod)));
                GUI.color = new Color(1f, 1f, 1f, pulse);
                var hintStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = GameBalance.TitleCardHintFontSize,
                };
                GUI.Label(new Rect(0, Screen.height * 0.7f, Screen.width, 40),
                    GameBalance.TitleCardHint, hintStyle);
            }
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
