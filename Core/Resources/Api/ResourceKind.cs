namespace Polaris.Res
{
    /// <summary>资源缓存中的对象种类；加载路径必须包含扩展名。</summary>
    public enum ResourceKind
    {
        /// <summary>原始字节，路径必须自带扩展名（不做任何探测）。</summary>
        Bytes,

        /// <summary>裸 <c>UnityEngine.Texture2D</c>。</summary>
        Texture,

        /// <summary>包了材质缓存的 <c>XX.MImage</c>。</summary>
        Image,

        /// <summary>PixelLiner 角色（<c>.pxls</c>/<c>.pxl</c>）。</summary>
        Pxls,

        /// <summary>原始音频（<c>.wav</c>/<c>.ogg</c>）。</summary>
        Audio,
    }
}
