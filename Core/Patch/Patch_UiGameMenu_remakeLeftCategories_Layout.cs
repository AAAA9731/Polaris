using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using nel.gm;

namespace Polaris.Patch
{
    /// <summary>
    /// 原版按钮高度 <c>(h - margin) / 10f - 8f</c> 把分类数写死成 10；改成读当前实际分类数，加了自定义分类后按钮才不会撑出栏外。
    /// 单独成类：这条 IL 匹配失败时只丢掉自适应高度，分类按钮本身照常工作。
    /// </summary>
    [HarmonyPatch(typeof(UiGameMenu), nameof(UiGameMenu.remakeLeftCategories))]
    internal static class Patch_UiGameMenu_remakeLeftCategories_Layout
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchStartForward(new CodeMatch(OpCodes.Sub), new CodeMatch(OpCodes.Ldc_R4, 10f))
                .ThrowIfInvalid("Could not find the IL pattern for the game menu category count constant 10")
                .Advance(1)
                .SetInstructionAndAdvance(CodeInstruction.Call(typeof(Patch_UiGameMenu_remakeLeftCategories_Layout), nameof(CategoryCount)))
                .Instructions();
        }

        static float CategoryCount() => GameMenuAPI.VanillaCategoryCount + PolarisAPI.GameMenu.VisibleCount;
    }
}
