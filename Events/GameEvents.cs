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
}
