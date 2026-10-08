using System;
using System.Reflection;

namespace Polaris.Infra
{
    /// <summary>
    /// 错误上报，从 <see cref="PolarisAPI.Errors"/> 取：主动上报异常，或用
    /// <see cref="Guard(Action, string, Assembly)"/> 包一层调用别人的代码，异常就地上报并吞掉。
    /// 没有后端时只写 BepInEx 日志；装了 Polaris 管理器则由它做归因与报告。
    /// </summary>
    public sealed class ErrorsAPI
    {
        sealed class NullScope : IDisposable
        {
            internal static readonly NullScope Instance = new NullScope();

            public void Dispose() { }
        }

        volatile IErrorBackend backend;

        internal ErrorsAPI() { }

        /// <summary>接入错误后端；同一时刻只有一个，后来者替换先前的。传 null 恢复为只写日志。</summary>
        public void SetBackend(IErrorBackend value) => backend = value;

        /// <summary>上报一个异常，责任方由后端推断；<paramref name="context"/> 是给人看的一句话。</summary>
        public void Report(Exception exception, string context = null) => Report(exception, context, null);

        /// <summary>上报一个异常并直接点名责任方——调用方已知是谁的错时用这个。</summary>
        /// <param name="culprit">责任方所在的程序集，通常是 <c>someObject.GetType().Assembly</c>。</param>
        public void Report(Exception exception, string context, Assembly culprit)
        {
            if (exception == null)
            {
                return;
            }

            IErrorBackend active = backend;
            if (active == null)
            {
                try
                {
                    CorePlugin.Logger?.LogError(
                        $"[{culprit?.GetName().Name ?? "Polaris"}] {context}: {exception}");
                }
                catch (Exception)
                {
                    // 日志器不可用时无处可报。
                }

                return;
            }

            try
            {
                active.Report(exception, context, culprit);
            }
            catch (Exception)
            {
                // 后端出错不能连累调用方。
            }
        }

        /// <summary>
        /// 安全地执行一段代码：抛异常就上报并吞掉，返回是否执行成功。
        /// <paramref name="culprit"/> 留空时按 <paramref name="action"/> 自己所在的程序集算账。
        /// </summary>
        public bool Guard(Action action, string context, Assembly culprit = null)
        {
            if (action == null)
            {
                return true;
            }

            try
            {
                using (Activity(context, culprit))
                {
                    action();
                }

                return true;
            }
            catch (Exception ex)
            {
                Report(ex, context, culprit ?? OwnerOf(action));
                return false;
            }
        }

        /// <summary><see cref="Guard(Action, string, Assembly)"/> 的有返回值版本；出错时返回 <paramref name="fallback"/>。</summary>
        public T Guard<T>(Func<T> func, T fallback, string context, Assembly culprit = null)
        {
            if (func == null)
            {
                return fallback;
            }

            try
            {
                using (Activity(context, culprit))
                {
                    return func();
                }
            }
            catch (Exception ex)
            {
                Report(ex, context, culprit ?? OwnerOf(func));
                return fallback;
            }
        }

        /// <summary>标记"这段时间在执行谁的代码"。没有后端时是空操作。</summary>
        public IDisposable Activity(string context, Assembly culprit = null)
        {
            try
            {
                return backend?.Activity(context, culprit) ?? NullScope.Instance;
            }
            catch (Exception)
            {
                return NullScope.Instance;
            }
        }

        internal void CallbackInvoked(string ownerName, string context, double milliseconds, bool threw)
        {
            try
            {
                backend?.CallbackInvoked(ownerName, context, milliseconds, threw);
            }
            catch (Exception)
            {
            }
        }

        static Assembly OwnerOf(Delegate action)
        {
            try
            {
                return action.Method?.DeclaringType?.Assembly;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
