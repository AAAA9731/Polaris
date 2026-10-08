using System;

namespace Polaris.Res
{
    /// <summary>一次性的资源租约；<see cref="Dispose"/> 减少引用计数，归零时卸载资源。重复 Dispose 必须无害。</summary>
    public interface IResourceLease<out T> : IDisposable
    {
        ResourceId Id { get; }

        /// <summary>已释放抛 ObjectDisposedException；加载失败直接抛出异常。</summary>
        T Value { get; }

        bool IsDisposed { get; }

    }
}
