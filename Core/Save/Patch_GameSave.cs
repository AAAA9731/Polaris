using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using nel;
using PixelLiner.PixelLinerLib;

namespace Polaris.Save
{
    /// <summary>复用旧 Save 的四个原版存档挂接点，不引入独立插件宿主。</summary>
    [HarmonyPatch(typeof(COOK), nameof(COOK.createBinary),
        new[] { typeof(ByteArray), typeof(SVD.sFile), typeof(NelM2DBase), typeof(bool), typeof(bool) })]
    internal static class Patch_COOK_createBinary
    {
        [HarmonyPrefix, HarmonyPriority(Priority.First)]
        static void Prefix() => SaveRuntime.Instance.Freeze();

        [HarmonyPostfix, HarmonyPriority(Priority.First)]
        static void Postfix(ByteArray __result)
        {
            if (__result == null) { return; }
            try
            {
                byte[] container = SaveRuntime.Instance.BuildContainer();
                __result.position = __result.Length;
                __result.writeBytes(container);
                SaveGate.Clear(__result);
            }
            catch (Exception ex)
            {
                SaveGate.Poison(__result, $"模组数据保存失败，已阻止覆盖存档：{ex.Message}");
                SaveIntegration.Report(ex, "appending mod save data");
            }
        }
    }

    [HarmonyPatch(typeof(COOK), "readBinaryContent",
        new[] { typeof(ByteArray), typeof(SVD.sFile), typeof(NelM2DBase) })]
    internal static class Patch_COOK_readBinaryContent
    {
        [HarmonyPostfix, HarmonyPriority(Priority.First)]
        static void Postfix(bool __result, ByteArray Ba)
        {
            if (!__result || Ba == null) { return; }
            try
            {
                SaveRuntime.Instance.Load(Ba.bytes, Ba.Length);
                PolarisAPI.Events.Post(new Events.SaveLoaded());
            }
            catch (Exception ex)
            {
                SaveIntegration.Report(ex, "loading mod save data");
            }
        }
    }

    [HarmonyPatch(typeof(COOK), nameof(COOK.newGame), new[] { typeof(NelM2DBase), typeof(bool) })]
    internal static class Patch_COOK_newGame
    {
        [HarmonyPostfix, HarmonyPriority(Priority.First)]
        static void Postfix()
        {
            try
            {
                SaveRuntime.Instance.ResetForNewGame();
                PolarisAPI.Events.Post(new Events.NewGameStarted());
            }
            catch (Exception ex) { SaveIntegration.Report(ex, "resetting mod save data"); }
        }
    }

    [HarmonyPatch(typeof(SVD), nameof(SVD.saveBinary), new[] { typeof(SVD.sFile), typeof(ByteArray) })]
    internal static class Patch_SVD_saveBinary
    {
        [HarmonyPrefix, HarmonyPriority(Priority.First)]
        static bool Prefix(ByteArray Ba, ref string __result)
        {
            string reason;
            try
            {
                reason = SaveGate.GetReason(Ba);
                if (reason == null && SaveRuntime.Instance.IsReadOnlyRecovery)
                {
                    reason = "模组存档数据无法完整加载，已阻止覆盖原存档。";
                }
            }
            catch (Exception ex)
            {
                SaveIntegration.Report(ex, "checking mod save data before writing");
                reason = $"无法确认模组存档状态：{ex.Message}";
            }
            if (reason == null) { return true; }
            CorePlugin.Logger.LogWarning($"[Polaris.Save] {reason}");
            __result = reason;
            return false;
        }
    }

    internal static class SaveGate
    {
        static readonly ConditionalWeakTable<ByteArray, string> Poisoned = new ConditionalWeakTable<ByteArray, string>();

        internal static void Poison(ByteArray serialized, string reason)
        {
            Poisoned.Remove(serialized);
            Poisoned.Add(serialized, reason);
        }

        // 失败字节流即使被重试也不能写入；只在重新成功序列化后清除。
        internal static string GetReason(ByteArray serialized)
            => serialized != null && Poisoned.TryGetValue(serialized, out string reason) ? reason : null;

        internal static void Clear(ByteArray serialized) => Poisoned.Remove(serialized);
    }

    internal static class SaveIntegration
    {
        internal static void Report(Exception exception, string context)
        {
            CorePlugin.Logger.LogError($"[Polaris.Save] {context}: {exception}");
            try { PolarisAPI.Errors.Report(exception, context); }
            catch (Exception ex) { CorePlugin.Logger.LogError($"[Polaris.Save] Failed to report save error: {ex.Message}"); }
        }
    }
}
