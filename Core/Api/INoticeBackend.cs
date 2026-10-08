namespace Polaris
{
    /// <summary>
    /// 游戏里还没有 <c>UILog</c>（标题画面、模组加载期间）时，由谁来显示 <see cref="PolarisAPI.Game.UI.Notify"/> 的提示。
    /// 同 <see cref="Infra.IErrorBackend"/>：Core 只定义接口，装了 ModManager 后由它接入，Core 不依赖它。
    /// </summary>
    public interface INoticeBackend
    {
        /// <summary>显示一条提示；文字已经解析过本地化键。可能在游戏主线程以外被调用，实现须自行保证线程安全。</summary>
        /// <param name="warning">是否为 <c>Warn</c> 级别</param>
        void Show(string text, bool warning);
    }
}
