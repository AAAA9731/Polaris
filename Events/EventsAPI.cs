using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Polaris.Events
{
    /// <summary>
    /// 最小事件总线：按事件的精确类型订阅与派发，事件就是普通类。
    /// 只在 Unity 主线程使用（不加锁）；回调里订阅、退订、再次 <see cref="Post{T}"/> 都安全。
    /// 单个订阅者抛异常只会被记到它所属的模组头上，不影响其余订阅者。
    /// </summary>
    public sealed class EventsAPI
    {
        /// <summary>防止 A 事件回调里 Post B、B 回调里又 Post A 的无限递归。</summary>
        const int MaxDepth = 16;

        readonly Dictionary<Type, Subscription[]> map = new Dictionary<Type, Subscription[]>();
        int depth;

        /// <summary>订阅事件 <typeparamref name="T"/>；返回的对象 Dispose 即退订。</summary>
        public IDisposable Subscribe<T>(Action<T> handler, EventPriority priority = EventPriority.Normal)
            where T : class
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Assembly owner = handler.Method.DeclaringType?.Assembly;
            var sub = new Subscription(this, typeof(T), o => handler((T)o), priority, owner, typeof(T).Name);

            map.TryGetValue(typeof(T), out Subscription[] current);
            current = current ?? Array.Empty<Subscription>();

            // 写时复制：派发中的快照不受影响。插到所有 priority 不大于它的订阅者之后，同级保持先来先到。
            int at = current.Length;
            while (at > 0 && current[at - 1].Priority > priority)
            {
                at--;
            }

            var next = new Subscription[current.Length + 1];
            Array.Copy(current, 0, next, 0, at);
            next[at] = sub;
            Array.Copy(current, at, next, at + 1, current.Length - at);
            map[typeof(T)] = next;
            return sub;
        }

        /// <summary>同步派发事件给所有订阅者。没有订阅者时几乎零开销。</summary>
        public void Post<T>(T evt) where T : class
        {
            if (evt == null)
            {
                throw new ArgumentNullException(nameof(evt));
            }

            if (!map.TryGetValue(typeof(T), out Subscription[] snapshot) || snapshot.Length == 0)
            {
                return;
            }

            if (depth >= MaxDepth)
            {
                PolarisAPI.Errors.Report(
                    new InvalidOperationException($"Event '{typeof(T).Name}' posted recursively more than {MaxDepth} levels deep; dropped."),
                    "Events.Post");
                return;
            }

            depth++;
            try
            {
                for (int i = 0; i < snapshot.Length; i++)
                {
                    Subscription sub = snapshot[i];
                    if (!sub.Removed)
                    {
                        sub.Invoke(evt);
                    }
                }
            }
            finally
            {
                depth--;
            }
        }

        void Remove(Subscription sub)
        {
            if (!map.TryGetValue(sub.EventType, out Subscription[] current))
            {
                return;
            }

            int at = Array.IndexOf(current, sub);
            if (at < 0)
            {
                return;
            }

            var next = new Subscription[current.Length - 1];
            Array.Copy(current, 0, next, 0, at);
            Array.Copy(current, at + 1, next, at, current.Length - at - 1);
            map[sub.EventType] = next;
        }

        sealed class Subscription : IDisposable
        {
            readonly EventsAPI bus;
            readonly Action<object> handler;
            readonly Assembly owner;
            readonly string context;
            readonly string ownerName;
            int removed;

            internal readonly Type EventType;
            internal readonly EventPriority Priority;

            internal Subscription(EventsAPI bus, Type eventType, Action<object> handler, EventPriority priority, Assembly owner, string eventName)
            {
                this.bus = bus;
                this.handler = handler;
                this.owner = owner;
                EventType = eventType;
                Priority = priority;
                context = "Event: " + eventName;
                ownerName = owner?.GetName().Name ?? "unknown";
            }

            internal bool Removed => Volatile.Read(ref removed) != 0;

            internal void Invoke(object evt)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                using (Diagnostics.DiagnosticsHost.Activity(context, owner))
                {
                    try
                    {
                        handler(evt);
                    }
                    catch (Exception ex)
                    {
                        PolarisAPI.Errors.Report(ex, context, owner);
                        Diagnostics.DiagnosticsHost.RecordCallbackException(ownerName, context);
                    }
                }

                Diagnostics.DiagnosticsHost.RecordCallbackInvocation(ownerName, context, stopwatch.Elapsed.TotalMilliseconds);
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref removed, 1) == 0)
                {
                    bus.Remove(this);
                }
            }
        }
    }
}
