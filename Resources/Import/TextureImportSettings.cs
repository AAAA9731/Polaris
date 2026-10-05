using UnityEngine;

namespace Polaris.Res.Import
{
    /// <summary>
    /// 纹理加载参数，由调用方直接传入，不读取旁路 JSON。
    /// <see cref="WrapMode"/> 故意改为 <c>Clamp</c>（原版用 <c>Repeat</c>）以避免图集边缘渗色。
    /// </summary>
    public sealed class TextureImportSettings
    {
        public FilterMode FilterMode = FilterMode.Point;
        public TextureWrapMode WrapMode = TextureWrapMode.Clamp;
        public bool Mipmaps = false;
        public bool Readable = false;
        public bool SRGB = true;

        public int AnisoLevel = 0;
        public TextureCompression Compress = TextureCompression.None;
    }

    /// <summary><see cref="TextureImportSettings.Compress"/> 的取值，对应 <c>Texture2D.Compress(bool)</c> 支持的两档。</summary>
    public enum TextureCompression
    {
        None,
        Normal,
        HighQuality,
    }
}
