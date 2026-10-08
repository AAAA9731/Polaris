namespace Polaris.Save
{
    /// <summary>显式注册模组数据，随原版存档保存、加载，在新游戏时重置。</summary>
    public static class SaveAPI
    {
        /// <summary>
        /// 在模组 Awake 中调用一次；T 是有公共无参构造函数的普通数据类。
        /// 公开字段和属性按 JSON 保存，构造出的实例作为新游戏和缺少分区时的默认值。
        /// </summary>
        /// <param name="id">全局唯一且发布后不变的 ID，建议使用 BepInEx GUID/数据名。</param>
        /// <param name="version">数据版本，从 1 开始；读取到更高版本时拒绝覆盖原存档。</param>
        public static SaveHandle<T> Register<T>(string id, ushort version = 1)
            where T : class, new()
            => SaveRuntime.Instance.Register<T>(id, version);

        /// <summary>首次新游戏、读档或保存之后，不能再增加分区。</summary>
        public static bool IsRegistrationFrozen => SaveRuntime.Instance.IsFrozen;

        /// <summary>本次读到的数据损坏或无法加载，禁止用默认值覆盖原存档。</summary>
        public static bool IsReadOnlyRecovery => SaveRuntime.Instance.IsReadOnlyRecovery;
    }
}
