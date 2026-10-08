using System;
using nel;
using Polaris.API;

namespace Polaris
{
    public static partial class PolarisAPI
    {
        public static partial class Game
        {
            /// <summary>原版自动存档入口。写入在调用返回前完成。</summary>
            public static class Save
            {
                /// <summary>判断当前玩家与地图状态是否允许普通自动存档。</summary>
                public static bool CanAutosave => Safe(static () => COOK.canSave(), false);

                /// <summary>请求自动存档；Force 模式要求调用方先确认游戏状态安全。</summary>
                public static bool RequestAutosave(GameAutosaveMode mode = GameAutosaveMode.Normal)
                {
                    if (!Enum.IsDefined(typeof(GameAutosaveMode), mode))
                    {
                        return false;
                    }

                    NelM2DBase game = GameBinding.NelM2D;
                    if (game == null)
                    {
                        return false;
                    }

                    try
                    {
                        return COOK.autoSave(
                            game,
                            is_bench: mode == GameAutosaveMode.Bench,
                            force: mode == GameAutosaveMode.Force) != null;
                    }
                    catch (Exception ex)
                    {
                        Errors.Report(ex, "Game.Save.RequestAutosave");
                        return false;
                    }
                }
            }

        }
    }
}

namespace Polaris.API
{
    /// <summary>原版自动存档的提示与安全检查模式。</summary>
    public enum GameAutosaveMode
    {
        /// <summary>普通自动存档；沿用原版的玩家存活与地图就绪检查。</summary>
        Normal = 0,

        /// <summary>长椅自动存档；安全检查与普通模式相同，但使用长椅提示样式。</summary>
        Bench = 1,

        /// <summary>强制自动存档；跳过原版的 <c>canSave</c> 检查。</summary>
        Force = 2,
    }

}