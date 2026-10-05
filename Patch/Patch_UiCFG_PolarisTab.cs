using System.Collections.Generic;
using HarmonyLib;
using nel;
using Polaris.Settings;
using XX;

namespace Polaris.Patch
{
    /// <summary>
    /// 给设置界面开一个 Polaris 专属标签页。原版的标签页由 <c>UiCFG.CATEG</c> 枚举写死（七个，循环上界也是常量），
    /// 没有现成的扩展口，所以借用其中的 <c>effects_sp</c>（“特殊效果”页）：它只在玩家解锁了对应特殊项时才出现，平时是空的、整页被隐藏。
    /// 做法：
    /// <list type="number">
    /// <item><see cref="Patch_UiCFG_PrepareEntries"/>：这个分类没有条目时塞一个占位条目，让原版把这页建出来；</item>
    /// <item><see cref="Patch_UiCFG_createBoxDesignerContentMain"/>：只有占位条目时，接管这页的内容绘制，画 Polaris 与各模组的设置项。</item>
    /// </list>
    /// 玩家真的解锁了特殊项时，这页已经有原版内容，就不接管，Polaris 的设置项追加在它们后面（见构造函数补丁里的委托）。
    /// </summary>
    internal static class PolarisTab
    {
        internal const UiCFG.CATEG Category = UiCFG.CATEG.effects_sp;

        /// <summary>占位条目：键名是 Polaris 自己的，原版按键名分派的 <c>changeConfigValue</c> 遇到不认识的键直接忽略。</summary>
        internal static readonly CfgEntry Placeholder = new CfgEntry("polaris_tab", 0f, () => 0);

        /// <summary>把被借用的那一页的标签图标换成 Polaris 的（原版给 effects_sp 画的是一颗心）。</summary>
        internal static void ApplyTabIcon(UiCFG cfg)
        {
            try
            {
                if (!IsOwned(cfg, Category))
                {
                    return;
                }

                // ✴（U+2734 八角黑星）在游戏自带字体里有字形；原版的图标是网格里的图片，没法塞进我们自己的贴图。
                aBtn tab = cfg.RTabCR?.Get(XX.FEnum<UiCFG.CATEG>.ToStr(Category));
                tab?.setSkinTitle("✴");
            }
            catch (System.Exception)
            {
            }
        }

        /// <summary>这一页是不是只有我们的占位条目（即由 Polaris 接管）。</summary>
        internal static bool IsOwned(UiCFG cfg, UiCFG.CATEG categ)
        {
            if (categ != Category)
            {
                return false;
            }

            List<CfgEntry> list = cfg.AAEntry[(int)categ];
            return list.Count == 1 && ReferenceEquals(list[0], Placeholder);
        }
    }

    /// <summary>设置界面顶部那块标签页标题：Polaris 接管的这一页显示“Polaris 设置”，而不是原版给这一页起的名字。</summary>
    [HarmonyPatch(typeof(UiCFG), nameof(UiCFG.getDesc))]
    internal static class Patch_UiCFG_getDesc
    {
        static void Postfix(UiCFG __instance, ref string __result)
        {
            if (string.IsNullOrEmpty(__result) || __instance.Atab_keys == null)
            {
                return;
            }

            int index = UiCFG.selection_tab_index;
            if (index < 0 || index >= __instance.Atab_keys.Length
                || __instance.Atab_keys[index] != XX.FEnum<UiCFG.CATEG>.ToStr(PolarisTab.Category)
                || !PolarisTab.IsOwned(__instance, PolarisTab.Category))
            {
                return;
            }

            __result = PolarisAPI.Localization.Text(Localization.PolarisStrings.TabTitle);
        }
    }

    [HarmonyPatch(typeof(UiCFG), nameof(UiCFG.PrepareEntries))]
    internal static class Patch_UiCFG_PrepareEntries
    {
        static void Postfix(List<CfgEntry> A, UiCFG.CATEG categ)
        {
            if (categ == PolarisTab.Category && A.Count == 0)
            {
                A.Add(PolarisTab.Placeholder);
            }
        }
    }

    [HarmonyPatch(typeof(UiCFG), nameof(UiCFG.createBoxDesignerContentMain))]
    internal static class Patch_UiCFG_createBoxDesignerContentMain
    {
        static bool Prefix(UiCFG __instance, Designer CurTab, UiCFG.CATEG categ)
        {
            if (!PolarisTab.IsOwned(__instance, categ))
            {
                return true;
            }

            // 与原版同样的起手式（见 createBoxDesignerContentMain），只是不画原版条目。
            __instance.BxOut.assignCurrentTargetTabManual(CurTab);
            try
            {
                CurTab.alignx = ALIGN.CENTER;
                CurTab.init();
                PolarisSettingsScreen.Append(__instance, ownTab: true);
            }
            finally
            {
                __instance.BxOut.assignCurrentTargetTabManual(null);
            }

            return false;
        }
    }
}
