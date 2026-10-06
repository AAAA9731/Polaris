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
}
