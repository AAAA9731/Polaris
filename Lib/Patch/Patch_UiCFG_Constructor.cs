using HarmonyLib;
using nel;
using Polaris.Settings;
using XX;

namespace Polaris.Patch
{
    /// <summary>
    /// 把 Polaris 的设置项渲染挂进原版设置界面，通过改写构造函数的 <c>ref</c> 参数
    /// <c>_FnDesignerCreateAfter</c>（原版扩展口）实现，链式调用而非替换，且排在原委托之前。
    /// ver030 起设置界面分成七个分类标签页，这个委托对每个标签页各调一次。Polaris 的设置项画在专属标签页上（见 <see cref="PolarisTab"/>）；
    /// 只有专属标签页没能建出来（游戏更新后转译器失效）时，才退回追加在“常规”页尾部。
    /// </summary>
    [HarmonyPatch(typeof(UiCFG), MethodType.Constructor,
        typeof(UiBoxDesignerFamily), typeof(UiBoxDesigner), typeof(UiBoxDesigner), typeof(Designer), typeof(bool), typeof(bool),
        typeof(UiCFG.FnCfgTabCreateAfter), typeof(bool))]
    internal static class Patch_UiCFG_Constructor
    {
        static void Prefix(UiCFG __instance, UiBoxDesigner _Bx, bool _is_title,
                           ref UiCFG.FnCfgTabCreateAfter _FnDesignerCreateAfter)
        {
            UiCFG.FnCfgTabCreateAfter original = _FnDesignerCreateAfter;

            _FnDesignerCreateAfter = (Designer tab, UiCFG.CATEG category) =>
            {
                if (!PolarisTab.Active && category == UiCFG.CATEG.general)
                {
                    PolarisSettingsScreen.Append(__instance);
                }

                original?.Invoke(tab, category);
            };

            // 此刻的值即"取消"要回滚到的基准。
            SettingsStore.Snapshot();
        }

        static void Postfix(UiCFG __instance)
        {
            PolarisTab.ApplyTabIcon(__instance);
        }
    }
}
