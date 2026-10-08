using System;
using m2d;
using nel;
using Polaris.API;
using UnityEngine;
using UnityEngine.InputSystem;
using XX;

namespace Polaris
{
    public static partial class PolarisAPI
    {
        /// <summary>常用游戏查询与接入；游戏对象直接返回原版类型，不维护包装器。</summary>
        public static partial class Game
        {
            /// <summary>只读查询的统一包装：取不到（游戏内部还没建好、或读取本身抛异常）就给回退值，绝不把异常漏给调用方。</summary>
            static TValue Safe<TValue>(Func<TValue> read, TValue fallback)
            {
                try
                {
                    return read();
                }
                catch (Exception)
                {
                    return fallback;
                }
            }

            /// <summary>游戏循环。</summary>
            public static class Loop
            {
                /// <summary>获取游戏自己的帧计数（非 Unity 帧数），读档/演出/暂停期间不推进。</summary>
                public static int FrameCount => Safe(static () => IN.totalframe, 0);

                /// <summary>判断游戏窗口当前是否持有输入焦点。</summary>
                public static bool HasFocus => Safe(static () => IN.application_focus, true);

                /// <summary>
                /// 把动作排到下一帧的主线程执行；可从任意线程（网络回调、异步 IO）调用。
                /// 动作里抛出的异常会被接住并记日志，不会影响其它排队的动作。
                /// </summary>
                public static void RunOnMainThread(Action action) => Res.Runtime.MainThreadDispatcher.Enqueue(action);
            }

            /// <summary>给玩家看的界面提示。</summary>
            public static class UI
            {
                /// <summary>最多攒这么多条尚无处可显示的提示，超出丢最旧的，免得没人接时无限增长。</summary>
                const int MaxPending = 16;

                static INoticeBackend backend;
                static readonly System.Collections.Generic.Queue<(string Text, bool Warning)> pending = new();

                /// <summary>
                /// 接入"游戏界面之外"的提示后端（标题画面、加载期间用）；同一时刻只有一个，传 null 取消。
                /// 装了 ModManager 时它会接入，模组一般不用管。
                /// </summary>
                public static void SetBackend(INoticeBackend value)
                {
                    backend = value;
                    Flush();
                }

                /// <summary>
                /// 给玩家弹一行普通提示；<c>&amp;</c> 开头的文字当作本地化键。
                /// 游戏里用原版的提示行（和拾取物品那种同一处）；标题画面、加载期间交给 <see cref="SetBackend"/> 接入的后端；都没有就先攒着，等有处可显示再弹。
                /// </summary>
                public static void Notify(string text) => Show(text, warning: false, "Game.UI.Notify");

                /// <summary>弹一行带警告样式的提示，规则同 <see cref="Notify"/>。</summary>
                public static void Warn(string text) => Show(text, warning: true, "Game.UI.Warn");

