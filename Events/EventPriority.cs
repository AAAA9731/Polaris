namespace Polaris.Events
{
    /// <summary>同一事件的订阅者执行顺序：Early 先于 Normal 先于 Late；同级按订阅先后。</summary>
    public enum EventPriority
    {
        Early = 0,
        Normal = 1,
        Late = 2,
    }
}
