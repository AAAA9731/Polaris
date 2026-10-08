using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Polaris.Save
{
    /// <summary>长期持有句柄，通过 Current 获取本局数据；新游戏与读档会替换实例。</summary>
    public sealed class SaveHandle<T> : ISaveHandle where T : class, new()
    {
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        internal SaveHandle(string id, ushort version)
        {
            Id = id;
            Version = version;
            Reset();
        }

        public string Id { get; }
        public ushort Version { get; }
        public T Current { get; private set; }
        public bool WasLoaded { get; private set; }

        void ISaveHandle.ResetToDefault() => Reset();

        void Reset()
        {
            Current = new T();
            WasLoaded = false;
        }

        byte[] ISaveHandle.WritePayload()
        {
            using (var writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                // Create 不采用其它模组可能设置的 JsonConvert.DefaultSettings。
                JsonSerializer.Create().Serialize(writer, Current);
                return Utf8.GetBytes(writer.ToString());
            }
        }

        void ISaveHandle.ReadPayload(byte[] payload)
        {
            using (var reader = new JsonTextReader(new StringReader(Utf8.GetString(payload))))
            {
                T loaded = JsonSerializer.Create(new JsonSerializerSettings
                    { CheckAdditionalContent = true, ObjectCreationHandling = ObjectCreationHandling.Replace })
                    .Deserialize<T>(reader);
                Current = loaded ?? throw new PolarisSaveException($"分区 {Id} 的数据为 null。");
                WasLoaded = true;
            }
        }
    }

    internal interface ISaveHandle
    {
        string Id { get; }
        ushort Version { get; }
        void ResetToDefault();
        byte[] WritePayload();
        void ReadPayload(byte[] payload);
    }
}
