using System;

namespace Polaris.Save
{
    /// <summary>
    /// 模组数据注册与存档读写错误：分区非法、超出格式上限或数据损坏。
    /// </summary>
    public sealed class PolarisSaveException : Exception
    {
        public PolarisSaveException(string message)
            : base(message)
        {
        }

        public PolarisSaveException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
