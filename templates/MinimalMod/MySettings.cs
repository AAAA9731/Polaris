using Polaris.Settings;
using UnityEngine.InputSystem;

namespace MinimalMod
{
    /// <summary>
    /// 设置项：字段本身就是值。Core 负责持久化（BepInEx 配置文件），并在游戏设置页的 Polaris 标签页里画出来。
    /// 控件类型由字段类型决定：bool 开关、int/float 滑条、enum（包括 Key）选择器、string 文本框。
    /// </summary>
    [PolarisSettingGroup("minimalmod", "Minimal Mod")]
    static class MySettings
    {
        [PolarisSetting("Show greeting", Desc = "Show a message when the hotkey is pressed.")]
        public static bool Greeting = true;

        // Key 枚举的字段就是"可改键的热键"，配合 PolarisAPI.Game.Input.Bind 使用。
        [PolarisSetting("Hotkey")]
        public static Key Hotkey = Key.F8;

        // 只有开了 Greeting 才显示这一项；VisibleWhen 填同类里 static bool 的方法、属性或字段名。
        [PolarisSetting("Message repeat", Min = 1, Max = 5, VisibleWhen = nameof(Greeting))]
        public static int Repeat = 1;

        // 设置页里的一行按钮，点一下执行。
        [PolarisButton("Reset to defaults")]
        static void Reset()
        {
            Greeting = true;
            Hotkey = Key.F8;
            Repeat = 1;
        }
    }
}
