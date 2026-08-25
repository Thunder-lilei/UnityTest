using BiuBiu.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BiuBiu.UI
{
    /// <summary>
    /// 开场电影卡（电影风格开场；设计文档 M4 开始页骨架；数值文档：开场电影卡分组）。
    /// 首次启动播放一次：纯黑底 + 居中两行台词（第一行问句大字 / 第二行署名半字号），按任意键开始；
    /// 若开局 5s 内未按任意键，右下角淡入「按任意键开始」呼吸提醒。确认后黑底快速淡出进 Main。
    /// 每次启动游戏仅播放一次（进程内静态标记 HasPlayedThisSession：本次进程已播则跳过；
    /// 回标题重进 Boot 不重播；完全退出游戏、下次再开（新进程）重新播放）。编辑器下强制重播以便验证。
    /// IMGUI（OnGUI）实现，与 GameHud / DeathPanel / PauseMenu 同风格，无 Canvas / 后处理。
    /// 挂在 Boot 场景（GameBootstrap 所在场景）的常驻对象上；自身用 DontDestroyOnLoad 保证淡出过渡连贯。
    /// </summary>
    public class TitleCard : MonoBehaviour
    {
        private enum Phase { FadeIn, Wait, FadeOut, Done }

        /// <summary>进程内是否已播放过开场卡（每次启动游戏仅播放一次；跨进程/重开游戏自动重置）</summary>
        public static bool HasPlayedThisSession { get; private set; }

        /// <summary>返回标题时重置：让 Boot 重新播放开场卡（标题界面），而非跳过直接进 Main</summary>
        public static void ResetForReturnToTitle()
        {
            HasPlayedThisSession = false;
        }

        private Phase phase = Phase.FadeIn;
        private float timer;          // 当前阶段已过去时间（秒）
        private float revealTimer;    // Done 阶段：黑底保持时长（盖住切场景瞬间，丝滑切换）
        private bool hintVisible;     // 是否到了显示右下角提醒的时机

        /// <summary>
        /// 尝试播放开场卡（进程内仅一次）：本次启动已播过则跳过。
        /// 创建 TitleCard 对象（自身 DontDestroyOnLoad，确认后 LoadScene("Main")）。
        /// Boot 路径（GameBootstrap）与 Main 路径（RuntimeSceneBuilder 编辑器兜底）共用此入口。
        /// </summary>
        public static void TryPlay()
        {
            if (HasPlayedThisSession) return;
            var go = new GameObject("[TitleCard]");
            go.AddComponent<TitleCard>();
        }

        private void Awake()
        {
#if !UNITY_EDITOR
            // 本次启动已播过（真机/打包）→ 跳过开场卡，直接进 Main
            if (HasPlayedThisSession)
            {
                SceneManager.LoadScene("Main");
                Destroy(gameObject);
                return;
            }
#endif
            DontDestroyOnLoad(gameObject); // 淡出过渡期间不随场景卸载消失
        }

        private void Update()
        {
            timer += Time.unscaledDeltaTime; // 开场不受 timeScale 影响

            switch (phase)
            {
                case Phase.FadeIn:
                    if (timer >= GameBalance.TitleCardFadeInTime) { phase = Phase.Wait; timer = 0f; }
                    break;

                case Phase.Wait:
                    // 超过延迟未按任意键 → 显示右下角提醒
                    if (timer >= GameBalance.TitleCardHintDelay) hintVisible = true;
                    break;

                case Phase.FadeOut:
                    if (timer >= GameBalance.TitleCardFadeOutTime)
                    {
                        phase = Phase.Done;
                        HasPlayedThisSession = true; // 标记本次启动已播（进程内仅一次）
                        SceneManager.LoadScene("Main"); // 触发切场景（异步）；黑底继续盖住直到新场景就位
                    }
                    break;

                case Phase.Done:
                    // 黑底保持一小段时间，盖住 Boot→Main 的切换瞬间（相机已在 Main 首帧 snap 到位），
                    // 避免黑底消失瞬间露出 Boot 空场景或镜头滑动；到时再销毁自身、露出已就位的游戏画面
                    revealTimer += Time.unscaledDeltaTime;
                    if (revealTimer >= GameBalance.TitleCardRevealHold)
                        Destroy(gameObject);
                    break;
            }
        }

        /// <summary>任意键 / 鼠标确认开始（仅在已显现后响应，避免 Awake 当帧误触）</summary>
        private void OnGUI()
        {
            // 黑底铺满全屏（开场卡基底；FadeIn/Wait/FadeOut/Done 全程不透明，Done 阶段盖住切场景瞬间）
            Color prev = GUI.color;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;

            // Done 阶段：仅保持黑底，不画台词、不接收输入，直到 revealTimer 到点销毁并露出已就位的游戏画面
            if (phase == Phase.Done) return;

            // 台词淡入进度（FadeIn 阶段 0→1；之后保持 1；FadeOut 阶段随黑底淡出由 alpha 控制）
            float textAlpha = phase == Phase.FadeIn
                ? Mathf.Clamp01(timer / GameBalance.TitleCardFadeInTime)
                : 1f;
            // 确认后退场：台词随 FadeOut 渐隐（黑底始终不透明，整体呈现为「台词淡出、黑幕保持」）
            if (phase == Phase.FadeOut)
                textAlpha *= (1f - Mathf.Clamp01(timer / GameBalance.TitleCardFadeOutTime));

            // 居中两行台词
            var line1Style = new GUIStyle(GUI.skin.label)
            {
                fontSize = GameBalance.TitleCardLine1FontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 1f, 1f, textAlpha) }
            };
            var line2Style = new GUIStyle(GUI.skin.label)
            {
                fontSize = GameBalance.TitleCardLine2FontSize,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f, textAlpha * 0.85f) } // 稍暗
            };

            float cx = Screen.width * 0.5f;
            float baseY = Screen.height * 0.5f;
            GUI.Label(new Rect(0f, baseY - GameBalance.TitleCardLine1FontSize, Screen.width,
                GameBalance.TitleCardLine1FontSize * 1.4f), GameBalance.TitleCardLine1, line1Style);
            GUI.Label(new Rect(0f, baseY + GameBalance.TitleCardLine2FontSize * 0.4f, Screen.width,
                GameBalance.TitleCardLine2FontSize * 1.4f), GameBalance.TitleCardLine2, line2Style);

            // 右下角超时提醒（呼吸闪烁；opacity 0.4~0.9 往复）
            if (hintVisible && phase == Phase.Wait)
            {
                float pulse = 0.4f + 0.5f * (0.5f + 0.5f * Mathf.Sin(
                    Mathf.PI * 2f * (timer - GameBalance.TitleCardHintDelay) / GameBalance.TitleCardHintPulsePeriod));
                var hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = GameBalance.TitleCardHintFontSize,
                    alignment = TextAnchor.LowerRight,
                    normal = { textColor = new Color(1f, 1f, 1f, pulse) }
                };
                GUI.Label(new Rect(0f, 0f, Screen.width - 24f, Screen.height - 18f),
                    GameBalance.TitleCardHint, hintStyle);
            }

            // 任意键 / 鼠标确认（FadeIn 完成后才接收，避免开场当帧误触）
            if (phase != Phase.FadeIn && Event.current.type == EventType.KeyDown)
            {
                Event.current.Use();
                Confirm();
            }
            else if (phase != Phase.FadeIn && Event.current.type == EventType.MouseDown)
            {
                Event.current.Use();
                Confirm();
            }
        }

        /// <summary>确认开始：进入淡出阶段</summary>
        private void Confirm()
        {
            if (phase == Phase.FadeOut || phase == Phase.Done) return;
            phase = Phase.FadeOut;
            timer = 0f;
            hintVisible = false;
            // 跳过开场卡的按键（常为按住左键）会残留到游戏内，导致误触发攻击；
            // 置标记让 SlingWeapon 不起手，直到玩家松开一次再按（见 SlingWeapon.Update / OnMouseUp 解除）
            BiuBiu.Core.GameState.SuppressFireUntilRelease = true;
        }
    }
}
