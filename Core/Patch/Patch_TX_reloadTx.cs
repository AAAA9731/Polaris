using HarmonyLib;
using XX;

namespace Polaris.Patch
{
    /// <summary>文案表每次重建后，把模组登记的文案文件（<see cref="Localization.LocalizationAPI.AddTextFiles"/>）补读进去。</summary>
    [HarmonyPatch(typeof(TX), nameof(TX.reloadTx))]
    internal static class Patch_TX_reloadTx
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            PolarisAPI.Localization.LoadAllTextFiles();
        }
    }
}
