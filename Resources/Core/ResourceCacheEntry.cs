using System;

namespace Polaris.Res.Core
{
    /// <summary>
    /// <see cref="ResourceCache"/> 主表里的一条记录，是所有加载路径共用的同一个类型。
    /// </summary>
    internal sealed class ResourceCacheEntry
    {
        internal ResourceId Id;

        /// <summary>缓存的 byte[]、Texture2D、MImage、PXLS 句柄或 AudioClip。</summary>
        internal object Value;

        internal int RefCount;


        /// <summary>卸载时调用的清理动作（比如销毁 Texture2D）。</summary>
        internal Action Unloader;
    }
}
