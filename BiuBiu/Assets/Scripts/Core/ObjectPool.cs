using System.Collections.Generic;
using UnityEngine;

namespace BiuBiu.Core
{
    /// <summary>
    /// 通用 GameObject 对象池（性能预算：同屏敌人 ≤300 / 经验块 ≤150 / 弹丸 ≤80，数值文档 6.3）。
    /// 按 prefab 分池；池实例统一挂到隐藏根物体下，局结束时 ClearAll 全量清理（再来一局闭环）。
    /// 使用方式：Get(prefab, pos, rot) 取实例（池空自动 Instantiate）；Release(instance) 还实例（自动归池）。
    /// 注意：Release 传入的实例必须是本池 Get 出来的；非池实例（场景手摆对象）禁止入池。
    /// </summary>
    public static class ObjectPool
    {
        /// <summary>各 prefab 的闲置实例栈（按 prefab 实例 ID 分池）</summary>
        private static readonly Dictionary<int, Stack<GameObject>> pools = new Dictionary<int, Stack<GameObject>>();

        /// <summary>实例 → 来源 prefab 映射（Release 免传 prefab）</summary>
        private static readonly Dictionary<GameObject, GameObject> sourceMap = new Dictionary<GameObject, GameObject>();

        /// <summary>池根物体（隐藏，不渲染不参与碰撞；局结束销毁）</summary>
        private static Transform poolRoot;

        /// <summary>池根惰性创建（热重载后静态字段被清空，入口自愈——工程纪律）</summary>
        private static Transform Root
        {
            get
            {
                if (poolRoot == null)
                {
                    var go = new GameObject("[ObjectPool]");
                    go.SetActive(false); // 整树禁用：子实例不再消耗 Update/渲染
                    Object.DontDestroyOnLoad(go);
                    poolRoot = go.transform;
                }
                return poolRoot;
            }
        }

        /// <summary>
        /// 取一个池实例（池空时 Instantiate 新建）。
        /// </summary>
        /// <param name="prefab">来源 prefab（分池键）</param>
        /// <param name="position">生成位置（世界坐标）</param>
        /// <param name="rotation">生成朝向</param>
        /// <returns>已激活的实例（位置/朝向已设置，父子关系在根下）</returns>
        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            int key = prefab.GetInstanceID();
            if (!pools.TryGetValue(key, out var stack))
            {
                stack = new Stack<GameObject>();
                pools[key] = stack;
            }

            GameObject inst;
            if (stack.Count > 0)
            {
                inst = stack.Pop();
                if (inst == null)
                {
                    // 池中实例被外部销毁（极端情况）：递归取下一个
                    return Get(prefab, position, rotation);
                }
                inst.transform.SetParent(null, false);
                inst.transform.SetPositionAndRotation(position, rotation);
                inst.SetActive(true);
            }
            else
            {
                inst = Object.Instantiate(prefab, position, rotation);
                inst.transform.SetParent(Root, false); // 挂到池根（DDOL），确保 ClearAll 能清到活跃实例（飞行中未回收的弹丸）
                inst.SetActive(true); // 模板统一为禁用态（SetActive(false)+DontDestroyOnLoad），Instantiate 复制该状态——不激活则实例不可见、Update 不跑
                sourceMap[inst] = prefab;
            }
            return inst;
        }

        /// <summary>
        /// 还一个实例回池（SetActive(false) 挂回根下）。
        /// 调用方需先停掉实例上的主动行为（协程/粒子等由组件自身 OnDisable 处理）。
        /// </summary>
        /// <param name="instance">Get 出来的实例</param>
        public static void Release(GameObject instance)
        {
            if (instance == null) return;
            if (!sourceMap.TryGetValue(instance, out var prefab))
            {
                // 非本池实例（防御）：直接销毁，不入池
                Object.Destroy(instance);
                return;
            }
            instance.SetActive(false);
            instance.transform.SetParent(Root, false);
            pools[prefab.GetInstanceID()].Push(instance);
        }

        /// <summary>
        /// 清空全部池（局结束/再来一局时调用：销毁全部池实例与根物体，防止跨局残留）。
        /// 关键：除池内闲置实例外，也销毁 Root 下仍活跃（未回池）的实例——如上一局飞行中未命中回收的弹丸、
        /// 还在场上的碎片等。这些实例挂在 DontDestroyOnLoad 的池根下，LoadScene 重载场景不会销毁它们，
        /// 若不在此主动清理，会活到下一局继续 Update 并误伤玩家。
        /// </summary>
        public static void ClearAll()
        {
            if (poolRoot != null)
            {
                // 先销毁 Root 下所有活跃（未回池）子实例
                for (int i = poolRoot.childCount - 1; i >= 0; i--)
                {
                    var child = poolRoot.GetChild(i);
                    if (child != null) Object.Destroy(child.gameObject);
                }
            }
            foreach (var stack in pools.Values)
            {
                while (stack.Count > 0)
                {
                    var inst = stack.Pop();
                    if (inst != null) Object.Destroy(inst);
                }
            }
            pools.Clear();
            sourceMap.Clear();
            if (poolRoot != null)
            {
                Object.Destroy(poolRoot.gameObject);
                poolRoot = null;
            }
        }
    }
}
