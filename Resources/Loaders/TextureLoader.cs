using System;
using Polaris.Res.Import;
using UnityEngine;

namespace Polaris.Res.Loaders
{
    /// <summary>
    /// 从 PNG/JPG 字节构造 Texture2D；参数直接由调用方提供。
    /// 默认 Clamp 避免图集边缘渗色；LoadImage 按图像内容决定像素格式。
    /// </summary>
    internal static class TextureLoader
    {
        internal static Texture2D FromBytes(byte[] bytes, ResourceId id, TextureImportSettings settings)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, mipChain: settings.Mipmaps, linear: !settings.SRGB)
            {
                filterMode = settings.FilterMode,
                wrapMode = settings.WrapMode,
                anisoLevel = settings.AnisoLevel,
            };

            bool ok;
            try
            {
                ok = texture.LoadImage(bytes, markNonReadable: false);
            }
            catch (Exception ex)
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new ResourceLoadException(id, $"Failed to decode image: {id}", ex);
            }

            if (!ok)
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new ResourceLoadException(id, $"Not valid PNG/JPG data: {id}");
            }

            if (settings.Compress != TextureCompression.None)
            {
                // Compress 要求纹理仍可读；必须在下面 Apply(makeNoLongerReadable) 之前做。
                try
                {
                    texture.Compress(highQuality: settings.Compress == TextureCompression.HighQuality);
                }
                catch (Exception ex)
                {
                    // 压缩失败（如尺寸非 4 的倍数）不应让整张纹理加载失败，跳过即可。
                    Plugin.Logger.LogWarning($"[PolarisRes] {id} failed to compress; skipped: {ex.Message}");
                }
            }

            texture.Apply(updateMipmaps: settings.Mipmaps, makeNoLongerReadable: !settings.Readable);
            texture.name = id.Path;
            return texture;
        }
    }
}
