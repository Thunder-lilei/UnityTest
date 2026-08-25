using System.Collections.Generic;
using BiuBiu.Core;
using UnityEngine;

namespace BiuBiu.Drops
{
    /// <summary>
    /// 地面血迹管理器（设计文档 9 章战斗痕迹；数值文档 6.3 痕迹池上限 500）。
    /// v3.3 起经验块/血瓶掉落已随旧设计整体移除，本类仅保留血迹职责。
    /// 单例：场景组件（EnemyBase2D 等调用方判空安全）。
    /// </summary>
    public class DropManager : MonoBehaviour
    {
        /// <summary>场景单例（Main 场景；RuntimeSceneBuilder 装配）</summary>
        public static DropManager Instance { get; private set; }

        /// <summary>活跃血迹（FIFO 超龄销毁用）</summary>
        private readonly Queue<GameObject> stains = new Queue<GameObject>();

        /// <summary>血迹灰盒模板</summary>
        private GameObject stainTemplate;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>地面血迹（痕迹池上限 500，超出销毁最旧）</summary>
        public void SpawnStain(Vector2 pos, float size)
        {
            var go = ObjectPool.Get(StainTemplate, pos, Quaternion.identity);
            go.transform.localScale = new Vector3(size * 0.8f, size * 0.8f, 1f); // 略小于本体
            // 随机旋转打散重复感
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.value * 360f);
            stains.Enqueue(go);

            // 超龄销毁（池上限 500）
            while (stains.Count > GameBalance.MaxGroundStains)
            {
                var oldest = stains.Dequeue();
                if (oldest != null) ObjectPool.Release(oldest);
            }
        }

        /// <summary>血迹灰盒模板（暗红斑块，无行为组件）</summary>
        private GameObject StainTemplate
        {
            get
            {
                if (stainTemplate == null)
                {
                    stainTemplate = GreyBoxFactory.MakeBox("StainGreyTemplate",
                        true, new Color(0.45f, 0.1f, 0.1f, 0.8f), Vector2.one);
                    var sr = stainTemplate.GetComponent<SpriteRenderer>();
                    sr.sortingOrder = 1; // 地面(0)之上、其他一切之下
                    stainTemplate.SetActive(false);
                    // 模板留在活动场景（不 DDOL）：随 LoadScene 卸载自动清理，杜绝跨局残留
                }
                return stainTemplate;
            }
        }
    }
}
