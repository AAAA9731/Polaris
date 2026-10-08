using System;
using System.Reflection;

namespace Polaris.Events
{
    /// <summary>扫描插件与 Polaris 组件里标了 <see cref="PolarisSubscribeAttribute"/> 的静态方法并订阅；只在 <c>Plugin.Start</c> 调一次。</summary>
    internal static class EventAttributeScanner
    {
        const BindingFlags Flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        static readonly MethodInfo SubscribeDefinition = typeof(EventsAPI).GetMethod(nameof(EventsAPI.Subscribe));

        static bool scanned;

        internal static void ScanAll()
        {
            if (scanned)
            {
                return;
            }

            scanned = true;

            int count = 0;
            foreach (Type type in PolarisAPI.Types.InModules())
            {
                if (type.IsGenericTypeDefinition)
                {
                    continue;
                }

                foreach (MethodInfo method in SafeMethods(type))
                {
                    var attr = (PolarisSubscribeAttribute)Attribute.GetCustomAttribute(method, typeof(PolarisSubscribeAttribute), false);
                    if (attr != null && TrySubscribe(type, method, attr))
                    {
                        count++;
                    }
                }
            }

            if (count > 0)
            {
                CorePlugin.Logger.LogMessage($"[Polaris.Events] Subscribed {count} handlers from attributes.");
            }
        }

        static MethodInfo[] SafeMethods(Type type)
        {
            try
            {
                return type.GetMethods(Flags);
            }
            catch (Exception)
            {
                // 缺可选依赖的类型取不出方法，跳过它而不是让整个扫描失败。
                return Array.Empty<MethodInfo>();
            }
        }

        static bool TrySubscribe(Type owner, MethodInfo method, PolarisSubscribeAttribute attr)
        {
            string name = owner.FullName + "." + method.Name;
            ParameterInfo[] parameters = method.GetParameters();

            if (method.ReturnType != typeof(void)
                || parameters.Length != 1
                || parameters[0].ParameterType.IsByRef
                || !parameters[0].ParameterType.IsClass
                || method.IsGenericMethodDefinition)
            {
                CorePlugin.Logger.LogWarning(
                    $"[Polaris.Events] {name} is marked PolarisSubscribe but its signature is not 'static void M(TEvent evt)' with a class-typed event; skipped.");
                return false;
            }

            try
            {
                Type eventType = parameters[0].ParameterType;
                Delegate handler = Delegate.CreateDelegate(typeof(Action<>).MakeGenericType(eventType), method);
                SubscribeDefinition.MakeGenericMethod(eventType).Invoke(PolarisAPI.Events, new object[] { handler, attr.Priority });
                return true;
            }
            catch (Exception e)
            {
                PolarisAPI.Errors.Report(e.InnerException ?? e, "subscribing " + name, owner.Assembly);
                return false;
            }
        }
    }
}
