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
