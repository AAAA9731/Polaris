using System;
using HarmonyLib;
using m2d;
using nel;
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
}
