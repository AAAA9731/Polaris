using HarmonyLib;
using nel.gm;
using XX;

namespace Polaris.Patch
{
    /// <summary>
    /// 原版 <c>appearCategory</c> 按分类写死 switch，认不得自定义分类；槽位属于自定义分类时，
    /// 照着原版的前后流程自己摆右侧内容，其余分类原样放行。
    /// </summary>
    [HarmonyPatch(typeof(UiGameMenu), nameof(UiGameMenu.appearCategory))]
    internal static class Patch_UiGameMenu_appearCategory
    {
        [HarmonyPrefix]
        static bool Prefix(UiGameMenu __instance, CATEG ct, bool force)
        {
            if ((int)ct < GameMenuAPI.VanillaCategoryCount)
            {
                return true;
            }

            IN.clearPushDown(true);
            if (!force && __instance.appear_categ == ct)
            {
                return false;
            }

            if (!PolarisAPI.GameMenu.TryGetContent(__instance, ct, out UiGMC content))
            {
                return true;
            }

            __instance.quitAppearCategory();
            __instance.EditFocusInitTo = null;
            __instance.appear_categ = ct;
            __instance.AppearC = content;
            __instance.BxRRemake(force);
            content?.initAppearWhole();
            return false;
        }
    }
}
