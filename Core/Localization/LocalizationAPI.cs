using XX;
using System;
using System.Collections.Generic;

namespace Polaris.Localization
{
    /// <summary>
    /// 本地化 resolver 注册表 + 内置文案表，从 <see cref="PolarisAPI.Localization"/> 取。
    /// resolver 未命中须返回 <c>null</c> 而非空串，否则会被当成命中，后续 resolver 和原版查表不会再被问到。
    /// </summary>
    public sealed class LocalizationAPI
    {
        internal LocalizationAPI() { }

        readonly List<Func<string, string>> resolvers = [];

        /// <summary>内置表，<see cref="Resolve"/> 优先查询。</summary>
        readonly Dictionary<string, LocalizedText> builtin = new(StringComparer.Ordinal);

        /// <summary>注册一个 resolver；按注册顺序依次尝试，第一个返回非 null 的结果生效。</summary>
        public void RegisterResolver(Func<string, string> resolver)
        {
            if (resolver != null)
            {
                resolvers.Add(resolver);
            }
        }

        /// <summary>往内置表登记一条文案；重复登记后者覆盖前者并记警告。须在设置界面首次显示前登记完成，模组 <c>Awake</c> 里登记即可。</summary>
        public void Register(string key, LocalizedText text)
        {
            if (string.IsNullOrEmpty(key) || text == null)
            {
                return;
            }

            if (builtin.ContainsKey(key))
            {
                // ?.：不保证 Polaris 自己的 Awake（Logger 赋值处）一定先于其它模组的 Awake 跑完。
                CorePlugin.Logger?.LogWarning($"[Polaris] Built-in text key \"{key}\" was registered more than once; the later one wins.");
            }

            builtin[key] = text;
        }

        /// <summary>
        /// 把"显示用字符串"解析为最终文案：<c>&amp;</c> 开头查表，<c>&amp;&amp;</c> 开头脱转义，其余原样返回；<paramref name="raw"/> 为 null 时返回 null。
        /// 查表顺序为内置表/resolver 链 → 原版 <c>TX.Get</c> → key 本身（兜底显示 key 便于定位未登记文案）。
        /// </summary>
        public string Text(string raw)
        {
            if (raw == null)
            {
                return null;
            }

            if (!LocalizedString.TryGetKey(raw, out string key))
            {
                return LocalizedString.Unescape(raw);
            }

            // resolver 契约是"未命中返回 null"，空串视为有效结果直接采纳。
            string resolved = Resolve(key);
            if (resolved != null)
            {
                return resolved;
            }

            // 兼容游戏自带 key；极早期 TX 的 family 表未建好时会抛异常，须接住避免拖垮整个设置界面。
            try
            {
                string vanilla = XX.TX.Get(key);
                if (!string.IsNullOrEmpty(vanilla))
                {
                    return vanilla;
                }
            }
            catch (Exception e)
            {
                CorePlugin.Logger?.LogWarning($"[Polaris] The vanilla lookup threw while querying localization key \"{key}\": {e.Message}");
            }

            return key;
        }

        /// <summary>
        /// 当前界面语言归到的大类：<c>zh*</c> 算中文，日文族（<c>_</c> 或 <c>ja*</c>）算日文，其余算英文。
        /// 游戏语言表没建好时按英文处理，不抛异常。每次读取都现查，需要在每帧用时请自行缓存。
        /// </summary>
        public Language Language => ClassifyLocale(CurrentLocale);

        /// <summary>
        /// 内联的三语文案，不用注册键：<c>Pick("你好", "Hello", "こんにちは")</c>。
        /// 适合小模组和"本地化机制本身可能不可用"的场合（错误提示、启动失败）。日文缺省时用英文。
        /// </summary>
        public string Pick(string zh, string en, string ja = null) => Pick(Language, zh, en, ja);

        /// <summary>同 <see cref="Pick(string, string, string)"/>，但语言由调用方给出（已缓存时用，省得每次去问游戏）。</summary>
        public string Pick(Language language, string zh, string en, string ja = null)
        {
            switch (language)
            {
                case Language.Chinese: return zh ?? en;
                case Language.Japanese: return ja ?? en;
                default: return en;
            }
        }

