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
    /// 只有玩家恰好解锁了那一页的原版特殊项、Polaris 无法接管时，才在这里追加到那一页尾部。
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
                if (category == PolarisTab.Category)
                {
                    PolarisSettingsScreen.Append(__instance);
                }

                original?.Invoke(tab, category);
            };

            // 此刻的值即"取消"要回滚到的基准。
            SettingsStore.Snapshot();

            // 必须在构造函数用 BxOut.use_h 定高之前缩面板，否则滚动区不会跟着缩。
            if (SettingsSearchWindow.Wanted(_is_title))
            {
                SettingsSearchWindow.ShrinkPanel(_Bx);
            }
        }

        /// <summary>设置项已画完，登记表是新鲜的，可以摆出搜索框了。</summary>
        static void Postfix(UiCFG __instance, UiBoxDesigner _Bx, bool _is_title)
        {
            PolarisTab.ApplyTabIcon(__instance);

            // 条件须与 Prefix 一致，否则缩了面板却不摆搜索框会留白。
            if (SettingsSearchWindow.Wanted(_is_title))
            {
                SettingsSearchWindow.ShowUnder(_Bx);
            }
        }
    }
}
