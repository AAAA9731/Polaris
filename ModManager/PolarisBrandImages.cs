using System;
using System.IO;
using UnityEngine;
using XX;

namespace Polaris
{
    /// <summary>Polaris 随包分发的自带图片，直接从 <see cref="Infra.PathsAPI.PolarisRoot"/> 读 PNG。</summary>
    internal static class PolarisBrandImages
    {
        const string LogoName = "polaris_icon";
        const string StarName = "polaris_star";

        static bool logoResolved;
        static MImage logo;
        static bool starResolved;
        static MImage star;

        /// <summary>Polaris 的 logo；图片缺失或读取失败时返回 null（纯装饰，调用方整行跳过）。</summary>
        internal static MImage Logo
        {
            get
            {
                if (!logoResolved)
                {
                    logoResolved = true;
                    logo = Load(LogoName);
                }
                return logo;
            }
        }

        /// <summary>八芒星剪影（深墨色、透明底），用在设置界面的 Polaris 分区标题上；图片缺失时返回 null，调用方整行跳过。</summary>
        internal static MImage Star
        {
            get
            {
                if (!starResolved)
                {
                    starResolved = true;
                    star = Load(StarName);
                }

                return star;
            }
        }

        static MImage Load(string name)
        {
            string path = Path.Combine(PolarisAPI.Paths.PolarisRoot, name + ".png");
            if (!File.Exists(path))
            {
                Plugin.Logger.LogInfo($"[Polaris] Bundled image {name}.png was not found in {PolarisAPI.Paths.PolarisRoot}; the UI that uses it is skipped.");
                return null;
            }

            try
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path)))
                {
                    return null;
                }
                return new MImage(texture);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[Polaris] Failed to load the bundled image {name}.png: {ex.Message}");
                return null;
            }
        }
    }
}