        /// <summary>把语言族名（<c>zh-cn</c>、<c>en</c>、<c>_</c>……）归类；null 按英文。</summary>
        public static Language ClassifyLocale(string locale)
        {
            if (locale != null && locale.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                return Language.Chinese;
            }

            // "_" 是游戏默认语言（日文）；ja/jp 之类的显式命名同样按日文处理。
            if (locale == LocalizedText.DefaultFamily
                || (locale != null && locale.StartsWith("ja", StringComparison.OrdinalIgnoreCase)))
            {
                return Language.Japanese;
            }

            return Language.English;
        }

        readonly List<(string Dir, string Suffix)> textDirs = [];

        /// <summary>
        /// 注册模组自带的文案文件，不用再拷进游戏的 StreamingAssets：目录下按语言族分子目录，
        /// 文件名 <c>&lt;族&gt;&lt;后缀&gt;.txt</c>（如 <c>en/en_mymod.txt</c>、<c>zh-cn/zh-cn_mymod.txt</c>，日文族目录名是 <c>_</c>），
        /// 格式与原版文案文件相同。游戏每次（重新）读取文案表后自动加载；注册得晚、文案表已就绪时立刻加载。某个语言缺文件就跳过。
        /// </summary>
        /// <param name="dir">文案根目录，通常是插件自己的文件夹</param>
        /// <param name="suffix">文件名里语言族之后的部分，如 <c>"_mymod"</c></param>
        public void AddTextFiles(string dir, string suffix)
        {
            if (string.IsNullOrEmpty(dir))
            {
                throw new ArgumentException("Text directory must not be empty", nameof(dir));
            }

            textDirs.Add((dir, suffix ?? ""));
            if (TX.OTxFam != null && TX.OTxFam.Count > 0)
            {
                LoadTextFiles(dir, suffix ?? "");
            }
        }

        /// <summary>供 <see cref="Patch.Patch_TX_reloadTx"/> 调用：文案表重建后，把所有登记过的文案文件重新读进去。</summary>
        internal void LoadAllTextFiles()
        {
            foreach ((string dir, string suffix) in textDirs)
            {
                LoadTextFiles(dir, suffix);
            }
        }

        static void LoadTextFiles(string dir, string suffix)
        {
            foreach (KeyValuePair<string, TX.TXFamily> family in TX.OTxFam)
            {
                string path = System.IO.Path.Combine(System.IO.Path.Combine(dir, family.Key), family.Key + suffix + ".txt");
                if (!System.IO.File.Exists(path))
                {
                    continue;
                }

                try
                {
                    TX.readTexts(System.IO.File.ReadAllText(path), family.Value);
                }
                catch (Exception ex)
                {
                    PolarisAPI.Errors.Report(ex, $"loading the text file {path}");
                    CorePlugin.Logger.LogError($"[Polaris] Failed to load the text file {path}; skipped.");
                }
            }
        }

        /// <summary><see cref="Text"/> 的数组版；null 进 null 出。</summary>
        public string[] TextAll(string[] raw)
        {
            if (raw == null)
            {
                return null;
            }

            var result = new string[raw.Length];
            for (int i = 0; i < raw.Length; i++)
            {
                result[i] = Text(raw[i]);
            }

            return result;
        }

        /// <summary>供 <see cref="Patch.Patch_TX_Get"/> 调用；全部未命中返回 null。内置表先查，确保 Polaris 自身文案不被其它模组顶掉，且启动极早期（无 resolver 注册时）也能答出。</summary>
        internal string Resolve(string key)
        {
            if (key != null && builtin.TryGetValue(key, out LocalizedText text))
            {
                return text.Pick(CurrentLocale);
            }

            foreach (Func<string, string> resolver in resolvers)
            {
                string value;
                try
                {
                    value = resolver(key);
                }
                catch (Exception ex)
                {
                    // 一个 resolver 抛异常不应连累其它 resolver 或原版查表（Harmony Prefix 未接住会直接打断游戏本身的查询），按未命中处理并继续。
                    PolarisAPI.Errors.Report(ex, $"a localization resolver handling \"{key}\"", resolver.Method?.DeclaringType?.Assembly);
                    CorePlugin.Logger.LogError($"[Polaris] A localization resolver threw while handling \"{key}\"; skipped.");
                    continue;
                }

                if (value != null)
                {
                    return value;
                }
            }

            return null;
        }

        /// <summary>当前语言族；启动极早期 family 表未建好时读取会抛异常，按未知语言处理（返回 null）。</summary>
        static string CurrentLocale
        {
            get
            {
                try
                {
                    return TX.getCurrentFamilyName();
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
}
