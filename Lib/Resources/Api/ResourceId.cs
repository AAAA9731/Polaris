using System;

namespace Polaris.Res
{
    /// <summary>
    /// 一个资源的逻辑身份：模组命名空间 + 种类 + 相对路径。
    /// 构造时规范化斜杠与大小写；路径必须包含扩展名。
    /// </summary>
    public readonly struct ResourceId : IEquatable<ResourceId>
    {
        public string ModId { get; }
        public ResourceKind Kind { get; }
        public string Path { get; }

        public ResourceId(string modId, ResourceKind kind, string path)
        {
            if (string.IsNullOrEmpty(modId))
            {
                throw new ArgumentException("modId cannot be empty.", nameof(modId));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("path cannot be empty.", nameof(path));
            }

            ModId = modId;
            Kind = kind;
            Path = Normalize(path);
        }

        private static string Normalize(string path)
        {
            string p = path.Replace('\\', '/').Trim().Trim('/');
            while (p.Contains("//"))
            {
                p = p.Replace("//", "/");
            }

            return p.ToLowerInvariant();
        }

        public bool Equals(ResourceId other) =>
            Kind == other.Kind
            && string.Equals(ModId, other.ModId, StringComparison.Ordinal)
            && string.Equals(Path, other.Path, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is ResourceId other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(ModId, Kind, Path);

        public override string ToString() => $"{ModId}:{Kind}:{Path}";

        public static bool operator ==(ResourceId left, ResourceId right) => left.Equals(right);
        public static bool operator !=(ResourceId left, ResourceId right) => !left.Equals(right);
    }
}
