using System;
using System.Collections.Concurrent;

namespace Polaris.Res.Runtime
{
    /// <summary>把租约终结器的释放动作排到主线程；出队仅在 Core 的 Update 中执行。</summary>
    internal static class MainThreadDispatcher
    {
        private static readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();

        /// <summary>从任意线程调用，把一个动作排队到下一次主线程 Drain。</summary>
        internal static void Enqueue(Action action)
        {
            if (action == null)
            {
                return;
            }

            queue.Enqueue(action);
        }

        /// <summary>由 Core 的主线程 Update 调用。</summary>
        internal static void Drain()
        {
            // 用计数上限而非无限循环，防止排队动作又排新动作导致死循环。
            int budget = 4096;
            while (budget-- > 0 && queue.TryDequeue(out Action action))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    LibPlugin.Logger.LogError($"[PolarisRes] An action in the main-thread dispatch queue threw an exception: {ex}");
                }
            }
        }
    }
}
