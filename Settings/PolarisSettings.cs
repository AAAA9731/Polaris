using Polaris.Localization;

namespace Polaris.Settings
{
    /// <summary>
    /// Polaris 自己暴露给玩家的设置项，出现在游戏"设置"界面的“常规”页尾部。只放玩家真会想改的偏好；
    /// 排障用的阈值旋钮在 <see cref="Diagnostics.DiagnosticsConfig"/> 里单独管理（看门狗须在 <c>Awake</c> 前带阈值起跑，等不到这里的特性扫描）。
    /// 这些值在 <c>Plugin.Start</c> 的扫描之前都是下面的默认值，之后才是玩家存的值。
    /// </summary>
    [PolarisSettingGroup("polaris", "Polaris", Order = -100)]
    internal static class PolarisSettings
    {
        [PolarisSetting(PolarisStrings.TitleVersionLine, Desc = PolarisStrings.TitleVersionLineDesc, Order = 10)]
        public static bool ShowTitleVersionLine = true;

        [PolarisSetting(PolarisStrings.ErrorNotice, Desc = PolarisStrings.ErrorNoticeDesc, Order = 20)]
        public static bool ShowErrorNotice = true;

        [PolarisSetting(PolarisStrings.Alerts, Desc = PolarisStrings.AlertsDesc, Order = 30)]
        public static bool ShowInGameAlerts = true;

        /// <summary>0 = 全部（含轻微），1 = 仅持续及以上。</summary>
        [PolarisSetting(PolarisStrings.AlertLevel, Desc = PolarisStrings.AlertLevelDesc, Order = 40,
            Choices = new[] { PolarisStrings.AlertLevelAll, PolarisStrings.AlertLevelPersistent })]
        public static int AlertMinLevel = 0;

        [PolarisSetting(PolarisStrings.AlertSeconds, Desc = PolarisStrings.AlertSecondsDesc, Order = 50, Min = 3, Max = 20, Step = 1)]
        public static float AlertSeconds = 8f;

        /// <summary>0 右上，1 左上，2 右下，3 左下。</summary>
        [PolarisSetting(PolarisStrings.AlertCorner, Desc = PolarisStrings.AlertCornerDesc, Order = 60,
            Choices = new[] { PolarisStrings.CornerTopRight, PolarisStrings.CornerTopLeft, PolarisStrings.CornerBottomRight, PolarisStrings.CornerBottomLeft })]
        public static int AlertCorner = 0;

        [PolarisSetting(PolarisStrings.CheckUpdates, Desc = PolarisStrings.CheckUpdatesDesc, Order = 70)]
        public static bool CheckForUpdates = true;

        [PolarisSetting(PolarisStrings.AutoQuit, Desc = PolarisStrings.AutoQuitDesc, Order = 80)]
        public static bool AutoQuitOnCritical = true;
    }
}
