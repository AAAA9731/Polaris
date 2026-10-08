using System;
using System.Collections.Generic;
using Polaris.Infra;

namespace Polaris
{
    /// <summary>
    /// 管理器提供的诊断 API（需要安装 Polaris 管理器）：致命错误、本局错误归档、会话健康状况。
    /// 库里的 <c>PolarisAPI.Errors.Report/Guard</c> 在管理器在场时会转到这里做归因。
    /// </summary>
    public static class PolarisDiagnostics
    {
        /// <summary>会话级健康状况：上一局是否正常结束、主线程是否仍在动。</summary>
        public static HealthAPI Health { get; } = new();

        /// <summary>
        /// 报出一个致命错误：模组环境坏了、这一局不该继续，Polaris 会写日志与报告并在标题画面拦住玩家、只留"退出游戏"一个出口
        /// （单个功能坏掉请用 <c>PolarisAPI.Errors.Report</c>）。用在模块初始化阶段；本方法只登记，不阻塞、不抛异常、不结束进程。
        /// </summary>
        public static void Fatal(Diagnostics.FatalError fatal) => Diagnostics.DiagnosticsHost.RaiseFatal(fatal);

        /// <summary>本局是否已经报出过致命错误。</summary>
        public static bool IsFatal => Diagnostics.DiagnosticsHost.IsFatal;

        /// <summary>本局已归档的错误，按首次出现顺序；同一类只有一条，重复次数看 <see cref="Diagnostics.ErrorIncident.Count"/>。</summary>
        public static IReadOnlyList<Diagnostics.ErrorIncident> Session => Diagnostics.DiagnosticsHost.Incidents;

        /// <summary>有新错误归档时触发（同一类只触发一次）；订阅者抛异常会被吞掉，不连累其它订阅者。</summary>
        public static event Action<Diagnostics.ErrorIncident> IncidentRecorded
        {
            add => Diagnostics.DiagnosticsHost.IncidentRecorded += value;
            remove => Diagnostics.DiagnosticsHost.IncidentRecorded -= value;
        }
    }
}
