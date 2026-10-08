namespace Polaris.Save
{
    /// <summary>容器里的一个分区：framing 元数据 + 未解释的 payload 字节。</summary>
    public sealed class SavePartitionRecord
    {
        public SavePartitionRecord(string id, ushort schemaVersion, ushort flags, byte[] payload)
        {
            Id = id;
            SchemaVersion = schemaVersion;
            Flags = flags;
            Payload = payload;
        }

        public string Id { get; }

        public ushort SchemaVersion { get; }

        public ushort Flags { get; }

        public byte[] Payload { get; }

        /// <summary>payload CRC 与记录不符。此时 <see cref="Payload"/> 仍是原始字节，只能原样保留、不得解析。</summary>
        public bool PayloadDamaged { get; internal set; }
    }
}
