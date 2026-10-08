using m2d;

namespace Polaris.Events
{
    /// <summary>原版存档成功读完、模组分区已还原之后。<c>SaveHandle.Current</c> 此时已是读到的数据。</summary>
    public sealed class SaveLoaded
    {
    }

    /// <summary>新游戏开始、模组存档数据已重置为默认值之后。</summary>
    public sealed class NewGameStarted
    {
    }

    /// <summary>游戏即将退出（<c>OnApplicationQuit</c> 最开始）。只做轻量收尾，此时 Polaris 自身也在关闭。</summary>
    public sealed class GameQuitting
    {
    }

    /// <summary>
    /// 游戏语言已切换（<c>TX.changeFamily</c> 之后）。游戏启动初始化语言时也会触发一次，
    /// 但那时通常还没有模组订阅；需要启动时的值请直接读 <c>PolarisAPI.Game.Localization.CurrentLocale</c>。
    /// </summary>
    public sealed class LocaleChanged
    {
        public string Locale { get; }

        public LocaleChanged(string locale) => Locale = locale;
    }

    /// <summary>
    /// 地图切换（<c>NelM2DBase.changeMap</c> 之后）。关闭地图时 <see cref="Current"/> 为 <c>null</c>；
    /// 原版对"切到当前所在地图"会先关再开，所以可能连续收到 <c>Current == null</c> 与重新打开两次。
    /// 不要跨切图缓存这两个引用。
    /// </summary>
    public sealed class MapChanged
    {
        public Map2d Previous { get; }
        public Map2d Current { get; }

        public MapChanged(Map2d previous, Map2d current)
        {
            Previous = previous;
            Current = current;
        }
    }

    /// <summary>玩家角色进入地图（读档、切图、传送之后都会触发）；此时玩家对象已建好，适合在它身上挂东西。</summary>
    public sealed class PlayerAppeared
    {
        public nel.PR Player { get; }
        public Map2d Map { get; }

        public PlayerAppeared(nel.PR player, Map2d map)
        {
            Player = player;
            Map = map;
        }
    }

    /// <summary>标题场景的按钮建好了（每个标题场景实例只触发一次，语言切换导致的重建不算）；这时可以动标题界面。</summary>
    public sealed class TitleReady
    {
        public nel.title.SceneTitleTemp Scene { get; }

        public TitleReady(nel.title.SceneTitleTemp scene) => Scene = scene;
    }

    /// <summary>暂停菜单（游戏菜单）打开了。</summary>
    public sealed class GameMenuOpened
    {
        public nel.gm.UiGameMenu Menu { get; }

        public GameMenuOpened(nel.gm.UiGameMenu menu) => Menu = menu;
    }

    /// <summary>暂停菜单关闭了。</summary>
    public sealed class GameMenuClosed
    {
        public nel.gm.UiGameMenu Menu { get; }

        public GameMenuClosed(nel.gm.UiGameMenu menu) => Menu = menu;
    }

    /// <summary>Unity 的活动场景换了（标题 ↔ 游戏等）；名字可能为空（场景刚卸载时）。</summary>
    public sealed class SceneChanged
    {
        public string Previous { get; }
        public string Current { get; }

        public SceneChanged(string previous, string current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
