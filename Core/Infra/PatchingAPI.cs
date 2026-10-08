using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace Polaris.Infra
{
    /// <summary>应用补丁的结果。</summary>
    public readonly struct PatchResult
    {
        /// <summary>真正打上补丁的类数（没标 <c>[HarmonyPatch]</c> 的类型不计）。</summary>
        public int Applied { get; }

        /// <summary>应用失败的类数；失败已经上报，其余补丁不受影响。</summary>
        public int Failed { get; }

        public PatchResult(int applied, int failed)
        {
            Applied = applied;
            Failed = failed;
        }

        public override string ToString() => $"{Applied} applied, {Failed} failed";
    }

    /// <summary>
    /// Harmony 补丁的安全应用，从 <see cref="PolarisAPI.Patching"/> 取。
    /// 逐个类应用而不是一把 <c>PatchAll()</c>：后者全有全无，一个补丁坏了会连累所有功能起不来；逐类应用则坏一个报错跳过，其余照常。
    /// </summary>
    public sealed class PatchingAPI
    {
        internal PatchingAPI() { }

        /// <summary>把 <paramref name="assembly"/> 里所有标了 <c>[HarmonyPatch]</c> 的类逐个应用到 <paramref name="harmony"/>。</summary>
        /// <param name="log">汇总日志和失败提示写到哪；为空则不写日志（失败仍会通过 <see cref="PolarisAPI.Errors"/> 上报）。</param>
        public PatchResult ApplyAll(Harmony harmony, Assembly assembly, ManualLogSource log = null)
        {
            if (harmony == null)
            {
                throw new ArgumentNullException(nameof(harmony));
            }

            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            int applied = 0;
            int failed = 0;
            string name = assembly.GetName().Name;

            foreach (Type type in AccessTools.GetTypesFromAssembly(assembly))
            {
                try
                {
                    // 面包屑：补丁应用涉及大量反射与 IL 生成，卡住时诊断要能说出卡在哪个补丁上。
                    using (PolarisAPI.Errors.Activity($"applying patch {type.Name}", assembly))
                    {
                        // 没标 [HarmonyPatch] 的类型，Patch() 是空操作。
                        if (harmony.CreateClassProcessor(type).Patch() != null)
                        {
                            applied++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    PolarisAPI.Errors.Report(ex, $"applying patch {type.Name}", assembly);
                    log?.LogError($"[{name}] The feature owned by patch {type.Name} is unavailable this session.");
                }
            }

            log?.LogMessage($"[{name}] Applied {applied} Harmony patches" + (failed > 0 ? $", {failed} failed." : "."));
            return new PatchResult(applied, failed);
        }
    }
}
