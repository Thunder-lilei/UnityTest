using System.Collections.Generic;
using BiuBiu.Data;
using BiuBiu.Core;
using UnityEngine;

namespace BiuBiu.UI
{
    /// <summary>
    /// 角色头顶消息气泡管理器（灰盒阶段：纯 OnGUI 绘制，零素材依赖）。
    /// 设计文档 14.x / 数值文档第 9 章（v3.6）。
    /// 气泡跟随目标 Transform 移动，存活 BubbleLifetime 后淡出消失；
    /// 同目标同类事件受 BubbleMinInterval 去重冷却限制，防刷屏。
    /// 用法：SpeechBubbleManager.Say(transform, SpeakerType.Player, SpeechEvent.Hit);
    /// </summary>
    public class SpeechBubbleManager : MonoBehaviour
    {
        [Tooltip("文案池（Resources 兜底加载，缺省自动找 Resources/Data/Speech/SpeechBank）")]
        public SpeechBank speechBank;

        // 单条活跃气泡
        private class Bubble
        {
            public Transform target;
            public string text;
            public float age;          // 已存活时间（unscaled）
            public float anchorY;      // 头顶锚点相对目标原点的偏移（tile，世界单位）
        }

        private readonly List<Bubble> _bubbles = new List<Bubble>();
        // 去重冷却：key = (targetInstanceId, speaker, event)
        private readonly Dictionary<int, float> _cooldown = new Dictionary<int, float>();

        private static SpeechBubbleManager _instance;
        private const string BANK_PATH = "Data/Speech/SpeechBank";

        private void Awake()
        {
            _instance = this;
            if (speechBank == null)
                speechBank = Resources.Load<SpeechBank>(BANK_PATH);
            // 无 SO 资产（SpeechBank.asset）时，创建内存实例——它仍会从
            // Resources/Data/Speech/speech.txt 读取文案（改文案只改 txt，无需动脚本）
            if (speechBank == null)
                speechBank = ScriptableObject.CreateInstance<SpeechBank>();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // 触发气泡（静态入口）
        public static void Say(Transform target, SpeakerType speaker, SpeechEvent speechEvent, float anchorY = 0.8f)
        {
            if (_instance == null || target == null) return;
            var line = _instance.speechBank != null ? _instance.speechBank.GetLine(speaker, speechEvent) : null;
            if (string.IsNullOrEmpty(line)) return;

            int key = TargetKey(target, speaker, speechEvent);
            // 去重冷却
            if (_instance._cooldown.TryGetValue(key, out float until) && Time.unscaledTime < until)
                return;
            _instance._cooldown[key] = Time.unscaledTime + GameBalance.BubbleMinInterval;

            _instance._bubbles.Add(new Bubble
            {
                target = target,
                text = line,
                age = 0f,
                anchorY = anchorY
            });
        }

        private static int TargetKey(Transform t, SpeakerType s, SpeechEvent e)
        {
            // 组合哈希：实例ID + 说话者 + 事件
            unchecked
            {
                int hash = t.GetInstanceID();
                hash = hash * 31 + (int)s;
                hash = hash * 31 + (int)e;
                return hash;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = _bubbles.Count - 1; i >= 0; i--)
            {
                var b = _bubbles[i];
                b.age += dt;
                // 目标已销毁则立即移除
                if (b.target == null || b.age >= GameBalance.BubbleLifetime)
                {
                    _bubbles.RemoveAt(i);
                }
            }
        }

        private void OnGUI()
        {
            if (_bubbles.Count == 0 || Camera.main == null) return;

            GUI.depth = -1000; // 压在最上层
            var style = GetBubbleStyle();

            foreach (var b in _bubbles)
            {
                if (b.target == null) continue;

                // 世界坐标 → 屏幕坐标（头顶锚点）
                Vector3 world = b.target.position + Vector3.up * b.anchorY;
                Vector3 screen = Camera.main.WorldToScreenPoint(world);
                if (screen.z < 0) continue; // 在相机背后

                // 透明度：显示期全不透明，淡出期线性渐隐
                float alpha = 1f;
                if (b.age > GameBalance.BubbleShowDuration)
                {
                    float f = (b.age - GameBalance.BubbleShowDuration) / GameBalance.BubbleFadeDuration;
                    alpha = Mathf.Clamp01(1f - f);
                }

                // 文本尺寸测量
                GUIContent content = new GUIContent(b.text);
                Vector2 size = style.CalcSize(content);
                float padX = 10f, padY = 6f;
                float boxW = size.x + padX * 2f;
                float boxH = size.y + padY * 2f;

                // 屏幕坐标 y 向下，WorldToScreenPoint 已翻转；GUI 用左上原点
                float x = screen.x - boxW / 2f;
                float y = Screen.height - screen.y - boxH - 6f; // 浮在头顶上方

                // 气泡框（半透明白底 + 黑边 + 小尾巴用三角形近似）
                Color prevBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 1f, 1f, alpha);
                GUI.Box(new Rect(x, y, boxW, boxH), content, style);
                GUI.backgroundColor = prevBg;

                // 小尾巴（朝下的三角）
                var tail = GetTailStyle(alpha);
                float tailW = 14f, tailH = 8f;
                GUI.Box(new Rect(screen.x - tailW / 2f, y + boxH - 1f, tailW, tailH), "", tail);
            }
        }

        private static GUIStyle _bubbleStyle;
        private GUIStyle GetBubbleStyle()
        {
            if (_bubbleStyle == null)
            {
                _bubbleStyle = new GUIStyle(GUI.skin.box);
                _bubbleStyle.normal.background = MakeTex(new Color(1f, 1f, 1f, 0.92f));
                _bubbleStyle.normal.textColor = Color.black;
                _bubbleStyle.fontSize = 16;
                _bubbleStyle.alignment = TextAnchor.MiddleCenter;
                _bubbleStyle.wordWrap = false;
                _bubbleStyle.border = new RectOffset(8, 8, 8, 8);
                _bubbleStyle.padding = new RectOffset(10, 10, 6, 6);
            }
            return _bubbleStyle;
        }

        private static GUIStyle _tailStyle;
        private GUIStyle GetTailStyle(float alpha)
        {
            if (_tailStyle == null)
            {
                _tailStyle = new GUIStyle(GUI.skin.box);
                _tailStyle.normal.background = MakeTex(new Color(1f, 1f, 1f, 0.92f));
                _tailStyle.border = new RectOffset(0, 0, 0, 0);
            }
            return _tailStyle;
        }

        private static Texture2D MakeTex(Color col)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, col);
            tex.Apply();
            // 跨场景重载存活：静态样式缓存的纹理若不被标记，LoadScene 后会被 Unity 卸载导致底色变透明
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }
    }
}
