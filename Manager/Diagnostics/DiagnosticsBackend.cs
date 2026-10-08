using System;
using System.Reflection;
using Polaris.Infra;

namespace Polaris.Diagnostics
{
    /// <summary>把库的 <see cref="ErrorsAPI"/> 接到管理器的诊断引擎：归因、去重、写报告、卡死面包屑、回调统计。</summary>
    internal sealed class DiagnosticsBackend : IErrorBackend
    {
        public void Report(Exception exception, string context, Assembly culprit)
            => DiagnosticsHost.Report(exception, context, culprit);

        public IDisposable Activity(string context, Assembly culprit)
            => DiagnosticsHost.Activity(context, culprit);

        public void CallbackInvoked(string ownerName, string context, double milliseconds, bool threw)
        {
            if (threw)
            {
                DiagnosticsHost.RecordCallbackException(ownerName, context);
            }

            DiagnosticsHost.RecordCallbackInvocation(ownerName, context, milliseconds);
        }
    }
}
