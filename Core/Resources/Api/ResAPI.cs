using System;
using System.Collections.Generic;

namespace Polaris.Res
{
    /// <summary>Core 中的资源加载入口，不扫描或挂载目录。</summary>
    public static class ResAPI
    {
        private static readonly Dictionary<string, ModResources> registry = new Dictionary<string, ModResources>();

        /// <summary>取得（或创建）某模组的资源句柄；同一个 <paramref name="modId"/> 永远返回同一实例，资源根目录由首次调用确定。</summary>
        public static ModResources For(string modId, string rootPath)
        {
            if (string.IsNullOrEmpty(modId))
            {
                throw new ArgumentException("modId cannot be empty.", nameof(modId));
            }

            if (!registry.TryGetValue(modId, out ModResources resources))
            {
                resources = new ModResources(modId, rootPath);
                registry[modId] = resources;
            }

            return resources;
        }

    }
}
