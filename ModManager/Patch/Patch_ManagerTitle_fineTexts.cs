using HarmonyLib;
using nel.title;

namespace Polaris.Patch
{
    /// <summary>
    /// <c>fineTexts</c> 换语言时会重写 <c>TxVer</c> 文本，冲掉 <see cref="Patch_SceneTitleTemp_initTitleLogo"/>
    /// 追加的版本行，因此在其跑完后重新追加；SENSITIVE_ANNOUNCE 状态下 <c>TxVer</c> 被挪用显示敏感内容告知，不追加版本行。
    /// </summary>
    [HarmonyPatch(typeof(SceneTitleTemp), nameof(SceneTitleTemp.fineTexts))]
    internal static class Patch_ManagerTitle_fineTexts
    {
        [HarmonyPostfix]
        static void Postfix(SceneTitleTemp __instance)
        {
            if (__instance.state != SceneTitleTemp.STATE.SENSITIVE_ANNOUNCE)
            {
                TitleVersionLine.Append(__instance);
            }
        }
    }
}
