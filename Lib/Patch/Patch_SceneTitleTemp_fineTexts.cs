using HarmonyLib;
using nel.title;

namespace Polaris.Patch
{
    /// <summary>
    /// <c>fineTexts</c> 换语言时会重排顶部按钮，冲掉 <see cref="Patch_SceneTitleTemp_initButtons"/> 的居中修正，
    /// 因此在其跑完后重新应用。
    /// </summary>
    [HarmonyPatch(typeof(SceneTitleTemp), nameof(SceneTitleTemp.fineTexts))]
    internal static class Patch_SceneTitleTemp_fineTexts
    {
        [HarmonyPostfix]
        static void Postfix(SceneTitleTemp __instance)
        {
            MainMenuAPI.CenterTopRow(__instance);
        }
    }
}
