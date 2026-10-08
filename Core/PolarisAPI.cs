namespace Polaris
{
    public static partial class PolarisAPI
    {
        /// <summary>主菜单按钮相关 API。</summary>
        public static MainMenuAPI MainMenu { get; } = new();

        /// <summary>设置项相关 API：声明的设置项会渲染进原版设置界面并自动持久化。</summary>
        public static Settings.SettingsAPI Settings { get; } = new();

        /// <summary>本地化 resolver 注册表：注册 key→文案回调，供原版 <c>TX.Get</c> 优先采用。</summary>
        public static Localization.LocalizationAPI Localization { get; } = new();

        /// <summary>事件总线：按类型订阅与派发，内置 <see cref="Events.SaveLoaded"/> 等游戏事件，模组也可自己 Post。</summary>
        public static Events.EventsAPI Events { get; } = new();

        /// <summary>Harmony 补丁的安全应用：逐类应用，坏一个只报错跳过。见 <see cref="Infra.PatchingAPI"/>。</summary>
        public static Infra.PatchingAPI Patching { get; } = new();

        // ── 以下是全库共用的基础设施，与任何单一子系统的领域无关（领域概念应去 UIAPI / ResAPI）。

        /// <summary>BepInEx 已加载插件的只读视图；软依赖判断走 <see cref="Infra.ModulesAPI.IsLoaded"/>。</summary>
        public static Infra.ModulesAPI Modules { get; } = new();

        /// <summary>Polaris 系列约定的目录结构。见 <see cref="Infra.PathsAPI"/>。</summary>
        public static Infra.PathsAPI Paths { get; } = new();

        /// <summary>全系列唯一的类型扫描器，带缓存与 <c>ReflectionTypeLoadException</c> 兜底。</summary>
        public static Infra.TypesAPI Types { get; } = new();

        /// <summary>错误上报：默认只写日志，装了 Polaris 管理器后由它做归因并写出报告。</summary>
        public static Infra.ErrorsAPI Errors { get; } = new();

    }
}
