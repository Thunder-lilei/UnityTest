using UnityEngine;

namespace Game.Enemy
{
    
    /// <summary>敌人数据配置（ScriptableObject 数据驱动）</summary>
    /// <remarks>
    /// 每种敌人一个 .asset 配置文件，在 Inspector 中调整数值，无需修改代码。
    /// 创建方式：Project 窗口右键 → Create → Config → EnemyData
    /// </remarks>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Config/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("基础属性")]
        [Tooltip("敌人名称")]
        public string enemyName = "Enemy";
    
        [Tooltip("是否为 Boss")]
        public bool isBoss = false;
    
        [Header("战斗属性")]
        [Tooltip("最大血量")]
        public float maxHealth = 2f;
    
        [Tooltip("移动速度")]
        public float moveSpeed = 2.5f;
    
        [Header("外观")]
        [Tooltip("体型缩放")]
        public float scale = 1f;
    
        [Header("掉落")]
        [Tooltip("血瓶掉落概率 (0~1)")]
        [Range(0f, 1f)]
        public float dropChance = 0.3f;
    
        [Tooltip("经验掉落数量")]
        public int expDrop = 1;
    
        [Header("引用")]
        [Tooltip("敌人 Prefab")]
        public GameObject prefab;
    }
    
}
