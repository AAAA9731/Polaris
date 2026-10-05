using System;

namespace Polaris.Save
{
    /// <summary>
    /// 存档容器的编码错误：分区非法、超出格式上限或容器损坏。
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
