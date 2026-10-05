using System;
using m2d;
using nel;

namespace Polaris.API
{
    /// <summary>
    /// 当前地图、玩家与游戏根对象的安全查询；不缓存或包装原版对象。
    /// </summary>
    internal static class GameBinding
    {
        /// <summary>当前地图；没有加载地图（标题画面、读档中）时返回 <c>null</c>。</summary>
        internal static Map2d CurrentMap => Safe(static () => M2DBase.Instance?.curMap);

        /// <summary>Nel 侧的 M2D。天气、危险度、背包都挂在它下面。</summary>
        internal static NelM2DBase NelM2D => Safe(static () => M2DBase.Instance as NelM2DBase);

        /// <summary>在场的玩家角色，不在场时为 <c>null</c>；不缓存引用是因为切图会重建玩家对象。</summary>
        internal static PR Player => Safe(static () => CurrentMap?.getKeyPr() as PR);

        /// <summary>取根引用的统一包装：读不出来（游戏内部还没建好、或读取本身抛异常）一律当作"没有"，绝不把异常漏给上层。</summary>
        static T Safe<T>(Func<T> read) where T : class
        {
            try
            {
                return read();
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
