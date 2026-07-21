using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UpgradeCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI titleText;     // 卡片标题文本
    public TextMeshProUGUI descText;      // 卡片描述文本
    public Image icon;                    // 卡片图标
    public Button button;                 // 卡片按钮
    public Color normalColor = new Color(0.2f, 0.2f, 0.3f, 0.9f);  // 默认背景色
    public Color hoverColor = new Color(0.4f, 0.4f, 0.6f, 1f);     // 悬浮背景色

    private UpgradeSystem.UpgradeType upgradeType;  // 当前卡片对应的升级类型
    private Image bgImage;               // 卡片背景 Image

    /// <summary>初始化背景色</summary>
    void Start()
    {
        bgImage = GetComponent<Image>();
        if (bgImage != null)
            bgImage.color = normalColor;
    }

    /// <summary>设置卡片的升级类型、标题和描述</summary>
    /// <param name="type">升级类型</param>
    /// <param name="title">标题文本</param>
    /// <param name="desc">描述文本</param>
    public void SetData(UpgradeSystem.UpgradeType type, string title, string desc)
    {
        upgradeType = type;
        if (titleText != null)
            titleText.text = title;
        if (descText != null)
            descText.text = desc;
    }

    /// <summary>注册点击回调到指定 UpgradeSystem</summary>
    /// <param name="system">UpgradeSystem 实例</param>
    public void SetupCallback(UpgradeSystem system)
    {
        if (bgImage == null)
            bgImage = GetComponent<Image>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                system.SelectUpgrade(upgradeType);
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