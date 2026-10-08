using System;

namespace Polaris
{
    /// <summary>把 <see cref="TX.getCurrentFamilyName()"/> 归到内置 zh/ja/en 三种语言之一，供错误提示直接使用。</summary>
    internal static class NoticeLocale
    {
        internal static NoticeLanguage Current
        {
            get
            {
                switch (PolarisAPI.Localization.Language)
                {
                    case Polaris.Localization.Language.Chinese: return NoticeLanguage.Chinese;
                    case Polaris.Localization.Language.Japanese: return NoticeLanguage.Japanese;
                    default: return NoticeLanguage.English;
                }
            }
        }
    }
}
