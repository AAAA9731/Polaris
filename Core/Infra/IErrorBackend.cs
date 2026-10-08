using System;
using System.Reflection;

namespace Polaris.Infra
{
    /// <summary>
    /// 错误上报的可替换后端。库本身只负责写日志；想要归因、报告文件、卡死监视的插件（例如 Polaris 管理器）
    /// 通过 <see cref="ErrorsAPI.SetBackend"/> 把自己接进来。所有方法都可能在任意模组的调用栈上被调用，实现不得抛异常。
    /// </summary>
    public interface IErrorBackend
    {
        /// <summary>收到一个被上报的异常；<paramref name="culprit"/> 为空表示由后端自己推断责任方。</summary>
        void Report(Exception exception, string context, Assembly culprit);

        /// <summary>标记"正在执行谁的代码"，供卡死检测回答"卡在哪"；返回的对象 Dispose 即结束。</summary>
        IDisposable Activity(string context, Assembly culprit);

        /// <summary>库代理执行了一次模组回调（事件订阅者等）；<paramref name="threw"/> 表示它是否抛了异常。</summary>
        void CallbackInvoked(string ownerName, string context, double milliseconds, bool threw);
    }
}
