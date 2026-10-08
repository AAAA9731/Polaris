using nel.gm;
using HarmonyLib;

namespace Polaris.Patch
{
    /// <summary>分类栏重建：先算出可见的自定义分类并扩容内容缓存，重建完再把它们的按钮补在原版 10 个后面。</summary>
    [HarmonyPatch(typeof(UiGameMenu), nameof(UiGameMenu.remakeLeftCategories))]
    internal static class Patch_UiGameMenu_remakeLeftCategories
    {
        [HarmonyPrefix]
        static void Prefix(UiGameMenu __instance)
        {
            PolarisAPI.GameMenu.BeginRemake(__instance);
        }

        [HarmonyPostfix]
        static void Postfix(UiGameMenu __instance)
        {
            PolarisAPI.GameMenu.AddButtons(__instance);
        }
    }
}
