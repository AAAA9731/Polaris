using System;
using System.Collections.Generic;
using Polaris.Res.Loaders;

namespace Polaris.Res.Runtime
{
    /// <summary>由 Core 的 Update 推进 PXLS 在途加载。</summary>
    internal static class PxlsPump
    {
        private static readonly List<PxlsLoadOperation> inFlight = new List<PxlsLoadOperation>();

        internal static void Enqueue(PxlsLoadOperation operation)
        {
            inFlight.Add(operation);
        }

        internal static void Advance()
        {
            // 每个 in-flight 单独 try/catch，避免一个模组的回调炸了连累其它 PXLS 的收尾。
            for (int i = inFlight.Count - 1; i >= 0; i--)
            {
                PxlsLoadOperation operation = inFlight[i];

                try
                {
                    operation.Tick();
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"[PolarisRes] PxlsLoadOperation.Tick threw an exception: {ex}");
                }

                if (operation.IsDone)
                {
                    inFlight.RemoveAt(i);
                }
            }
        }
    }
}
