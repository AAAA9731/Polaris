namespace Polaris.Diagnostics
{
    /// <summary>
    /// 把 Core 的 <c>Game.UI.Notify/Warn</c> 在游戏界面之外（标题画面、加载期间）显示成角落小提示。
    /// 走和错误提示同一套 IMGUI 浮层，所以标题画面也能画；入队线程安全。
    /// </summary>
    internal sealed class NoticeBackend : INoticeBackend
    {
        public void Show(string text, bool warning) => InGameAlert.ShowPlain(text, warning);
    }
}
