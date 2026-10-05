using System;
using System.Collections.Generic;
using System.Text;

namespace Polaris.Save
{
    /// <summary>从旧 SaveRuntime 缩减的显式注册表与存档状态；不发现文件或扫描类型。</summary>
    internal sealed class SaveRuntime
    {
        internal static SaveRuntime Instance { get; } = new SaveRuntime();

        readonly object gate = new object();
        readonly List<ISaveHandle> ordered = new List<ISaveHandle>();
        readonly Dictionary<string, ISaveHandle> byId = new Dictionary<string, ISaveHandle>(StringComparer.Ordinal);
        readonly List<SavePartitionRecord> preserved = new List<SavePartitionRecord>();
        bool frozen;
        bool readOnly;

        internal bool IsFrozen { get { lock (gate) { return frozen; } } }
        internal bool IsReadOnlyRecovery { get { lock (gate) { return readOnly; } } }

        internal SaveHandle<T> Register<T>(string id, ushort version) where T : class, new()
        {
            ValidateId(id);
            if (version == 0)
            {
                throw new PolarisSaveException($"分区 {id} 的版本必须从 1 开始。");
            }

            lock (gate)
            {
                if (frozen)
                {
                    throw new PolarisSaveException("存档注册已冻结，请在模组 Awake 中完成注册。");
                }
                if (byId.ContainsKey(id))
                {
                    throw new PolarisSaveException($"分区 ID {id} 已经被注册过。");
                }

                var handle = new SaveHandle<T>(id, version);
                byId.Add(id, handle);
                ordered.Add(handle);
                return handle;
            }
        }

        internal void Freeze()
        {
            lock (gate) { frozen = true; }
        }

        internal void ResetForNewGame()
        {
            lock (gate)
            {
                frozen = true;
                ResetLocked();
            }
        }

        internal int Load(byte[] data, ulong length)
        {
            lock (gate)
            {
                frozen = true;
                ResetLocked();
                readOnly = true;
                if (data == null || length > int.MaxValue || length > (ulong)data.Length)
                {
                    throw new PolarisSaveException("存档字节流长度无效。");
                }
                SaveContainerReadResult result = SaveContainerReader.Read(data, (int)length);
                if (result.Status == SaveContainerStatus.Corrupt
                    || result.Status == SaveContainerStatus.UnsupportedFormat)
                {
                    readOnly = true;
                    throw new PolarisSaveException(result.Message);
                }

                readOnly = result.Status == SaveContainerStatus.PartiallyCorrupt;
                foreach (SavePartitionRecord record in result.Partitions)
                {
                    if (record.PayloadDamaged || !byId.TryGetValue(record.Id, out ISaveHandle handle))
                    {
                        preserved.Add(record);
                        continue;
                    }

                    if (record.SchemaVersion > handle.Version || record.Flags != 0)
                    {
                        preserved.Add(record);
                        readOnly = true;
                        SaveIntegration.Report(new PolarisSaveException($"分区 {record.Id} 的版本或标志不受支持。"), "loading mod save data");
                        continue;
                    }

                    try
                    {
                        handle.ReadPayload(record.Payload);
                    }
                    catch (Exception ex)
                    {
                        preserved.Add(record);
                        readOnly = true;
                        SaveIntegration.Report(ex, $"loading save partition {record.Id}");
                    }
                }

                if (result.Status == SaveContainerStatus.PartiallyCorrupt)
                {
                    SaveIntegration.Report(new PolarisSaveException("模组存档分区 CRC 校验失败，已阻止覆盖。"), "loading mod save data");
                }
                return result.ContainerStart;
            }
        }

        internal byte[] BuildContainer()
        {
            lock (gate)
            {
                frozen = true;
                if (readOnly)
                {
                    throw new PolarisSaveException("模组存档数据无法完整加载，拒绝用默认值覆盖原存档。");
                }
                if (ordered.Count == 0 && preserved.Count == 0)
                {
                    return Array.Empty<byte>();
                }

                var records = new List<SavePartitionRecord>(ordered.Count + preserved.Count);
                foreach (ISaveHandle handle in ordered)
                {
                    records.Add(new SavePartitionRecord(handle.Id, handle.Version, 0, handle.WritePayload()));
                }
                records.AddRange(preserved);
                records.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
                return SaveContainerWriter.Write(records);
            }
        }

        void ResetLocked()
        {
            // 构造默认数据失败时也禁止保存，避免沿用上一局的部分状态。
            readOnly = true;
            preserved.Clear();
            foreach (ISaveHandle handle in ordered)
            {
                handle.ResetToDefault();
            }
            readOnly = false;
        }

        static void ValidateId(string id)
        {
            if (string.IsNullOrEmpty(id) || Encoding.UTF8.GetByteCount(id) > SaveFormatLimits.MaxIdBytes)
            {
                throw new PolarisSaveException("存档 ID 必须为 1 到 128 字节。");
            }
            if (id[0] == '/' || id[id.Length - 1] == '/')
            {
                throw new PolarisSaveException("存档 ID 不能以 / 开头或结尾。");
            }
            char previous = '\0';
            foreach (char c in id)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')
                    || c == '.' || c == '-' || c == '_' || c == '/';
                if (!ok || (c == '/' && previous == '/'))
                {
                    throw new PolarisSaveException("存档 ID 只允许字母、数字、. - _ /，且不能含连续的 /。");
                }
                previous = c;
            }
        }
    }
}