                static void Show(string text, bool warning, string where)
                {
                    if (string.IsNullOrEmpty(text))
                    {
                        return;
                    }

                    try
                    {
                        string resolved = PolarisAPI.Localization.Text(text);
                        if (!TryDeliver(resolved, warning))
                        {
                            lock (pending)
                            {
                                pending.Enqueue((resolved, warning));
                                while (pending.Count > MaxPending)
                                {
                                    pending.Dequeue();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Errors.Report(ex, where);
                    }
                }

                /// <summary>先试游戏里的提示行，不行再试后端；都不行返回 false。</summary>
                static bool TryDeliver(string text, bool warning)
                {
                    UILog log = Safe(static () => UILog.Instance, null);
                    if (log != null)
                    {
                        if (warning)
                        {
                            log.AddAlert(text);
                        }
                        else
                        {
                            log.AddLog(text);
                        }

                        return true;
                    }

                    INoticeBackend target = backend;
                    if (target != null)
                    {
                        target.Show(text, warning);
                        return true;
                    }

                    return false;
                }

                /// <summary>把攒着的提示尽量弹出去；由 Core 的 Update 每帧调用。</summary>
                internal static void Flush()
                {
                    lock (pending)
                    {
                        while (pending.Count > 0)
                        {
                            (string text, bool warning) = pending.Peek();
                            bool delivered;
                            try
                            {
                                delivered = TryDeliver(text, warning);
                            }
                            catch (Exception ex)
                            {
                                Errors.Report(ex, "Game.UI.Flush");
                                delivered = true;
                            }

                            if (!delivered)
                            {
                                return;
                            }

                            pending.Dequeue();
                        }
                    }
                }
            }

            public static class Input
            {
                /// <summary>获取鼠标当前的屏幕坐标（游戏的 GUI 坐标系，1280×720 基准）。</summary>
                public static Vector2 MousePosition => Safe(static () => (Vector2)IN.Mouse, Vector2.zero);

                /// <summary>获取本帧鼠标滚轮的滚动量。</summary>
                public static Vector2 MouseWheelDelta => Safe(static () => (Vector2)IN.MouseWheel, Vector2.zero);


                /// <summary>该键本帧刚按下；玩家正在输入框里打字时恒为 false（和原版快捷键一样不抢输入）。</summary>
                public static bool WasPressed(Key key) => Safe(() => IN.getKD(key), false);

                /// <summary>该键正被按住；输入框里打字时恒为 false。</summary>
                public static bool IsHeld(Key key) => Safe(() => IN.getK(key), false);

                /// <summary>该键本帧刚松开；输入框里打字时恒为 false。</summary>
                public static bool WasReleased(Key key) => Safe(() => IN.getKU(key), false);

                /// <summary>
                /// 绑定一个热键：每帧检查 <paramref name="key"/> 当前返回的键，按下就执行 <paramref name="onPressed"/>。
                /// 键用回调给出，配合 <c>[PolarisSetting] static Key</c> 字段就能让玩家在设置页改键（<c>Bind(() =&gt; MyConfig.Hotkey, ...)</c>），改完立即生效。
                /// 键为 <c>Key.None</c> 时不触发；回调抛异常会被接住并上报。
                /// </summary>
                /// <returns>解除绑定的句柄，Dispose 即取消</returns>
                public static IDisposable Bind(Func<Key> key, Action onPressed)
                {
                    return Hotkeys.Add(key ?? throw new ArgumentNullException(nameof(key)),
                                       onPressed ?? throw new ArgumentNullException(nameof(onPressed)));
                }

                /// <summary>清除指定输入动作的当前按键状态；<paramref name="onlyPressDown"/> 为真时只清"刚按下"沿，保留持续按住状态。</summary>
                public static void ClearState(string key, bool onlyPressDown = true)
                {
                    if (string.IsNullOrEmpty(key))
                    {
                        return;
                    }

                    try
                    {
                        IN.clearKeyState(key, onlyPressDown);
                    }
                    catch (Exception ex)
                    {
                        Errors.Report(ex, "Game.Input.ClearState");
                    }
                }
            }


            /// <summary>游戏资源的加载进度。</summary>
            public static class Assets
            {
                /// <summary>
                /// 获取 <c>MTRX</c> 内部加载阶段值，7 表示全部就绪（之前访问 <c>MTRX.OMI</c> 等会 NullReferenceException）。
                /// 刻意读字段而非 <c>MTRX.prepared</c> 属性，因为该 getter 有副作用会推进加载阶段。
                /// </summary>
                public static int LoadStage => Safe(static () => MTRX.loaded, 0);
            }

            /// <summary>游戏语言。</summary>
            public static class Localization
            {
                /// <summary>获取游戏当前使用的语言区域代码（如 <c>"_"</c>/<c>"en"</c>/<c>"zh-cn"</c>）。</summary>
                public static string CurrentLocale => Safe(static () => TX.getCurrentFamilyName(), null);

                /// <summary>获取游戏默认的语言区域代码。</summary>
                public static string DefaultLocale => Safe(static () => TX.default_family, null);

                /// <summary>切换游戏当前使用的语言。</summary>
                public static void Change(string locale)
                {
                    if (string.IsNullOrEmpty(locale))
                    {
                        return;
                    }

                    try
                    {
                        TX.changeFamily(locale);
                    }
                    catch (Exception ex)
                    {
                        Errors.Report(ex, "Game.Localization.Change");
                    }
                }

                /// <summary>判断指定语言是否为当前语言。</summary>
                public static bool IsCurrent(string locale)
                    => !string.IsNullOrEmpty(locale) && Safe(() => TX.familyIs(locale), false);
            }


            /// <summary>当前地图和玩家的原版引用。</summary>
            public static class World
            {
                /// <summary>
                /// 取得当前地图实例；没有加载地图（标题画面、读档中）时为 <c>null</c>。
                /// </summary>
                public static Map2d CurrentMap => GameBinding.CurrentMap;

                /// <summary>取得当前玩家实例；玩家不在场时为 <c>null</c>。</summary>
                public static PR CurrentPlayer => GameBinding.Player;

                /// <summary>Nel 侧的 M2D（天气、危险度、背包、魔法管理器都挂在它下面）；尚未建好时为 <c>null</c>。</summary>
                public static NelM2DBase NelM2D => GameBinding.NelM2D;


            }
        }
    }
}
