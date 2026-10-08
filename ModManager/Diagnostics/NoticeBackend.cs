using System;

namespace Polaris.Diagnostics
{
    /// <summary>
    /// 把 Core 的 <c>Game.UI.Notify/Warn</c> 在游戏界面之外（标题画面、加载期间）显示成角落小提示。
    /// 走和错误提示同一套 IMGUI 浮层，所以标题画面也能画；入队线程安全。
    /// </summary>
    internal sealed class NoticeBackend : INoticeBackend
    {
        static int sequence;

        public void Show(string text, bool warning) => InGameAlert.ShowPlain(text, warning);

        public void Confirm(string title, string text, string confirmLabel, string cancelLabel, Action onConfirm, Action onCancel)
        {
            // 按钮从右往左排：确定在最右，取消在它左边；Esc 触发最后一个（取消）。
            InGameAlert.ShowNotice(new InGameAlert.Item
            {
                Key = "confirm:" + System.Threading.Interlocked.Increment(ref sequence),
                Title = title,
                Body = text,
                Buttons =
                [
                    new InGameAlert.NoticeButton { Label = confirmLabel, OnClick = () => Run(onConfirm) },
                    new InGameAlert.NoticeButton { Label = cancelLabel, OnClick = () => Run(onCancel) },
                ],
            });
        }

        static bool Run(Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                PolarisAPI.Errors.Report(ex, "a Game.UI.Confirm callback", action?.Method?.DeclaringType?.Assembly);
            }

            return true;
        }
    }
}
