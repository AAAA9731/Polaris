namespace Polaris.Res.Import
{
    public sealed class PxlsImportSettings
    {
        public float PixelsPerUnit = 64f;
        public bool AutoFlipX = true;

        /// <summary>
        /// 登记到游戏里的角色名（<c>PxlsLoader.getPxlCharacter</c> 按它找）。缺省是 Core 生成的 <c>pr:&lt;模组&gt;/&lt;路径&gt;</c>，不会和别的模组撞名。
        /// 需要沿用游戏里按名字查找的流程（比如 <c>PrPoseContainer</c>）时填原名；同名已被占用则加载失败。
        /// </summary>
        public string Title;

        /// <summary>外部图集纹理的导入设置；缺省与 <see cref="TextureImportSettings"/> 默认相同（Point 过滤）。原版素材是 Bilinear，复刻原版素材时按需改。</summary>
        public TextureImportSettings Texture = new TextureImportSettings();
    }
}
