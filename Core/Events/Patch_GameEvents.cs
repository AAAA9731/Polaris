using System;
using HarmonyLib;
using m2d;
using nel;
using nel.gm;
using XX;

namespace Polaris.Events
{
    [HarmonyPatch(typeof(TX), nameof(TX.changeFamily), new[] { typeof(string) })]
    internal static class Patch_TX_changeFamily
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            try { PolarisAPI.Events.Post(new LocaleChanged(TX.getCurrentFamilyName())); }
            catch (Exception ex) { PolarisAPI.Errors.Report(ex, "posting LocaleChanged"); }
        }
    }

    [HarmonyPatch(typeof(NelM2DBase), nameof(NelM2DBase.changeMap), new[] { typeof(Map2d) })]
    internal static class Patch_NelM2DBase_changeMap
    {
        [HarmonyPrefix]
        static void Prefix(NelM2DBase __instance, out Map2d __state)
        {
            __state = __instance.curMap;
        }

        [HarmonyPostfix]
        static void Postfix(Map2d __state, Map2d __result)
        {
            try { PolarisAPI.Events.Post(new MapChanged(__state, __result)); }
            catch (Exception ex) { PolarisAPI.Errors.Report(ex, "posting MapChanged"); }
        }
    }

    [HarmonyPatch(typeof(PRNoel), nameof(PRNoel.appear))]
    internal static class Patch_PRNoel_appear
    {
        [HarmonyPostfix]
        static void Postfix(PRNoel __instance, Map2d Mp)
        {
            try { PolarisAPI.Events.Post(new PlayerAppeared(__instance, Mp)); }
            catch (Exception ex) { PolarisAPI.Errors.Report(ex, "posting PlayerAppeared"); }
        }
    }

    [HarmonyPatch(typeof(UiGameMenu), nameof(UiGameMenu.activate))]
    internal static class Patch_UiGameMenu_activate
    {
        [HarmonyPostfix]
        static void Postfix(UiGameMenu __instance)
        {
            try { PolarisAPI.Events.Post(new GameMenuOpened(__instance)); }
            catch (Exception ex) { PolarisAPI.Errors.Report(ex, "posting GameMenuOpened"); }
        }
    }

    [HarmonyPatch(typeof(UiGameMenu), nameof(UiGameMenu.deactivate))]
    internal static class Patch_UiGameMenu_deactivate
    {
        // 原版 deactivate 对已关闭的菜单是空操作，只在真的从开到关时发事件。
        [HarmonyPrefix]
        static void Prefix(UiGameMenu __instance, out bool __state)
        {
            __state = __instance.isActive();
        }

        [HarmonyPostfix]
        static void Postfix(UiGameMenu __instance, bool __state)
        {
            if (!__state)
            {
                return;
            }

            try { PolarisAPI.Events.Post(new GameMenuClosed(__instance)); }
            catch (Exception ex) { PolarisAPI.Errors.Report(ex, "posting GameMenuClosed"); }
        }
    }
}
