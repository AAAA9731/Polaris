using System;

namespace Polaris.Res
{
    /// <summary>资源被找到，但解析/构造过程本身失败（文件损坏、格式不对等）。</summary>
    public sealed class ResourceLoadException : Exception
    {
        public ResourceId Id { get; }

        public ResourceLoadException(ResourceId id, string message, Exception inner = null)
            : base(message, inner)
        {
            Id = id;
        }
    }
}
