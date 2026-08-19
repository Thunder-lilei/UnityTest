using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Audio;
using Game.Combat;
using Game.Player;
using Game.Systems;

namespace Game.UI
{
    
    /// <summary>
    /// 升级选择系统（Lua 驱动版）
    /// 升级数据从 upgrades.lua 加载，C# 只负责执行
    /// 新增升级类型只需改 Lua 文件，无需改 C# 代码
    /// </summary>
    public class UpgradeSystem : MonoBehaviour
    {
        [Header("UI 引用")]
        public GameObject upgradePanel;
        public UpgradeCard[] cards;

        // 拆分后引用各子系统组件
        private PlayerMovement playerMovement;
        private PlayerCombat playerCombat;
        private MeleeCombat meleeCombat;
        private HealthBar healthBar;
        private MagnetDetector magnetDetector;
        private ShockwaveEffect shockwaveEffect;

        /// <summary>缓存各组件引用</summary>
        void Start()
        {
            playerMovement = GetComponent<PlayerMovement>();
            playerCombat = GetComponent<PlayerCombat>();
            meleeCombat = GetComponent<MeleeCombat>();
            healthBar = GetComponent<HealthBar>();
            magnetDetector = GetComponentInChildren<MagnetDetector>();
            shockwaveEffect = GetComponent<ShockwaveEffect>();
        }

        /// <summary>
        /// 显示升级面板：先播放升级冲击波特效，等待播放完毕后再弹出升级选择面板
        /// </summary>
        public void ShowUpgrades()
        {
            StartCoroutine(ShowUpgradesAfterShockwave());
        }

        /// <summary>
        /// 冲击波播放完毕后显示升级面板的协程
        /// </summary>
        private IEnumerator ShowUpgradesAfterShockwave()
        {
            // 先触发冲击波（Time.timeScale 仍为 1，确保物理和动画正常）
            if (shockwaveEffect != null)
                shockwaveEffect.Trigger();

            // 暂停各子系统的输入和 AI，但保持 Time.timeScale=1 让冲击波动画播放完
            if (playerMovement != null) playerMovement.SetPaused(true);
            if (playerCombat != null) playerCombat.SetPaused(true);
            if (meleeCombat != null) meleeCombat.SetPaused(true);

            // 等待冲击波特效播放完毕（手动计时，不受 timeScale 影响）
            float waitTime = shockwaveEffect != null ? shockwaveEffect.expandDuration : 1f;
            float timer = 0f;
            while (timer < waitTime)
            {
                timer += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
                yield return null;
            }

            // 冲击波播放完毕，暂停游戏并显示升级面板
            if (shockwaveEffect != null) shockwaveEffect.SetPaused(true);
            Time.timeScale = 0;
            upgradePanel.SetActive(true);

            // 从 Lua 获取随机升级
            List<UpgradeData> selected = LuaManager.Instance?.GetRandomUpgrades(cards.Length);

            if (selected == null || selected.Count == 0)
            {
                Debug.LogError("[UpgradeSystem] Lua 返回的升级列表为空");
                yield break;
            }

            // 填充卡片
            for (int i = 0; i < cards.Length && i < selected.Count; i++)
            {
                UpgradeData data = selected[i];
                cards[i].SetData(data.title, data.desc, data.iconName);
                cards[i].SetupCallback(this, data.action, data.value);
            }

            // 显示鼠标供玩家选择升级卡片
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        /// <summary>
        /// 应用选中的升级效果并恢复游戏
        /// action 字符串由 Lua 配置定义，在此分发到对应 C# 方法
        /// </summary>
        /// <param name="action">Lua 配置中的 action 字段</param>
        /// <param name="value">Lua 配置中的 value 字段</param>
        public void SelectUpgrade(string action, float value)
        {
            ApplyAction(action, value);

            AudioManager.Instance?.PlayUpgradeConfirm();
            Time.timeScale = 1;
            if (playerMovement != null) playerMovement.SetPaused(false);
            if (playerCombat != null) playerCombat.SetPaused(false);
            if (meleeCombat != null) meleeCombat.SetPaused(false);
            if (shockwaveEffect != null) shockwaveEffect.SetPaused(false);
            upgradePanel.SetActive(false);

            // 隐藏鼠标恢复游戏操作
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        /// <summary>
        /// 根据 action 字符串分发到对应 C# 方法
        /// 新增升级时只需在 Lua 中添加 action 名，并在此添加 case
        /// </summary>
        private void ApplyAction(string action, float value)
        {
            switch (action)
            {
                case "IncreaseMaxHealth":
                    if (healthBar != null)
                        healthBar.IncreaseMaxHealth(value);
                    break;
                case "AddSpeed":
                    if (playerMovement != null)
                        playerMovement.speed += value;
                    break;
                case "AddFireballCount":
                    if (playerCombat != null)
                        playerCombat.fireballCount += (int)value;
                    break;
                case "IncreaseMagnetRadius":
                    if (magnetDetector != null)
                        magnetDetector.IncreaseRadius(value);
                    break;
                case "ReduceFireInterval":
                    if (playerCombat != null)
                        playerCombat.ReduceFireInterval(value);
                    break;
                case "IncreaseMeleeDamage":
                    if (meleeCombat != null)
                        meleeCombat.slashDamage += value;
                    break;
                case "ReduceSlashInterval":
                    if (meleeCombat != null)
                        meleeCombat.ReduceSlashInterval(value);
                    break;
                default:
                    Debug.LogWarning($"[UpgradeSystem] 未知升级 action: {action}，请在 ApplyAction 中添加对应 case");
                    break;
            }
        }
    }
}
