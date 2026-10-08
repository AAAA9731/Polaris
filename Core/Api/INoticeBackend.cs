using System;

namespace Polaris
{
    /// <summary>
    /// 游戏里还没有 <c>UILog</c>（标题画面、模组加载期间）时，由谁来显示 <see cref="PolarisAPI.Game.UI.Notify"/> 的提示；
    /// 也负责显示 <see cref="PolarisAPI.Game.UI.Confirm"/> 的确认框。
    /// 同 <see cref="Infra.IErrorBackend"/>：Core 只定义接口，装了 ModManager 后由它接入，Core 不依赖它。
    /// </summary>
    public interface INoticeBackend
    {
        /// <summary>显示一条提示；文字已经解析过本地化键。可能在游戏主线程以外被调用，实现须自行保证线程安全。</summary>
        /// <param name="warning">是否为 <c>Warn</c> 级别</param>
        void Show(string text, bool warning);

        /// <summary>
        /// 显示一个确认框，玩家点按钮后调用对应回调（至多一个）。文字、标题、按钮文案都已解析好。
        /// 取消按钮在玩家按 Esc 时同样触发。回调抛出的异常由实现接住上报。
        /// </summary>
        void Confirm(string title, string text, string confirmLabel, string cancelLabel, Action onConfirm, Action onCancel);
    }
}
