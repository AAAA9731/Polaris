namespace Polaris.Localization
{
    /// <summary>库自己用到的内置文案（目前只有设置界面里 Polaris 专属标签页的标题）。</summary>
    internal static class LibStrings
    {
        internal const string TabTitle = "&polaris.settings.tab_title";

        static bool registered;

        /// <summary>由 <c>LibPlugin.Awake</c> 调一次。</summary>
        internal static void Register()
        {
            if (registered)
            {
                return;
            }

            registered = true;

            PolarisAPI.Localization.Register("polaris.settings.tab_title", new LocalizedText("Polaris Settings")
            {
                ["zh"] = "Polaris 设置",
                ["ja"] = "Polaris 設定",
            });
        }
    }
}
