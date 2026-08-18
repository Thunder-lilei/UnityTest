using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace Game.UI
{
    
    /// <summary>
    /// 升级卡片（Lua 驱动版）
    /// 不再依赖 UpgradeSystem.UpgradeType 枚举
    /// 改为使用 string action + float value 由 Lua 配置驱动
    /// </summary>
    public class UpgradeCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public TextMeshProUGUI titleText;     // 卡片标题文本
        public TextMeshProUGUI descText;      // 卡片描述文本
        public Image iconImage;               // 卡片图标（Sprite 图片）
        public Button button;                 // 卡片按钮
        public Color normalColor = new Color(0.2f, 0.2f, 0.3f, 0.9f);  // 默认背景色
        public Color hoverColor = new Color(0.4f, 0.4f, 0.6f, 1f);     // 悬浮背景色

        private Image bgImage;               // 卡片背景 Image

        /// <summary>初始化背景色</summary>
        void Start()
        {
            bgImage = GetComponent<Image>();
            if (bgImage != null)
                bgImage.color = normalColor;
        }

        /// <summary>设置卡片的标题、描述和图标（数据来自 Lua）</summary>
        /// <param name="title">标题文本</param>
        /// <param name="desc">描述文本</param>
        /// <param name="iconName">图标名称（在 Resources/Icons/ 下查找）</param>
        public void SetData(string title, string desc, string iconName)
        {
            if (titleText != null)
                titleText.text = title;
            if (descText != null)
                descText.text = desc;
            if (iconImage != null)
            {
                Sprite icon = Resources.Load<Sprite>("Icons/" + iconName);
                if (icon != null)
                {
                    iconImage.sprite = icon;
                    iconImage.enabled = true;
                }
                else
                {
                    iconImage.enabled = false;
                }
            }
        }

        /// <summary>注册点击回调，携带 action 和 value（由 Lua 配置定义）</summary>
        /// <param name="system">UpgradeSystem 实例</param>
        /// <param name="action">升级动作标识（对应 C# 方法名）</param>
        /// <param name="value">升级数值</param>
        public void SetupCallback(UpgradeSystem system, string action, float value)
        {
            if (bgImage == null)
                bgImage = GetComponent<Image>();

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    system.SelectUpgrade(action, value);
                });
            }
        }

        /// <summary>鼠标悬浮：变色 + 放大 1.05 倍</summary>
        /// <param name="eventData">指针事件数据</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (bgImage != null)
                bgImage.color = hoverColor;
            transform.localScale = Vector3.one * 1.05f;
        }

        /// <summary>鼠标离开：恢复颜色和缩放</summary>
        /// <param name="eventData">指针事件数据</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (bgImage != null)
                bgImage.color = normalColor;
            transform.localScale = Vector3.one;
        }
    }
}
