using System.Collections.Generic;

namespace Polaris.Save
{
    public enum SaveContainerStatus
    {
        /// <summary>字节流末尾没有容器。</summary>
        Absent,

        /// <summary>framing 与全部 CRC 都通过。</summary>
        Ok,

        /// <summary>格式完好，但个别分区 payload 的 CRC 不符；调用方应检查 PayloadDamaged。</summary>
        PartiallyCorrupt,

        /// <summary>framing 或元数据本身坏了，整个容器不可解释。</summary>
        Corrupt,

        /// <summary>容器格式版本不受支持。</summary>
        UnsupportedFormat,
    }

    public sealed class SaveContainerReadResult
    {
        SaveContainerReadResult(SaveContainerStatus status, string message)
        {
            Status = status;
            Message = message;
            Partitions = new List<SavePartitionRecord>();
        }

        public SaveContainerStatus Status { get; }

        public string Message { get; }

        public List<SavePartitionRecord> Partitions { get; }

        /// <summary>容器在存档字节流中的起点，用于和原版读取结束位置对账。</summary>
        public int ContainerStart { get; private set; } = -1;

        internal static SaveContainerReadResult Absent() =>
            new SaveContainerReadResult(SaveContainerStatus.Absent, null);

        internal static SaveContainerReadResult Corrupt(string message) =>
            new SaveContainerReadResult(SaveContainerStatus.Corrupt, message);

        internal static SaveContainerReadResult Unsupported(string message) =>
            new SaveContainerReadResult(SaveContainerStatus.UnsupportedFormat, message);

        internal static SaveContainerReadResult Parsed(
            List<SavePartitionRecord> partitions,
            int containerStart,
            bool anyDamaged,
            string message)
        {
            var result = new SaveContainerReadResult(
                anyDamaged ? SaveContainerStatus.PartiallyCorrupt : SaveContainerStatus.Ok,
                message)
            {
                ContainerStart = containerStart,
            };

            result.Partitions.AddRange(partitions);
            return result;
        }
    }
}
