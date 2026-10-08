using HarmonyLib;
using nel.title;
using UnityEngine;

namespace Polaris.Patch
{
    /// <summary>
    /// 标题界面每帧驱动入口：推进按钮窗口的淡入动画并侦测其关闭；每帧重新应用顶部按钮居中修正
    /// （内部布局重算无独立挂载点会冲掉该修正，故逐帧重新断言，CenterTopRow 本身开销可忽略）。
    /// </summary>
    [HarmonyPatch(typeof(SceneTitleTemp), "runIRD")]
    internal static class Patch_SceneTitleTemp_runIRD
    {
        [HarmonyPostfix]
        static void Postfix(SceneTitleTemp __instance)
        {
            MainMenuAPI.CenterTopRow(__instance);

            MainMenuAPI mainMenu = PolarisAPI.MainMenu;
            if (mainMenu.CurrentOpenButton == null)
            {
                return;
            }

            mainMenu.AdvanceCommandBarFade(Time.deltaTime);

            if (!mainMenu.IsCurrentWindowStillOpen())
            {
                mainMenu.ReturnToTop();
                return;
            }

            if (MainMenuAPI.IsCancelInputPressed())
            {
                mainMenu.RaiseEscaped();
            }
        }
    }
}
