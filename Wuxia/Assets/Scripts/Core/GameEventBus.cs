using System;
using System.Collections.Generic;

namespace Wuxia.Core
{
    /// <summary>
    /// 全局事件总线：发布/订阅模式，解耦系统间通信。
    /// AI 行为、攻击命中、玩家状态变化等均通过事件传递。
    /// </summary>
    public static class GameEventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> s_subscribers = new();

        /// <summary>
        /// 订阅指定类型的事件。
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            var type = typeof(T);
            if (!s_subscribers.ContainsKey(type))
                s_subscribers[type] = new List<Delegate>();
            s_subscribers[type].Add(handler);
        }

        /// <summary>
        /// 取消订阅指定类型的事件。
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            var type = typeof(T);
            if (s_subscribers.TryGetValue(type, out var list))
                list.Remove(handler);
        }

        /// <summary>
        /// 发布指定类型的事件，所有订阅者都会收到。
        /// </summary>
        public static void Publish<T>(T evt) where T : IGameEvent
        {
            var type = typeof(T);
            if (s_subscribers.TryGetValue(type, out var list))
            {
                var snapshot = new List<Delegate>(list);
                foreach (var handler in snapshot)
                    ((Action<T>)handler).Invoke(evt);
            }
        }

        /// <summary>
        /// 清除所有订阅（场景切换时调用）。
        /// </summary>
        public static void Clear()
        {
            s_subscribers.Clear();
        }
    }
}
