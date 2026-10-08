using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using nel;
using Polaris.Settings;
using XX;

namespace Polaris.Patch
{
    /// <summary>
    /// 给设置界面加一个 Polaris 专属标签页。原版的标签页由 <c>UiCFG.CATEG</c> 的七个值依次建出来（构造函数里一个 <c>for (i &lt; 7)</c> 循环，
    /// 每个分类准备一份条目、条目非空就建一页），没有现成的注册口。这里把循环上界改成 8，让多出来的第 8 个 id（<see cref="Id"/>，即 <c>CATEG._MAX</c> 的值）
    /// 也走同一套流程：
    /// <list type="number">
    /// <item>构造函数补丁的转译器：把"标签页数组与循环上界 7"改成 8；</item>
    /// <item><see cref="Patch_UiCFG_PrepareEntries"/>：这个 id 的条目准备阶段放一个占位条目，让原版把这一页建出来；</item>
    /// <item><see cref="Patch_UiCFG_createBoxDesignerContentMain"/>：这一页要填内容时，画 Polaris 与各模组的设置项，而不是原版条目；</item>
    /// <item>图标、页眉标题各一个小补丁。</item>
    /// </list>
    /// <c>_MAX</c> 在原版里本来就是“没有分类”的哨兵值，所有用到它的地方（记忆上次选中的按钮、重置、说明文字）都当作“跳过”处理，所以这一页不会被原版的分类逻辑误伤，
    /// “恢复初始设置”也不会碰 Polaris 的设置项。
    /// 转译器没找到目标指令（游戏更新后代码变了）时补丁整体失败并留日志，设置项退回到追加在“常规”页尾部（见构造函数补丁里的委托）。
    /// </summary>
    internal static class PolarisTab
    {
        /// <summary>Polaris 标签页的分类 id：原版七个分类之后的第八个。</summary>
        internal const UiCFG.CATEG Id = UiCFG.CATEG._MAX;

        /// <summary>构造函数转译器成功改写了标签页数量；为 false 时 Polaris 的设置项退回追加在“常规”页尾部。</summary>
        internal static bool Active { get; set; }

        /// <summary>占位条目：键名是 Polaris 自己的，原版按键名分派的 <c>changeConfigValue</c> 遇到不认识的键直接忽略。</summary>
        internal static readonly CfgEntry Placeholder = new CfgEntry("polaris_tab", 0f, () => 0);

        /// <summary>这一页是不是 Polaris 的（只有我们的占位条目）。</summary>
        internal static bool IsOurs(UiCFG cfg, UiCFG.CATEG categ)
        {
            if (!Active || categ != Id || cfg.AAEntry == null || cfg.AAEntry.Length <= (int)categ)
            {
                return false;
            }

            List<CfgEntry> list = cfg.AAEntry[(int)categ];
            return list != null && list.Count == 1 && ReferenceEquals(list[0], Placeholder);
        }

        /// <summary>标签页按钮的键名，原版用分类枚举的名字当键。</summary>
        internal static string Key => FEnum<UiCFG.CATEG>.ToStr(Id);

        /// <summary>原版为每个标签按钮设置的图标是图集里叫 <c>config_&lt;键名&gt;</c> 的图片，我们没有，改用字体里有字形的 ✴（八角黑星）。</summary>
        internal static void ApplyTabIcon(UiCFG cfg)
        {
            try
            {
                if (!Active)
                {
                    return;
                }

                cfg.RTabCR?.Get(Key)?.setSkinTitle("✴");
            }
            catch (Exception)
            {
                // 图标只是装饰，出问题就保留原版那个找不到图的空图标。
            }
        }
    }

    /// <summary>把构造函数里“标签页数量 7”改成 8。</summary>
    [HarmonyPatch]
    internal static class Patch_UiCFG_TabCount
    {
        static System.Reflection.MethodBase TargetMethod()
            => AccessTools.Constructor(
                typeof(UiCFG),
                [typeof(UiBoxDesignerFamily), typeof(UiBoxDesigner), typeof(UiBoxDesigner), typeof(Designer), typeof(bool), typeof(bool), typeof(UiCFG.FnCfgTabCreateAfter), typeof(bool)]);

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new(instructions);

            // 目标形态：ldc.i4.7 ; stloc N ; ldarg.0 ; ldloc N ; newarr Designer ; stfld ATabCateg
            // 构造函数里还有一处 ListBuffer.Pop(7)（只是容量参数），靠后面紧跟 newarr Designer 区分。
            for (int i = 0; i + 4 < code.Count; i++)
            {
                if (code[i].opcode != OpCodes.Ldc_I4_7 || !code[i + 1].IsStloc())
                {
                    continue;
                }

                bool newsDesignerArray = false;
                for (int j = i + 2; j < i + 6 && j < code.Count; j++)
                {
                    if (code[j].opcode == OpCodes.Newarr && code[j].operand is Type t && t == typeof(Designer))
                    {
                        newsDesignerArray = true;
                        break;
                    }
                }

                if (!newsDesignerArray)
                {
                    continue;
                }

                code[i] = new CodeInstruction(OpCodes.Ldc_I4_8).WithLabels(code[i].labels);
                PolarisTab.Active = true;
                return code;
            }

            throw new InvalidOperationException("Could not find the tab-count constant (7) in the UiCFG constructor.");
        }
    }

    [HarmonyPatch(typeof(UiCFG), nameof(UiCFG.PrepareEntries))]
    internal static class Patch_UiCFG_PrepareEntries
    {
        static void Postfix(List<CfgEntry> A, UiCFG.CATEG categ)
        {
            if (PolarisTab.Active && categ == PolarisTab.Id && A.Count == 0)
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
            if (!PolarisTab.IsOurs(__instance, categ))
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

    /// <summary>设置界面顶部那块标签页标题：Polaris 这一页显示“Polaris 设置”（原版按 <c>Config_category_&lt;键名&gt;</c> 查文案，我们没有这条）。</summary>
    [HarmonyPatch(typeof(UiCFG), nameof(UiCFG.getDesc))]
    internal static class Patch_UiCFG_getDesc
    {
        static void Postfix(UiCFG __instance, ref string __result)
        {
            if (!PolarisTab.Active || __instance.Atab_keys == null)
            {
                return;
            }

            // 与原版同样的“不显示”条件：界面没激活、或正在摇杆灵敏度检测时返回空串，这里不要覆盖。
            if (!__instance.isActive() || __instance.isStickSensitivityState())
            {
                return;
            }

            int index = UiCFG.selection_tab_index;
            if (index < 0 || index >= __instance.Atab_keys.Length || __instance.Atab_keys[index] != PolarisTab.Key)
            {
                return;
            }

            __result = PolarisAPI.Localization.Text(Localization.CoreStrings.TabTitle);
        }
    }
}
