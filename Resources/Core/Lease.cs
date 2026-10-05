using System;
using System.Threading;
using Polaris.Res.Runtime;

namespace Polaris.Res.Core
{
    /// <summary><see cref="IResourceLease{T}"/> 的唯一实现，被所有加载路径（同步/异步）共用。</summary>
    internal sealed class Lease<T> : IResourceLease<T>
    {
        private readonly ResourceCacheEntry entry;
        private int disposedFlag; // 0 = 活跃, 1 = 已释放

        internal Lease(ResourceCacheEntry entry)
        {
            this.entry = entry;
        }

        public ResourceId Id => entry.Id;

        public bool IsDisposed => Volatile.Read(ref disposedFlag) != 0;

        public T Value
        {
            get
            {
                if (IsDisposed)
                {
                    throw new ObjectDisposedException(nameof(Lease<T>), $"Lease already released: {entry.Id}");
                }

                return (T)entry.Value;
            }
        }

        public void Dispose()
        {
            // CAS 只可能成功一次，保证重复 Dispose（using/手动/终结器）无害。
            if (Interlocked.Exchange(ref disposedFlag, 1) != 0)
            {
                return;
            }

            GC.SuppressFinalize(this);
            ResourceCache.Release(entry);
        }

        ~Lease()
        {
            if (Volatile.Read(ref disposedFlag) != 0)
            {
                return;
            }

            // 终结器线程不能触碰 Unity 对象；把释放动作交给 Core 的主线程 Update。
            try
            {
                Plugin.Logger.LogWarning($"[PolarisRes] Detected an unreleased lease (reclaimed by the finalizer): {entry.Id}");
            }
            catch
            {
                // 终结器里绝不能再抛异常。
            }

            MainThreadDispatcher.Enqueue(() => ResourceCache.Release(entry));
        }
    }
}
