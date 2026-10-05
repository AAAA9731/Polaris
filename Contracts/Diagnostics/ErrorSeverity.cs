namespace Polaris.Diagnostics
{
    /// <summary>
    /// 错误严重度阶梯：级别越高，玩家看到的反馈越强。判定规则集中在 <see cref="SeverityPolicy"/>。
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>与模组无关（原版自身的错误）：只计数，不打扰玩家。</summary>
        Noted = 0,

        /// <summary>轻微：模组相关错误首次出现。后台记录并写报告，游戏内角落弹一条小提示。</summary>
        Minor,

        /// <summary>持续：同类错误反复发生（风暴）或同一模组错误种类过多。弹出提示框，由玩家决定是否禁用该模组。</summary>
        Persistent,

        /// <summary>严重：继续运行已无意义或会得到错误结果。Polaris 提示后主动结束游戏。</summary>
        Critical,
    }
}
