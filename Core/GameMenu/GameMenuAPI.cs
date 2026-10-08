using System;
using System.Collections.Generic;
using nel.gm;

namespace Polaris
{
    /// <summary>
    /// 游戏内暂停菜单（左侧分类栏）。模组可以在原版 10 个分类后面追加自己的分类。
    /// 分类的右侧内容是一个 <see cref="UiGMC"/> 子类，由 <paramref name="create"/> 工厂首次打开时创建并缓存。
    /// </summary>
    public sealed class GameMenuAPI
    {
        /// <summary>原版分类数；自定义分类的槽位从这里开始往后排。</summary>
        internal const int VanillaCategoryCount = 10;

        sealed class Entry
        {
            public string Id;
            public Func<string> Title;
            public Func<UiGameMenu, CATEG, UiGMC> Create;
            public Func<bool> Visible;
        }

        readonly List<Entry> entries = [];

        // 当前这次左侧栏重建里可见的分类，下标 + VanillaCategoryCount 就是它的槽位。
        // 每次重建在 remakeLeftCategories 的前置补丁里重算，布局、按钮、内容工厂都读这一份，保证一致。
        readonly List<Entry> layout = [];
        UiGameMenu current;

        internal GameMenuAPI() { }

        /// <summary>追加一个分类；标题是 <c>TX</c> 本地化键，随语言切换自动刷新。</summary>
        /// <param name="id">分类的唯一名称，同名会报错</param>
        /// <param name="titleKey">标题的本地化键</param>
        /// <param name="create">创建右侧内容的工厂，参数是菜单实例与分配到的分类槽位</param>
        /// <param name="visible">可见条件，每次重建分类栏时求值；为空则始终可见</param>
        public void AddCategory(string id, string titleKey, Func<UiGameMenu, CATEG, UiGMC> create, Func<bool> visible = null)
        {
            AddCategory(id, () => XX.TX.Get(titleKey), create, visible);
        }

        /// <summary>追加一个分类；标题由回调给出，每次重建分类栏时求值。</summary>
        public void AddCategory(string id, Func<string> title, Func<UiGameMenu, CATEG, UiGMC> create, Func<bool> visible = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Category id must not be empty", nameof(id));
            }

            if (entries.Exists(e => e.Id == id))
            {
                throw new ArgumentException($"Category \"{id}\" is already registered", nameof(id));
            }

            entries.Add(new Entry
            {
                Id = id,
                Title = title ?? throw new ArgumentNullException(nameof(title)),
                Create = create ?? throw new ArgumentNullException(nameof(create)),
                Visible = visible,
            });
        }

        /// <summary>移除分类；返回是否确实存在。下次分类栏重建（或调用 <see cref="Refresh"/>）后生效。</summary>
        public bool RemoveCategory(string id) => entries.RemoveAll(e => e.Id == id) > 0;

        /// <summary>
        /// 让当前菜单按最新的可见条件重建分类栏。分类栏平时只在菜单创建和切换语言时重建，
        /// 可见条件中途变了（比如进入联机）就调这个。菜单还没创建时什么都不做。
        /// </summary>
        public void Refresh()
        {
            UiGameMenu menu = current;
            if (menu == null)
            {
                return;
            }

            try
            {
                menu.remakeLeftCategories();
            }
            catch (Exception ex)
            {
                PolarisAPI.Errors.Report(ex, "refreshing the game menu categories");
            }
        }

        /// <summary>分类当前所在的槽位（即它的 <see cref="CATEG"/> 值）；当前不可见或未注册时为 -1。</summary>
        public int SlotOf(string id)
        {
            int index = layout.FindIndex(e => e.Id == id);
            return index < 0 ? -1 : VanillaCategoryCount + index;
        }

        // ── 以下给补丁用 ──

        internal int VisibleCount => layout.Count;

        /// <summary>重算可见分类并按需扩容 / 清理内容缓存；在 remakeLeftCategories 之前调用。</summary>
        internal void BeginRemake(UiGameMenu menu)
        {
            current = menu;

            List<Entry> previous = new(layout);
            layout.Clear();
            foreach (Entry entry in entries)
            {
                if (IsVisible(entry))
                {
                    layout.Add(entry);
                }
            }

            int needed = Math.Max(VanillaCategoryCount + layout.Count, menu.AGmcCache.Length);
            if (needed > menu.AGmcCache.Length)
            {
                Array.Resize(ref menu.AGmcCache, needed);
            }

            // 槽位上换了人，旧的内容缓存就不属于它了。
            for (int i = 0; i < Math.Max(previous.Count, layout.Count); i++)
            {
                Entry before = i < previous.Count ? previous[i] : null;
                Entry after = i < layout.Count ? layout[i] : null;
                int slot = VanillaCategoryCount + i;
                if (before != after && slot < menu.AGmcCache.Length)
                {
                    UiGMC.releaseGMCInstance(ref menu.AGmcCache[slot]);
                }
            }
        }

        static bool IsVisible(Entry entry)
        {
            if (entry.Visible == null)
            {
                return true;
            }

            try
            {
                return entry.Visible();
            }
            catch (Exception ex)
            {
                PolarisAPI.Errors.Report(ex, $"the visibility condition of game menu category \"{entry.Id}\"", entry.Visible.Method?.DeclaringType?.Assembly);
                return false;
            }
        }

        /// <summary>为 <paramref name="menu"/> 补上可见的自定义分类按钮；在 remakeLeftCategories 之后调用。</summary>
        internal void AddButtons(UiGameMenu menu)
        {
            XX.Designer bx = menu.BxCategory;
            float height = (bx.h - bx.margin_in_tb) / (VanillaCategoryCount + layout.Count) - 8f;
            for (int i = 0; i < layout.Count; i++)
            {
                string name = "categ_" + (VanillaCategoryCount + i);
                XX.DsnDataButton dsn = new()
                {
                    name = name,
                    // 原版的悬停 / 点击回调靠 title 里的 "categ_数字" 认分类，沿用它们就能走原版全套流程。
                    title = name,
                    skin = "ui_category",
                    skin_title = SafeTitle(layout[i]),
                    w = bx.use_w,
                    h = height,
                    hover_to_select = true,
                    navi_auto_fill = false,
                    fnHover = menu.fnHoverCategory,
                    fnOut = menu.fnOutCategory,
                    fnClick = menu.fnClickCategory,
                };
                XX.aBtn button = bx.addButton(dsn);
                button.hover_snd = "";
                bx.Br();
            }
        }

        static string SafeTitle(Entry entry)
        {
            try
            {
                return entry.Title();
            }
            catch (Exception ex)
            {
                PolarisAPI.Errors.Report(ex, $"the title of game menu category \"{entry.Id}\"", entry.Title.Method?.DeclaringType?.Assembly);
                return entry.Id;
            }
        }

        /// <summary>槽位上的自定义分类内容；创建失败返回 null（调用方回落到原版的占位页）。</summary>
        internal bool TryGetContent(UiGameMenu menu, CATEG slot, out UiGMC content)
        {
            content = null;
            int index = (int)slot - VanillaCategoryCount;
            if (index < 0 || index >= layout.Count)
            {
                return false;
            }

            Entry entry = layout[index];
            content = menu.AGmcCache[(int)slot];
            if (content != null)
            {
                return true;
            }

            try
            {
                content = entry.Create(menu, slot);
            }
            catch (Exception ex)
            {
                PolarisAPI.Errors.Report(ex, $"creating the content of game menu category \"{entry.Id}\"", entry.Create.Method?.DeclaringType?.Assembly);
            }

            menu.AGmcCache[(int)slot] = content;
            return true;
        }
    }
}
