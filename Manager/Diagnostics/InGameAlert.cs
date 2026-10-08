using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Polaris.Diagnostics
{
    /// <summary>
    /// 游戏内提示：用 Unity IMGUI 画，不依赖游戏自己的 UI 类，游戏更新后不容易坏。
    /// 三种形态：角落小提示（Toast，可点开详情）、居中的提示框（Persistent，可禁用模组）、全屏的退出提示（Critical）。
    /// 任何线程都能入队；绘制与交互只在主线程的 <see cref="OnGUI"/>/<see cref="Update"/> 里做。
    /// 这里的任何异常都必须被吞掉，且绝不能再触发错误上报，否则风暴时会形成死循环。
    /// </summary>
    internal static class InGameAlert
    {
        const float VirtualHeight = 720f;
        const int MaxToasts = 3;

        /// <summary>入场/离场时长（秒）。入场用回弹缓出，离场用缓入，不直进直出。</summary>
        const float InSeconds = 0.38f;
        const float OutSeconds = 0.26f;

        /// <summary>提示框上的一个自定义按钮；<see cref="OnClick"/> 返回 true 表示点完后关闭提示框。</summary>
        internal sealed class NoticeButton
        {
            internal string Label;
            internal Func<bool> OnClick;
        }

        internal sealed class Item
        {
            /// <summary>非空时提示框画这些按钮，而不是"禁用/报告/忽略"那三个（自动更新等场景用）。</summary>
            internal List<NoticeButton> Buttons;

            internal string Key;
            internal string Title;
            internal string Body;
            internal AssemblyOwner Owner;
            internal string ReportPath;
            internal float CreatedAt;
            internal float Born;
            internal float DyingAt = -1f;
            internal float Y = -1f;
            internal bool Expanded;
            internal string Status;
            internal int Sequence;
        }

        sealed class CriticalItem
        {
            internal string Reason;
            internal string Culprit;
            internal AssemblyOwner Owner;
            internal bool Quit;
            internal float Deadline;
            internal float Born;
            internal float DyingAt = -1f;
            internal bool Dismissed;
        }

        enum Command { Toast, Persistent, Critical }

        sealed class Inbound
        {
            internal Command Command;
            internal Item Item;
            internal CriticalItem Critical;
        }

        static readonly ConcurrentQueue<Inbound> inbox = new();
        static readonly List<Item> toasts = new();
        static readonly Queue<Item> modals = new();
        static Item modal;
        static CriticalItem critical;
        static int sequence;
        static float lastLanguageCheck = -10f;

        // IMGUI 资源，只能在 OnGUI 里创建。
        static Font bodyFont, titleFont;
        static GUIStyle titleStyle, toastTitleStyle, bodyStyle, hintStyle, buttonStyle;

        // 取自游戏自己的界面：米色纸面、深灰墨色、细线双边框、菱形装饰。
        static readonly Color Paper = new Color(0.91f, 0.89f, 0.85f, 0.97f);
        static readonly Color Ink = new Color(0.22f, 0.22f, 0.23f, 1f);
        static readonly Color InkSoft = new Color(0.40f, 0.40f, 0.42f, 1f);
        static readonly Color Accent = new Color(0.62f, 0.24f, 0.20f, 1f);
        static readonly Color Hover = new Color(0.22f, 0.22f, 0.23f, 0.10f);

        // ================== 入队（任意线程）==================

        internal static void Toast(string key, string title, string body, AssemblyOwner owner, string reportPath)
        {
            // "仅持续及以上"：轻微级别不弹，只写报告。
            if (!DiagnosticsConfig.ToastEnabled || Settings.PolarisSettings.AlertMinLevel >= 1)
            {
                return;
            }

            inbox.Enqueue(new Inbound { Command = Command.Toast, Item = new Item { Key = key, Title = title, Body = body, Owner = owner, ReportPath = reportPath } });
        }

        internal static void Persistent(string key, string title, string body, AssemblyOwner owner, string reportPath)
        {
            if (!DiagnosticsConfig.ToastEnabled)
            {
                return;
            }

            inbox.Enqueue(new Inbound { Command = Command.Persistent, Item = new Item { Key = key, Title = title, Body = body, Owner = owner, ReportPath = reportPath } });
        }

        /// <summary>弹出一个带自定义按钮的提示框（主线程调用）；字段可在显示后继续修改，用来推进"下载中→已就绪"这类状态。</summary>
        internal static void ShowNotice(Item item)
        {
            inbox.Enqueue(new Inbound { Command = Command.Persistent, Item = item });
        }

        internal static void Critical(string reason, string culprit, AssemblyOwner owner, bool quit)
        {
            inbox.Enqueue(new Inbound { Command = Command.Critical, Critical = new CriticalItem { Reason = reason, Culprit = culprit, Owner = owner, Quit = quit } });
        }

        // ================== 主线程每帧 ==================

        internal static void Update()
        {
            try
            {
                float now = Time.realtimeSinceStartup;

                if (now - lastLanguageCheck >= 1f)
                {
                    lastLanguageCheck = now;
                    AlertStrings.Language = NoticeLocale.Current;
                }

                while (inbox.TryDequeue(out Inbound entry))
                {
                    Accept(entry, now);
                }

                foreach (Item t in toasts)
                {
                    if (t.DyingAt < 0f && !t.Expanded && now - t.CreatedAt > DiagnosticsConfig.ToastSeconds)
                    {
                        t.DyingAt = now;
                    }
                }

                toasts.RemoveAll(t => t.DyingAt >= 0f && now - t.DyingAt > OutSeconds);

                if (modal != null && modal.DyingAt >= 0f && now - modal.DyingAt > OutSeconds)
                {
                    modal = null;
                }

                if (critical != null && critical.DyingAt >= 0f && now - critical.DyingAt > OutSeconds)
                {
                    critical = null;
                }

                if (modal == null && modals.Count > 0)
                {
                    modal = modals.Dequeue();
                    modal.Born = now;
                }

                if (critical != null && critical.Quit && !critical.Dismissed && now >= critical.Deadline)
                {
                    critical.Dismissed = true;
                    FatalExit.QuitNow();
                }

                SeverityPolicy.Tick();
            }
            catch (Exception)
            {
            }
        }

        static void Accept(Inbound entry, float now)
        {
            switch (entry.Command)
            {
                case Command.Toast:
                {
                    Item item = entry.Item;
                    item.CreatedAt = now;
                    item.Born = now;
                    item.Sequence = ++sequence;

                    Item existing = toasts.Find(t => t.Key == item.Key);
                    if (existing != null)
                    {
                        // 同一模组的后续错误：只刷新文字和计时，不新开一条（冷却内合并）。
                        existing.Title = item.Title;
                        existing.Body = item.Body;
                        existing.CreatedAt = now;
                        existing.DyingAt = -1f;
                        return;
                    }

                    toasts.Add(item);
                    while (toasts.Count > MaxToasts)
                    {
                        toasts.RemoveAt(0);
                    }

                    break;
                }

                case Command.Persistent:
                    entry.Item.CreatedAt = now;
                    entry.Item.Born = now;
                    entry.Item.Sequence = ++sequence;
                    modals.Enqueue(entry.Item);
                    break;

                case Command.Critical:
                    if (critical == null || !critical.Quit)
                    {
                        critical = entry.Critical;
                        critical.Deadline = now + DiagnosticsConfig.QuitCountdownSeconds;
                        critical.Born = now;
                    }

                    break;
            }
        }

        // ================== 绘制 ==================

        internal static void OnGUI()
        {
            if (toasts.Count == 0 && modal == null && critical == null)
            {
                return;
            }

            try
            {
                EnsureStyles();

                float scale = Screen.height / VirtualHeight;
                Matrix4x4 savedMatrix = GUI.matrix;
                Color savedColor = GUI.color;
                int savedDepth = GUI.depth;
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
                GUI.depth = -10000;

                float width = Screen.width / scale;
                float height = VirtualHeight;

                DrawToasts(width, height);

                if (modal != null && critical == null)
                {
                    DrawModal(width, height);
                }

                if (critical != null && !(critical.Quit && critical.Dismissed))
                {
                    DrawCritical(width, height);
                }

                GUI.depth = savedDepth;
                GUI.color = savedColor;
                GUI.matrix = savedMatrix;
            }
            catch (Exception)
            {
            }
        }

        // ================== 缓动 ==================

        static float EaseOutCubic(float t)
        {
            t = 1f - t;
            return 1f - t * t * t;
        }

        static float EaseInCubic(float t) => t * t * t;

        /// <summary>回弹缓出：先略微冲过头再落回，窗口"弹"出来的手感。</summary>
        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            t -= 1f;
            return 1f + c3 * t * t * t + c1 * t * t;
        }

        /// <summary>
        /// 一个窗口当前的动画相位。<paramref name="alpha"/> 是透明度；<paramref name="pop"/> 是位移/缩放用的进度，
        /// 0 = 完全没出现，1 = 就位，入场时会略微超过 1。离场时两者都按缓入从 1 退回 0。
        /// </summary>
        static void Phase(float born, float dyingAt, out float alpha, out float pop)
        {
            float now = Time.realtimeSinceStartup;

            if (dyingAt >= 0f)
            {
                float t = Mathf.Clamp01((now - dyingAt) / OutSeconds);
                alpha = 1f - EaseInCubic(t);
                pop = 1f - EaseInCubic(t);
                return;
            }

            float u = Mathf.Clamp01((now - born) / InSeconds);
            alpha = EaseOutCubic(Mathf.Clamp01(u * 1.6f));
            pop = EaseOutBack(u);
        }

        /// <summary>以矩形中心为轴缩放之后的绘制矩阵；用完要用返回值还原。</summary>
        static Matrix4x4 PushScale(Rect r, float k)
        {
            Matrix4x4 saved = GUI.matrix;
            Vector2 c = r.center;
            GUI.matrix = saved
                * Matrix4x4.Translate(new Vector3(c.x, c.y, 0f))
                * Matrix4x4.Scale(new Vector3(k, k, 1f))
                * Matrix4x4.Translate(new Vector3(-c.x, -c.y, 0f));
            return saved;
        }

        static void DrawToasts(float width, float height)
        {
            int corner = Settings.PolarisSettings.AlertCorner;
            bool right = corner == 0 || corner == 2;
            bool bottom = corner >= 2;

            // 顶部角从上往下排，底部角从下往上排；离场的不占位。
            float y = bottom ? height - 16f : 16f;
            float follow = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 14f);

            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                Item toast = toasts[i];
                float w = 400f;
                float h = toast.Expanded ? 204f : 70f;

                float targetY = bottom ? y - h : y;

                // 位置平滑跟随：别的提示消失或展开时，这条会顺滑地挪过去，而不是瞬移。
                toast.Y = toast.Y < 0f ? targetY : Mathf.Lerp(toast.Y, targetY, follow);

                Phase(toast.Born, toast.DyingAt, out float alpha, out float pop);

                // 从所在一侧的屏幕边缘滑入、滑出。
                float slide = (1f - pop) * 80f * (right ? 1f : -1f);
                var rect = new Rect((right ? width - w - 16f : 16f) + slide, toast.Y, w, h);

                bool live = toast.DyingAt < 0f;
                bool enabled = GUI.enabled;
                GUI.enabled = live;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                Panel(rect, accent: true);

                float tx = rect.x + 18f;
                float tw = w - 34f;

                if (!toast.Expanded)
                {
                    GUI.Label(new Rect(tx, rect.y + 8f, tw, 32f), toast.Title, toastTitleStyle);
                    GUI.Label(new Rect(tx, rect.y + 42f, tw, 22f), AlertStrings.ToastHint, hintStyle);

                    if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                    {
                        toast.Expanded = true;
                    }
                }
                else
                {
                    GUI.Label(new Rect(tx, rect.y + 8f, tw, 32f), toast.Title, toastTitleStyle);
                    GUI.Label(new Rect(tx, rect.y + 44f, tw, 70f), toast.Body, bodyStyle);

                    if (toast.Status != null)
                    {
                        GUI.Label(new Rect(tx, rect.y + 120f, tw, 22f), toast.Status, hintStyle);
                    }

                    float by = rect.yMax - 46f;
                    if (CanDisable(toast.Owner) && Button(new Rect(tx, by, 190f, 32f), AlertStrings.BtnDisable))
                    {
                        toast.Status = Disable(toast.Owner);
                    }

                    if (toast.ReportPath != null && Button(new Rect(tx + 198f, by, 90f, 32f), AlertStrings.BtnReport))
                    {
                        OpenFile(toast.ReportPath);
                    }

                    if (Button(new Rect(rect.xMax - 18f - 44f, by, 44f, 32f), "×", diamond: false))
                    {
                        toast.DyingAt = Time.realtimeSinceStartup;
                    }
                }

                GUI.enabled = enabled;

                // 正在离场的提示不再占位，让后面的提示提前补上来。
                if (live)
                {
                    y += bottom ? -(h + 8f) : h + 8f;
                }
            }

            GUI.color = Color.white;
        }

        static void DrawModal(float width, float height)
        {
            Item item = modal;
            var rect = new Rect((width - 580f) / 2f, (height - 280f) / 2f, 580f, 280f);

            Phase(item.Born, item.DyingAt, out float alpha, out float pop);
            bool live = item.DyingAt < 0f;
            bool enabled = GUI.enabled;
            GUI.enabled = live;

            Matrix4x4 saved = PushScale(rect, Mathf.LerpUnclamped(0.88f, 1f, pop));
            GUI.color = new Color(1f, 1f, 1f, alpha);
            Panel(rect, accent: false);

            float tx = rect.x + 28f;
            float tw = rect.width - 56f;

            GUI.Label(new Rect(tx, rect.y + 20f, tw, 30f), item.Title, titleStyle);
            Rule(new Rect(tx, rect.y + 54f, tw, 1f));
            GUI.Label(new Rect(tx, rect.y + 64f, tw, 120f), item.Body, bodyStyle);

            if (item.Status != null)
            {
                GUI.Label(new Rect(tx, rect.y + 188f, tw, 22f), item.Status, hintStyle);
            }

            float by = rect.yMax - 58f;

            if (item.Buttons != null)
            {
                // 自定义按钮：从右往左排；点中返回 true 的会关闭提示框。
                float x = rect.xMax - 28f;
                for (int i = 0; i < item.Buttons.Count; i++)
                {
                    NoticeButton nb = item.Buttons[i];
                    float w = Mathf.Max(110f, nb.Label.Length * 15f + 56f);
                    x -= w;
                    if (Button(new Rect(x, by, w, 36f), nb.Label))
                    {
                        bool close = false;
                        try
                        {
                            close = nb.OnClick();
                        }
                        catch (Exception)
                        {
                        }

                        if (close)
                        {
                            item.DyingAt = Time.realtimeSinceStartup;
                        }

                        break;
                    }

                    x -= 10f;
                }
            }
            else
            {
                if (CanDisable(item.Owner) && Button(new Rect(tx, by, 230f, 36f), AlertStrings.BtnDisable))
                {
                    item.Status = Disable(item.Owner);
                }

                if (item.ReportPath != null && Button(new Rect(tx + 238f, by, 110f, 36f), AlertStrings.BtnReport))
                {
                    OpenFile(item.ReportPath);
                }

                if (Button(new Rect(rect.xMax - 28f - 130f, by, 130f, 36f), AlertStrings.BtnIgnore)
                    || live && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                {
                    item.DyingAt = Time.realtimeSinceStartup;
                }
            }

            GUI.matrix = saved;
            GUI.enabled = enabled;
            GUI.color = Color.white;
        }

        static void DrawCritical(float width, float height)
        {
            CriticalItem item = critical;
            Phase(item.Born, item.DyingAt, out float alpha, out float pop);
            bool live = item.DyingAt < 0f;
            bool enabled = GUI.enabled;
            GUI.enabled = live;

            // 压暗背景，和游戏自己的全屏告知页一样把注意力收到中间。
            GUI.color = new Color(0f, 0f, 0f, 0.6f * alpha);
            GUI.DrawTexture(new Rect(0f, 0f, width, height), Texture2D.whiteTexture);

            var rect = new Rect((width - 640f) / 2f, (height - 310f) / 2f, 640f, 310f);
            Matrix4x4 saved = PushScale(rect, Mathf.LerpUnclamped(0.88f, 1f, pop));
            GUI.color = new Color(1f, 1f, 1f, alpha);
            Panel(rect, accent: true);

            float tx = rect.x + 32f;
            float tw = rect.width - 64f;

            GUI.Label(new Rect(tx, rect.y + 20f, tw, 34f), AlertStrings.CritTitle, titleStyle);
            Rule(new Rect(tx, rect.y + 58f, tw, 1f));

            string body = item.Reason;
            if (!string.IsNullOrEmpty(item.Culprit))
            {
                body += "\n\n" + AlertStrings.Responsible(item.Culprit);
            }

            GUI.Label(new Rect(tx, rect.y + 68f, tw, 120f), body, bodyStyle);

            string line = item.Quit
                ? AlertStrings.CritCountdown(Mathf.Max(0, Mathf.CeilToInt(item.Deadline - Time.realtimeSinceStartup)))
                : AlertStrings.CritNoQuit;
            GUI.Label(new Rect(tx, rect.y + 196f, tw, 44f), line, hintStyle);

            float by = rect.yMax - 58f;
            if (item.Quit)
            {
                if (Button(new Rect(rect.xMax - 32f - 150f, by, 150f, 38f), AlertStrings.BtnQuitNow))
                {
                    item.Dismissed = true;
                    FatalExit.QuitNow();
                }
            }
            else if (Button(new Rect(rect.xMax - 32f - 150f, by, 150f, 38f), AlertStrings.BtnDismiss))
            {
                item.DyingAt = Time.realtimeSinceStartup;
            }

            if (CanDisable(item.Owner) && !item.Dismissed && Button(new Rect(tx, by, 230f, 38f), AlertStrings.BtnDisable))
            {
                Disable(item.Owner);
            }

            GUI.matrix = saved;
            GUI.enabled = enabled;
            GUI.color = Color.white;
        }

        // ================== 控件：用纯色块拼出游戏界面的质感 ==================

        static void Fill(Rect r, Color c)
        {
            Color saved = GUI.color;
            GUI.color = new Color(c.r, c.g, c.b, c.a * saved.a);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = saved;
        }

        static void Frame(Rect r, float thickness, Color c)
        {
            Fill(new Rect(r.x, r.y, r.width, thickness), c);
            Fill(new Rect(r.x, r.yMax - thickness, r.width, thickness), c);
            Fill(new Rect(r.x, r.y, thickness, r.height), c);
            Fill(new Rect(r.xMax - thickness, r.y, thickness, r.height), c);
        }

        static void Rule(Rect r) => Fill(r, new Color(Ink.r, Ink.g, Ink.b, 0.35f));

        /// <summary>小菱形，游戏的窗口角饰与按钮端点都用它。</summary>
        static void Diamond(Vector2 center, float half, Color c)
        {
            Matrix4x4 saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(45f, center);
            Fill(new Rect(center.x - half, center.y - half, half * 2f, half * 2f), c);
            GUI.matrix = saved;
        }

        /// <summary>米色纸面 + 细线双边框 + 四角菱形；accent 时左侧加一道暗红色条，标明这是警告。</summary>
        static void Panel(Rect r, bool accent)
        {
            Fill(new Rect(r.x + 3f, r.y + 4f, r.width, r.height), new Color(0f, 0f, 0f, 0.25f));
            Fill(r, Paper);
            Frame(r, 1.5f, Ink);
            Frame(new Rect(r.x + 5f, r.y + 5f, r.width - 10f, r.height - 10f), 1f, new Color(Ink.r, Ink.g, Ink.b, 0.45f));

            if (accent)
            {
                Fill(new Rect(r.x + 5f, r.y + 5f, 4f, r.height - 10f), Accent);
            }

            const float half = 3.5f;
            Diamond(new Vector2(r.x, r.y), half, Ink);
            Diamond(new Vector2(r.xMax, r.y), half, Ink);
            Diamond(new Vector2(r.x, r.yMax), half, Ink);
            Diamond(new Vector2(r.xMax, r.yMax), half, Ink);
        }

        /// <summary>仿游戏按钮：细线描边的长条，左端一个菱形，悬停时淡淡压暗。</summary>
        static bool Button(Rect r, string text, bool diamond = true)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            if (hover)
            {
                Fill(r, Hover);
            }

            Frame(r, 1f, Ink);

            if (diamond)
            {
                Diamond(new Vector2(r.x + 14f, r.center.y), 3.5f, Ink);
            }

            return GUI.Button(r, text, buttonStyle);
        }

        // ================== 动作 ==================

        static bool CanDisable(AssemblyOwner owner)
            => owner != null && owner.Kind == OwnerKind.Mod && !string.IsNullOrEmpty(owner.FullPath);

        /// <summary>把责任模组的 dll 改名为 <c>.dll.disabled</c>，与模组管理页同一套机制，重启后生效。</summary>
        static string Disable(AssemblyOwner owner)
        {
            if (!CanDisable(owner))
            {
                return AlertStrings.CannotDisable;
            }

            try
            {
                string path = owner.FullPath;
                string target = path + ".disabled";
                if (File.Exists(target))
                {
                    File.Delete(target);
                }

                File.Move(path, target);
                return AlertStrings.DisabledOk;
            }
            catch (Exception e)
            {
                return AlertStrings.DisableFail(e.Message);
            }
        }

        static void OpenFile(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception)
            {
            }
        }

        // ================== 样式 ==================

        /// <summary>优先取游戏自己加载的字体（界面圆体 / 标题宋体），保证字形一致；找不到再退回系统字体。</summary>
        static Font FindGameFont(string name)
        {
            try
            {
                foreach (Font f in Resources.FindObjectsOfTypeAll<Font>())
                {
                    if (f != null && f.name == name)
                    {
                        return f;
                    }
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        static void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            Font os = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Yu Gothic UI", "Meiryo UI", "Segoe UI", "Arial" }, 16);

            bodyFont = FindGameFont("ResourceHanRoundedCN-Medium") ?? os;
            titleFont = FindGameFont("SourceHanSerifSC-SemiBold") ?? bodyFont;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = titleFont,
                fontSize = 20,
                wordWrap = true,
                normal = { textColor = Ink },
            };

            toastTitleStyle = new GUIStyle(titleStyle) { fontSize = 16 };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                font = bodyFont,
                fontSize = 16,
                wordWrap = true,
                normal = { textColor = Ink },
            };

            hintStyle = new GUIStyle(GUI.skin.label)
            {
                font = bodyFont,
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = InkSoft },
            };

            var clear = new GUIStyleState { textColor = Ink };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                font = bodyFont,
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter,
                normal = clear,
                hover = clear,
                active = clear,
                focused = clear,
                onNormal = clear,
                onHover = clear,
                onActive = clear,
                onFocused = clear,
                border = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(26, 8, 2, 2),
            };
        }
    }
}
