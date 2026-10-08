using System;
using System.IO;
using PixelLiner;
using Polaris.Res.Core;
using Polaris.Res.Import;
using Polaris.Res.Loaders;
using Polaris.Res.Pxls;
using Polaris.Res.Runtime;
using UnityEngine;

namespace Polaris.Res
{
    /// <summary>一个模组的固定资源目录；路径须包含扩展名，资源通过 Dispose 释放。</summary>
    public sealed class ModResources
    {
        private readonly string root;

        public string ModId { get; }

        internal ModResources(string modId, string rootPath)
        {
            ModId = modId;
            root = Path.GetFullPath(rootPath);
        }

        public IResourceLease<byte[]> Bytes(string path)
        {
            ResourceId id = new ResourceId(ModId, ResourceKind.Bytes, path);
            return ResourceCache.AcquireSync<byte[]>(id, () => (LoadBytes(id, out _), null));
        }

        public IResourceLease<Texture2D> Texture(string path, TextureImportSettings settings = null)
        {
            ResourceId id = new ResourceId(ModId, ResourceKind.Texture, path);
            return ResourceCache.AcquireSync<Texture2D>(id, () =>
            {
                byte[] bytes = LoadBytes(id, out _);
                Texture2D texture = TextureLoader.FromBytes(bytes, id, settings ?? new TextureImportSettings());
                return (texture, (Action)(() => UnityEngine.Object.DestroyImmediate(texture)));
            });
        }

        public IResourceLease<XX.MImage> Image(string path, TextureImportSettings settings = null)
        {
            ResourceId id = new ResourceId(ModId, ResourceKind.Image, path);
            return ResourceCache.AcquireSync<XX.MImage>(id, () =>
            {
                IResourceLease<Texture2D> textureLease = Texture(path, settings);
                XX.MImage image;
                try
                {
                    image = new XX.MImage(textureLease.Value) { dispose_texture = false };
                }
                catch
                {
                    textureLease.Dispose();
                    throw;
                }

                void Unload()
                {
                    image.DisposeMaterial();
                    image.Dispose();
                    textureLease.Dispose();
                }

                return (image, (Action)Unload);
            });
        }

        /// <summary>游戏素材加载完成后调用；Value 返回句柄，解析完成后触发句柄的 Ready/Faulted。</summary>
        public IResourceLease<PxlsCharacterHandle> Pxls(string path, PxlsImportSettings settings = null)
        {
            ResourceId id = new ResourceId(ModId, ResourceKind.Pxls, path);
            return ResourceCache.AcquireSync<PxlsCharacterHandle>(id, () =>
            {
                if (PolarisAPI.Game.Assets.LoadStage != 7)
                {
                    throw new InvalidOperationException("PXLS must be loaded after the game assets are ready.");
                }

                byte[] bytes = LoadBytes(id, out string absolutePath);
                settings ??= new PxlsImportSettings();
                string title = string.IsNullOrEmpty(settings.Title) ? PxlsNaming.BuildTitle(ModId, id.Path) : settings.Title;
                PxlCharacter character = PxlsLoader.loadCharacterASync(title, bytes, null, settings.PixelsPerUnit, settings.AutoFlipX);
                if (character == null)
                {
                    throw new ResourceLoadException(id, $"PXLS load failed: title \"{title}\" already exists.");
                }

                character.no_load_external_texture_on_first = true;
                PxlsCharacterHandle handle = new PxlsCharacterHandle(id, title);
                PxlsLoadOperation operation = new PxlsLoadOperation(
                    handle, character, absolutePath, title, settings.Texture ?? new TextureImportSettings());
                PxlsPump.Enqueue(operation);
                return (handle, (Action)operation.RequestDispose);
            });
        }

        /// <summary>加载 WAV/OGG；播放交给调用方的 AudioSource。</summary>
        public IResourceLease<AudioClip> Audio(string path)
        {
            ResourceId id = new ResourceId(ModId, ResourceKind.Audio, path);
            return ResourceCache.AcquireSync<AudioClip>(id, () =>
            {
                byte[] bytes = LoadBytes(id, out string absolutePath);
                AudioClip clip = AudioLoader.FromBytes(bytes, absolutePath, id);
                return (clip, (Action)(() => UnityEngine.Object.DestroyImmediate(clip)));
            });
        }

        /// <summary>
        /// 把本模组目录下的 <paramref name="sourceDir"/> 同步到游戏资源目录 <c>StreamingAssets/&lt;gameDir&gt;</c>，让游戏自己的加载器
        /// （<c>MTRX.loadMtiPxc</c>、<c>MTI.LoadContainerOneImage</c> 这类只认 StreamingAssets 的）能读到模组带的文件。
        /// 只复制新增或有变化的文件（比较大小和修改时间），没变的跳过，所以每次启动调用都很便宜；不会删除目标里多出来的文件。
        /// 某个文件被占用等原因复制失败只记警告，其余照常。
        /// </summary>
        /// <param name="sourceDir">相对模组根目录的源文件夹，空串表示根目录本身</param>
        /// <param name="gameDir">相对 StreamingAssets 的目标文件夹，如 <c>"MyModRes/pxls"</c></param>
        /// <returns>目标文件夹的绝对路径；源文件夹不存在时返回 null</returns>
        /// <exception cref="ArgumentException">路径越出模组目录或 StreamingAssets 时抛出</exception>
        public string MountToGame(string sourceDir, string gameDir)
        {
            string source = PathSandbox.Sanitize(root, Path.Combine(root, sourceDir ?? ""));
            string streaming = Path.GetFullPath(Application.streamingAssetsPath);
            string target = PathSandbox.Sanitize(streaming, Path.Combine(streaming, gameDir ?? ""));
            if (source == null)
            {
                throw new ArgumentException($"Source directory leaves the mod directory: {sourceDir}", nameof(sourceDir));
            }

            if (target == null || string.Equals(target, streaming, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Target directory must be a subfolder of StreamingAssets: {gameDir}", nameof(gameDir));
            }

            if (!Directory.Exists(source))
            {
                return null;
            }

            int copied = 0;
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string destination = Path.Combine(target, file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                try
                {
                    FileInfo from = new FileInfo(file);
                    FileInfo to = new FileInfo(destination);
                    if (to.Exists && to.Length == from.Length && to.LastWriteTimeUtc == from.LastWriteTimeUtc)
                    {
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    File.Copy(file, destination, true);
                    File.SetLastWriteTimeUtc(destination, from.LastWriteTimeUtc);
                    copied++;
                }
                catch (Exception ex)
                {
                    CorePlugin.Logger?.LogWarning($"[PolarisRes] Failed to mount {file} to the game directory: {ex.Message}");
                }
            }

            if (copied > 0)
            {
                CorePlugin.Logger?.LogInfo($"[PolarisRes] Mounted {copied} file(s) of {ModId} into StreamingAssets/{gameDir}.");
            }

            return target;
        }

        private byte[] LoadBytes(ResourceId id, out string absolutePath)
        {
            absolutePath = PathSandbox.Sanitize(root, Path.Combine(root, id.Path));
            if (absolutePath == null)
            {
                throw new ResourceLoadException(id, $"Resource path leaves its directory: {id.Path}");
            }

            try
            {
                return File.ReadAllBytes(absolutePath);
            }
            catch (Exception ex)
            {
                throw new ResourceLoadException(id, $"Failed to read file: {absolutePath}", ex);
            }
        }
    }
}
