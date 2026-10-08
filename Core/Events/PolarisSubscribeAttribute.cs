using System;

namespace Polaris.Events
{
    /// <summary>
    /// 标在静态方法上，Polaris 会在所有模组 Awake 之后自动把它订阅到事件总线。
    /// 方法签名必须严格是 <c>static void Name(TEvent evt)</c>：唯一参数是事件类，返回 void；
    /// 不符合的方法会被警告并跳过，不会抛异常。需要实例方法或动态退订时请用 <see cref="EventsAPI.Subscribe{T}"/>。
    /// 因为在 Polaris 的 <c>Start</c> 里才扫描，Awake 阶段发出的事件收不到。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class PolarisSubscribeAttribute : Attribute
    {
        public EventPriority Priority { get; }

        public PolarisSubscribeAttribute(EventPriority priority = EventPriority.Normal) => Priority = priority;
    }
}
