using HarmonyLib;
using nel.title;
using UnityEngine;

namespace Polaris.Patch
{
    /// <summary>
    /// 管理器对标题界面每帧驱动入口的补充：推进告知页（见 <see cref="TitleOverlays"/>）的淡入，并在告知页显示期间压住常驻按钮。
    /// 库自己的 runIRD 补丁负责主菜单按钮窗口。
    /// </summary>
    [HarmonyPatch(typeof(SceneTitleTemp), "runIRD")]
    internal static class Patch_ManagerTitle_runIRD
    {
        [HarmonyPostfix]
        static void Postfix(SceneTitleTemp __instance)
        {
            TitleOverlays.AdvanceFade(Time.deltaTime);

            // 告知页显示期间压住语言切换行/外链按钮/底部按键提示；必须放在 Postfix 里，
            // 因为原版 runIRD 每帧都会重写这些 alpha，只有跑在它之后写的值才是最终值。
            TitleChrome.Apply(__instance, TitleOverlays.IsShowing);
        }
    }
}
